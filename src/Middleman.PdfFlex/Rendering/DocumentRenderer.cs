// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf.Fonts;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.Advanced;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Main entry point for rendering a <see cref="Document"/> to PDF. Walks the element
/// tree through the layout engine, creates PDF pages, and dispatches each layout node
/// to the appropriate specialized renderer.
/// </summary>
public static class DocumentRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders a document to a PDF file at the specified path.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="filePath">The output file path for the generated PDF.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> or <paramref name="filePath"/> is null.
    /// </exception>
    public static void Render(Document document, string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(filePath);

        using var stream = File.Create(filePath);
        Render(document, stream);
    }

    /// <summary>
    /// Renders a document to a PDF written to the specified stream.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">The output stream for the generated PDF.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> or <paramref name="stream"/> is null.
    /// </exception>
    public static void Render(Document document, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);

        ValidateAccessibility(document);

        FontRegistry.EnsureInitialized();

        using var pdfDoc = new PdfDocument();
        pdfDoc.Conformance = document.Conformance;

        // Forward document language to the PDF/UA metadata layer.
        if (!string.IsNullOrEmpty(document.Language) && pdfDoc._uaManager != null)
            pdfDoc._uaManager.SetDocumentLanguage(document.Language);

        double pageWidth = document.PageSize.Width;
        double pageHeight = document.PageSize.Height;
        double contentWidth = pageWidth - document.Margins.HorizontalTotal;
        double contentHeight = pageHeight - document.Margins.VerticalTotal;

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            // Margins exceed page dimensions. Create a single blank page.
            pdfDoc.AddPage();
            pdfDoc.Save(stream);
            return;
        }

        // Split the document children into page groups at PageBreak elements.
        var pageGroups = SplitByPageBreaks(document.Children);

        if (pageGroups.Count == 0)
        {
            // No content. Create a single blank page.
            var blankPage = pdfDoc.AddPage();
            SetPageSize(blankPage, pageWidth, pageHeight);
            RenderWatermarkIfPresent(pdfDoc, document, blankPage);
            pdfDoc.Save(stream);
            return;
        }

        // Phase 1: Collect all page render actions across all groups.
        var allPages = new List<List<Action<RenderContext>>>();

        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                // Empty group from consecutive PageBreaks: produce a blank page.
                allPages.Add(new List<Action<RenderContext>>());
                continue;
            }

            // Wrap the group in a Column so the layout engine processes them as a unit.
            // Use unlimited height (1e6) so flex-shrink does not compress content.
            // Pagination handles overflow by distributing children across pages.
            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(
                wrapper, contentWidth, 1e6,
                document.Margins.Left, document.Margins.Top);

            // Paginate the root Column's children across multiple pages.
            var groupPages = PaginateGroup(document, rootNode, contentWidth, contentHeight);
            allPages.AddRange(groupPages);
        }

        if (allPages.Count == 0)
        {
            allPages.Add(new List<Action<RenderContext>>());
        }

        // Phase 2: Render all pages with correct page numbers.
        int totalPages = allPages.Count;
        var sb = pdfDoc._uaManager?.StructureBuilder;
        bool hasUaManager = pdfDoc._uaManager != null;

        for (int pageIdx = 0; pageIdx < allPages.Count; pageIdx++)
        {
            var pdfPage = pdfDoc.AddPage();
            SetPageSize(pdfPage, pageWidth, pageHeight);

            // Content graphics must be disposed before creating watermark graphics
            // for the same page (only one XGraphics per page at a time).
            using (var gfx = XGraphics.FromPdfPage(pdfPage))
            {
                var ctx = new RenderContext(gfx, pageIdx + 1, totalPages, pdfDoc.Conformance, sb);

                foreach (var action in allPages[pageIdx])
                {
                    action(ctx);
                }
            }

            // When UAManager is active, render watermark per-page while we're still
            // on the current page. UAManager rejects XGraphics for non-current pages.
            if (hasUaManager)
                RenderWatermarkIfPresent(pdfDoc, document, pdfPage);
        }

        // For non-PDF/UA documents, render watermarks in a final pass.
        if (!hasUaManager)
            RenderWatermarkIfPresent(pdfDoc, document);

        pdfDoc.Save(stream);
    }

    /// <summary>
    /// Renders a document and returns the PDF as a byte array.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <returns>The generated PDF as a byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> is null.
    /// </exception>
    public static byte[] RenderToBytes(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var ms = new MemoryStream();
        Render(document, ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders a document to a PDF file using streaming mode. Pages are rendered
    /// one at a time and content stream memory is released after each page is written,
    /// enabling generation of documents with 50,000+ pages.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="filePath">The output file path for the generated PDF.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> or <paramref name="filePath"/> is null.
    /// </exception>
    public static void RenderStreaming(Document document, string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(filePath);

        using var stream = File.Create(filePath);
        RenderStreaming(document, stream);
    }

    /// <summary>
    /// Renders a document to a PDF stream using streaming mode. Pages are rendered
    /// one at a time and content stream memory is released after each page is written,
    /// enabling generation of documents with 50,000+ pages.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">The output stream for the generated PDF.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document"/> or <paramref name="stream"/> is null.
    /// </exception>
    public static void RenderStreaming(Document document, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);

        ValidateAccessibility(document);

        FontRegistry.EnsureInitialized();

        using var pdfDoc = new PdfDocument();
        pdfDoc.Conformance = document.Conformance;

        // Forward document language to the PDF/UA metadata layer.
        if (!string.IsNullOrEmpty(document.Language) && pdfDoc._uaManager != null)
            pdfDoc._uaManager.SetDocumentLanguage(document.Language);

        double pageWidth = document.PageSize.Width;
        double pageHeight = document.PageSize.Height;
        double contentWidth = pageWidth - document.Margins.HorizontalTotal;
        double contentHeight = pageHeight - document.Margins.VerticalTotal;

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            // Margins exceed page dimensions. Create a single blank page.
            pdfDoc.AddPage();
            pdfDoc.Save(stream);
            return;
        }

        // Split the document children into page groups at PageBreak elements.
        var pageGroups = SplitByPageBreaks(document.Children);

        if (pageGroups.Count == 0)
        {
            // No content. Create a single blank page.
            var blankPage = pdfDoc.AddPage();
            SetPageSize(blankPage, pageWidth, pageHeight);
            RenderWatermarkIfPresent(pdfDoc, document, blankPage);
            pdfDoc.Save(stream);
            return;
        }

        // Counting pass: determine total pages if {pages} token is used.
        bool needsTotalPages = HasTotalPagesToken(document.Children);
        int totalPages = needsTotalPages
            ? CountPages(document, contentWidth, contentHeight)
            : 0;

        int currentPageNumber = 0;

        // Render pages one at a time instead of collecting closures for all pages.
        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                // Blank page from consecutive PageBreaks.
                currentPageNumber++;
                var blankPage = pdfDoc.AddPage();
                SetPageSize(blankPage, pageWidth, pageHeight);
                MarkPageContentForRelease(blankPage);
                continue;
            }

            // Wrap the group in a Column so the layout engine processes them as a unit.
            // Use unlimited height (1e6) so flex-shrink does not compress content.
            // Pagination handles overflow by distributing children across pages.
            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(
                wrapper, contentWidth, 1e6,
                document.Margins.Left, document.Margins.Top);

            // Render pages for this group immediately, one at a time.
            RenderGroupStreaming(pdfDoc, document, rootNode, pageWidth, pageHeight,
                contentWidth, contentHeight, ref currentPageNumber,
                needsTotalPages ? totalPages : 0);
        }

        // For non-PDF/UA documents, render watermarks in a final pass.
        // PDF/UA watermarks are rendered per-page inside FlushPage.
        if (pdfDoc._uaManager == null)
            RenderWatermarkIfPresent(pdfDoc, document);

        pdfDoc.Save(stream);
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Recursively checks whether any <see cref="TextBlock"/> in the element tree contains
    /// the <c>{pages}</c> token. Used by the streaming renderer to determine if a
    /// layout-only counting pass is needed.
    /// </summary>
    /// <param name="elements">The elements to inspect.</param>
    /// <returns><c>true</c> if any text block contains the <c>{pages}</c> token.</returns>
    internal static bool HasTotalPagesToken(IEnumerable<Element> elements)
    {
        foreach (var element in elements)
        {
            if (element is TextBlock tb &&
                !string.IsNullOrEmpty(tb.Text) &&
                tb.Text.Contains("{pages}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (element is Container container && HasTotalPagesToken(container.Children))
                return true;

            if (element is Table table)
            {
                if (HasTotalPagesTokenInRow(table.Columns))
                    return true;
                foreach (var row in table.Rows)
                {
                    if (HasTotalPagesTokenInRow(row))
                        return true;
                }
                if (table.FooterRow != null && HasTotalPagesTokenInRow(table.FooterRow))
                    return true;
            }
        }

        return false;
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Checks whether any cell value in a table row contains the <c>{pages}</c> token.
    /// </summary>
    private static bool HasTotalPagesTokenInRow(IEnumerable<object> cells)
    {
        foreach (var cell in cells)
        {
            if (cell is string s && s.Contains("{pages}", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether any column header in a table contains the <c>{pages}</c> token.
    /// </summary>
    private static bool HasTotalPagesTokenInRow(IEnumerable<TableColumn> columns)
    {
        foreach (var col in columns)
        {
            if (col.Header.Contains("{pages}", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Recursively renders a layout node and its children. Draws background and border first,
    /// then dispatches to the appropriate content renderer based on the source element type.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node to render.</param>
    private static void RenderNode(RenderContext ctx, LayoutNode node)
    {
        var element = node.Source;
        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;

        // Skip zero-area nodes.
        if (node.Width <= 0 && node.Height <= 0)
            return;

        // Wrap decorative content (background + border) in Artifact for PDF/UA.
        if (sb != null)
        {
            sb.BeginArtifact();
            BackgroundRenderer.Render(gfx, node);
            BorderRenderer.Render(gfx, node);
            sb.End();
        }
        else
        {
            BackgroundRenderer.Render(gfx, node);
            BorderRenderer.Render(gfx, node);
        }

        // Render element-specific content.
        switch (element)
        {
            case TextBlock tb:
                TextRenderer.RenderTextBlock(ctx, node, tb);
                break;

            case RichText rt:
                TextRenderer.RenderRichText(ctx, node, rt);
                break;

            case ImageBox img:
                ImageRenderer.Render(ctx, node, img);
                break;

            case SvgBox svg:
                SvgRenderer.Render(ctx, node, svg);
                break;

            case Divider div:
                if (sb != null) sb.BeginArtifact();
                DividerRenderer.Render(gfx, node, div);
                if (sb != null) sb.End();
                break;

            case Table tbl:
                TableRenderer.Render(ctx, node, tbl);
                break;

            // Containers (Row, Column, Box, etc.): render children recursively.
            default:
                foreach (var child in node.Children)
                {
                    RenderNode(ctx, child);
                }
                break;
        }
    }

    /// <summary>
    /// Paginates a layout group's children across multiple pages. Walks the root Column's
    /// children, tracking the vertical cursor position. When an element overflows the current
    /// page, it is moved to the next page. Tables are split across pages with header repetition.
    /// Returns the collected page render actions without creating PDF pages.
    /// </summary>
    /// <param name="document">The PdfFlex document for page size and margin info.</param>
    /// <param name="rootNode">The root Column layout node with unlimited-height layout.</param>
    /// <param name="contentWidth">The content area width (page minus horizontal margins).</param>
    /// <param name="contentHeight">The content area height (page minus vertical margins).</param>
    /// <returns>A list of pages, each containing a list of render actions.</returns>
    private static List<List<Action<RenderContext>>> PaginateGroup(
        Document document,
        LayoutNode rootNode,
        double contentWidth,
        double contentHeight)
    {
        double marginLeft = document.Margins.Left;
        double marginTop = document.Margins.Top;
        double pageBottom = marginTop + contentHeight;

        // Collect page assignments: each entry is a list of render actions for that page.
        var pages = new List<List<Action<RenderContext>>>();
        pages.Add(new List<Action<RenderContext>>());

        // cursorY tracks the current Y position on the current page (in page coordinates).
        double cursorY = marginTop;
        int currentPageIndex = 0;

        for (int i = 0; i < rootNode.Children.Count; i++)
        {
            var childNode = rootNode.Children[i];
            var childElement = childNode.Source;
            double childHeight = childNode.Height;

            // -- Table: paginate with row-level splitting --
            if (childElement is Table table)
            {
                PaginateTable(table, childNode, marginLeft, contentWidth,
                    ref cursorY, pageBottom, marginTop, pages, ref currentPageIndex);
                continue;
            }

            // -- Non-table element --
            // If the element doesn't fit on the current page and we're not at the page top,
            // move to a new page.
            if (cursorY + childHeight > pageBottom + 0.5 && cursorY > marginTop + 0.5)
            {
                currentPageIndex++;
                if (currentPageIndex >= pages.Count)
                    pages.Add(new List<Action<RenderContext>>());
                cursorY = marginTop;
            }

            // Capture the Y offset for this element on this page.
            double targetY = cursorY;
            double sourceY = childNode.Y;
            double yOffset = targetY - sourceY;
            var capturedNode = childNode;

            pages[currentPageIndex].Add(ctx =>
            {
                var state = ctx.Graphics.Save();
                ctx.Graphics.TranslateTransform(0, yOffset);
                RenderNode(ctx, capturedNode);
                ctx.Graphics.Restore(state);
            });

            cursorY += childHeight;

            // Account for gap between children in the Column layout.
            if (i < rootNode.Children.Count - 1 && rootNode.Source is Column col)
            {
                cursorY += col.Gap;
            }
        }

        return pages;
    }

    /// <summary>
    /// Paginates a table element across multiple pages with header repetition,
    /// continuation text, orphan prevention, and footer row handling.
    /// </summary>
    /// <param name="table">The table element to paginate.</param>
    /// <param name="tableNode">The layout node for the table.</param>
    /// <param name="x">The left X position of the table.</param>
    /// <param name="width">The available width for the table.</param>
    /// <param name="cursorY">The current Y position on the current page (updated on return).</param>
    /// <param name="pageBottom">The Y coordinate of the bottom of the content area.</param>
    /// <param name="marginTop">The top margin offset.</param>
    /// <param name="pages">The list of page render action lists.</param>
    /// <param name="currentPageIndex">The current page index (updated on return).</param>
    private static void PaginateTable(
        Table table,
        LayoutNode tableNode,
        double x,
        double width,
        ref double cursorY,
        double pageBottom,
        double marginTop,
        List<List<Action<RenderContext>>> pages,
        ref int currentPageIndex)
    {
        double dataRowHeight = TableRenderer.GetRowHeight(table);
        double headerHeight = TableRenderer.GetHeaderRowHeight(table);
        double footerHeight = table.FooterRow != null ? TableRenderer.GetFooterRowHeight(table) : 0;
        int totalDataRows = table.Rows.Count;
        int currentRow = 0;
        bool isFirstSegment = true;

        while (currentRow < totalDataRows)
        {
            double availableOnPage = pageBottom - cursorY;

            // Calculate how many data rows fit on this page after the header.
            double spaceAfterHeader = availableOnPage - headerHeight;
            int remainingRows = totalDataRows - currentRow;

            int rowsThatFit = (int)Math.Floor(spaceAfterHeader / dataRowHeight);

            // Check if this will be the final segment (all remaining rows fit).
            if (rowsThatFit >= remainingRows)
            {
                // Verify footer fits too.
                double neededHeight = headerHeight + (remainingRows * dataRowHeight) + footerHeight;
                if (neededHeight > availableOnPage + 0.5)
                {
                    // Footer doesn't fit. Reduce rows.
                    rowsThatFit = (int)Math.Floor((availableOnPage - headerHeight - footerHeight) / dataRowHeight);
                }
            }

            // Orphan prevention: if fewer than MinRowsBeforeBreak data rows fit
            // and we're not at the page top, move the entire table start to a new page.
            if (rowsThatFit < table.MinRowsBeforeBreak && cursorY > marginTop + 0.5)
            {
                currentPageIndex++;
                if (currentPageIndex >= pages.Count)
                    pages.Add(new List<Action<RenderContext>>());
                cursorY = marginTop;
                continue; // Re-evaluate with full page space.
            }

            // If even a full page can't fit MinRowsBeforeBreak, render what we can.
            if (rowsThatFit <= 0)
            {
                rowsThatFit = 1; // Must render at least one row to make progress.
            }

            // Clamp to remaining rows.
            int rowsThisPage = Math.Min(rowsThatFit, remainingRows);

            // Determine if this is the final segment (footer goes here).
            bool isFinalSegment = (currentRow + rowsThisPage >= totalDataRows);
            bool includeFooter = isFinalSegment && table.FooterRow != null;

            // If final and footer would push us over, reduce rows.
            if (includeFooter)
            {
                double totalNeeded = headerHeight + (rowsThisPage * dataRowHeight) + footerHeight;
                while (rowsThisPage > 1 && totalNeeded > pageBottom - cursorY + 0.5)
                {
                    rowsThisPage--;
                    totalNeeded = headerHeight + (rowsThisPage * dataRowHeight) + footerHeight;
                }

                // Recheck if this is still the final segment after row reduction.
                isFinalSegment = (currentRow + rowsThisPage >= totalDataRows);
                includeFooter = isFinalSegment && table.FooterRow != null;
            }

            int endRow = currentRow + rowsThisPage;
            bool isContinuation = !isFirstSegment;
            double segmentY = cursorY;

            // Capture values for the closure.
            int capturedStartRow = currentRow;
            int capturedEndRow = endRow;
            bool capturedIsContinuation = isContinuation;
            bool capturedIncludeFooter = includeFooter;
            double capturedX = x;
            double capturedY = segmentY;
            double capturedWidth = width;

            pages[currentPageIndex].Add(ctx =>
            {
                TableRenderer.RenderSegment(ctx, table, capturedX, capturedY, capturedWidth,
                    capturedStartRow, capturedEndRow, capturedIsContinuation, capturedIncludeFooter);
            });

            // Advance cursor.
            double segmentHeight = headerHeight + (rowsThisPage * dataRowHeight);
            if (includeFooter)
                segmentHeight += footerHeight;
            cursorY += segmentHeight;

            currentRow = endRow;
            isFirstSegment = false;

            // If more rows remain, move to the next page.
            if (currentRow < totalDataRows)
            {
                currentPageIndex++;
                if (currentPageIndex >= pages.Count)
                    pages.Add(new List<Action<RenderContext>>());
                cursorY = marginTop;
            }
        }

        // Handle tables with zero data rows (header-only).
        if (totalDataRows == 0)
        {
            double segmentY = cursorY;
            bool includeFooter = table.FooterRow != null;

            pages[currentPageIndex].Add(ctx =>
            {
                TableRenderer.RenderSegment(ctx, table, x, segmentY, width,
                    0, 0, false, includeFooter);
            });

            cursorY += headerHeight + (includeFooter ? footerHeight : 0);
        }
    }

    /// <summary>
    /// Splits a list of elements into groups separated by <see cref="PageBreak"/> elements.
    /// Each group represents the content for one page.
    /// </summary>
    private static List<List<Element>> SplitByPageBreaks(List<Element> elements)
    {
        var groups = new List<List<Element>>();
        var current = new List<Element>();

        foreach (var element in elements)
        {
            if (element is PageBreak)
            {
                groups.Add(current);
                current = new List<Element>();
            }
            else
            {
                current.Add(element);
            }
        }

        // Add the final group if it has content.
        if (current.Count > 0)
        {
            groups.Add(current);
        }

        return groups;
    }

    /// <summary>
    /// Sets the page dimensions on a PdfFlex <see cref="PdfPage"/>.
    /// </summary>
    private static void SetPageSize(PdfPage page, double width, double height)
    {
        page.Width = XUnit.FromPoint(width);
        page.Height = XUnit.FromPoint(height);
    }

    /// <summary>
    /// Renders the watermark on every page in the document if one is defined.
    /// The watermark is drawn using <see cref="XGraphicsPdfPageOptions.Prepend"/>
    /// so it appears behind the page content.
    /// </summary>
    private static void RenderWatermarkIfPresent(PdfDocument pdfDoc, Document document)
    {
        if (document.Watermark == null)
            return;

        for (int i = 0; i < pdfDoc.PageCount; i++)
        {
            RenderWatermarkIfPresent(pdfDoc, document, pdfDoc.Pages[i]);
        }
    }

    /// <summary>
    /// Renders the watermark on a single page, drawing behind existing content.
    /// </summary>
    private static void RenderWatermarkIfPresent(PdfDocument pdfDoc, Document document, PdfPage page)
    {
        if (document.Watermark == null)
            return;

        // Use Prepend mode so the watermark renders behind the main content.
        using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);

        var sb = pdfDoc._uaManager?.StructureBuilder;
        if (sb != null) sb.BeginArtifact();

        WatermarkRenderer.Render(
            gfx,
            document.Watermark,
            document.PageSize.Width,
            document.PageSize.Height,
            pdfDoc.Conformance);

        if (sb != null) sb.End();
    }

    /// <summary>
    /// Counts the total number of pages that would be produced by the document
    /// without creating PDF objects or rendering. Used by the streaming renderer
    /// when <c>{pages}</c> tokens are present.
    /// </summary>
    /// <param name="document">The document to count pages for.</param>
    /// <param name="contentWidth">The content area width (page minus horizontal margins).</param>
    /// <param name="contentHeight">The content area height (page minus vertical margins).</param>
    /// <returns>The total number of pages.</returns>
    private static int CountPages(Document document, double contentWidth, double contentHeight)
    {
        var pageGroups = SplitByPageBreaks(document.Children);
        if (pageGroups.Count == 0)
            return 1;

        int totalPages = 0;

        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                totalPages++; // Blank page from consecutive PageBreaks.
                continue;
            }

            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(
                wrapper, contentWidth, 1e6,
                document.Margins.Left, document.Margins.Top);

            totalPages += CountPagesInGroup(document, rootNode, contentHeight);
        }

        return Math.Max(totalPages, 1);
    }

    /// <summary>
    /// Counts the number of pages a single layout group would produce.
    /// Mirrors the pagination logic of <see cref="PaginateGroup"/> without rendering.
    /// </summary>
    /// <param name="document">The document for margin information.</param>
    /// <param name="rootNode">The root Column layout node.</param>
    /// <param name="contentHeight">The content area height per page.</param>
    /// <returns>The number of pages for this group.</returns>
    private static int CountPagesInGroup(
        Document document,
        LayoutNode rootNode,
        double contentHeight)
    {
        double marginTop = document.Margins.Top;
        double pageBottom = marginTop + contentHeight;

        int pageCount = 1;
        double cursorY = marginTop;

        for (int i = 0; i < rootNode.Children.Count; i++)
        {
            var childNode = rootNode.Children[i];
            var childElement = childNode.Source;
            double childHeight = childNode.Height;

            if (childElement is Table table)
            {
                CountTablePages(table, ref cursorY, pageBottom, marginTop, ref pageCount);
                continue;
            }

            if (cursorY + childHeight > pageBottom + 0.5 && cursorY > marginTop + 0.5)
            {
                pageCount++;
                cursorY = marginTop;
            }

            cursorY += childHeight;

            if (i < rootNode.Children.Count - 1 && rootNode.Source is Column col)
            {
                cursorY += col.Gap;
            }
        }

        return pageCount;
    }

    /// <summary>
    /// Counts the number of pages a table would consume during pagination.
    /// Mirrors the row-splitting logic of <see cref="PaginateTable"/> without rendering.
    /// </summary>
    /// <param name="table">The table element.</param>
    /// <param name="cursorY">The current Y position on the current page (updated on return).</param>
    /// <param name="pageBottom">The Y coordinate of the bottom of the content area.</param>
    /// <param name="marginTop">The top margin offset.</param>
    /// <param name="pageCount">The running page count (updated on return).</param>
    private static void CountTablePages(
        Table table,
        ref double cursorY,
        double pageBottom,
        double marginTop,
        ref int pageCount)
    {
        double dataRowHeight = TableRenderer.GetRowHeight(table);
        double headerHeight = TableRenderer.GetHeaderRowHeight(table);
        double footerHeight = table.FooterRow != null ? TableRenderer.GetFooterRowHeight(table) : 0;
        int totalDataRows = table.Rows.Count;
        int currentRow = 0;

        while (currentRow < totalDataRows)
        {
            double availableOnPage = pageBottom - cursorY;
            double spaceAfterHeader = availableOnPage - headerHeight;
            int remainingRows = totalDataRows - currentRow;
            int rowsThatFit = (int)Math.Floor(spaceAfterHeader / dataRowHeight);

            // Check if this will be the final segment (all remaining rows fit).
            if (rowsThatFit >= remainingRows)
            {
                double neededHeight = headerHeight + (remainingRows * dataRowHeight) + footerHeight;
                if (neededHeight > availableOnPage + 0.5)
                    rowsThatFit = (int)Math.Floor((availableOnPage - headerHeight - footerHeight) / dataRowHeight);
            }

            // Orphan prevention.
            if (rowsThatFit < table.MinRowsBeforeBreak && cursorY > marginTop + 0.5)
            {
                pageCount++;
                cursorY = marginTop;
                continue;
            }

            if (rowsThatFit <= 0)
                rowsThatFit = 1;

            int rowsThisPage = Math.Min(rowsThatFit, remainingRows);

            bool isFinalSegment = (currentRow + rowsThisPage >= totalDataRows);
            bool includeFooter = isFinalSegment && table.FooterRow != null;

            // If final and footer would push us over, reduce rows.
            if (includeFooter)
            {
                double totalNeeded = headerHeight + (rowsThisPage * dataRowHeight) + footerHeight;
                while (rowsThisPage > 1 && totalNeeded > pageBottom - cursorY + 0.5)
                {
                    rowsThisPage--;
                    totalNeeded = headerHeight + (rowsThisPage * dataRowHeight) + footerHeight;
                }
                isFinalSegment = (currentRow + rowsThisPage >= totalDataRows);
                includeFooter = isFinalSegment && table.FooterRow != null;
            }

            double segmentHeight = headerHeight + (rowsThisPage * dataRowHeight);
            if (includeFooter)
                segmentHeight += footerHeight;
            cursorY += segmentHeight;

            currentRow += rowsThisPage;

            if (currentRow < totalDataRows)
            {
                pageCount++;
                cursorY = marginTop;
            }
        }

        // Handle tables with zero data rows (header-only).
        if (totalDataRows == 0)
        {
            cursorY += headerHeight + (table.FooterRow != null ? footerHeight : 0);
        }
    }

    /// <summary>
    /// Renders a page group in streaming mode. Each page is rendered immediately
    /// rather than collecting closures for deferred rendering.
    /// </summary>
    /// <param name="pdfDoc">The PDF document being built.</param>
    /// <param name="document">The PdfFlex document for margins and page size.</param>
    /// <param name="rootNode">The root Column layout node.</param>
    /// <param name="pageWidth">The page width in points.</param>
    /// <param name="pageHeight">The page height in points.</param>
    /// <param name="contentWidth">The content area width.</param>
    /// <param name="contentHeight">The content area height.</param>
    /// <param name="currentPageNumber">The current page number (updated on return).</param>
    /// <param name="totalPages">The total number of pages (0 if unknown).</param>
    private static void RenderGroupStreaming(
        PdfDocument pdfDoc,
        Document document,
        LayoutNode rootNode,
        double pageWidth,
        double pageHeight,
        double contentWidth,
        double contentHeight,
        ref int currentPageNumber,
        int totalPages)
    {
        double marginTop = document.Margins.Top;
        double pageBottom = marginTop + contentHeight;

        var currentPageActions = new List<Action<RenderContext>>();
        double cursorY = marginTop;
        var sb = pdfDoc._uaManager?.StructureBuilder;

        // Use a local variable because ref parameters cannot be captured in local functions.
        int pageNumber = currentPageNumber;

        void FlushPage()
        {
            pageNumber++;
            var pdfPage = pdfDoc.AddPage();
            SetPageSize(pdfPage, pageWidth, pageHeight);

            using (var gfx = XGraphics.FromPdfPage(pdfPage))
            {
                var ctx = new RenderContext(gfx, pageNumber, totalPages, pdfDoc.Conformance, sb);

                foreach (var action in currentPageActions)
                    action(ctx);
            }

            // Render watermark per-page when UAManager is active (it rejects
            // XGraphics for non-current pages in the final loop).
            if (pdfDoc._uaManager != null)
                RenderWatermarkIfPresent(pdfDoc, document, pdfPage);

            currentPageActions.Clear();
            MarkPageContentForRelease(pdfPage);
        }

        for (int i = 0; i < rootNode.Children.Count; i++)
        {
            var childNode = rootNode.Children[i];
            var childElement = childNode.Source;
            double childHeight = childNode.Height;

            // Table: paginate with row-level splitting, rendering each segment immediately.
            if (childElement is Table table)
            {
                PaginateTableStreaming(table, childNode, document.Margins.Left, contentWidth,
                    ref cursorY, pageBottom, marginTop, currentPageActions, FlushPage);
                continue;
            }

            // Non-table element: if it overflows the current page, flush and start a new page.
            if (cursorY + childHeight > pageBottom + 0.5 && cursorY > marginTop + 0.5)
            {
                FlushPage();
                cursorY = marginTop;
            }

            double targetY = cursorY;
            double sourceY = childNode.Y;
            double yOffset = targetY - sourceY;
            var capturedNode = childNode;

            currentPageActions.Add(ctx =>
            {
                var state = ctx.Graphics.Save();
                ctx.Graphics.TranslateTransform(0, yOffset);
                RenderNode(ctx, capturedNode);
                ctx.Graphics.Restore(state);
            });

            cursorY += childHeight;

            // Account for gap between children in the Column layout.
            if (i < rootNode.Children.Count - 1 && rootNode.Source is Column col)
            {
                cursorY += col.Gap;
            }
        }

        // Flush remaining content on the last page.
        if (currentPageActions.Count > 0)
        {
            FlushPage();
        }

        // Write back the updated page number.
        currentPageNumber = pageNumber;
    }

    /// <summary>
    /// Paginates a table in streaming mode with header repetition, continuation text,
    /// orphan prevention, and footer row handling. Adds render actions to the current
    /// page and calls <paramref name="flushPage"/> when moving to a new page.
    /// </summary>
    /// <param name="table">The table element to paginate.</param>
    /// <param name="tableNode">The layout node for the table.</param>
    /// <param name="x">The left X position of the table.</param>
    /// <param name="width">The available width for the table.</param>
    /// <param name="cursorY">The current Y position on the current page (updated on return).</param>
    /// <param name="pageBottom">The Y coordinate of the bottom of the content area.</param>
    /// <param name="marginTop">The top margin offset.</param>
    /// <param name="currentPageActions">The render actions for the current page.</param>
    /// <param name="flushPage">Delegate that flushes the current page and starts a new one.</param>
    private static void PaginateTableStreaming(
        Table table,
        LayoutNode tableNode,
        double x,
        double width,
        ref double cursorY,
        double pageBottom,
        double marginTop,
        List<Action<RenderContext>> currentPageActions,
        Action flushPage)
    {
        double dataRowHeight = TableRenderer.GetRowHeight(table);
        double headerHeight = TableRenderer.GetHeaderRowHeight(table);
        double footerHeight = table.FooterRow != null ? TableRenderer.GetFooterRowHeight(table) : 0;
        int totalDataRows = table.Rows.Count;
        int currentRow = 0;
        bool isFirstSegment = true;

        while (currentRow < totalDataRows)
        {
            double availableOnPage = pageBottom - cursorY;

            // Calculate how many data rows fit on this page after the header.
            double spaceAfterHeader = availableOnPage - headerHeight;
            int remainingRows = totalDataRows - currentRow;

            int rowsThatFit = (int)Math.Floor(spaceAfterHeader / dataRowHeight);

            // Check if this will be the final segment (all remaining rows fit).
            if (rowsThatFit >= remainingRows)
            {
                // Verify footer fits too.
                double neededHeight = headerHeight + (remainingRows * dataRowHeight) + footerHeight;
                if (neededHeight > availableOnPage + 0.5)
                {
                    // Footer doesn't fit. Reduce rows.
                    rowsThatFit = (int)Math.Floor((availableOnPage - headerHeight - footerHeight) / dataRowHeight);
                }
            }

            // Orphan prevention: if fewer than MinRowsBeforeBreak data rows fit
            // and we're not at the page top, move the entire table start to a new page.
            if (rowsThatFit < table.MinRowsBeforeBreak && cursorY > marginTop + 0.5)
            {
                flushPage();
                cursorY = marginTop;
                continue; // Re-evaluate with full page space.
            }

            // If even a full page can't fit MinRowsBeforeBreak, render what we can.
            if (rowsThatFit <= 0)
            {
                rowsThatFit = 1; // Must render at least one row to make progress.
            }

            // Clamp to remaining rows.
            int rowsThisPage = Math.Min(rowsThatFit, remainingRows);

            // Determine if this is the final segment (footer goes here).
            bool isFinalSegment = (currentRow + rowsThisPage >= totalDataRows);
            bool includeFooter = isFinalSegment && table.FooterRow != null;

            // If final and footer would push us over, reduce rows.
            if (includeFooter)
            {
                double totalNeeded = headerHeight + (rowsThisPage * dataRowHeight) + footerHeight;
                while (rowsThisPage > 1 && totalNeeded > pageBottom - cursorY + 0.5)
                {
                    rowsThisPage--;
                    totalNeeded = headerHeight + (rowsThisPage * dataRowHeight) + footerHeight;
                }

                // Recheck if this is still the final segment after row reduction.
                isFinalSegment = (currentRow + rowsThisPage >= totalDataRows);
                includeFooter = isFinalSegment && table.FooterRow != null;
            }

            int endRow = currentRow + rowsThisPage;
            bool isContinuation = !isFirstSegment;
            double segmentY = cursorY;

            // Capture values for the closure.
            int capturedStartRow = currentRow;
            int capturedEndRow = endRow;
            bool capturedIsContinuation = isContinuation;
            bool capturedIncludeFooter = includeFooter;
            double capturedX = x;
            double capturedY = segmentY;
            double capturedWidth = width;

            currentPageActions.Add(ctx =>
            {
                TableRenderer.RenderSegment(ctx, table, capturedX, capturedY, capturedWidth,
                    capturedStartRow, capturedEndRow, capturedIsContinuation, capturedIncludeFooter);
            });

            // Advance cursor.
            double segmentHeight = headerHeight + (rowsThisPage * dataRowHeight);
            if (includeFooter)
                segmentHeight += footerHeight;
            cursorY += segmentHeight;

            currentRow = endRow;
            isFirstSegment = false;

            // If more rows remain, move to the next page.
            if (currentRow < totalDataRows)
            {
                flushPage();
                cursorY = marginTop;
            }
        }

        // Handle tables with zero data rows (header-only).
        if (totalDataRows == 0)
        {
            double segmentY = cursorY;
            bool includeFooter = table.FooterRow != null;

            currentPageActions.Add(ctx =>
            {
                TableRenderer.RenderSegment(ctx, table, x, segmentY, width,
                    0, 0, false, includeFooter);
            });

            cursorY += headerHeight + (includeFooter ? footerHeight : 0);
        }
    }

    /// <summary>
    /// Marks a page's content streams for memory release after writing.
    /// The content stream bytes will be cleared after they are written to the output,
    /// freeing memory for large documents.
    /// </summary>
    /// <param name="page">The page whose content streams should be released after writing.</param>
    private static void MarkPageContentForRelease(PdfPage page)
    {
        var contents = page.Contents;
        for (int i = 0; i < contents.Elements.Count; i++)
        {
            if (contents.Elements[i] is PdfReference reference &&
                reference.Value is PdfContent content)
            {
                content.ReleaseAfterWrite = true;
            }
        }
    }

    /// <summary>
    /// Validates accessibility requirements before rendering begins. Throws when
    /// the conformance profile requires tagged structure and the element tree does
    /// not meet the requirements (missing alt text, missing document language).
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the document fails accessibility validation.
    /// </exception>
    private static void ValidateAccessibility(Document document)
    {
        var conformance = document.Conformance;

        if (conformance.RequiresDocumentLanguage && string.IsNullOrEmpty(document.Language))
        {
            throw new InvalidOperationException(
                "Document.Language is required when using conformance profile " +
                $"'{conformance}'. Set Document.Language to a BCP 47 language tag (e.g. \"en-US\").");
        }

        if (conformance.RequiresTaggedStructure)
        {
            ValidateAltText(document.Children);
        }
    }

    /// <summary>
    /// Recursively validates that all <see cref="ImageBox"/> and <see cref="SvgBox"/>
    /// elements have non-empty <c>AltText</c> for accessibility compliance.
    /// </summary>
    /// <param name="elements">The elements to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an image or SVG element is missing alt text.
    /// </exception>
    private static void ValidateAltText(IEnumerable<Element> elements)
    {
        foreach (var element in elements)
        {
            if (element is ImageBox img && string.IsNullOrEmpty(img.AltText))
            {
                throw new InvalidOperationException(
                    "ImageBox.AltText is required when using a conformance profile that requires " +
                    "tagged structure (e.g. PDF/UA-1). Set AltText on all ImageBox elements.");
            }

            if (element is SvgBox svg && string.IsNullOrEmpty(svg.AltText))
            {
                throw new InvalidOperationException(
                    "SvgBox.AltText is required when using a conformance profile that requires " +
                    "tagged structure (e.g. PDF/UA-1). Set AltText on all SvgBox elements.");
            }

            if (element is Container container)
            {
                ValidateAltText(container.Children);
            }
        }
    }

    #endregion Private Methods
}
