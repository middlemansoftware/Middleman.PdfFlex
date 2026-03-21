// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Single-style text element that renders a string with a given font specification.
/// Text measurement currently uses a rough character-count approximation; real font metrics
/// will be integrated when the PDF rendering engine is connected.
/// </summary>
public class TextBlock : Element
{
    /// <summary>Average character width as a fraction of font size, used for estimation.</summary>
    internal const double AverageCharWidthRatio = 0.5;

    /// <summary>Line height as a multiplier of font size, used for estimation.</summary>
    internal const double LineHeightMultiplier = 1.2;

    #region Public Properties

    /// <summary>Gets the text content to render.</summary>
    public string Text { get; }

    /// <summary>Gets the optional font specification. When null, font properties are inherited from the parent chain.</summary>
    public FontSpec? Font { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a text element with the specified content and optional font.</summary>
    /// <param name="text">The text content to render.</param>
    /// <param name="font">Optional font specification. Null inherits from parent chain.</param>
    /// <param name="style">Optional style to apply to this text element.</param>
    public TextBlock(string text, FontSpec? font = null, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        Font = font;
        Style = style;
    }

    #endregion Constructors

    #region Public Methods

    /// <summary>
    /// Estimates the rendered width of this text block in points. Uses a rough approximation
    /// based on character count and font size until real font metrics are available.
    /// </summary>
    /// <param name="fontSize">The resolved font size in points.</param>
    /// <returns>The estimated width in points.</returns>
    public double EstimateWidth(double fontSize)
    {
        return Text.Length * fontSize * AverageCharWidthRatio;
    }

    /// <summary>
    /// Estimates the rendered height of this text block in points.
    /// </summary>
    /// <param name="fontSize">The resolved font size in points.</param>
    /// <returns>The estimated height in points.</returns>
    public double EstimateHeight(double fontSize)
    {
        return fontSize * LineHeightMultiplier;
    }

    #endregion Public Methods
}
