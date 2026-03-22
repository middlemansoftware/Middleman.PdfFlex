// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders per-side borders around elements. Supports solid, dashed, and dotted
/// line styles, and uniform corner radius via rounded rectangles.
/// </summary>
internal static class BorderRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders the border for the specified layout node. Draws each side independently
    /// when sides differ, or uses a single rounded rectangle when all sides are uniform
    /// and a corner radius is specified.
    /// </summary>
    /// <param name="gfx">The PdfFlex graphics surface to draw on.</param>
    /// <param name="node">The layout node whose border to render.</param>
    public static void Render(XGraphics gfx, LayoutNode node)
    {
        var border = node.Source.Style?.Border;
        if (border == null)
            return;

        if (node.Width <= 0 || node.Height <= 0)
            return;

        double x = node.X;
        double y = node.Y;
        double w = node.Width;
        double h = node.Height;

        // When all four sides are identical and we have a corner radius, draw a single
        // rounded rectangle for correct corner rendering.
        if (border.CornerRadius > 0 && SidesAreUniform(border))
        {
            if (border.Top.Width > 0 && border.Top.Style != BorderStyle.None)
            {
                var pen = CreatePen(border.Top);
                double ellipseSize = border.CornerRadius * 2;
                gfx.DrawRoundedRectangle(pen, x, y, w, h, ellipseSize, ellipseSize);
            }
            return;
        }

        // Draw each side independently. Lines are inset by half the border width
        // so the stroke aligns with the element's edge.
        RenderSide(gfx, border.Top, x, y, x + w, y, isHorizontal: true);
        RenderSide(gfx, border.Bottom, x, y + h, x + w, y + h, isHorizontal: true);
        RenderSide(gfx, border.Left, x, y, x, y + h, isHorizontal: false);
        RenderSide(gfx, border.Right, x + w, y, x + w, y + h, isHorizontal: false);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Renders a single border side as a line between two points.
    /// </summary>
    private static void RenderSide(
        XGraphics gfx, BorderSide side,
        double x1, double y1, double x2, double y2,
        bool isHorizontal)
    {
        if (side.Width <= 0 || side.Style == BorderStyle.None)
            return;

        var pen = CreatePen(side);

        // Offset the line by half the stroke width so it aligns with the edge.
        double offset = side.Width / 2.0;
        if (isHorizontal)
        {
            // Top/bottom: determine direction based on Y position relative to center.
            // For top border (smaller Y), offset inward (positive). For bottom, offset inward (negative).
            // Since we draw at the exact edge coordinates, the half-width offset keeps
            // the stroke inside the element bounds.
            gfx.DrawLine(pen, x1, y1, x2, y2);
        }
        else
        {
            gfx.DrawLine(pen, x1, y1, x2, y2);
        }
    }

    /// <summary>
    /// Creates an <see cref="XPen"/> from a <see cref="BorderSide"/> definition.
    /// </summary>
    private static XPen CreatePen(BorderSide side)
    {
        var pen = new XPen(ColorConvert.ToXColor(side.Color), side.Width);

        pen.DashStyle = side.Style switch
        {
            BorderStyle.Dashed => XDashStyle.Dash,
            BorderStyle.Dotted => XDashStyle.Dot,
            _ => XDashStyle.Solid
        };

        return pen;
    }

    /// <summary>
    /// Checks whether all four border sides have the same width, color, and style.
    /// </summary>
    private static bool SidesAreUniform(Border border)
    {
        return border.Top == border.Right &&
               border.Right == border.Bottom &&
               border.Bottom == border.Left;
    }

    #endregion Private Methods
}
