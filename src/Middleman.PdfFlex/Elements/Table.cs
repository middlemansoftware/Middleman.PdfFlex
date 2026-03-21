// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A first-class table element with column definitions, header repetition,
/// footer row, alternating row styles, and pagination support.
/// </summary>
public class Table : Element
{
    #region Public Properties

    /// <summary>Gets the column definitions for this table.</summary>
    public IReadOnlyList<TableColumn> Columns { get; }

    /// <summary>Gets the data rows. Each row is an array of cell values matching the column order.</summary>
    public IReadOnlyList<object[]> Rows { get; }

    /// <summary>Gets the optional footer row values.</summary>
    public object[]? FooterRow { get; }

    // ── Styles ──────────────────────────────────────────────────

    /// <summary>Gets the border applied to the table and its cells.</summary>
    public Border? Border { get; }

    /// <summary>Gets the style applied to header cells.</summary>
    public Style? HeaderStyle { get; }

    /// <summary>Gets the style applied to data cells.</summary>
    public Style? CellStyle { get; }

    /// <summary>Gets the style applied to data rows.</summary>
    public Style? RowStyle { get; }

    /// <summary>Gets the style applied to alternating (even) data rows.</summary>
    public Style? AlternateRowStyle { get; }

    /// <summary>Gets the style applied to the footer row.</summary>
    public Style? FooterStyle { get; }

    // ── Pagination ──────────────────────────────────────────────

    /// <summary>Gets the text appended to the header when the table spans multiple pages. Null disables continuation text.</summary>
    public string? ContinuationText { get; }

    /// <summary>Gets the minimum number of data rows required before a page break is inserted.</summary>
    public int MinRowsBeforeBreak { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a table element with the specified columns, rows, and styling options.</summary>
    /// <param name="columns">The column definitions.</param>
    /// <param name="rows">The data rows.</param>
    /// <param name="border">Optional border for the table and cells.</param>
    /// <param name="headerStyle">Optional style for header cells.</param>
    /// <param name="cellStyle">Optional style for data cells.</param>
    /// <param name="rowStyle">Optional style for data rows.</param>
    /// <param name="alternateRowStyle">Optional style for alternating (even) data rows.</param>
    /// <param name="footerRow">Optional footer row values.</param>
    /// <param name="footerStyle">Optional style for the footer row.</param>
    /// <param name="continuationText">Text appended to headers on continuation pages. Defaults to "(continued)".</param>
    /// <param name="minRowsBeforeBreak">Minimum data rows before allowing a page break. Defaults to 2.</param>
    /// <param name="style">Optional style to apply to the table element.</param>
    public Table(
        IEnumerable<TableColumn> columns,
        IEnumerable<object[]> rows,
        Border? border = null,
        Style? headerStyle = null,
        Style? cellStyle = null,
        Style? rowStyle = null,
        Style? alternateRowStyle = null,
        object[]? footerRow = null,
        Style? footerStyle = null,
        string? continuationText = "(continued)",
        int minRowsBeforeBreak = 2,
        Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        Columns = columns.ToList();
        Rows = rows.ToList();
        Border = border;
        HeaderStyle = headerStyle;
        CellStyle = cellStyle;
        RowStyle = rowStyle;
        AlternateRowStyle = alternateRowStyle;
        FooterRow = footerRow;
        FooterStyle = footerStyle;
        ContinuationText = continuationText;
        MinRowsBeforeBreak = minRowsBeforeBreak;
        Style = style;
    }

    #endregion Constructors
}
