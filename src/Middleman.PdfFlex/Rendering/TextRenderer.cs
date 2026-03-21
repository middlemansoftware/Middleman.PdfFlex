// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;
using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="TextBlock"/> and <see cref="RichText"/> elements.
/// Handles text alignment, explicit newlines, and per-span styling for rich text.
/// </summary>
internal static class TextRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders a <see cref="TextBlock"/> element within the bounds of the specified layout node.
    /// Supports explicit newline splitting and horizontal text alignment.
    /// </summary>
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="node">The layout node positioning the text.</param>
    /// <param name="textBlock">The text block element to render.</param>
    public static void RenderTextBlock(XGraphics gfx, LayoutNode node, TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(textBlock.Text))
            return;

        var font = Rendering.FontHelper.CreateFontFromElement(textBlock);
        var color = StyleResolver.GetFontColor(textBlock);
        var brush = new XSolidBrush(ColorConvert.ToXColor(color));
        var textAlign = ResolveTextAlign(textBlock);
        var format = CreateStringFormat(textAlign);

        double lineHeight = font.GetHeight();
        var lines = textBlock.Text.Split('\n');
        double cursorY = node.Y;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (cursorY + lineHeight > node.Y + node.Height + 0.5)
                break;

            var lineRect = new XRect(node.X, cursorY, node.Width, lineHeight);
            gfx.DrawString(line, font, brush, lineRect, format);
            cursorY += lineHeight;
        }
    }

    /// <summary>
    /// Renders a <see cref="RichText"/> element by iterating its spans, applying per-span
    /// font and color styling, and drawing each segment sequentially.
    /// </summary>
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="node">The layout node positioning the rich text.</param>
    /// <param name="richText">The rich text element to render.</param>
    public static void RenderRichText(XGraphics gfx, LayoutNode node, RichText richText)
    {
        if (richText.Spans.Count == 0)
            return;

        var textAlign = ResolveTextAlign(richText);
        double cursorX = node.X;
        double cursorY = node.Y;
        double maxLineHeight = 0;

        // Pre-calculate the first line height for baseline alignment.
        foreach (var span in richText.Spans)
        {
            var spanFont = Rendering.FontHelper.CreateFontFromSpan(span.Style, richText);
            double spanLineHeight = spanFont.GetHeight();
            if (spanLineHeight > maxLineHeight)
                maxLineHeight = spanLineHeight;
        }

        foreach (var span in richText.Spans)
        {
            if (string.IsNullOrEmpty(span.Text))
                continue;

            var spanFont = Rendering.FontHelper.CreateFontFromSpan(span.Style, richText);
            var spanColor = span.Style?.FontColor ?? StyleResolver.GetFontColor(richText);
            var spanBrush = new XSolidBrush(ColorConvert.ToXColor(spanColor));
            var spanSize = gfx.MeasureString(span.Text, spanFont);

            // Handle explicit newlines within a span.
            var segments = span.Text.Split('\n');
            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0)
                {
                    // Newline: advance Y, reset X.
                    cursorX = node.X;
                    cursorY += maxLineHeight;
                }

                if (cursorY + maxLineHeight > node.Y + node.Height + 0.5)
                    break;

                string segment = segments[i];
                if (segment.Length == 0)
                    continue;

                var segSize = gfx.MeasureString(segment, spanFont);

                // Simple overflow: if the segment exceeds the available width, clip it.
                // Full word-wrapping across span boundaries is a future enhancement.
                var segRect = new XRect(cursorX, cursorY, segSize.Width, maxLineHeight);
                gfx.DrawString(segment, spanFont, spanBrush, segRect, XStringFormats.TopLeft);

                // Draw underline or strikethrough decorations.
                RenderDecorations(gfx, span.Style, spanFont, spanBrush, cursorX, cursorY, segSize.Width, maxLineHeight);

                cursorX += segSize.Width;
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Resolves the text alignment from the element's style cascade.
    /// </summary>
    private static TextAlign ResolveTextAlign(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.Style?.TextAlign is { } align)
                return align;
            current = current.Parent;
        }
        return TextAlign.Left;
    }

    /// <summary>
    /// Creates an <see cref="XStringFormat"/> matching the specified text alignment.
    /// Vertical alignment is always top-aligned within the layout rect.
    /// </summary>
    private static XStringFormat CreateStringFormat(TextAlign align)
    {
        return align switch
        {
            TextAlign.Center => XStringFormats.TopCenter,
            TextAlign.Right => XStringFormats.TopRight,
            // Justify is treated as left-aligned for v1; proper justification
            // requires per-word spacing calculations.
            _ => XStringFormats.TopLeft
        };
    }

    /// <summary>
    /// Renders underline and strikethrough text decorations for a span.
    /// </summary>
    private static void RenderDecorations(
        XGraphics gfx, SpanStyle? style, XFont font, XSolidBrush brush,
        double x, double y, double width, double lineHeight)
    {
        if (style == null)
            return;

        var pen = new XPen(brush.Color, Math.Max(0.5, font.Size / 20.0));

        if (style.Underline == true)
        {
            double underlineY = y + lineHeight - (font.Size * 0.15);
            gfx.DrawLine(pen, x, underlineY, x + width, underlineY);
        }

        if (style.Strikethrough == true)
        {
            double strikeY = y + (lineHeight / 2.0);
            gfx.DrawLine(pen, x, strikeY, x + width, strikeY);
        }
    }

    #endregion Private Methods
}
