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
    /// Uses word wrapping to break text into lines that fit within the node's width,
    /// matching the layout engine's text measurement.
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

        // Use the same word-wrapping logic as the layout engine to ensure
        // rendered text matches the measured height.
        var wrapResult = TextMeasurer.WrapTextBlock(textBlock, node.Width);
        double cursorY = node.Y;

        for (int i = 0; i < wrapResult.Lines.Count; i++)
        {
            if (cursorY + lineHeight > node.Y + node.Height + 0.5)
                break;

            var lineRect = new XRect(node.X, cursorY, node.Width, lineHeight);
            gfx.DrawString(wrapResult.Lines[i], font, brush, lineRect, format);
            cursorY += lineHeight;
        }
    }

    /// <summary>
    /// Renders a <see cref="RichText"/> element using word-wrapping across span boundaries.
    /// Each line is rendered by drawing its span segments sequentially with per-span styling.
    /// </summary>
    /// <param name="gfx">The PdfSharp graphics surface to draw on.</param>
    /// <param name="node">The layout node positioning the rich text.</param>
    /// <param name="richText">The rich text element to render.</param>
    public static void RenderRichText(XGraphics gfx, LayoutNode node, RichText richText)
    {
        if (richText.Spans.Count == 0)
            return;

        // Pre-calculate the line height as the tallest span's line height.
        double maxLineHeight = 0;
        for (int i = 0; i < richText.Spans.Count; i++)
        {
            var spanFont = Rendering.FontHelper.CreateFontFromSpan(richText.Spans[i].Style, richText);
            double spanLineHeight = spanFont.GetHeight();
            if (spanLineHeight > maxLineHeight)
                maxLineHeight = spanLineHeight;
        }

        // Use the same word-wrapping logic as the layout engine.
        var wrapResult = TextMeasurer.WrapRichText(richText, node.Width);

        double cursorY = node.Y;

        for (int lineIdx = 0; lineIdx < wrapResult.Lines.Count; lineIdx++)
        {
            if (cursorY + maxLineHeight > node.Y + node.Height + 0.5)
                break;

            double cursorX = node.X;
            var line = wrapResult.Lines[lineIdx];

            for (int segIdx = 0; segIdx < line.Count; segIdx++)
            {
                var segment = line[segIdx];
                var spanFont = Rendering.FontHelper.CreateFontFromSpan(segment.Span.Style, richText);
                var spanColor = segment.Span.Style?.FontColor ?? StyleResolver.GetFontColor(richText);
                var spanBrush = new XSolidBrush(ColorConvert.ToXColor(spanColor));

                // Use TextMeasurer's measurement context for positioning to match
                // the layout engine's measurements and avoid DPI/transform drift.
                var segSize = TextMeasurer.MeasureString(segment.Text, spanFont);
                var segRect = new XRect(cursorX, cursorY, segSize.Width, maxLineHeight);
                gfx.DrawString(segment.Text, spanFont, spanBrush, segRect, XStringFormats.TopLeft);

                // Draw underline or strikethrough decorations.
                RenderDecorations(gfx, segment.Span.Style, spanFont, spanBrush,
                    cursorX, cursorY, segSize.Width, maxLineHeight);

                cursorX += segSize.Width;

                // Add a space between segments on the same line (words were split on spaces).
                if (segIdx < line.Count - 1)
                {
                    cursorX += TextMeasurer.MeasureString(" ", spanFont).Width;
                }
            }

            cursorY += maxLineHeight;
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
