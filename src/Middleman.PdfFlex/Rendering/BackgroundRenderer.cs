// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders solid-color backgrounds for elements. Handles both rectangular
/// and rounded-rectangle fills based on the element's border corner radius.
/// </summary>
internal static class BackgroundRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders the background fill for the specified layout node. Does nothing if the
    /// element has no background defined or if the node has zero area.
    /// </summary>
    /// <param name="gfx">The PdfFlex graphics surface to draw on.</param>
    /// <param name="node">The layout node whose background to render.</param>
    public static void Render(XGraphics gfx, LayoutNode node)
    {
        var background = node.Source.Style?.Background;
        if (background == null)
            return;

        if (node.Width <= 0 || node.Height <= 0)
            return;

        var brush = new XSolidBrush(ColorConvert.ToXColor(background.Color));
        double cornerRadius = node.Source.Style?.Border?.CornerRadius ?? 0;

        if (cornerRadius > 0)
        {
            double ellipseSize = cornerRadius * 2;
            gfx.DrawRoundedRectangle(
                brush,
                node.X, node.Y, node.Width, node.Height,
                ellipseSize, ellipseSize);
        }
        else
        {
            gfx.DrawRectangle(brush, node.X, node.Y, node.Width, node.Height);
        }
    }

    #endregion Public Methods
}
