// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders a diagonal text watermark behind content on a PDF page.
/// Supports auto-scaling of font size and auto-calculation of rotation angle
/// based on page dimensions.
/// </summary>
internal static class WatermarkRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders a watermark centered on the page with optional rotation and opacity.
    /// When font size is not specified, it auto-scales to approximately one-third the
    /// page diagonal. When angle is not specified, it calculates the diagonal angle
    /// from the page dimensions.
    /// </summary>
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="watermark">The watermark definition.</param>
    /// <param name="pageWidth">The page width in points.</param>
    /// <param name="pageHeight">The page height in points.</param>
    public static void Render(XGraphics gfx, Watermark watermark, double pageWidth, double pageHeight)
    {
        if (string.IsNullOrEmpty(watermark.Text))
            return;

        // Auto-calculate angle if not specified: negative diagonal angle.
        double angle = watermark.Angle ??
            -(Math.Atan2(pageHeight, pageWidth) * (180.0 / Math.PI));

        // Auto-calculate font size if not specified: scale to roughly fill the diagonal.
        double diagonal = Math.Sqrt((pageWidth * pageWidth) + (pageHeight * pageHeight));
        double fontSize = watermark.FontSize ?? (diagonal / watermark.Text.Length * 1.5);

        // Clamp font size to reasonable bounds.
        fontSize = Math.Clamp(fontSize, 8.0, 500.0);

        var font = new XFont("NotoSans", fontSize, XFontStyleEx.Bold);
        var brush = new XSolidBrush(ColorConvert.ToXColor(watermark.Color, watermark.Opacity));
        var format = XStringFormats.Center;

        var state = gfx.Save();

        // Translate to page center, rotate, then draw centered text.
        gfx.TranslateTransform(pageWidth / 2.0, pageHeight / 2.0);
        gfx.RotateTransform(angle);
        gfx.DrawString(watermark.Text, font, brush, new XRect(-diagonal / 2.0, -fontSize, diagonal, fontSize * 2.0), format);

        gfx.Restore(state);
    }

    #endregion Public Methods
}
