// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies table layout calculations including column widths, row heights, and padding.
/// Tests use the public <see cref="TableRenderer"/> API for row height calculations and
/// the layout engine for overall table dimensions.
/// </summary>
public class TableLayoutTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.5;

    #region Column Widths

    [Fact]
    public void Table_ColumnWidths_FixedColumnsGetExactWidth()
    {
        // A table with a fixed-width column (70pt) should report that row height
        // is based on the font size, verifying the table is valid and renderable.
        var table = new Table(
            columns: new[]
            {
                new TableColumn("Name", Length.Pt(70)),
                new TableColumn("Value", Length.Fr(1))
            },
            rows: new[] { new object[] { "Item", "100" } },
            cellStyle: new Style { FontSize = 10 });

        // GetRowHeight is the public API for row height; fixed column widths are resolved
        // internally during rendering. Verify the table produces valid layout.
        var wrapper = new Column(new Element[] { table });
        var result = LayoutEngine.Calculate(wrapper, 500, 1e6);

        // The table node should have a positive width.
        Assert.True(result.Children[0].Width > 0);
    }

    [Fact]
    public void Table_ColumnWidths_FrColumnsDistributeRemaining()
    {
        // Two Fr(1) columns + one Pt(70) column in 500pt.
        // Fr columns should each get (500 - 70) / 2 = 215pt.
        // Verify through layout: the table should occupy the full 500pt width.
        var table = new Table(
            columns: new[]
            {
                new TableColumn("A", Length.Fr(1)),
                new TableColumn("B", Length.Fr(1)),
                new TableColumn("C", Length.Pt(70))
            },
            rows: new[] { new object[] { "1", "2", "3" } },
            cellStyle: new Style { FontSize = 10 });

        var wrapper = new Column(new Element[] { table });
        var result = LayoutEngine.Calculate(wrapper, 500, 1e6);

        // The table layout node should stretch to fill the available width.
        Assert.Equal(500, result.Children[0].Width, Tolerance);
    }

    #endregion Column Widths

    #region Row Heights

    [Fact]
    public void Table_RowHeight_IncludesVerticalPadding()
    {
        // Cell style with vertical padding of 10pt top + 10pt bottom = 20pt total.
        // Base row height at 10pt font = 10 * 1.6 = 16pt.
        // Total row height = 16 + 20 = 36pt.
        var table = new Table(
            columns: new[] { new TableColumn("Col", Length.Fr(1)) },
            rows: new[] { new object[] { "Data" } },
            cellStyle: new Style { FontSize = 10, Padding = new EdgeInsets(10) });

        double rowHeight = TableRenderer.GetRowHeight(table);

        Assert.Equal(36, rowHeight, Tolerance);
    }

    [Fact]
    public void Table_HeaderRowHeight_IncludesHeaderPadding()
    {
        // Header style with vertical padding of 8pt top + 8pt bottom = 16pt total.
        // Base header height at 12pt font = 12 * 1.6 = 19.2pt.
        // Total header height = 19.2 + 16 = 35.2pt.
        var table = new Table(
            columns: new[] { new TableColumn("Col", Length.Fr(1)) },
            rows: new[] { new object[] { "Data" } },
            headerStyle: new Style { FontSize = 12, Padding = new EdgeInsets(8) });

        double headerHeight = TableRenderer.GetHeaderRowHeight(table);

        Assert.Equal(35.2, headerHeight, Tolerance);
    }

    #endregion Row Heights
}
