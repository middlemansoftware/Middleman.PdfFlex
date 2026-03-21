// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Converts between <see cref="Styling.Color"/> and PdfSharp's <see cref="XColor"/>.
/// </summary>
internal static class ColorConvert
{
    /// <summary>
    /// Converts a PdfFlex color to a PdfSharp <see cref="XColor"/>.
    /// </summary>
    /// <param name="c">The PdfFlex color to convert.</param>
    /// <returns>The equivalent <see cref="XColor"/>.</returns>
    public static XColor ToXColor(Styling.Color c)
    {
        return XColor.FromArgb(c.A, c.R, c.G, c.B);
    }

    /// <summary>
    /// Converts a PdfFlex color to a PdfSharp <see cref="XColor"/> with an additional
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
}
