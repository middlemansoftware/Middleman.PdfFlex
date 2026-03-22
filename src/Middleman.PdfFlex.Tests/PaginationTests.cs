// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies general document pagination: elements that overflow a page, explicit
/// page breaks, consecutive page breaks, and page dimension consistency.
/// </summary>
public class PaginationTests
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

    #region Page Overflow

    [Fact]
    public void Element_ExceedsPageHeight_MovesToNextPage()
    {
        // A Box(height=700pt) in a Document(Letter, margins=50) where content height = 692pt.
        // The box exceeds the content height, so it should be placed on the page (possibly
        // overflowing) or moved to a new page. Either way, we need at least 1 page.
        // With a smaller leading element + the tall box, we get 2 pages.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new Box(style: new Style { Height = Length.Pt(100) }));
        doc.Add(new Box(style: new Style { Height = Length.Pt(700) }));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    #endregion Page Overflow

    #region Page Breaks

    [Fact]
    public void PageBreak_ForcesNewPage()
    {
        // Content + PageBreak + Content should produce 2 pages.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page 1 content", new FontSpec("Arial", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 2 content", new FontSpec("Arial", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    [Fact]
    public void PageBreak_ConsecutivePageBreaks_CreateBlankPages()
    {
        // Content + PageBreak + PageBreak + Content should produce 3 pages.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page 1", new FontSpec("Arial", 12)));
        doc.Add(new PageBreak());
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 3", new FontSpec("Arial", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(3, pageCount);
    }

    [Fact]
    public void MultiplePages_AllPagesHaveCorrectDimensions()
    {
        // A multi-page document should have all pages at the same size.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page 1", new FontSpec("Arial", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 2", new FontSpec("Arial", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 3", new FontSpec("Arial", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);

        using var stream = new MemoryStream(bytes);
        using var pdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        Assert.Equal(3, pdfDoc.PageCount);
        for (int i = 0; i < pdfDoc.PageCount; i++)
        {
            var page = pdfDoc.Pages[i];
            Assert.Equal(612, page.Width.Point, 0.5);
            Assert.Equal(792, page.Height.Point, 0.5);
        }
    }

    #endregion Page Breaks
}
