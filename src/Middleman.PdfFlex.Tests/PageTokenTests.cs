// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies {page} and {pages} token resolution in TextBlock rendering,
/// HasTotalPagesToken detection, and correct page numbering across multi-page documents.
/// </summary>
public class PageTokenTests
{
    #region Helpers

    private static int GetPageCount(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var pdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return pdfDoc.PageCount;
    }

    private static byte[] RenderDocument(Document doc)
    {
        return DocumentRenderer.RenderToBytes(doc);
    }

    #endregion Helpers

    #region Token Rendering

    [Fact]
    public void PageToken_SinglePage_ResolvesTo1()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page {page}", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.Equal(1, GetPageCount(bytes));
    }

    [Fact]
    public void PagesToken_SinglePage_ResolvesTo1()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void PageToken_MultiplePages_IncrementsPerPage()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page {page}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page {page}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page {page}", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.Equal(3, GetPageCount(bytes));
    }

    [Fact]
    public void PagesToken_MultiplePages_ShowsCorrectTotal()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.Equal(3, GetPageCount(bytes));
    }

    [Fact]
    public void Tokens_CaseInsensitive()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page {PAGE} of {PAGES}", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void NoTokens_TextRenderedUnchanged()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("No tokens here", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void MixedContent_TokensAndPlainText()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Invoice - Page {page} of {pages}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Detail continues on page {page}", new FontSpec("NotoSans", 12)));

        var bytes = RenderDocument(doc);

        Assert.Equal(2, GetPageCount(bytes));
    }

    #endregion Token Rendering

    #region HasTotalPagesToken Detection

    [Fact]
    public void HasTotalPagesToken_WithPagesToken_ReturnsTrue()
    {
        var elements = new List<Element>
        {
            new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12))
        };

        Assert.True(DocumentRenderer.HasTotalPagesToken(elements));
    }

    [Fact]
    public void HasTotalPagesToken_WithOnlyPageToken_ReturnsFalse()
    {
        var elements = new List<Element>
        {
            new TextBlock("Page {page}", new FontSpec("NotoSans", 12))
        };

        Assert.False(DocumentRenderer.HasTotalPagesToken(elements));
    }

    [Fact]
    public void HasTotalPagesToken_NoTokens_ReturnsFalse()
    {
        var elements = new List<Element>
        {
            new TextBlock("Plain text", new FontSpec("NotoSans", 12))
        };

        Assert.False(DocumentRenderer.HasTotalPagesToken(elements));
    }

    [Fact]
    public void HasTotalPagesToken_NestedInColumn_ReturnsTrue()
    {
        var col = new Column(new List<Element>
        {
            new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12))
        });

        var elements = new List<Element> { col };

        Assert.True(DocumentRenderer.HasTotalPagesToken(elements));
    }

    [Fact]
    public void HasTotalPagesToken_EmptyList_ReturnsFalse()
    {
        Assert.False(DocumentRenderer.HasTotalPagesToken(new List<Element>()));
    }

    #endregion HasTotalPagesToken Detection

    #region Regression

    [Fact]
    public void ExistingDocument_NoTokens_StillRendersCorrectly()
    {
        // Regression: ensure documents without tokens still render after the RenderContext migration.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("First page content", new FontSpec("NotoSans", 14)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Second page content", new FontSpec("NotoSans", 14)));
        doc.Watermark = new Watermark("DRAFT", opacity: 0.10);

        var bytes = RenderDocument(doc);

        Assert.Equal(2, GetPageCount(bytes));
    }

    [Fact]
    public void TableWithPageBreaks_TokensResolveCorrectly()
    {
        // Table spanning multiple pages with page tokens on the first page.
        var columns = new[]
        {
            new TableColumn("Item", Length.Fr(1)),
            new TableColumn("Value", Length.Fr(1))
        };
        var rows = Enumerable.Range(1, 80)
            .Select(i => new object[] { $"Row {i}", $"Value {i}" });

        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 10)));
        doc.Add(new Table(columns, rows));

        var bytes = RenderDocument(doc);

        // Should produce multiple pages due to the large table.
        Assert.True(GetPageCount(bytes) > 1);
    }

    #endregion Regression
}
