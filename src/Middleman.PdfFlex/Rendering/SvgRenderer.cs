// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Globalization;
using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf.Structure;
using Middleman.Svg.Model;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="SvgBox"/> elements by converting SVG shapes and text to PdfFlex
/// drawing operations. Shapes produce native PDF vector content; text elements are drawn
/// using the PdfFlex font infrastructure with support for PostScript name resolution,
/// CSS font stacks, letter spacing, and text-anchor alignment.
/// </summary>
internal static class SvgRenderer
{
    #region Constants

    /// <summary>Last-resort font family when all resolution attempts fail.</summary>
    private const string FallbackFontFamily = "Arial";

    /// <summary>
    /// Generic CSS font family mappings to platform defaults.
    /// </summary>
    private static readonly Dictionary<string, string> GenericFamilyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["serif"] = "Times New Roman",
        ["sans-serif"] = "Arial",
        ["monospace"] = "Courier New",
        ["cursive"] = "Comic Sans MS",
        ["fantasy"] = "Impact",
    };

    #endregion Constants

    #region Public Methods

    /// <summary>
    /// Renders an SVG element within the specified layout node bounds. The SVG is scaled
    /// uniformly to fit while preserving aspect ratio, then each element (shape or text)
    /// is drawn in document order.
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

        foreach (var element in doc.Elements)
        {
            if (!element.Visible)
                continue;

            if (element is SvgShape shape)
            {
                RenderShape(gfx, shape);
            }
            else if (element is SvgText text)
            {
                RenderText(gfx, text);
            }
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
                PdfStructureElement? linkSte = ctx.StructureBuilder?.CreateLinkStructureElement();
                DocumentRenderer.QueueInternalLink(
                    ctx, linkRect, linkTarget, svgBox.AltText, linkSte);
            }
        }
    }

    #endregion Public Methods

    #region Shape Rendering

    /// <summary>
    /// Renders a single SVG shape. All subpaths are combined into one XGraphicsPath
    /// so the fill rule (even-odd or nonzero winding) correctly handles compound paths
    /// with holes (e.g., letter counters in "D", "A", "O").
    /// </summary>
    private static void RenderShape(XGraphics gfx, SvgShape shape)
    {
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

        double prevX = pts[0];
        double prevY = pts[1];
        int i = 2;

        while (i + 5 < pts.Length)
        {
            xpath.AddBezier(
                prevX, prevY,
                pts[i], pts[i + 1],
                pts[i + 2], pts[i + 3],
                pts[i + 4], pts[i + 5]);

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

    #endregion Shape Rendering

    #region Text Rendering

    /// <summary>
    /// Renders an SVG text element with its child tspan elements. Handles font resolution
    /// (including PostScript names and CSS font stacks), letter spacing via per-character
    /// drawing, and text-anchor alignment.
    /// </summary>
    private static void RenderText(XGraphics gfx, SvgText text)
    {
        string content = "";
        float letterSpacing = text.LetterSpacing;

        // Determine effective content: if spans exist, they carry the text.
        // If only direct text, use that.
        if (text.Spans.Count > 0)
        {
            // Render spans
            RenderTextSpans(gfx, text);
            return;
        }

        content = text.Text;
        if (string.IsNullOrEmpty(content))
            return;

        var font = ResolveFont(text.FontFamily, text.FontSize, text.FontWeight, text.FontStyle);
        var brush = CreateTextBrush(text.Fill, text.Opacity);

        double drawX = text.X;
        double drawY = text.Y;

        // Apply text-anchor alignment
        double totalWidth = MeasureTextWidth(gfx, content, font, letterSpacing);
        drawX = ApplyTextAnchor(drawX, totalWidth, text.TextAnchor);

        // Draw with or without letter spacing
        if (Math.Abs(letterSpacing) > 0.001f)
        {
            DrawStringWithSpacing(gfx, content, font, brush, drawX, drawY, letterSpacing);
        }
        else
        {
            gfx.DrawString(content, font, brush, drawX, drawY, XStringFormats.Default);
        }
    }

    /// <summary>
    /// Renders tspan children of a text element. Processes each span sequentially,
    /// accumulating position offsets and applying per-span font and fill overrides.
    /// </summary>
    private static void RenderTextSpans(XGraphics gfx, SvgText text)
    {
        double cursorX = text.X;
        double cursorY = text.Y;

        // For text-anchor, we need total width of all spans combined.
        double totalWidth = 0;
        var spanFonts = new XFont[text.Spans.Count];
        var spanSpacings = new float[text.Spans.Count];

        for (int i = 0; i < text.Spans.Count; i++)
        {
            var span = text.Spans[i];
            float fs = span.FontSize ?? text.FontSize;
            int fw = span.FontWeight ?? text.FontWeight;
            string fst = span.FontStyle ?? text.FontStyle;
            string ff = span.FontFamily ?? text.FontFamily;
            float ls = span.LetterSpacing ?? text.LetterSpacing;

            spanFonts[i] = ResolveFont(ff, fs, fw, fst);
            spanSpacings[i] = ls;
            totalWidth += MeasureTextWidth(gfx, span.Text, spanFonts[i], ls);
        }

        // Apply text-anchor to the starting position
        cursorX = ApplyTextAnchor(cursorX, totalWidth, text.TextAnchor);

        for (int i = 0; i < text.Spans.Count; i++)
        {
            var span = text.Spans[i];
            if (string.IsNullOrEmpty(span.Text))
                continue;

            // Absolute repositioning
            if (span.X.HasValue) cursorX = span.X.Value;
            if (span.Y.HasValue) cursorY = span.Y.Value;

            // Relative offsets
            if (span.Dx.HasValue) cursorX += span.Dx.Value;
            if (span.Dy.HasValue) cursorY += span.Dy.Value;

            // Re-apply text-anchor when absolute X is set on tspan
            if (span.X.HasValue)
            {
                double spanWidth = MeasureTextWidth(gfx, span.Text, spanFonts[i], spanSpacings[i]);
                cursorX = ApplyTextAnchor(cursorX, spanWidth, text.TextAnchor);
            }

            float[] spanFill = span.Fill ?? text.Fill;
            var brush = CreateTextBrush(spanFill, text.Opacity);

            if (Math.Abs(spanSpacings[i]) > 0.001f)
            {
                cursorX = DrawStringWithSpacing(gfx, span.Text, spanFonts[i], brush,
                    cursorX, cursorY, spanSpacings[i]);
            }
            else
            {
                gfx.DrawString(span.Text, spanFonts[i], brush, cursorX, cursorY,
                    XStringFormats.Default);
                cursorX += gfx.MeasureString(span.Text, spanFonts[i]).Width;
            }
        }
    }

    /// <summary>
    /// Draws a string text element by text element with custom letter spacing.
    /// Uses <see cref="StringInfo.GetTextElementEnumerator(string)"/> for correct
    /// handling of surrogate pairs and combining character sequences.
    /// Returns the X position after the last element (for cursor advancement).
    /// </summary>
    private static double DrawStringWithSpacing(
        XGraphics gfx, string text, XFont font, XBrush brush,
        double x, double y, float letterSpacing)
    {


        double cursorX = x;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        bool isFirst = true;
        while (enumerator.MoveNext())
        {
            if (!isFirst)
                cursorX += letterSpacing;
            isFirst = false;

            string element = enumerator.GetTextElement();
            gfx.DrawString(element, font, brush, cursorX, y, XStringFormats.Default);
            cursorX += gfx.MeasureString(element, font).Width;
        }
        return cursorX;
    }

    /// <summary>
    /// Measures the total width of a text string including letter spacing between text elements.
    /// Uses <see cref="StringInfo.GetTextElementEnumerator(string)"/> for correct
    /// handling of surrogate pairs and combining character sequences.
    /// </summary>
    private static double MeasureTextWidth(XGraphics gfx, string text, XFont font, float letterSpacing)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        if (Math.Abs(letterSpacing) < 0.001f)
            return gfx.MeasureString(text, font).Width;

        // With letter spacing, measure each text element individually.
        double totalWidth = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        bool isFirst = true;
        while (enumerator.MoveNext())
        {
            if (!isFirst)
                totalWidth += letterSpacing;
            isFirst = false;

            totalWidth += gfx.MeasureString(enumerator.GetTextElement(), font).Width;
        }
        return totalWidth;
    }

    /// <summary>
    /// Adjusts the X drawing position based on the SVG text-anchor attribute.
    /// </summary>
    private static double ApplyTextAnchor(double x, double textWidth, string anchor)
    {
        return anchor switch
        {
            "middle" => x - (textWidth / 2.0),
            "end" => x - textWidth,
            _ => x // "start" is the default
        };
    }

    /// <summary>
    /// Creates a solid brush from RGBA float components [0..1] and element opacity.
    /// </summary>
    private static XSolidBrush CreateTextBrush(float[] fill, float opacity)
    {
        int r = (int)(fill[0] * 255);
        int g = (int)(fill[1] * 255);
        int b = (int)(fill[2] * 255);
        int a = (int)(fill[3] * opacity * 255);
        var xColor = XColor.FromArgb(a, r, g, b);
        return new XSolidBrush(xColor);
    }

    #endregion Text Rendering

    #region Font Resolution

    /// <summary>
    /// Resolves an SVG font specification to an <see cref="XFont"/>. Handles three scenarios:
    /// <list type="number">
    /// <item>Standard font-family with separate weight/style attributes.</item>
    /// <item>PostScript names (e.g., "Arial-BoldMT" from Illustrator exports).</item>
    /// <item>CSS font stacks (comma-separated list with generic family fallbacks).</item>
    /// </list>
    /// Falls back to Arial if all resolution attempts fail.
    /// </summary>
    private static XFont ResolveFont(string fontFamily, float fontSize, int fontWeight, string fontStyle)
    {
        var xStyle = MapFontStyle(fontWeight, fontStyle);
        double size = Math.Max(1.0, fontSize);

        // Handle CSS font stack (comma-separated)
        string[] families = fontFamily.Split(',');
        foreach (string rawFamily in families)
        {
            string family = rawFamily.Trim().Trim('"').Trim('\'');
            if (string.IsNullOrEmpty(family))
                continue;

            // Check generic family mapping
            if (GenericFamilyMap.TryGetValue(family, out string? mapped))
                family = mapped;

            // Try direct creation
            XFont? font = TryCreateFont(family, size, xStyle);
            if (font != null)
                return font;

            // Try PostScript name parsing (e.g., "Arial-BoldMT")
            font = TryParsePostScriptName(family, size);
            if (font != null)
                return font;
        }

        // Last resort fallback
        return new XFont(FallbackFontFamily, size, xStyle);
    }

    /// <summary>
    /// Attempts to create an XFont, returning null on failure instead of throwing.
    /// </summary>
    private static XFont? TryCreateFont(string family, double size, XFontStyleEx style)
    {
        try
        {
            return new XFont(family, size, style);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a PostScript font name (e.g., "Arial-BoldMT", "ArialMT") by splitting on
    /// the last hyphen to extract family and style descriptors. Strips the "MT" Monotype
    /// identifier and maps style suffixes to XFontStyleEx flags.
    /// </summary>
    private static XFont? TryParsePostScriptName(string postScriptName, double size)
    {
        // First try the full name as-is (some systems register PostScript names directly)
        XFont? font = TryCreateFont(postScriptName, size, XFontStyleEx.Regular);
        if (font != null)
            return font;

        // Strip MT (Monotype identifier) suffix
        string name = postScriptName;
        if (name.EndsWith("MT", StringComparison.Ordinal))
            name = name[..^2];

        // Split on last hyphen: prefix = family candidate, suffix = style descriptor
        int hyphen = name.LastIndexOf('-');
        if (hyphen <= 0)
        {
            // No hyphen -- try the stripped name as a family
            font = TryCreateFont(name, size, XFontStyleEx.Regular);
            if (font != null)
                return font;
            return null;
        }

        string family = name[..hyphen];
        string suffix = name[(hyphen + 1)..];

        // Parse style from suffix
        bool bold = suffix.Contains("Bold", StringComparison.OrdinalIgnoreCase);
        bool italic = suffix.Contains("Italic", StringComparison.OrdinalIgnoreCase)
                   || suffix.Contains("Oblique", StringComparison.OrdinalIgnoreCase);

        var style = (bold, italic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular
        };

        return TryCreateFont(family, size, style);
    }

    /// <summary>
    /// Maps CSS numeric font weight and font style string to XFontStyleEx flags.
    /// </summary>
    private static XFontStyleEx MapFontStyle(int fontWeight, string fontStyle)
    {
        bool bold = fontWeight >= 700;
        bool italic = fontStyle is "italic" or "oblique";

        return (bold, italic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular
        };
    }

    #endregion Font Resolution
}
