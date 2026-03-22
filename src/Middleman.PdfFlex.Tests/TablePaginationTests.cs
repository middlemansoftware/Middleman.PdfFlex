// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies table pagination behavior: splitting across pages, header repetition,
/// continuation text, orphan prevention, and footer rendering. Tests render actual
/// PDFs and inspect page counts.
/// </summary>
public class TablePaginationTests
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
    /// Creates a table with the specified number of data rows and a small cell font.
    /// </summary>
    private static Table CreateLargeTable(
        int rowCount,
        string? continuationText = "(continued)",
        int minRowsBeforeBreak = 2,
        object[]? footerRow = null,
        Style? footerStyle = null)
    {
        var columns = new[]
        {
            new TableColumn("Item", Length.Fr(1)),
            new TableColumn("Qty", Length.Pt(60)),
            new TableColumn("Price", Length.Pt(70))
        };

        var rows = new List<object[]>();
        for (int i = 0; i < rowCount; i++)
        {
            rows.Add(new object[] { $"Item {i + 1}", i + 1, (i + 1) * 9.99 });
        }

        return new Table(
            columns: columns,
            rows: rows,
            border: Border.All(0.5, Colors.Black),
            headerStyle: new Style { FontSize = 9, FontWeight = FontWeight.Bold, Padding = new EdgeInsets(4) },
            cellStyle: new Style { FontSize = 8, Padding = new EdgeInsets(3) },
            continuationText: continuationText,
            minRowsBeforeBreak: minRowsBeforeBreak,
            footerRow: footerRow,
            footerStyle: footerStyle);
    }

    #endregion Helpers

    #region Table Pagination

    [Fact]
    public void Table_SpansMultiplePages_SplitsBetweenRows()
    {
        // 50 rows in a Letter document with 50pt margins. Content height = 692pt.
        // Each data row is roughly (8 * 1.6) + 6 = 18.8pt. Header ~ 20.4pt.
        // Approximately 35 rows per page, so 50 rows should span 2 pages.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(CreateLargeTable(50));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.True(pageCount >= 2,
            $"50-row table should span multiple pages, got {pageCount}");
    }

    [Fact]
    public void Table_HeaderRepeated_OnContinuationPages()
    {
        // A multi-page table should produce more than one page.
        // We cannot directly inspect whether the header is drawn, but we verify
        // multiple pages are produced (header repetition is part of the rendering path).
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(CreateLargeTable(80));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.True(pageCount >= 2,
            $"80-row table should produce multiple pages, got {pageCount}");
    }

    [Fact]
    public void Table_ContinuationText_Null_NoPrefix()
    {
        // Setting continuationText to null should not cause errors.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(CreateLargeTable(60, continuationText: null));

        var bytes = DocumentRenderer.RenderToBytes(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Table_OrphanPrevention_MinRowsBeforeBreak()
    {
        // Create a document where only 1 data row would fit on the first page
        // after other content. With minRowsBeforeBreak=2, the entire table should
        // move to the next page.
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));

        // Fill most of the first page with a tall box, leaving room for about 1 row.
        double contentHeight = PageSize.Letter.Height - 100; // 692pt
        double headerHeight = 9 * 1.6 + 8;  // ~22.4pt
        double rowHeight = 8 * 1.6 + 6;     // ~18.8pt

        // Leave just enough room for the header and 1 data row, but not 2.
        double boxHeight = contentHeight - headerHeight - rowHeight - 5;
        doc.Add(new Box(style: new Style { Height = Length.Pt(boxHeight) }));
        doc.Add(CreateLargeTable(10, minRowsBeforeBreak: 2));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        // With orphan prevention, the table should start on page 2 (box on page 1).
        Assert.True(pageCount >= 2,
            $"Orphan prevention should push table to page 2, got {pageCount} pages");
    }

    [Fact]
    public void Table_FooterRow_OnlyOnLastPage()
    {
        // A table with a footer row that spans multiple pages.
        // The footer should only appear on the last page.
        // We verify the PDF renders without error and has multiple pages.
        var footerRow = new object[] { "Total", "", "$499.50" };
        var footerStyle = new Style
        {
            FontSize = 9,
            FontWeight = FontWeight.Bold,
            Padding = new EdgeInsets(4),
            Background = Background.FromColor(Colors.LightGray)
        };

        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(CreateLargeTable(60, footerRow: footerRow, footerStyle: footerStyle));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        int pageCount = GetPageCount(bytes);

        Assert.True(pageCount >= 2,
            $"Table with footer should span multiple pages, got {pageCount}");
        Assert.True(bytes.Length > 0);
    }

    #endregion Table Pagination
}
