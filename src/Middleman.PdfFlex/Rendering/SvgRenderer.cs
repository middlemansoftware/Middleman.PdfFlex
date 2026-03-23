// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf.Structure;
using Middleman.Svg.Model;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="SvgBox"/> elements by converting SVG shapes and their cubic
/// bezier paths to PdfFlex vector drawing operations. Produces native PDF vector
/// content rather than rasterized images.
/// </summary>
internal static class SvgRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders an SVG element within the specified layout node bounds. The SVG is scaled
    /// uniformly to fit while preserving aspect ratio, then each shape's fill and stroke
    /// are drawn as vector paths.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node positioning the SVG.</param>
    /// <param name="svgBox">The SVG element to render.</param>
    public static void Render(RenderContext ctx, LayoutNode node, SvgBox svgBox)
    {
        if (node.Width <= 0 || node.Height <= 0)
            return;

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;

        var doc = svgBox.GetDocument();
        if (doc.Width <= 0 || doc.Height <= 0)
            return;

        // Calculate uniform scale to fit within node bounds.
        double scaleX = node.Width / doc.Width;
        double scaleY = node.Height / doc.Height;
        double scale = Math.Min(scaleX, scaleY);

        // Center the scaled SVG within the node.
        double scaledWidth = doc.Width * scale;
        double scaledHeight = doc.Height * scale;
        double offsetX = node.X + ((node.Width - scaledWidth) / 2.0);
        double offsetY = node.Y + ((node.Height - scaledHeight) / 2.0);

        if (sb != null)
        {
            var bbox = new XRect(offsetX, offsetY, scaledWidth, scaledHeight);
            sb.BeginElement(PdfIllustrationElementTag.Figure, svgBox.AltText ?? "", bbox);
        }

        var state = gfx.Save();
        gfx.TranslateTransform(offsetX, offsetY);
        gfx.ScaleTransform(scale, scale);

        foreach (var shape in doc.Shapes)
        {
            if (!shape.Visible)
                continue;

            RenderShape(gfx, shape);
        }

        gfx.Restore(state);

        if (sb != null) sb.End();

        // Create link annotation covering the drawn SVG area.
        string? linkTarget = svgBox.LinkTarget;
        if (!string.IsNullOrEmpty(linkTarget) && ctx.Page != null)
        {
            var linkRect = new XRect(offsetX, offsetY, scaledWidth, scaledHeight);
            if (DocumentRenderer.IsExternalLink(linkTarget))
            {
                DocumentRenderer.CreateUriLinkAnnotation(
                    ctx.Page, linkRect, linkTarget, ctx.PageHeight, sb,
                    svgBox.AltText ?? linkTarget);
            }
            else
            {
                // Create the /Link structure element now while the element
                // stack is correctly positioned. The annotation will be
                // associated with this element during deferred resolution.
                PdfStructureElement? linkSte = ctx.StructureBuilder?.CreateLinkStructureElement();
                DocumentRenderer.QueueInternalLink(
                    ctx, linkRect, linkTarget, svgBox.AltText, linkSte);
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Renders a single SVG shape. All subpaths are combined into one XGraphicsPath
    /// so the fill rule (even-odd or nonzero winding) correctly handles compound paths
    /// with holes (e.g., letter counters in "D", "A", "O").
    /// </summary>
    private static void RenderShape(XGraphics gfx, SvgShape shape)
    {
        // Combine all subpaths into one XGraphicsPath for correct fill rule behavior.
        var fillMode = shape.FillRule == SvgFillRule.EvenOdd
            ? XFillMode.Alternate
            : XFillMode.Winding;

        var combinedPath = new XGraphicsPath { FillMode = fillMode };
        bool hasSegments = false;

        foreach (var path in shape.Paths)
        {
            if (AddSubpath(combinedPath, path))
                hasSegments = true;
        }

        if (!hasSegments)
            return;

        XBrush? fillBrush = null;
        XPen? strokePen = null;

        if (shape.Fill.Type == SvgPaintType.Color)
        {
            fillBrush = CreateBrush(shape.Fill, shape.Opacity);
        }

        if (shape.Stroke.Type == SvgPaintType.Color && shape.StrokeWidth > 0)
        {
            strokePen = CreatePen(shape);
        }

        if (fillBrush != null || strokePen != null)
        {
            gfx.DrawPath(strokePen, fillBrush, combinedPath);
        }
    }

    /// <summary>
    /// Adds an SVG subpath to an existing XGraphicsPath as a new figure.
    /// The SVG path's flat point array follows the format: start point (x0, y0) then
    /// groups of three points per cubic bezier segment (cp1x, cp1y, cp2x, cp2y, x, y).
    /// </summary>
    /// <returns>True if segments were added, false if the path was empty.</returns>
    private static bool AddSubpath(XGraphicsPath xpath, SvgPath svgPath)
    {
        var pts = svgPath.Points;
        if (pts.Length < 2)
            return false;

        xpath.StartFigure();

        // First point is the start position.
        double prevX = pts[0];
        double prevY = pts[1];
        int i = 2;

        // Remaining points are cubic bezier triplets (6 floats per segment).
        while (i + 5 < pts.Length)
        {
            xpath.AddBezier(
                prevX, prevY,         // Start point
                pts[i], pts[i + 1],   // Control point 1
                pts[i + 2], pts[i + 3], // Control point 2
                pts[i + 4], pts[i + 5]); // End point

            prevX = pts[i + 4];
            prevY = pts[i + 5];
            i += 6;
        }

        if (svgPath.Closed)
            xpath.CloseFigure();

        return true;
    }

    /// <summary>
    /// Creates a solid fill brush from an SVG paint, applying the shape's opacity.
    /// </summary>
    private static XSolidBrush CreateBrush(SvgPaint paint, float opacity)
    {
        int alpha = (int)(opacity * paint.A);
        var xColor = XColor.FromArgb(alpha, paint.R, paint.G, paint.B);
        return new XSolidBrush(xColor);
    }

    /// <summary>
    /// Creates a stroke pen from an SVG shape's stroke properties.
    /// </summary>
    private static XPen CreatePen(SvgShape shape)
    {
        int alpha = (int)(shape.Opacity * shape.Stroke.A);
        var xColor = XColor.FromArgb(alpha, shape.Stroke.R, shape.Stroke.G, shape.Stroke.B);
        var pen = new XPen(xColor, shape.StrokeWidth);

        if (shape.StrokeDashArray.Length > 0)
        {
            double[] dashPattern = new double[shape.StrokeDashArray.Length];
            for (int i = 0; i < shape.StrokeDashArray.Length; i++)
            {
                dashPattern[i] = shape.StrokeDashArray[i];
            }
            pen.DashPattern = dashPattern;
        }

        return pen;
    }

    #endregion Private Methods
}
