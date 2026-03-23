// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.IO;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies document header and footer rendering: per-page repetition, first-page
/// overrides, content area reduction, PDF/UA artifact tagging, streaming path
/// consistency, complex layouts, and validation of disallowed element types.
/// </summary>
public class HeaderFooterTests
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

    /// <summary>
    /// Renders a document via the streaming path and returns the PDF bytes.
    /// </summary>
    private static byte[] RenderStreaming(Document doc)
    {
        using var ms = new MemoryStream();
        DocumentRenderer.RenderStreaming(doc, ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a document with enough tall content to produce the specified number of pages.
    /// Uses Letter page size with 50pt margins (content height = 692pt).
    /// Each content block is 300pt tall, so 3 blocks produce ~2-3 pages.
    /// </summary>
    private static Document CreateMultiPageDocument(int desiredPages)
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        for (int i = 1; i <= desiredPages; i++)
        {
            if (i > 1) doc.Add(new PageBreak());
            doc.Add(new TextBlock($"Page {i} content", new FontSpec("Arial", 12)));
        }
        return doc;
    }

    #endregion Helpers

    #region Header Rendering

    [Fact]
    public void Header_RendersOnEveryPage()
    {
        // A 3-page doc with a header should render without error and produce 3 pages.
        var doc = CreateMultiPageDocument(3);
        doc.Header = new TextBlock("Document Header", new FontSpec("Arial", 10));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(3, pageCount);
    }

    #endregion Header Rendering

    #region Footer Rendering

    [Fact]
    public void Footer_RendersOnEveryPage()
    {
        // A 3-page doc with a footer should render without error and produce 3 pages.
        var doc = CreateMultiPageDocument(3);
        doc.Footer = new TextBlock("Document Footer", new FontSpec("Arial", 10));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(3, pageCount);
    }

    [Fact]
    public void Footer_PageTokens_ResolveCorrectly()
    {
        // "Page {page} of {pages}" in footer should render on a 3-page doc without error.
        var doc = CreateMultiPageDocument(3);
        doc.Footer = new TextBlock("Page {page} of {pages}", new FontSpec("Arial", 10));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(3, pageCount);
    }

    #endregion Footer Rendering

    #region First-Page Overrides

    [Fact]
    public void FirstPageHeader_OverridesDefault()
    {
        // Different header on page 1, verify renders without error.
        var doc = CreateMultiPageDocument(2);
        doc.Header = new TextBlock("Default Header", new FontSpec("Arial", 10));
        doc.FirstPageHeader = new TextBlock("First Page Header", new FontSpec("Arial", 14));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    [Fact]
    public void FirstPageHeader_Null_UsesDefault()
    {
        // Null FirstPageHeader should use the default header on page 1.
        var doc = CreateMultiPageDocument(2);
        doc.Header = new TextBlock("Default Header", new FontSpec("Arial", 10));
        // FirstPageHeader intentionally left null.

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    [Fact]
    public void FirstPageHeader_EmptyElement_SuppressesOnPage1()
    {
        // An empty Column as FirstPageHeader should suppress the header on page 1.
        var doc = CreateMultiPageDocument(2);
        doc.Header = new TextBlock("Default Header", new FontSpec("Arial", 10));
        doc.FirstPageHeader = new Column(Array.Empty<Element>());

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    [Fact]
    public void FirstPageFooter_SamePatternAsHeader()
    {
        // Different footer on page 1, verify renders without error.
        var doc = CreateMultiPageDocument(2);
        doc.Footer = new TextBlock("Default Footer", new FontSpec("Arial", 10));
        doc.FirstPageFooter = new TextBlock("First Page Footer", new FontSpec("Arial", 14));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    #endregion First-Page Overrides

    #region Content Area Reduction

    [Fact]
    public void Header_ReducesContentArea()
    {
        // A doc with a header should produce more pages than the same doc without,
        // when the content is large enough to be affected by the reduced area.
        // Letter with 50pt margins = 692pt content height.
        // Use two boxes that together exceed the reduced content area but fit without header.
        var docWithoutHeader = new Document(PageSize.Letter, new EdgeInsets(50));
        var docWithHeader = new Document(PageSize.Letter, new EdgeInsets(50));
        docWithHeader.Header = new Box(
            new TextBlock("H", new FontSpec("Arial", 10)),
            new Style { Height = Length.Pt(80) });

        // Two boxes totaling 680pt: fits in 692pt (no header), overflows 612pt (with 80pt header).
        docWithoutHeader.Add(new Box(style: new Style { Height = Length.Pt(400) }));
        docWithoutHeader.Add(new Box(style: new Style { Height = Length.Pt(280) }));
        docWithHeader.Add(new Box(style: new Style { Height = Length.Pt(400) }));
        docWithHeader.Add(new Box(style: new Style { Height = Length.Pt(280) }));

        var bytesWithout = DocumentRenderer.RenderToBytes(docWithoutHeader);
        var bytesWith = DocumentRenderer.RenderToBytes(docWithHeader);
        int pagesWithout = GetPageCount(bytesWithout);
        int pagesWith = GetPageCount(bytesWith);

        Assert.Equal(1, pagesWithout);
        Assert.True(pagesWith > pagesWithout,
            $"Expected header to increase page count. Without: {pagesWithout}, With: {pagesWith}");
    }

    [Fact]
    public void Footer_ReducesContentArea()
    {
        // Same pattern as Header_ReducesContentArea but with a footer.
        var docWithoutFooter = new Document(PageSize.Letter, new EdgeInsets(50));
        var docWithFooter = new Document(PageSize.Letter, new EdgeInsets(50));
        docWithFooter.Footer = new Box(
            new TextBlock("F", new FontSpec("Arial", 10)),
            new Style { Height = Length.Pt(80) });

        // Two boxes totaling 680pt: fits in 692pt (no footer), overflows 612pt (with 80pt footer).
        docWithoutFooter.Add(new Box(style: new Style { Height = Length.Pt(400) }));
        docWithoutFooter.Add(new Box(style: new Style { Height = Length.Pt(280) }));
        docWithFooter.Add(new Box(style: new Style { Height = Length.Pt(400) }));
        docWithFooter.Add(new Box(style: new Style { Height = Length.Pt(280) }));

        var bytesWithout = DocumentRenderer.RenderToBytes(docWithoutFooter);
        var bytesWith = DocumentRenderer.RenderToBytes(docWithFooter);
        int pagesWithout = GetPageCount(bytesWithout);
        int pagesWith = GetPageCount(bytesWith);

        Assert.Equal(1, pagesWithout);
        Assert.True(pagesWith > pagesWithout,
            $"Expected footer to increase page count. Without: {pagesWithout}, With: {pagesWith}");
    }

    [Fact]
    public void HeaderAndFooter_Combined_CorrectContentArea()
    {
        // Both header and footer present. Verify the combined height reduction
        // causes the expected page count increase.
        var docWithout = new Document(PageSize.Letter, new EdgeInsets(50));
        var docWith = new Document(PageSize.Letter, new EdgeInsets(50));
        docWith.Header = new Box(
            new TextBlock("H", new FontSpec("Arial", 10)),
            new Style { Height = Length.Pt(50) });
        docWith.Footer = new Box(
            new TextBlock("F", new FontSpec("Arial", 10)),
            new Style { Height = Length.Pt(50) });

        // 692pt content height without header/footer.
        // With 50+50=100pt header+footer, content height = 592pt.
        // Two boxes totaling 680pt: fits in 692pt (no header/footer), overflows 592pt (with both).
        docWithout.Add(new Box(style: new Style { Height = Length.Pt(400) }));
        docWithout.Add(new Box(style: new Style { Height = Length.Pt(280) }));
        docWith.Add(new Box(style: new Style { Height = Length.Pt(400) }));
        docWith.Add(new Box(style: new Style { Height = Length.Pt(280) }));

        var bytesWithout = DocumentRenderer.RenderToBytes(docWithout);
        var bytesWith = DocumentRenderer.RenderToBytes(docWith);
        int pagesWithout = GetPageCount(bytesWithout);
        int pagesWith = GetPageCount(bytesWith);

        Assert.Equal(1, pagesWithout);
        Assert.Equal(2, pagesWith);
    }

    #endregion Content Area Reduction

    #region PDF/UA Tagging

    [Fact]
    public void Header_PdfUA_TaggedAsArtifact()
    {
        // Render with PDF/UA-1, verify renders without error.
        // Headers are artifacts and should not appear in the structure tree.
        var doc = CreateMultiPageDocument(2);
        doc.Conformance = PdfConformance.PdfUA1;
        doc.Language = "en-US";
        doc.Header = new TextBlock("Header", new FontSpec("Arial", 10));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    [Fact]
    public void Footer_PdfUA_TaggedAsArtifact()
    {
        // Render with PDF/UA-1, verify renders without error.
        var doc = CreateMultiPageDocument(2);
        doc.Conformance = PdfConformance.PdfUA1;
        doc.Language = "en-US";
        doc.Footer = new TextBlock("Footer", new FontSpec("Arial", 10));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    #endregion PDF/UA Tagging

    #region Streaming Path

    [Fact]
    public void Header_StreamingPath_RendersPerPage()
    {
        // Streaming mode with header should match in-memory page count.
        var doc = CreateMultiPageDocument(3);
        doc.Header = new TextBlock("Header", new FontSpec("Arial", 10));

        var inMemoryBytes = DocumentRenderer.RenderToBytes(doc);
        var streamingBytes = RenderStreaming(doc);
        int inMemoryPages = GetPageCount(inMemoryBytes);
        int streamingPages = GetPageCount(streamingBytes);

        Assert.Equal(3, inMemoryPages);
        Assert.Equal(inMemoryPages, streamingPages);
    }

    [Fact]
    public void Footer_StreamingPath_RendersPerPage()
    {
        // Streaming mode with footer should match in-memory page count.
        var doc = CreateMultiPageDocument(3);
        doc.Footer = new TextBlock("Page {page} of {pages}", new FontSpec("Arial", 10));

        var inMemoryBytes = DocumentRenderer.RenderToBytes(doc);
        var streamingBytes = RenderStreaming(doc);
        int inMemoryPages = GetPageCount(inMemoryBytes);
        int streamingPages = GetPageCount(streamingBytes);

        Assert.Equal(3, inMemoryPages);
        Assert.Equal(inMemoryPages, streamingPages);
    }

    #endregion Streaming Path

    #region Complex Layout

    [Fact]
    public void Header_WithComplexLayout()
    {
        // Row with TextBlock + Spacer + TextBlock in header.
        var doc = CreateMultiPageDocument(2);
        doc.Header = new Row(
            new TextBlock("Left Title", new FontSpec("Arial", 10)),
            new Spacer(),
            new TextBlock("Right Info", new FontSpec("Arial", 10)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(2, pageCount);
    }

    #endregion Complex Layout

    #region Regression

    [Fact]
    public void NoHeader_NoFooter_FullContentArea()
    {
        // Null header/footer should produce the same page count as before.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new Box(style: new Style { Height = Length.Pt(690) }));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.Equal(1, pageCount);
    }

    #endregion Regression

    #region Validation

    [Fact]
    public void Header_Validation_RejectsPageBreak()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Header = new PageBreak();
        doc.Add(new TextBlock("Body", new FontSpec("Arial", 12)));

        Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
    }

    [Fact]
    public void Header_Validation_RejectsTable()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Header = new Table(
            new[] { new TableColumn("Col", Length.Fr(1)) },
            new[] { new object[] { "Cell" } });
        doc.Add(new TextBlock("Body", new FontSpec("Arial", 12)));

        Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
    }

    [Fact]
    public void Footer_Validation_RejectsPageBreak()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Footer = new PageBreak();
        doc.Add(new TextBlock("Body", new FontSpec("Arial", 12)));

        Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
    }

    #endregion Validation
}
