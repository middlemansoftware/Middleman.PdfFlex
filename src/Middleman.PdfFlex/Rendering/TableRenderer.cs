// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;
using PdfSharp.Drawing;

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
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="node">The layout node positioning the table.</param>
    /// <param name="table">The table element to render.</param>
    public static void Render(XGraphics gfx, LayoutNode node, Table table)
    {
        if (table.Columns.Count == 0)
            return;

        double tableX = node.X;
        double tableY = node.Y;
        double tableWidth = node.Width;

        // Resolve column widths proportionally within the available table width.
        double[] columnWidths = ResolveColumnWidths(table, tableWidth);

        // Determine row height from header style font size, falling back to cell style, then default.
        double fontSize = ResolveTableFontSize(table.HeaderStyle) ?? ResolveTableFontSize(table.CellStyle) ?? 10.0;
        double rowHeight = fontSize * 1.6;
        double cellPadding = 4.0;

        double cursorY = tableY;

        // ── Header Row ──────────────────────────────────────────────
        var headerFont = CreateFontFromStyle(table.HeaderStyle, fallbackWeight: FontWeight.Bold);
        var headerColor = table.HeaderStyle?.FontColor ?? Colors.Black;
        var headerBrush = new XSolidBrush(ColorConvert.ToXColor(headerColor));

        // Draw header background.
        if (table.HeaderStyle?.Background != null)
        {
            var bgBrush = new XSolidBrush(ColorConvert.ToXColor(table.HeaderStyle.Background.Color));
            gfx.DrawRectangle(bgBrush, tableX, cursorY, tableWidth, rowHeight);
        }

        DrawRow(gfx, table, columnWidths, tableX, cursorY, rowHeight, cellPadding,
            headerFont, headerBrush, isHeader: true, rowValues: null);

        cursorY += rowHeight;

        // ── Data Rows ───────────────────────────────────────────────
        var cellFont = CreateFontFromStyle(table.CellStyle);
        var cellColor = table.CellStyle?.FontColor ?? Colors.Black;
        var cellBrush = new XSolidBrush(ColorConvert.ToXColor(cellColor));

        for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            // Stop if we exceed the layout node bounds.
            if (cursorY + rowHeight > node.Y + node.Height + 0.5)
                break;

            bool isAlternate = rowIndex % 2 == 1;
            var rowStyle = isAlternate && table.AlternateRowStyle != null
                ? table.AlternateRowStyle
                : table.RowStyle;

            // Draw row background.
            var bgStyle = rowStyle ?? (isAlternate ? table.AlternateRowStyle : null);
            if (bgStyle?.Background != null)
            {
                var bgBrush = new XSolidBrush(ColorConvert.ToXColor(bgStyle.Background.Color));
                gfx.DrawRectangle(bgBrush, tableX, cursorY, tableWidth, rowHeight);
            }

            DrawRow(gfx, table, columnWidths, tableX, cursorY, rowHeight, cellPadding,
                cellFont, cellBrush, isHeader: false, rowValues: table.Rows[rowIndex]);

            cursorY += rowHeight;
        }

        // ── Borders ─────────────────────────────────────────────────
        if (table.Border != null)
        {
            DrawBorders(gfx, table.Border, table, columnWidths, tableX, tableY,
                tableWidth, cursorY - tableY, rowHeight);
        }
    }

    #endregion Public Methods

    #region Private Methods

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
        object[]? rowValues)
    {
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

            gfx.DrawString(cellText, font, brush, cellRect, format);

            cellX += columnWidths[col];
        }
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
        double rowHeight)
    {
        // Use the top border side's properties for all grid lines (simple approach).
        var side = border.Top;
        if (side.Width <= 0 || side.Style == BorderStyle.None)
            return;

        var pen = new XPen(ColorConvert.ToXColor(side.Color), side.Width);

        // Outer rectangle.
        gfx.DrawRectangle(pen, tableX, tableY, tableWidth, tableHeight);

        // Horizontal row dividers.
        int totalRows = 1 + table.Rows.Count; // header + data rows
        for (int row = 1; row < totalRows; row++)
        {
            double y = tableY + (row * rowHeight);
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
    /// Maps a PdfFlex <see cref="TextAlign"/> to a PdfSharp <see cref="XStringFormat"/>
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
