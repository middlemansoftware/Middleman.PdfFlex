// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Wraps a single table cell value with optional per-cell style overrides.
/// When a <see cref="Style"/> is set it takes precedence over the row-level
/// and table-level cell styles for font, color, padding, and background.
/// </summary>
public class TableCell
{
    /// <summary>Gets the cell content. Rendered via <c>ToString()</c>.</summary>
    public object Content { get; }

    /// <summary>Gets the optional style override for this cell.</summary>
    public Style? Style { get; }

    /// <summary>Gets the number of columns this cell spans. Defaults to 1.</summary>
    /// <remarks>Column spanning is reserved for future use and is currently ignored by the renderer.</remarks>
    public int ColSpan { get; }

    /// <summary>Creates a table cell with content and optional style override.</summary>
    /// <param name="content">The cell value (rendered via <c>ToString()</c>).</param>
    /// <param name="style">Optional style override for this cell.</param>
    /// <param name="colSpan">Number of columns to span. Defaults to 1.</param>
    public TableCell(object content, Style? style = null, int colSpan = 1)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
        Style = style;
        ColSpan = Math.Max(1, colSpan);
    }

    /// <summary>Implicitly converts a string to a <see cref="TableCell"/> with no style override.</summary>
    public static implicit operator TableCell(string text) => new(text);
}
