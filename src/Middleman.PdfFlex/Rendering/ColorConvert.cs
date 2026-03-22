// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Converts between <see cref="Styling.Color"/> and PdfFlex's <see cref="XColor"/>.
/// </summary>
internal static class ColorConvert
{
    /// <summary>
    /// Converts a PdfFlex color to a PdfFlex <see cref="XColor"/>.
    /// </summary>
    /// <param name="c">The PdfFlex color to convert.</param>
    /// <returns>The equivalent <see cref="XColor"/>.</returns>
    public static XColor ToXColor(Styling.Color c)
    {
        return XColor.FromArgb(c.A, c.R, c.G, c.B);
    }

    /// <summary>
    /// Converts a PdfFlex color to a PdfFlex <see cref="XColor"/> with an additional
    /// opacity multiplier applied to the alpha channel.
    /// </summary>
    /// <param name="c">The PdfFlex color to convert.</param>
    /// <param name="opacity">Opacity multiplier in the range [0.0, 1.0].</param>
    /// <returns>The equivalent <see cref="XColor"/> with adjusted alpha.</returns>
    public static XColor ToXColor(Styling.Color c, double opacity)
    {
        int alpha = (int)(Math.Clamp(opacity, 0.0, 1.0) * c.A);
        return XColor.FromArgb(alpha, c.R, c.G, c.B);
    }

    /// <summary>
    /// Pre-blends a color against white at the specified opacity, returning a fully opaque
    /// result. This produces a solid color that visually approximates the original color at
    /// the given opacity on a white background, without requiring alpha transparency.
    /// Used for PDF/A compliance where transparency is prohibited (ISO 19005-1 clause 6.4).
    /// </summary>
    /// <param name="c">The PdfFlex color to blend.</param>
    /// <param name="opacity">Opacity multiplier in the range [0.0, 1.0].</param>
    /// <returns>A fully opaque <see cref="XColor"/> pre-blended against white.</returns>
    public static XColor ToXColorPreBlended(Styling.Color c, double opacity)
    {
        double op = Math.Clamp(opacity, 0.0, 1.0);
        byte r = (byte)(255.0 * (1.0 - op) + c.R * op);
        byte g = (byte)(255.0 * (1.0 - op) + c.G * op);
        byte b = (byte)(255.0 * (1.0 - op) + c.B * op);
        return XColor.FromArgb(255, r, g, b);
    }
}
