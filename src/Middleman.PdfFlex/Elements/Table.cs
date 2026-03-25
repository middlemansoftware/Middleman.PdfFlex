// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A first-class table element with column definitions, header repetition,
/// footer row, alternating row styles, per-row/per-cell style overrides,
/// and pagination support.
/// </summary>
public class Table : Element
{
    #region Public Properties

    /// <summary>Gets the column definitions for this table.</summary>
    public IReadOnlyList<TableColumn> Columns { get; }

    /// <summary>
    /// Gets the data rows. Each entry is either a <see cref="TableRow"/> (with optional
    /// per-row/per-cell styles) or an <c>object[]</c> (legacy format, no style overrides).
    /// </summary>
    public IReadOnlyList<object> Rows { get; }

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

    /// <summary>Creates a table element with legacy <c>object[]</c> rows.</summary>
    /// <param name="columns">The column definitions.</param>
    /// <param name="rows">The data rows as object arrays.</param>
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
        : this(columns, Guard(rows).Cast<object>(), border, headerStyle, cellStyle,
            rowStyle, alternateRowStyle, footerRow, footerStyle,
            continuationText, minRowsBeforeBreak, style)
    {
    }

    /// <summary>Creates a table element with <see cref="TableRow"/> rows supporting per-row and per-cell styles.</summary>
    /// <param name="columns">The column definitions.</param>
    /// <param name="rows">The data rows with optional style overrides.</param>
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
        IEnumerable<TableRow> rows,
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
        : this(columns, Guard(rows).Cast<object>(), border, headerStyle, cellStyle,
            rowStyle, alternateRowStyle, footerRow, footerStyle,
            continuationText, minRowsBeforeBreak, style)
    {
    }

    /// <summary>Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is null; returns it otherwise.</summary>
    private static T Guard<T>(T value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? name = null)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        return value;
    }

    /// <summary>Internal constructor that stores rows as a heterogeneous list.</summary>
    private Table(
        IEnumerable<TableColumn> columns,
        IEnumerable<object> rows,
        Border? border,
        Style? headerStyle,
        Style? cellStyle,
        Style? rowStyle,
        Style? alternateRowStyle,
        object[]? footerRow,
        Style? footerStyle,
        string? continuationText,
        int minRowsBeforeBreak,
        Style? style)
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
