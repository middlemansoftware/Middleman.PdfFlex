// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.UniversalAccessibility;

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
    /// matching the layout engine's text measurement. Resolves <c>{page}</c> and <c>{pages}</c>
    /// tokens before rendering.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node positioning the text.</param>
    /// <param name="textBlock">The text block element to render.</param>
    public static void RenderTextBlock(RenderContext ctx, LayoutNode node, TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(textBlock.Text))
            return;

        // Resolve page tokens before rendering.
        string resolvedText = ResolveTokens(textBlock.Text, ctx.CurrentPage, ctx.TotalPages);

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;
        var font = Rendering.FontHelper.CreateFontFromElement(textBlock);
        var color = StyleResolver.GetFontColor(textBlock);
        var brush = new XSolidBrush(ColorConvert.ToXColor(color));
        var textAlign = ResolveTextAlign(textBlock);
        var format = CreateStringFormat(textAlign);

        double lineHeight = font.GetHeight();

        // Open structure element: P (paragraph) or H1-H6 based on heading level.
        if (sb != null)
        {
            var tag = textBlock.HeadingLevel switch
            {
                1 => PdfBlockLevelElementTag.Heading1,
                2 => PdfBlockLevelElementTag.Heading2,
                3 => PdfBlockLevelElementTag.Heading3,
                4 => PdfBlockLevelElementTag.Heading4,
                5 => PdfBlockLevelElementTag.Heading5,
                6 => PdfBlockLevelElementTag.Heading6,
                _ => PdfBlockLevelElementTag.Paragraph
            };
            sb.BeginElement(tag);
        }

        // Wrap the resolved text (not the raw token text) to ensure rendered
        // text matches the actual characters being drawn.
        var wrapResult = TextMeasurer.WrapText(resolvedText, font, node.Width);
        double cursorY = node.Y;

        for (int i = 0; i < wrapResult.Lines.Count; i++)
        {
            if (cursorY + lineHeight > node.Y + node.Height + 0.5)
                break;

            var lineRect = new XRect(node.X, cursorY, node.Width, lineHeight);
            gfx.DrawString(wrapResult.Lines[i], font, brush, lineRect, format);
            cursorY += lineHeight;
        }

        if (sb != null) sb.End();
    }

    /// <summary>
    /// Renders a <see cref="RichText"/> element using word-wrapping across span boundaries.
    /// Each line is rendered by drawing its span segments sequentially with per-span styling.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node positioning the rich text.</param>
    /// <param name="richText">The rich text element to render.</param>
    public static void RenderRichText(RenderContext ctx, LayoutNode node, RichText richText)
    {
        if (richText.Spans.Count == 0)
            return;

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;

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

        // Open P structure element for the entire rich text block.
        if (sb != null) sb.BeginElement(PdfBlockLevelElementTag.Paragraph);

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

                if (sb != null) sb.BeginElement(PdfInlineLevelElementTag.Span);
                gfx.DrawString(segment.Text, spanFont, spanBrush, segRect, XStringFormats.TopLeft);

                // Draw underline or strikethrough decorations.
                RenderDecorations(gfx, segment.Span.Style, spanFont, spanBrush,
                    cursorX, cursorY, segSize.Width, maxLineHeight);
                if (sb != null) sb.End(); // Span

                cursorX += segSize.Width;

                // Add a space between segments on the same line (words were split on spaces).
                if (segIdx < line.Count - 1)
                {
                    cursorX += TextMeasurer.MeasureString(" ", spanFont).Width;
                }
            }

            cursorY += maxLineHeight;
        }

        if (sb != null) sb.End(); // P
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Replaces page number tokens in text with actual values.
    /// Supported tokens: <c>{page}</c> (current page number), <c>{pages}</c> (total page count).
    /// Token matching is case-insensitive.
    /// </summary>
    /// <param name="text">The text potentially containing tokens.</param>
    /// <param name="currentPage">The 1-based current page number.</param>
    /// <param name="totalPages">The total number of pages in the document.</param>
    /// <returns>The text with all tokens replaced by their numeric values.</returns>
    private static string ResolveTokens(string text, int currentPage, int totalPages)
    {
        if (text.IndexOf('{') < 0)
            return text;

        // Replace {pages} before {page} to avoid partial match ({page} is a substring of {pages}).
        return text
            .Replace("{pages}", totalPages.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{page}", currentPage.ToString(), StringComparison.OrdinalIgnoreCase);
    }

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

/// <summary>
/// Result of word-wrapping a <see cref="TextBlock"/> to a constrained width.
/// </summary>
/// <param name="Lines">The wrapped text lines.</param>
/// <param name="TotalHeight">The total height in points of all wrapped lines.</param>
internal readonly record struct TextWrapResult(IReadOnlyList<string> Lines, double TotalHeight);

/// <summary>
/// Result of word-wrapping a <see cref="RichText"/> element to a constrained width.
/// Each line contains one or more <see cref="SpanSegment"/> entries representing the
/// portions of spans that fit on that line.
/// </summary>
/// <param name="Lines">The wrapped lines, each containing span segments.</param>
/// <param name="TotalHeight">The total height in points of all wrapped lines.</param>
internal readonly record struct RichTextWrapResult(
    IReadOnlyList<IReadOnlyList<SpanSegment>> Lines,
    double TotalHeight);

/// <summary>
/// A segment of a <see cref="Span"/> after word-wrapping. Contains the original span
/// (for style information) and the portion of text assigned to a particular line.
/// </summary>
/// <param name="Span">The source span for style/font information.</param>
/// <param name="Text">The text content for this segment of the span.</param>
internal readonly record struct SpanSegment(Span Span, string Text);
