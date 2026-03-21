// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// The main style class for elements. All properties are nullable to support cascading
/// style inheritance. When a property is null, the value is inherited from the parent
/// element or falls back to the default.
/// </summary>
public class Style
{
    // ── Box model ──────────────────────────────────────────────

    /// <summary>Gets or sets the inner padding. Null inherits from the parent.</summary>
    public EdgeInsets? Padding { get; set; }

    /// <summary>Gets or sets the outer margin. Null inherits from the parent.</summary>
    public EdgeInsets? Margin { get; set; }

    /// <summary>Gets or sets the border. Null inherits from the parent.</summary>
    public Border? Border { get; set; }

    /// <summary>Gets or sets the background. Null inherits from the parent.</summary>
    public Background? Background { get; set; }

    // ── Sizing ─────────────────────────────────────────────────

    /// <summary>Gets or sets the explicit width. Null means auto-sized.</summary>
    public Length? Width { get; set; }

    /// <summary>Gets or sets the explicit height. Null means auto-sized.</summary>
    public Length? Height { get; set; }

    /// <summary>Gets or sets the minimum width constraint.</summary>
    public Length? MinWidth { get; set; }

    /// <summary>Gets or sets the maximum width constraint.</summary>
    public Length? MaxWidth { get; set; }

    /// <summary>Gets or sets the minimum height constraint.</summary>
    public Length? MinHeight { get; set; }

    /// <summary>Gets or sets the maximum height constraint.</summary>
    public Length? MaxHeight { get; set; }

    // ── Flex layout ────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the flex grow factor. Determines how much this element grows
    /// relative to siblings when extra space is available. Defaults to 0 (no growth).
    /// </summary>
    public float FlexGrow { get; set; }

    /// <summary>
    /// Gets or sets the flex shrink factor. Determines how much this element shrinks
    /// relative to siblings when space is insufficient. Defaults to 1.
    /// </summary>
    public float FlexShrink { get; set; } = 1;

    // ── Typography ─────────────────────────────────────────────

    /// <summary>Gets or sets the font family name. Null inherits from the parent.</summary>
    public string? FontFamily { get; set; }

    /// <summary>Gets or sets the font size in points. Null inherits from the parent.</summary>
    public double? FontSize { get; set; }

    /// <summary>Gets or sets the font weight. Null inherits from the parent.</summary>
    public FontWeight? FontWeight { get; set; }

    /// <summary>Gets or sets the font style. Null inherits from the parent.</summary>
    public FontStyle? FontStyle { get; set; }

    /// <summary>Gets or sets the text color. Null inherits from the parent.</summary>
    public Color? FontColor { get; set; }

    /// <summary>Gets or sets the text alignment. Null inherits from the parent.</summary>
    public TextAlign? TextAlign { get; set; }

    /// <summary>Gets or sets the line height multiplier. Null inherits from the parent.</summary>
    public double? LineHeight { get; set; }
}
