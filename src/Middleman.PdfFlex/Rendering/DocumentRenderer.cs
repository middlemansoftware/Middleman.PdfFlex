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
            var wrapper = new Column(group);
            var rootNode = LayoutEngine.Calculate(wrapper, contentWidth, contentHeight);

            var pdfPage = pdfDoc.AddPage();
            SetPageSize(pdfPage, pageWidth, pageHeight);

            using var gfx = XGraphics.FromPdfPage(pdfPage);

            // Offset the root node by the margins so content renders in the content area.
            rootNode.X = document.Margins.Left;
            rootNode.Y = document.Margins.Top;

            RenderNode(gfx, rootNode);
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
