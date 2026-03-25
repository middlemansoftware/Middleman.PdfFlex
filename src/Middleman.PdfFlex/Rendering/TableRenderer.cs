// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Pdf.Structure;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="Table"/> elements by drawing header rows, data rows,
/// and cell borders directly onto the graphics surface.
/// </summary>
internal static class TableRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders a table within the bounds of the specified layout node.
    /// Draws the header row, data rows with optional alternating styles,
    /// and cell borders when a border is specified.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node positioning the table.</param>
    /// <param name="table">The table element to render.</param>
    public static void Render(RenderContext ctx, LayoutNode node, Table table)
    {
        if (table.Columns.Count == 0)
            return;

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;

        double tableX = node.X;
        double tableY = node.Y;
        double tableWidth = node.Width;

        // Resolve column widths proportionally within the available table width.
        double[] columnWidths = ResolveColumnWidths(table, tableWidth);

        // Row heights incorporate vertical padding from styles for proper cell spacing.
        double headerRowHeight = GetHeaderRowHeight(table);
        double dataRowHeight = GetRowHeight(table);
        double headerCellPadding = ResolveCellPadding(table.HeaderStyle);
        double dataCellPadding = ResolveCellPadding(table.CellStyle);

        double cursorY = tableY;

        // Open table structure.
        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.Table);

        // ── Header Row ──────────────────────────────────────────────
        var headerFont = CreateFontFromStyle(table.HeaderStyle, fallbackWeight: FontWeight.Bold);
        var headerColor = table.HeaderStyle?.FontColor ?? Colors.Black;
        var headerBrush = new XSolidBrush(ColorConvert.ToXColor(headerColor));

        // Draw header background (Artifact).
        if (table.HeaderStyle?.Background != null)
        {
            if (sb != null) sb.BeginArtifact();
            var bgBrush = new XSolidBrush(ColorConvert.ToXColor(table.HeaderStyle.Background.Color));
            gfx.DrawRectangle(bgBrush, tableX, cursorY, tableWidth, headerRowHeight);
            if (sb != null) sb.End();
        }

        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableHeadRowGroup);
        DrawRow(gfx, table, columnWidths, tableX, cursorY, headerRowHeight, headerCellPadding,
            headerFont, headerBrush, isHeader: true, rowValues: null, sb: sb);
        if (sb != null) sb.End(); // THead

        cursorY += headerRowHeight;

        // ── Data Rows ───────────────────────────────────────────────
        var cellFont = CreateFontFromStyle(table.CellStyle);
        var cellColor = table.CellStyle?.FontColor ?? Colors.Black;
        var cellBrush = new XSolidBrush(ColorConvert.ToXColor(cellColor));

        int rowCount = 0;
        if (table.Rows.Count > 0 && sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableBodyRowGroup);

        for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            // Stop if we exceed the layout node bounds.
            if (cursorY + dataRowHeight > node.Y + node.Height + 0.5)
                break;

            var row = table.Rows[rowIndex];

            var bgStyle = ResolveRowBackground(row, rowIndex, table);

            if (bgStyle?.Background != null)
            {
                if (sb != null) sb.BeginArtifact();
                var bgBrush = new XSolidBrush(ColorConvert.ToXColor(bgStyle.Background.Color));
                gfx.DrawRectangle(bgBrush, tableX, cursorY, tableWidth, dataRowHeight);
                if (sb != null) sb.End();
            }

            DrawDataRow(gfx, table, columnWidths, tableX, cursorY, dataRowHeight,
                cellFont, cellBrush, dataCellPadding, row, sb);

            cursorY += dataRowHeight;
            rowCount++;
        }

        if (rowCount > 0 && sb != null) sb.End(); // TBody

        // ── Borders (Artifact) ──────────────────────────────────────
        if (table.Border != null)
        {
            if (sb != null) sb.BeginArtifact();
            DrawBorders(gfx, table.Border, table, columnWidths, tableX, tableY,
                tableWidth, cursorY - tableY, headerRowHeight, dataRowHeight);
            if (sb != null) sb.End();
        }

        if (sb != null) sb.End(); // Table
    }

    /// <summary>
    /// Renders a segment of a table (a subset of data rows) at the specified position.
    /// Used by the pagination system to split tables across multiple pages, with header
    /// repetition and optional continuation text.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="table">The table element to render.</param>
    /// <param name="x">The left X position of the table segment.</param>
    /// <param name="y">The top Y position of the table segment.</param>
    /// <param name="width">The available width for the table segment.</param>
    /// <param name="startRow">The zero-based index of the first data row to render (inclusive).</param>
    /// <param name="endRow">The zero-based index of the last data row to render (exclusive).</param>
    /// <param name="isContinuation">
    /// When <see langword="true"/>, the header is rendered with continuation text
    /// prepended to the first column header if <see cref="Table.ContinuationText"/> is set.
    /// </param>
    /// <param name="includeFooter">
    /// When <see langword="true"/>, the table's footer row is rendered after the last data row.
    /// </param>
    public static void RenderSegment(
        RenderContext ctx,
        Table table,
        double x,
        double y,
        double width,
        int startRow,
        int endRow,
        bool isContinuation,
        bool includeFooter = false)
    {
        if (table.Columns.Count == 0)
            return;

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;

        double[] columnWidths = ResolveColumnWidths(table, width);

        double headerRowHeight = GetHeaderRowHeight(table);
        double dataRowHeight = GetRowHeight(table);
        double headerCellPadding = ResolveCellPadding(table.HeaderStyle);
        double dataCellPadding = ResolveCellPadding(table.CellStyle);

        double cursorY = y;

        // Open table structure for this segment.
        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.Table);

        // ── Header Row ──────────────────────────────────────────────
        var headerFont = CreateFontFromStyle(table.HeaderStyle, fallbackWeight: FontWeight.Bold);
        var headerColor = table.HeaderStyle?.FontColor ?? Colors.Black;
        var headerBrush = new XSolidBrush(ColorConvert.ToXColor(headerColor));

        if (table.HeaderStyle?.Background != null)
        {
            if (sb != null) sb.BeginArtifact();
            var bgBrush = new XSolidBrush(ColorConvert.ToXColor(table.HeaderStyle.Background.Color));
            gfx.DrawRectangle(bgBrush, x, cursorY, width, headerRowHeight);
            if (sb != null) sb.End();
        }

        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableHeadRowGroup);
        DrawHeaderRow(gfx, table, columnWidths, x, cursorY, headerRowHeight, headerCellPadding,
            headerFont, headerBrush, isContinuation, sb);
        if (sb != null) sb.End(); // THead

        cursorY += headerRowHeight;

        // ── Data Rows ───────────────────────────────────────────────
        var cellFont = CreateFontFromStyle(table.CellStyle);
        var cellColor = table.CellStyle?.FontColor ?? Colors.Black;
        var cellBrush = new XSolidBrush(ColorConvert.ToXColor(cellColor));

        int rowCount = endRow - startRow;
        if (rowCount > 0 && sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableBodyRowGroup);

        for (int rowIndex = startRow; rowIndex < endRow && rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = table.Rows[rowIndex];

            var bgStyle = ResolveRowBackground(row, rowIndex, table);

            if (bgStyle?.Background != null)
            {
                if (sb != null) sb.BeginArtifact();
                var bgBrush = new XSolidBrush(ColorConvert.ToXColor(bgStyle.Background.Color));
                gfx.DrawRectangle(bgBrush, x, cursorY, width, dataRowHeight);
                if (sb != null) sb.End();
            }

            DrawDataRow(gfx, table, columnWidths, x, cursorY, dataRowHeight,
                cellFont, cellBrush, dataCellPadding, row, sb);

            cursorY += dataRowHeight;
        }

        if (rowCount > 0 && sb != null) sb.End(); // TBody

        // ── Footer Row ──────────────────────────────────────────────
        if (includeFooter && table.FooterRow != null)
        {
            double footerCellPadding = ResolveCellPadding(table.FooterStyle);
            double footerRowHeight = GetFooterRowHeight(table);

            var footerFont = CreateFontFromStyle(table.FooterStyle, fallbackWeight: FontWeight.Bold);
            var footerColor = table.FooterStyle?.FontColor ?? Colors.Black;
            var footerBrush = new XSolidBrush(ColorConvert.ToXColor(footerColor));

            if (table.FooterStyle?.Background != null)
            {
                if (sb != null) sb.BeginArtifact();
                var bgBrush = new XSolidBrush(ColorConvert.ToXColor(table.FooterStyle.Background.Color));
                gfx.DrawRectangle(bgBrush, x, cursorY, width, footerRowHeight);
                if (sb != null) sb.End();
            }

            if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableFooterRowGroup);
            DrawRow(gfx, table, columnWidths, x, cursorY, footerRowHeight, footerCellPadding,
                footerFont, footerBrush, isHeader: false, rowValues: table.FooterRow, sb: sb);
            if (sb != null) sb.End(); // TFoot

            cursorY += footerRowHeight;
        }

        // ── Borders (Artifact) ──────────────────────────────────────
        if (table.Border != null)
        {
            if (sb != null) sb.BeginArtifact();
            DrawBorders(gfx, table.Border, table, columnWidths, x, y,
                width, cursorY - y, headerRowHeight, dataRowHeight);
            if (sb != null) sb.End();
        }

        if (sb != null) sb.End(); // Table
    }

    /// <summary>
    /// Returns the computed data row height for a table, including vertical padding
    /// from the cell style.
    /// </summary>
    /// <param name="table">The table element.</param>
    /// <returns>The data row height in points.</returns>
    public static double GetRowHeight(Table table)
    {
        double fontSize = ResolveTableFontSize(table.CellStyle) ?? ResolveTableFontSize(table.HeaderStyle) ?? 10.0;
        double baseHeight = fontSize * 1.6;
        double verticalPadding = table.CellStyle?.Padding?.VerticalTotal ?? 0;
        return baseHeight + verticalPadding;
    }

    /// <summary>
    /// Returns the computed header row height for a table, including vertical padding
    /// from the header style.
    /// </summary>
    /// <param name="table">The table element.</param>
    /// <returns>The header row height in points.</returns>
    public static double GetHeaderRowHeight(Table table)
    {
        double fontSize = ResolveTableFontSize(table.HeaderStyle) ?? ResolveTableFontSize(table.CellStyle) ?? 10.0;
        double baseHeight = fontSize * 1.6;
        double verticalPadding = table.HeaderStyle?.Padding?.VerticalTotal ?? 0;
        return baseHeight + verticalPadding;
    }

    /// <summary>
    /// Returns the computed footer row height for a table, including vertical padding
    /// from the footer style. Falls back to data row height if no footer style is set.
    /// </summary>
    /// <param name="table">The table element.</param>
    /// <returns>The footer row height in points.</returns>
    public static double GetFooterRowHeight(Table table)
    {
        if (table.FooterStyle == null)
            return GetRowHeight(table);

        double fontSize = ResolveTableFontSize(table.FooterStyle) ?? ResolveTableFontSize(table.CellStyle) ?? 10.0;
        double baseHeight = fontSize * 1.6;
        double verticalPadding = table.FooterStyle.Padding?.VerticalTotal ?? 0;
        return baseHeight + verticalPadding;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Resolves the background style for a data row. Per-row <see cref="TableRow.RowStyle"/>
    /// overrides the table-level alternating row logic.
    /// </summary>
    private static Style? ResolveRowBackground(object row, int rowIndex, Table table)
    {
        if (row is TableRow tableRow && tableRow.RowStyle?.Background != null)
            return tableRow.RowStyle;

        bool isAlternate = rowIndex % 2 == 1;
        return isAlternate && table.AlternateRowStyle != null
            ? table.AlternateRowStyle
            : table.RowStyle;
    }

    /// <summary>
    /// Resolves the horizontal cell padding from a style's padding, falling back to
    /// a default of 4pt when no padding is specified.
    /// </summary>
    private static double ResolveCellPadding(Style? style)
    {
        return style?.Padding?.Left ?? 4.0;
    }

    /// <summary>
    /// Resolves the right-side cell padding from a style, falling back to the left padding
    /// value for backward compatibility with symmetric padding usage.
    /// </summary>
    private static double ResolveCellPaddingRight(Style? style)
    {
        return style?.Padding?.Right ?? ResolveCellPadding(style);
    }

    /// <summary>
    /// Resolves column widths from the table's column definitions. Absolute widths are
    /// used directly; fractional and other relative widths share the remaining space proportionally.
    /// </summary>
    private static double[] ResolveColumnWidths(Table table, double availableWidth)
    {
        double[] widths = new double[table.Columns.Count];
        double totalFixed = 0;
        double totalFr = 0;

        for (int i = 0; i < table.Columns.Count; i++)
        {
            var colWidth = table.Columns[i].Width;
            if (colWidth.IsAbsolute)
            {
                widths[i] = colWidth.ToPoints();
                totalFixed += widths[i];
            }
            else if (colWidth.Type == Length.Unit.Fr)
            {
                totalFr += colWidth.Value;
            }
            else if (colWidth.Type == Length.Unit.Percent)
            {
                widths[i] = availableWidth * colWidth.Value / 100.0;
                totalFixed += widths[i];
            }
            // Auto and other types will be treated as 1fr.
            else
            {
                totalFr += 1;
            }
        }

        // Distribute remaining space among fractional columns.
        double remaining = Math.Max(0, availableWidth - totalFixed);

        if (totalFr > 0)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var colWidth = table.Columns[i].Width;
                if (colWidth.Type == Length.Unit.Fr)
                {
                    widths[i] = remaining * (colWidth.Value / totalFr);
                }
                else if (!colWidth.IsAbsolute && colWidth.Type != Length.Unit.Percent)
                {
                    // Auto and unhandled types get equal share as 1fr.
                    widths[i] = remaining * (1.0 / totalFr);
                }
            }
        }

        return widths;
    }

    /// <summary>
    /// Draws a header row with optional continuation text prepended to the first column.
    /// </summary>
    private static void DrawHeaderRow(
        XGraphics gfx,
        Table table,
        double[] columnWidths,
        double rowX,
        double rowY,
        double rowHeight,
        double cellPadding,
        XFont font,
        XSolidBrush brush,
        bool isContinuation,
        StructureBuilder? sb = null)
    {
        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableRow);

        double cellX = rowX;

        for (int col = 0; col < table.Columns.Count; col++)
        {
            string cellText = table.Columns[col].Header;

            // Prepend continuation text to the first column header on continuation pages.
            if (isContinuation && col == 0 && table.ContinuationText != null)
            {
                cellText = table.ContinuationText + " " + cellText;
            }

            var align = table.Columns[col].Align;
            var format = MapTextAlign(align);

            var cellRect = new XRect(
                cellX + cellPadding,
                rowY,
                Math.Max(0, columnWidths[col] - (cellPadding * 2)),
                rowHeight);

            if (sb != null)
            {
                sb.BeginElement(PdfBlockLevelElementTag.TableHeaderCell);
                // Set Column scope on the TH structure element for screen reader navigation.
                sb.CurrentStructureElement.TableAttributes.Elements.SetName("/Scope", "/Column");
            }

            gfx.DrawString(cellText, font, brush, cellRect, format);

            if (sb != null) sb.End(); // TH

            cellX += columnWidths[col];
        }

        if (sb != null) sb.End(); // TR
    }

    /// <summary>
    /// Draws a single row of cells (either header or data).
    /// </summary>
    private static void DrawRow(
        XGraphics gfx,
        Table table,
        double[] columnWidths,
        double rowX,
        double rowY,
        double rowHeight,
        double cellPadding,
        XFont font,
        XSolidBrush brush,
        bool isHeader,
        object[]? rowValues,
        StructureBuilder? sb = null)
    {
        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableRow);

        double cellX = rowX;

        for (int col = 0; col < table.Columns.Count; col++)
        {
            string cellText = isHeader
                ? table.Columns[col].Header
                : (col < (rowValues?.Length ?? 0) ? rowValues![col]?.ToString() ?? string.Empty : string.Empty);

            var align = table.Columns[col].Align;
            var format = MapTextAlign(align);

            var cellRect = new XRect(
                cellX + cellPadding,
                rowY,
                Math.Max(0, columnWidths[col] - (cellPadding * 2)),
                rowHeight);

            var cellTag = isHeader
                ? PdfBlockLevelElementTag.TableHeaderCell
                : PdfBlockLevelElementTag.TableDataCell;
            if (sb != null) sb.BeginElement(cellTag);

            gfx.DrawString(cellText, font, brush, cellRect, format);

            if (sb != null) sb.End(); // TH or TD

            cellX += columnWidths[col];
        }

        if (sb != null) sb.End(); // TR
    }

    /// <summary>
    /// Draws a data row, handling both legacy <c>object[]</c> rows and typed
    /// <see cref="TableRow"/> rows with per-row and per-cell style overrides.
    /// </summary>
    private static void DrawDataRow(
        XGraphics gfx,
        Table table,
        double[] columnWidths,
        double rowX,
        double rowY,
        double rowHeight,
        XFont defaultFont,
        XSolidBrush defaultBrush,
        double defaultCellPadding,
        object row,
        StructureBuilder? sb)
    {
        if (row is object[] values)
        {
            // Legacy path — no style overrides, delegate to existing DrawRow.
            DrawRow(gfx, table, columnWidths, rowX, rowY, rowHeight, defaultCellPadding,
                defaultFont, defaultBrush, isHeader: false, rowValues: values, sb: sb);
            return;
        }

        if (row is not TableRow tableRow)
        {
            // Unknown row type — treat as single-cell legacy row.
            DrawRow(gfx, table, columnWidths, rowX, rowY, rowHeight, defaultCellPadding,
                defaultFont, defaultBrush, isHeader: false,
                rowValues: new[] { row }, sb: sb);
            return;
        }

        // Typed row — resolve row-level cell style cascade.
        var rowCellStyle = tableRow.CellStyle ?? table.CellStyle;

        // Build row-level font/brush/padding only when they differ from the table default.
        XFont rowFont = defaultFont;
        XSolidBrush rowBrush = defaultBrush;
        double rowPadding = defaultCellPadding;

        if (tableRow.CellStyle != null)
        {
            rowFont = CreateFontFromStyle(rowCellStyle);
            var rowColor = rowCellStyle?.FontColor ?? Colors.Black;
            rowBrush = new XSolidBrush(ColorConvert.ToXColor(rowColor));
            rowPadding = ResolveCellPadding(rowCellStyle);
        }

        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableRow);

        double cellX = rowX;

        for (int col = 0; col < table.Columns.Count; col++)
        {
            // Resolve cell content and style from the TableRow cells.
            string cellText;
            XFont cellFont;
            XSolidBrush cellBrush;
            double cellPaddingLeft;
            double cellPaddingRight;

            if (col < tableRow.Cells.Count)
            {
                var cell = tableRow.Cells[col];
                cellText = cell.Content?.ToString() ?? string.Empty;

                if (cell.Style != null)
                {
                    // Per-cell style overrides everything.
                    var effectiveStyle = cell.Style;
                    cellFont = CreateFontFromStyle(effectiveStyle);
                    var cellColor = effectiveStyle.FontColor ?? rowCellStyle?.FontColor ?? Colors.Black;
                    cellBrush = new XSolidBrush(ColorConvert.ToXColor(cellColor));
                    cellPaddingLeft = ResolveCellPadding(effectiveStyle);
                    cellPaddingRight = ResolveCellPaddingRight(effectiveStyle);
                }
                else
                {
                    // Fall through to row-level resolved font/brush/padding.
                    cellFont = rowFont;
                    cellBrush = rowBrush;
                    cellPaddingLeft = rowPadding;
                    cellPaddingRight = rowPadding;
                }
            }
            else
            {
                // No cell data for this column — empty cell with row defaults.
                cellText = string.Empty;
                cellFont = rowFont;
                cellBrush = rowBrush;
                cellPaddingLeft = rowPadding;
                cellPaddingRight = rowPadding;
            }

            var align = table.Columns[col].Align;
            var format = MapTextAlign(align);

            var cellRect = new XRect(
                cellX + cellPaddingLeft,
                rowY,
                Math.Max(0, columnWidths[col] - cellPaddingLeft - cellPaddingRight),
                rowHeight);

            if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.TableDataCell);

            gfx.DrawString(cellText, cellFont, cellBrush, cellRect, format);

            if (sb != null) sb.End(); // TD

            cellX += columnWidths[col];
        }

        if (sb != null) sb.End(); // TR
    }

    /// <summary>
    /// Draws table grid borders: outer border and internal cell dividers.
    /// </summary>
    private static void DrawBorders(
        XGraphics gfx,
        Border border,
        Table table,
        double[] columnWidths,
        double tableX,
        double tableY,
        double tableWidth,
        double tableHeight,
        double headerRowHeight,
        double dataRowHeight)
    {
        // Use the top border side's properties for all grid lines (simple approach).
        var side = border.Top;
        if (side.Width <= 0 || side.Style == BorderStyle.None)
            return;

        var pen = new XPen(ColorConvert.ToXColor(side.Color), side.Width);

        // Outer rectangle.
        gfx.DrawRectangle(pen, tableX, tableY, tableWidth, tableHeight);

        // Horizontal row dividers. The first divider separates header from data rows.
        int totalRows = 1 + table.Rows.Count; // header + data rows
        for (int row = 1; row < totalRows; row++)
        {
            double y = tableY + headerRowHeight + ((row - 1) * dataRowHeight);
            if (y < tableY + tableHeight)
            {
                gfx.DrawLine(pen, tableX, y, tableX + tableWidth, y);
            }
        }

        // Vertical column dividers.
        double x = tableX;
        for (int col = 0; col < columnWidths.Length - 1; col++)
        {
            x += columnWidths[col];
            gfx.DrawLine(pen, x, tableY, x, tableY + tableHeight);
        }
    }

    /// <summary>
    /// Creates an <see cref="XFont"/> from a <see cref="Style"/>, with optional fallback weight.
    /// </summary>
    private static XFont CreateFontFromStyle(Style? style, FontWeight fallbackWeight = FontWeight.Normal)
    {
        string family = style?.FontFamily ?? "Arial";
        double size = style?.FontSize ?? 10.0;
        var weight = style?.FontWeight ?? fallbackWeight;
        var fontStyle = style?.FontStyle ?? Styling.FontStyle.Normal;

        bool bold = weight >= FontWeight.Bold;
        bool italic = fontStyle is Styling.FontStyle.Italic or Styling.FontStyle.Oblique;

        var xStyle = (bold, italic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular
        };

        return new XFont(family, size, xStyle);
    }

    /// <summary>
    /// Resolves the font size from a style, returning null if not specified.
    /// </summary>
    private static double? ResolveTableFontSize(Style? style)
    {
        return style?.FontSize;
    }

    /// <summary>
    /// Maps a PdfFlex <see cref="TextAlign"/> to a PdfFlex <see cref="XStringFormat"/>
    /// with vertical centering for table cells.
    /// </summary>
    private static XStringFormat MapTextAlign(TextAlign align)
    {
        var format = new XStringFormat
        {
            LineAlignment = XLineAlignment.Center,
            Alignment = align switch
            {
                TextAlign.Center => XStringAlignment.Center,
                TextAlign.Right => XStringAlignment.Far,
                _ => XStringAlignment.Near
            }
        };
        return format;
    }

    #endregion Private Methods
}
