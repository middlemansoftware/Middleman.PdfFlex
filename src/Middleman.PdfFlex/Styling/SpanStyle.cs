// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Inline text style for rich-text spans. All properties are nullable to support
/// cascading inheritance from the parent element's style.
/// </summary>
public class SpanStyle
{
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

    /// <summary>Gets or sets whether text is underlined. Null inherits from the parent.</summary>
    public bool? Underline { get; set; }

    /// <summary>Gets or sets whether text has a strikethrough. Null inherits from the parent.</summary>
    public bool? Strikethrough { get; set; }

    /// <summary>Gets or sets the line height multiplier. Null inherits from the parent.</summary>
    public double? LineHeight { get; set; }
}
