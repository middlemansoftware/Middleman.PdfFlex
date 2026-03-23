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
        ValidateHeaderFooter(document);

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

        // Measure the default header/footer heights (used for pages 2+).
        // Headers/footers render at full page width, so measure with pageWidth at x=0.
        double headerHeight = MeasureElementHeight(document.Header, pageWidth, 0, 0);
        double footerHeight = MeasureElementHeight(document.Footer, pageWidth, 0, 0);
        double defaultContentHeight = contentHeight - (headerHeight + footerHeight);

        // Measure first-page header/footer heights (used for page 1).
        double firstPageHeaderHeight = MeasureElementHeight(
            GetHeaderForPage(document, 1), pageWidth, 0, 0);
        double firstPageFooterHeight = MeasureElementHeight(
            GetFooterForPage(document, 1), pageWidth, 0, 0);
        double firstPageContentHeight = contentHeight - (firstPageHeaderHeight + firstPageFooterHeight);

        if (defaultContentHeight <= 0 || firstPageContentHeight <= 0)
        {
            // Header/footer consume all available space. Create a single blank page.
            pdfDoc.AddPage();
            pdfDoc.Save(stream);
            return;
        }

        // Split the document children into page groups at PageBreak elements.
        var pageGroups = SplitByPageBreaks(document.Children);

        if (pageGroups.Count == 0)
        {
            // No content. Create a single blank page with header/footer.
            var blankPage = pdfDoc.AddPage();
            SetPageSize(blankPage, pageWidth, pageHeight);

            using (var gfx = XGraphics.FromPdfPage(blankPage))
            {
                var sb0 = pdfDoc._uaManager?.StructureBuilder;
                var ctx = new RenderContext(gfx, 1, 1, pdfDoc.Conformance, sb0);
                RenderHeaderFooter(GetHeaderForPage(document, 1), ctx,
                    0, 0, pageWidth);
                RenderHeaderFooter(GetFooterForPage(document, 1), ctx,
                    0, pageHeight - firstPageFooterHeight, pageWidth);
            }

            RenderWatermarkIfPresent(pdfDoc, document, blankPage);
            pdfDoc.Save(stream);
            return;
        }

        // Phase 1: Collect all page render actions across all groups.
        var allPages = new List<List<Action<RenderContext>>>();

        // The body content top is shifted down by the margin and header height.
        // Layout uses the default header height; PaginateGroup applies per-page offsets.
        double bodyMarginTop = document.Margins.Top + headerHeight;

        int absolutePageIdx = 0;

        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                // Empty group from consecutive PageBreaks: produce a blank page.
                allPages.Add(new List<Action<RenderContext>>());
                absolutePageIdx++;
                continue;
            }

            // Wrap the group in a Column so the layout engine processes them as a unit.
            // Use unlimited height (1e6) so flex-shrink does not compress content.
            // Pagination handles overflow by distributing children across pages.
            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(
                wrapper, contentWidth, 1e6,
                document.Margins.Left, bodyMarginTop);

            // Paginate the root Column's children across multiple pages.
            var groupPages = PaginateGroup(document, rootNode, contentWidth, defaultContentHeight,
                headerHeight, footerHeight,
                firstPageContentHeight, firstPageHeaderHeight, absolutePageIdx);
            allPages.AddRange(groupPages);
            absolutePageIdx += groupPages.Count;
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
            int pageNum = pageIdx + 1;

            // Determine the actual footer height for this page.
            double actualFooterHeight = (pageNum == 1) ? firstPageFooterHeight : footerHeight;

            // Content graphics must be disposed before creating watermark graphics
            // for the same page (only one XGraphics per page at a time).
            using (var gfx = XGraphics.FromPdfPage(pdfPage))
            {
                var ctx = new RenderContext(gfx, pageNum, totalPages, pdfDoc.Conformance, sb);

                foreach (var action in allPages[pageIdx])
                {
                    action(ctx);
                }

                // Render header and footer at full page width, edge to edge.
                RenderHeaderFooter(GetHeaderForPage(document, pageNum), ctx,
                    0, 0, pageWidth);
                RenderHeaderFooter(GetFooterForPage(document, pageNum), ctx,
                    0, pageHeight - actualFooterHeight, pageWidth);
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
        ValidateHeaderFooter(document);

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

        // Measure the default header/footer heights (used for pages 2+).
        // Headers/footers render at full page width, so measure with pageWidth at x=0.
        double headerHeight = MeasureElementHeight(document.Header, pageWidth, 0, 0);
        double footerHeight = MeasureElementHeight(document.Footer, pageWidth, 0, 0);

        // Body content area shrinks by the header/footer heights.
        double defaultContentHeight = contentHeight - (headerHeight + footerHeight);

        // Measure first-page header/footer heights (used for page 1).
        double firstPageHeaderHeight = MeasureElementHeight(
            GetHeaderForPage(document, 1), pageWidth, 0, 0);
        double firstPageFooterHeight = MeasureElementHeight(
            GetFooterForPage(document, 1), pageWidth, 0, 0);
        double firstPageContentHeight = contentHeight - (firstPageHeaderHeight + firstPageFooterHeight);

        if (defaultContentHeight <= 0 || firstPageContentHeight <= 0)
        {
            // Header/footer consume all available space. Create a single blank page.
            pdfDoc.AddPage();
            pdfDoc.Save(stream);
            return;
        }

        // Split the document children into page groups at PageBreak elements.
        var pageGroups = SplitByPageBreaks(document.Children);

        if (pageGroups.Count == 0)
        {
            // No content. Create a single blank page with header/footer.
            var blankPage = pdfDoc.AddPage();
            SetPageSize(blankPage, pageWidth, pageHeight);

            using (var gfx = XGraphics.FromPdfPage(blankPage))
            {
                var sb0 = pdfDoc._uaManager?.StructureBuilder;
                var ctx = new RenderContext(gfx, 1, 1, pdfDoc.Conformance, sb0);
                RenderHeaderFooter(GetHeaderForPage(document, 1), ctx,
                    0, 0, pageWidth);
                RenderHeaderFooter(GetFooterForPage(document, 1), ctx,
                    0, pageHeight - firstPageFooterHeight, pageWidth);
            }

            RenderWatermarkIfPresent(pdfDoc, document, blankPage);
            pdfDoc.Save(stream);
            return;
        }

        // Counting pass: determine total pages if {pages} token is used.
        // Check both body content and header/footer elements for the token.
        bool needsTotalPages = HasTotalPagesToken(document.Children)
            || HasTotalPagesTokenInElement(document.Header)
            || HasTotalPagesTokenInElement(document.Footer)
            || HasTotalPagesTokenInElement(document.FirstPageHeader)
            || HasTotalPagesTokenInElement(document.FirstPageFooter);
        int totalPages = needsTotalPages
            ? CountPages(document, contentWidth, defaultContentHeight, headerHeight,
                firstPageContentHeight, firstPageHeaderHeight)
            : 0;

        int currentPageNumber = 0;
        double bodyMarginTop = document.Margins.Top + headerHeight;

        // Render pages one at a time instead of collecting closures for all pages.
        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                // Blank page from consecutive PageBreaks — render header/footer.
                currentPageNumber++;
                var blankPage = pdfDoc.AddPage();
                SetPageSize(blankPage, pageWidth, pageHeight);

                // Determine the actual footer height for this page.
                double actualFooterHeight = (currentPageNumber == 1) ? firstPageFooterHeight : footerHeight;

                using (var gfx = XGraphics.FromPdfPage(blankPage))
                {
                    var sbBlank = pdfDoc._uaManager?.StructureBuilder;
                    var ctx = new RenderContext(gfx, currentPageNumber, totalPages, pdfDoc.Conformance, sbBlank);
                    RenderHeaderFooter(GetHeaderForPage(document, currentPageNumber), ctx,
                        0, 0, pageWidth);
                    RenderHeaderFooter(GetFooterForPage(document, currentPageNumber), ctx,
                        0, pageHeight - actualFooterHeight, pageWidth);
                }

                if (pdfDoc._uaManager != null)
                    RenderWatermarkIfPresent(pdfDoc, document, blankPage);

                MarkPageContentForRelease(blankPage);
                continue;
            }

            // Wrap the group in a Column so the layout engine processes them as a unit.
            // Use unlimited height (1e6) so flex-shrink does not compress content.
            // Pagination handles overflow by distributing children across pages.
            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(
                wrapper, contentWidth, 1e6,
                document.Margins.Left, bodyMarginTop);

            // Render pages for this group immediately, one at a time.
            RenderGroupStreaming(pdfDoc, document, rootNode, pageWidth, pageHeight,
                contentWidth, defaultContentHeight, headerHeight, footerHeight,
                ref currentPageNumber, needsTotalPages ? totalPages : 0,
                firstPageContentHeight, firstPageHeaderHeight, firstPageFooterHeight);
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

    /// <summary>
    /// Checks whether a single element (or its children) contains the <c>{pages}</c> token.
    /// Used to scan header/footer elements that are not part of the document body list.
    /// </summary>
    /// <param name="element">The element to inspect, or null.</param>
    /// <returns><c>true</c> if any text block in the element tree contains the <c>{pages}</c> token.</returns>
    internal static bool HasTotalPagesTokenInElement(Element? element)
    {
        if (element == null) return false;
        return HasTotalPagesToken(new[] { element });
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
    /// <param name="contentHeight">The content area height for pages 2+ (minus default header/footer).</param>
    /// <param name="docHeaderHeight">The default document header height in points.</param>
    /// <param name="docFooterHeight">The default document footer height in points.</param>
    /// <param name="firstPageContentHeight">The content area height for page 1 (minus first-page header/footer). When 0, uses <paramref name="contentHeight"/>.</param>
    /// <param name="firstPageHeaderHeight">The first-page header height in points.</param>
    /// <param name="startingPageIndex">The absolute page index (0-based) at which this group starts in the document.</param>
    /// <returns>A list of pages, each containing a list of render actions.</returns>
    private static List<List<Action<RenderContext>>> PaginateGroup(
        Document document,
        LayoutNode rootNode,
        double contentWidth,
        double contentHeight,
        double docHeaderHeight = 0,
        double docFooterHeight = 0,
        double firstPageContentHeight = 0,
        double firstPageHeaderHeight = 0,
        int startingPageIndex = 0)
    {
        double marginLeft = document.Margins.Left;

        // Determine initial margins based on whether this group starts on page 1.
        double marginTop;
        double pageBottom;
        if (startingPageIndex == 0 && firstPageContentHeight > 0)
        {
            marginTop = document.Margins.Top + firstPageHeaderHeight;
            pageBottom = marginTop + firstPageContentHeight;
        }
        else
        {
            marginTop = document.Margins.Top + docHeaderHeight;
            pageBottom = marginTop + contentHeight;
        }

        // Default margins used for pages 2+ within this group.
        double defaultMarginTop = document.Margins.Top + docHeaderHeight;
        double defaultPageBottom = defaultMarginTop + contentHeight;

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
                    ref cursorY, pageBottom, marginTop, pages, ref currentPageIndex,
                    defaultPageBottom, defaultMarginTop);
                // After table pagination, switch to default margins if we moved past page 1.
                if (startingPageIndex + currentPageIndex > 0)
                {
                    marginTop = defaultMarginTop;
                    pageBottom = defaultPageBottom;
                }
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

                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
    /// <param name="defaultPageBottom">The default page bottom for pages 2+ (0 = use <paramref name="pageBottom"/>).</param>
    /// <param name="defaultMarginTop">The default margin top for pages 2+ (0 = use <paramref name="marginTop"/>).</param>
    private static void PaginateTable(
        Table table,
        LayoutNode tableNode,
        double x,
        double width,
        ref double cursorY,
        double pageBottom,
        double marginTop,
        List<List<Action<RenderContext>>> pages,
        ref int currentPageIndex,
        double defaultPageBottom = 0,
        double defaultMarginTop = 0)
    {
        // When default values are not provided, use the current page values.
        if (defaultPageBottom == 0) defaultPageBottom = pageBottom;
        if (defaultMarginTop == 0) defaultMarginTop = marginTop;

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

                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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

                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
    /// <param name="contentHeight">The content area height for pages 2+ (minus default header/footer).</param>
    /// <param name="headerHeight">The default header height in points (already measured by caller).</param>
    /// <param name="firstPageContentHeight">The content area height for page 1. When 0, uses <paramref name="contentHeight"/>.</param>
    /// <param name="firstPageHeaderHeight">The first-page header height in points.</param>
    /// <returns>The total number of pages.</returns>
    private static int CountPages(
        Document document,
        double contentWidth,
        double contentHeight,
        double headerHeight,
        double firstPageContentHeight = 0,
        double firstPageHeaderHeight = 0)
    {
        double bodyMarginTop = document.Margins.Top + headerHeight;

        var pageGroups = SplitByPageBreaks(document.Children);
        if (pageGroups.Count == 0)
            return 1;

        int absolutePageIdx = 0;

        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                absolutePageIdx++; // Blank page from consecutive PageBreaks.
                continue;
            }

            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(
                wrapper, contentWidth, 1e6,
                document.Margins.Left, bodyMarginTop);

            absolutePageIdx += CountPagesInGroup(bodyMarginTop, rootNode, contentHeight,
                firstPageContentHeight, firstPageHeaderHeight,
                document.Margins.Top, absolutePageIdx);
        }

        return Math.Max(absolutePageIdx, 1);
    }

    /// <summary>
    /// Counts the number of pages a single layout group would produce.
    /// Mirrors the pagination logic of <see cref="PaginateGroup"/> without rendering.
    /// </summary>
    /// <param name="defaultMarginTop">The effective top margin for pages 2+ (includes default header height).</param>
    /// <param name="rootNode">The root Column layout node.</param>
    /// <param name="contentHeight">The content area height for pages 2+ (minus default header/footer).</param>
    /// <param name="firstPageContentHeight">The content area height for page 1. When 0, uses <paramref name="contentHeight"/>.</param>
    /// <param name="firstPageHeaderHeight">The first-page header height in points.</param>
    /// <param name="rawMarginTop">The raw top margin (without header height) for computing first-page margin.</param>
    /// <param name="startingPageIndex">The absolute page index (0-based) at which this group starts.</param>
    /// <returns>The number of pages for this group.</returns>
    private static int CountPagesInGroup(
        double defaultMarginTop,
        LayoutNode rootNode,
        double contentHeight,
        double firstPageContentHeight = 0,
        double firstPageHeaderHeight = 0,
        double rawMarginTop = 0,
        int startingPageIndex = 0)
    {
        // Determine initial margins based on whether this group starts on page 1.
        double marginTop;
        double pageBottom;
        if (startingPageIndex == 0 && firstPageContentHeight > 0)
        {
            marginTop = rawMarginTop + firstPageHeaderHeight;
            pageBottom = marginTop + firstPageContentHeight;
        }
        else
        {
            marginTop = defaultMarginTop;
            pageBottom = defaultMarginTop + contentHeight;
        }

        double defaultPageBottom = defaultMarginTop + contentHeight;

        int pageCount = 1;
        double cursorY = marginTop;

        for (int i = 0; i < rootNode.Children.Count; i++)
        {
            var childNode = rootNode.Children[i];
            var childElement = childNode.Source;
            double childHeight = childNode.Height;

            if (childElement is Table table)
            {
                CountTablePages(table, ref cursorY, pageBottom, marginTop, ref pageCount,
                    defaultPageBottom, defaultMarginTop);
                // After table pagination, switch to default margins if we moved past page 1.
                if (startingPageIndex + pageCount - 1 > 0)
                {
                    marginTop = defaultMarginTop;
                    pageBottom = defaultPageBottom;
                }
                continue;
            }

            if (cursorY + childHeight > pageBottom + 0.5 && cursorY > marginTop + 0.5)
            {
                pageCount++;
                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
    /// <param name="defaultPageBottom">The default page bottom for pages 2+ (0 = use <paramref name="pageBottom"/>).</param>
    /// <param name="defaultMarginTop">The default margin top for pages 2+ (0 = use <paramref name="marginTop"/>).</param>
    private static void CountTablePages(
        Table table,
        ref double cursorY,
        double pageBottom,
        double marginTop,
        ref int pageCount,
        double defaultPageBottom = 0,
        double defaultMarginTop = 0)
    {
        // When default values are not provided, use the current page values.
        if (defaultPageBottom == 0) defaultPageBottom = pageBottom;
        if (defaultMarginTop == 0) defaultMarginTop = marginTop;

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
                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
    /// <param name="contentHeight">The content area height for pages 2+ (minus default header/footer).</param>
    /// <param name="docHeaderHeight">The default document header height in points.</param>
    /// <param name="docFooterHeight">The default document footer height in points.</param>
    /// <param name="currentPageNumber">The current page number (updated on return).</param>
    /// <param name="totalPages">The total number of pages (0 if unknown).</param>
    /// <param name="firstPageContentHeight">The content area height for page 1. When 0, uses <paramref name="contentHeight"/>.</param>
    /// <param name="firstPageHeaderHeight">The first-page header height in points.</param>
    /// <param name="firstPageFooterHeight">The first-page footer height in points.</param>
    private static void RenderGroupStreaming(
        PdfDocument pdfDoc,
        Document document,
        LayoutNode rootNode,
        double pageWidth,
        double pageHeight,
        double contentWidth,
        double contentHeight,
        double docHeaderHeight,
        double docFooterHeight,
        ref int currentPageNumber,
        int totalPages,
        double firstPageContentHeight = 0,
        double firstPageHeaderHeight = 0,
        double firstPageFooterHeight = 0)
    {
        // Default margins for pages 2+.
        double defaultMarginTop = document.Margins.Top + docHeaderHeight;
        double defaultPageBottom = defaultMarginTop + contentHeight;

        // Determine initial margins based on whether this group starts on page 1.
        double marginTop;
        double pageBottom;
        if (currentPageNumber == 0 && firstPageContentHeight > 0)
        {
            marginTop = document.Margins.Top + firstPageHeaderHeight;
            pageBottom = marginTop + firstPageContentHeight;
        }
        else
        {
            marginTop = defaultMarginTop;
            pageBottom = defaultPageBottom;
        }

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

            // Determine the actual footer height for this page.
            double actualFooterHeight = (pageNumber == 1) ? firstPageFooterHeight : docFooterHeight;

            using (var gfx = XGraphics.FromPdfPage(pdfPage))
            {
                var ctx = new RenderContext(gfx, pageNumber, totalPages, pdfDoc.Conformance, sb);

                foreach (var action in currentPageActions)
                    action(ctx);

                // Render header and footer at full page width, edge to edge.
                RenderHeaderFooter(GetHeaderForPage(document, pageNumber), ctx,
                    0, 0, pageWidth);
                RenderHeaderFooter(GetFooterForPage(document, pageNumber), ctx,
                    0, pageHeight - actualFooterHeight, pageWidth);
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
                    ref cursorY, pageBottom, marginTop, currentPageActions, FlushPage,
                    defaultPageBottom, defaultMarginTop);
                // After table pagination, switch to default margins if we moved past page 1.
                if (pageNumber > 0)
                {
                    marginTop = defaultMarginTop;
                    pageBottom = defaultPageBottom;
                }
                continue;
            }

            // Non-table element: if it overflows the current page, flush and start a new page.
            if (cursorY + childHeight > pageBottom + 0.5 && cursorY > marginTop + 0.5)
            {
                FlushPage();
                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
    /// <param name="defaultPageBottom">The default page bottom for pages 2+ (0 = use <paramref name="pageBottom"/>).</param>
    /// <param name="defaultMarginTop">The default margin top for pages 2+ (0 = use <paramref name="marginTop"/>).</param>
    private static void PaginateTableStreaming(
        Table table,
        LayoutNode tableNode,
        double x,
        double width,
        ref double cursorY,
        double pageBottom,
        double marginTop,
        List<Action<RenderContext>> currentPageActions,
        Action flushPage,
        double defaultPageBottom = 0,
        double defaultMarginTop = 0)
    {
        // When default values are not provided, use the current page values.
        if (defaultPageBottom == 0) defaultPageBottom = pageBottom;
        if (defaultMarginTop == 0) defaultMarginTop = marginTop;
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
                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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
                // After page 1, always use default margins.
                marginTop = defaultMarginTop;
                pageBottom = defaultPageBottom;
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

    /// <summary>
    /// Validates that document headers and footers do not contain disallowed element types.
    /// <see cref="PageBreak"/> and <see cref="Table"/> elements are not permitted in
    /// headers or footers (including recursively within containers).
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a header or footer contains a disallowed element type.
    /// </exception>
    private static void ValidateHeaderFooter(Document document)
    {
        ValidateHeaderFooterElement(document.Header, "Header");
        ValidateHeaderFooterElement(document.Footer, "Footer");
        ValidateHeaderFooterElement(document.FirstPageHeader, "FirstPageHeader");
        ValidateHeaderFooterElement(document.FirstPageFooter, "FirstPageFooter");
    }

    /// <summary>
    /// Recursively validates that an element tree does not contain disallowed types
    /// for use in a header or footer.
    /// </summary>
    /// <param name="element">The element to validate, or null.</param>
    /// <param name="location">The name of the header/footer property for error messages.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the element or any of its descendants is a <see cref="PageBreak"/> or <see cref="Table"/>.
    /// </exception>
    private static void ValidateHeaderFooterElement(Element? element, string location)
    {
        if (element == null) return;

        if (element is PageBreak)
        {
            throw new InvalidOperationException(
                $"PageBreak is not allowed in Document.{location}. " +
                "Headers and footers cannot contain page break elements.");
        }

        if (element is Table)
        {
            throw new InvalidOperationException(
                $"Table is not allowed in Document.{location}. " +
                "Headers and footers cannot contain table elements.");
        }

        if (element is Container container)
        {
            foreach (var child in container.Children)
            {
                ValidateHeaderFooterElement(child, location);
            }
        }
    }

    /// <summary>
    /// Measures the height of an element by running it through the layout engine.
    /// Returns 0 when the element is null. Wraps the element in a Column and measures
    /// the actual content height (sum of children) rather than the Column's resolved height,
    /// which would expand to fill the available space.
    /// </summary>
    /// <param name="element">The element to measure, or null.</param>
    /// <param name="contentWidth">The available content width.</param>
    /// <param name="marginLeft">The left margin offset.</param>
    /// <param name="marginTop">The top margin offset.</param>
    /// <returns>The measured height in points, or 0 if the element is null.</returns>
    private static double MeasureElementHeight(
        Element? element, double contentWidth, double marginLeft, double marginTop)
    {
        if (element == null) return 0;
        var wrapper = new Column(new List<Element> { element });
        var node = LayoutEngine.Calculate(wrapper, contentWidth, 1e6, marginLeft, marginTop);

        // The Column container fills available height (1e6), but we need the actual
        // content height. Sum children heights to get the true measurement.
        double totalHeight = 0;
        foreach (var child in node.Children)
            totalHeight += child.Height;
        return totalHeight;
    }

    /// <summary>
    /// Renders a header or footer element on a page at the specified position.
    /// Wraps the element in a PDF/UA Artifact tag when structure tagging is active.
    /// </summary>
    /// <param name="element">The header or footer element to render, or null.</param>
    /// <param name="ctx">The render context for the current page.</param>
    /// <param name="x">The left X position for the header/footer.</param>
    /// <param name="y">The top Y position for the header/footer.</param>
    /// <param name="width">The available width for the header/footer.</param>
    private static void RenderHeaderFooter(
        Element? element, RenderContext ctx, double x, double y, double width)
    {
        if (element == null) return;
        var wrapper = new Column(new List<Element> { element });
        var node = LayoutEngine.Calculate(wrapper, width, 1e6, x, y);

        var sb = ctx.StructureBuilder;
        if (sb != null) sb.BeginArtifact();
        RenderNode(ctx, node);
        if (sb != null) sb.End();
    }

    /// <summary>
    /// Returns the appropriate header element for a given page number.
    /// Page 1 uses <see cref="Document.FirstPageHeader"/> when set, otherwise falls back
    /// to <see cref="Document.Header"/>.
    /// </summary>
    /// <param name="document">The document containing header definitions.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <returns>The header element for the page, or null.</returns>
    private static Element? GetHeaderForPage(Document document, int pageNumber)
    {
        if (pageNumber == 1 && document.FirstPageHeader != null)
            return document.FirstPageHeader;
        return document.Header;
    }

    /// <summary>
    /// Returns the appropriate footer element for a given page number.
    /// Page 1 uses <see cref="Document.FirstPageFooter"/> when set, otherwise falls back
    /// to <see cref="Document.Footer"/>.
    /// </summary>
    /// <param name="document">The document containing footer definitions.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <returns>The footer element for the page, or null.</returns>
    private static Element? GetFooterForPage(Document document, int pageNumber)
    {
        if (pageNumber == 1 && document.FirstPageFooter != null)
            return document.FirstPageFooter;
        return document.Footer;
    }

    #endregion Private Methods
}
