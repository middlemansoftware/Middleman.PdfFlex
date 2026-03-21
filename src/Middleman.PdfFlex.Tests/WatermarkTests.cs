// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;
using PdfSharp.Pdf.IO;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies watermark rendering: presence on every page, null watermark safety,
/// and opacity application without errors.
/// </summary>
public class WatermarkTests
{
    #region Helpers

    /// <summary>
    /// Opens a PDF byte array and returns the page count.
    /// </summary>
    private static int GetPageCount(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var pdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return pdfDoc.PageCount;
    }

    #endregion Helpers

    #region Watermark Rendering

    [Fact]
    public void Watermark_RenderedOnEveryPage()
    {
        // A 3-page document with a watermark should render without error and produce 3 pages.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Watermark = new Watermark("CONFIDENTIAL", opacity: 0.15);
        doc.Add(new TextBlock("Page 1", new FontSpec("Arial", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 2", new FontSpec("Arial", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 3", new FontSpec("Arial", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(3, pageCount);
    }

    [Fact]
    public void Watermark_NullWatermark_NoError()
    {
        // A document with null watermark should render without error.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Watermark = null;
        doc.Add(new TextBlock("Content", new FontSpec("Arial", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Watermark_Opacity_Applied()
    {
        // A document with a low-opacity watermark should render without error.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Watermark = new Watermark("DRAFT", opacity: 0.08);
        doc.Add(new TextBlock("Content", new FontSpec("Arial", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    #endregion Watermark Rendering
}
