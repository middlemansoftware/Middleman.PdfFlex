// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="Divider"/> elements as horizontal or vertical lines.
/// </summary>
internal static class DividerRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders a divider line within the bounds of the specified layout node.
    /// </summary>
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="node">The layout node positioning the divider.</param>
    /// <param name="divider">The divider element to render.</param>
    public static void Render(XGraphics gfx, LayoutNode node, Divider divider)
    {
        if (divider.Thickness <= 0)
            return;

        var pen = new XPen(ColorConvert.ToXColor(divider.Color), divider.Thickness);

        if (divider.IsVertical)
        {
            // Vertical line centered horizontally within the node.
            double centerX = node.X + (node.Width / 2.0);
            gfx.DrawLine(pen, centerX, node.Y, centerX, node.Y + node.Height);
        }
        else
        {
            // Horizontal line centered vertically within the node.
            double centerY = node.Y + (node.Height / 2.0);
            gfx.DrawLine(pen, node.X, centerY, node.X + node.Width, centerY);
        }
    }

    #endregion Public Methods
}
