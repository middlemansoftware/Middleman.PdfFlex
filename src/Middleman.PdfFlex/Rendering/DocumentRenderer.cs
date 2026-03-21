// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf.Fonts;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

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

        FontRegistry.EnsureInitialized();

        using var pdfDoc = new PdfDocument();

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

        foreach (var group in pageGroups)
        {
            if (group.Count == 0)
            {
                // Empty group from consecutive PageBreaks: produce a blank page.
                var blankPage = pdfDoc.AddPage();
                SetPageSize(blankPage, pageWidth, pageHeight);
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
            PaginateGroup(pdfDoc, document, rootNode, pageWidth, pageHeight,
                contentWidth, contentHeight);
        }

        // Render watermark on every page behind content.
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

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Recursively renders a layout node and its children. Draws background and border first,
    /// then dispatches to the appropriate content renderer based on the source element type.
    /// </summary>
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="node">The layout node to render.</param>
    private static void RenderNode(XGraphics gfx, LayoutNode node)
    {
        var element = node.Source;

        // Skip zero-area nodes.
        if (node.Width <= 0 && node.Height <= 0)
            return;

        // Render background behind content.
        BackgroundRenderer.Render(gfx, node);

        // Render border around the element.
        BorderRenderer.Render(gfx, node);

        // Render element-specific content.
        switch (element)
        {
            case TextBlock tb:
                TextRenderer.RenderTextBlock(gfx, node, tb);
                break;

            case RichText rt:
                TextRenderer.RenderRichText(gfx, node, rt);
                break;

            case ImageBox img:
                ImageRenderer.Render(gfx, node, img);
                break;

            case SvgBox svg:
                SvgRenderer.Render(gfx, node, svg);
                break;

            case Divider div:
                DividerRenderer.Render(gfx, node, div);
                break;

            case Table tbl:
                TableRenderer.Render(gfx, node, tbl);
                break;

            // Containers (Row, Column, Box, etc.): render children recursively.
            default:
                foreach (var child in node.Children)
                {
                    RenderNode(gfx, child);
                }
                break;
        }
    }

    /// <summary>
    /// Paginates a layout group's children across multiple PDF pages. Walks the root Column's
    /// children, tracking the vertical cursor position. When an element overflows the current
    /// page, it is moved to the next page. Tables are split across pages with header repetition.
    /// </summary>
    /// <param name="pdfDoc">The PDF document to add pages to.</param>
    /// <param name="document">The PdfFlex document for page size and margin info.</param>
    /// <param name="rootNode">The root Column layout node with unlimited-height layout.</param>
    /// <param name="pageWidth">The page width in points.</param>
    /// <param name="pageHeight">The page height in points.</param>
    /// <param name="contentWidth">The content area width (page minus horizontal margins).</param>
    /// <param name="contentHeight">The content area height (page minus vertical margins).</param>
    private static void PaginateGroup(
        PdfDocument pdfDoc,
        Document document,
        LayoutNode rootNode,
        double pageWidth,
        double pageHeight,
        double contentWidth,
        double contentHeight)
    {
        double marginLeft = document.Margins.Left;
        double marginTop = document.Margins.Top;
        double pageBottom = marginTop + contentHeight;

        // Collect page assignments: each entry is a list of render actions for that page.
        var pages = new List<List<Action<XGraphics>>>();
        pages.Add(new List<Action<XGraphics>>());

        // cursorY tracks the current Y position on the current page (in page coordinates).
        double cursorY = marginTop;
        int currentPageIndex = 0;

        for (int i = 0; i < rootNode.Children.Count; i++)
        {
            var childNode = rootNode.Children[i];
            var childElement = childNode.Source;
            double childHeight = childNode.Height;

            // ── Table: paginate with row-level splitting ─────────────
            if (childElement is Table table)
            {
                PaginateTable(table, childNode, marginLeft, contentWidth,
                    ref cursorY, pageBottom, marginTop, pages, ref currentPageIndex);
                continue;
            }

            // ── Non-table element ────────────────────────────────────
            // If the element doesn't fit on the current page and we're not at the page top,
            // move to a new page.
            if (cursorY + childHeight > pageBottom + 0.5 && cursorY > marginTop + 0.5)
            {
                currentPageIndex++;
                if (currentPageIndex >= pages.Count)
                    pages.Add(new List<Action<XGraphics>>());
                cursorY = marginTop;
            }

            // Capture the Y offset for this element on this page.
            double targetY = cursorY;
            double sourceY = childNode.Y;
            double yOffset = targetY - sourceY;
            var capturedNode = childNode;

            pages[currentPageIndex].Add(gfx =>
            {
                var state = gfx.Save();
                gfx.TranslateTransform(0, yOffset);
                RenderNode(gfx, capturedNode);
                gfx.Restore(state);
            });

            cursorY += childHeight;

            // Account for gap between children in the Column layout.
            if (i < rootNode.Children.Count - 1 && rootNode.Source is Column col)
            {
                cursorY += col.Gap;
            }
        }

        // Create actual PDF pages and execute the render actions.
        foreach (var pageActions in pages)
        {
            var pdfPage = pdfDoc.AddPage();
            SetPageSize(pdfPage, pageWidth, pageHeight);

            using var gfx = XGraphics.FromPdfPage(pdfPage);

            foreach (var action in pageActions)
            {
                action(gfx);
            }
        }
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
        List<List<Action<XGraphics>>> pages,
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
                    pages.Add(new List<Action<XGraphics>>());
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

            pages[currentPageIndex].Add(gfx =>
            {
                TableRenderer.RenderSegment(gfx, table, capturedX, capturedY, capturedWidth,
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
                    pages.Add(new List<Action<XGraphics>>());
                cursorY = marginTop;
            }
        }

        // Handle tables with zero data rows (header-only).
        if (totalDataRows == 0)
        {
            double segmentY = cursorY;
            bool includeFooter = table.FooterRow != null;

            pages[currentPageIndex].Add(gfx =>
            {
                TableRenderer.RenderSegment(gfx, table, x, segmentY, width,
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
    /// Sets the page dimensions on a PdfSharp <see cref="PdfPage"/>.
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
        WatermarkRenderer.Render(
            gfx,
            document.Watermark,
            document.PageSize.Width,
            document.PageSize.Height);
    }

    #endregion Private Methods
}
