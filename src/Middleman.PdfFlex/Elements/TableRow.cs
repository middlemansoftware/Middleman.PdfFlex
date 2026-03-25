// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Wraps a table data row with optional per-row style overrides for cell text
/// and row-level decoration (background, border). When styles are set they
/// take precedence over the table-level defaults.
/// </summary>
public class TableRow
{
    /// <summary>Gets the cells in this row.</summary>
    public IReadOnlyList<TableCell> Cells { get; }

    /// <summary>
    /// Gets the optional cell style applied to all cells in this row.
    /// Overrides <see cref="Table.CellStyle"/> but is itself overridden
    /// by individual <see cref="TableCell.Style"/> values.
    /// </summary>
    public Style? CellStyle { get; }

    /// <summary>
    /// Gets the optional row-level style controlling background and border.
    /// When set, overrides <see cref="Table.RowStyle"/> and
    /// <see cref="Table.AlternateRowStyle"/> for this row.
    /// </summary>
    public Style? RowStyle { get; }

    /// <summary>Creates a table row from typed cells with optional style overrides.</summary>
    /// <param name="cells">The cells in the row.</param>
    /// <param name="cellStyle">Optional cell style override for all cells in this row.</param>
    /// <param name="rowStyle">Optional row-level style for background/border.</param>
    public TableRow(IEnumerable<TableCell> cells, Style? cellStyle = null, Style? rowStyle = null)
    {
        ArgumentNullException.ThrowIfNull(cells);
        Cells = cells.ToList();
        CellStyle = cellStyle;
        RowStyle = rowStyle;
    }

    /// <summary>Creates a table row from raw values (converted to <see cref="TableCell"/> via ToString).</summary>
    /// <param name="values">The cell values.</param>
    /// <param name="cellStyle">Optional cell style override for all cells in this row.</param>
    /// <param name="rowStyle">Optional row-level style for background/border.</param>
    public TableRow(IEnumerable<object> values, Style? cellStyle = null, Style? rowStyle = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        Cells = values.Select(v => v is TableCell tc ? tc : new TableCell(v)).ToList();
        CellStyle = cellStyle;
        RowStyle = rowStyle;
    }
}
