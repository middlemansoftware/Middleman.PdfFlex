// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A diagonal text watermark rendered behind content on every page.
/// </summary>
public class Watermark
{
    #region Public Properties

    /// <summary>Gets the watermark text.</summary>
    public string Text { get; }

    /// <summary>Gets the opacity (0.0 fully transparent to 1.0 fully opaque).</summary>
    public double Opacity { get; }

    /// <summary>Gets the rotation angle in degrees, or null to auto-calculate the diagonal angle from the page dimensions.</summary>
    public double? Angle { get; }

    /// <summary>Gets the watermark text color.</summary>
    public Color Color { get; }

    /// <summary>Gets the font size in points, or null to auto-scale based on page dimensions.</summary>
    public double? FontSize { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a watermark with the specified text and appearance settings.</summary>
    /// <param name="text">The watermark text.</param>
    /// <param name="opacity">The opacity (0.0 to 1.0). Defaults to 0.15.</param>
    /// <param name="angle">The rotation angle in degrees, or null to auto-calculate.</param>
    /// <param name="color">The text color. Defaults to <see cref="Colors.Gray"/>.</param>
    /// <param name="fontSize">The font size in points, or null to auto-scale.</param>
    public Watermark(
        string text,
        double opacity = 0.15,
        double? angle = null,
        Color? color = null,
        double? fontSize = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        Opacity = opacity;
        Angle = angle;
        Color = color ?? Colors.Gray;
        FontSize = fontSize;
    }

    #endregion Constructors
}
