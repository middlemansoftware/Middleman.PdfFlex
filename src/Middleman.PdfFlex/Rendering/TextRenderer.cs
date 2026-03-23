// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf.Structure;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="TextBlock"/> and <see cref="RichText"/> elements.
/// Handles text alignment, explicit newlines, per-span styling for rich text,
/// link annotations, and anchor page token resolution.
/// </summary>
internal static class TextRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders a <see cref="TextBlock"/> element within the bounds of the specified layout node.
    /// Uses word wrapping to break text into lines that fit within the node's width,
    /// matching the layout engine's text measurement. Resolves <c>{page}</c>, <c>{pages}</c>,
    /// and <c>{page:id}</c> tokens before rendering.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node positioning the text.</param>
    /// <param name="textBlock">The text block element to render.</param>
    public static void RenderTextBlock(RenderContext ctx, LayoutNode node, TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(textBlock.Text))
            return;

        // Resolve page tokens before rendering.
        string resolvedText = ResolveTokens(textBlock.Text, ctx.CurrentPage, ctx.TotalPages, ctx.AnchorRegistry);

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;
        var font = Rendering.FontHelper.CreateFontFromElement(textBlock);
        var color = StyleResolver.GetFontColor(textBlock);
        var brush = new XSolidBrush(ColorConvert.ToXColor(color));
        var textAlign = ResolveTextAlign(textBlock);
        var format = CreateStringFormat(textAlign);

        double lineHeight = font.GetHeight();
        string? linkTarget = textBlock.LinkTarget;
        bool hasLink = !string.IsNullOrEmpty(linkTarget);

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

        // Create link annotation covering the entire text block area.
        if (hasLink && ctx.Page != null)
        {
            var linkRect = new XRect(node.X, node.Y, node.Width,
                Math.Min(cursorY - node.Y, node.Height));

            if (DocumentRenderer.IsExternalLink(linkTarget!))
            {
                DocumentRenderer.CreateUriLinkAnnotation(
                    ctx.Page, linkRect, linkTarget!, ctx.PageHeight, sb, resolvedText);
            }
            else
            {
                // Create the /Link structure element now while the element stack
                // is correctly positioned (inside the P/H block). The annotation
                // will be associated with this element during deferred resolution.
                PdfStructureElement? linkSte = sb?.CreateLinkStructureElement();
                DocumentRenderer.QueueInternalLink(ctx, linkRect, linkTarget!, resolvedText, linkSte);
            }
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

                // Check for span-level link target.
                string? spanLinkTarget = segment.Span.Style?.LinkTarget;

                if (sb != null) sb.BeginElement(PdfInlineLevelElementTag.Span);
                gfx.DrawString(segment.Text, spanFont, spanBrush, segRect, XStringFormats.TopLeft);

                // Draw underline or strikethrough decorations.
                RenderDecorations(gfx, segment.Span.Style, spanFont, spanBrush,
                    cursorX, cursorY, segSize.Width, maxLineHeight);
                if (sb != null) sb.End(); // Span

                // Create link annotation for this span segment.
                if (!string.IsNullOrEmpty(spanLinkTarget) && ctx.Page != null)
                {
                    if (DocumentRenderer.IsExternalLink(spanLinkTarget))
                    {
                        DocumentRenderer.CreateUriLinkAnnotation(
                            ctx.Page, segRect, spanLinkTarget, ctx.PageHeight, sb, segment.Text);
                    }
                    else
                    {
                        // Create the /Link structure element now while the element
                        // stack is correctly positioned (inside the P block).
                        PdfStructureElement? linkSte = sb?.CreateLinkStructureElement();
                        DocumentRenderer.QueueInternalLink(ctx, segRect, spanLinkTarget, segment.Text, linkSte);
                    }
                }

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

    #region Internal Methods

    /// <summary>
    /// Replaces page number tokens in text with actual values.
    /// Supported tokens: <c>{page}</c> (current page number), <c>{pages}</c> (total page count),
    /// <c>{page:id}</c> (page number of the element with the specified Id).
    /// Token matching is case-insensitive.
    /// </summary>
    /// <param name="text">The text potentially containing tokens.</param>
    /// <param name="currentPage">The 1-based current page number.</param>
    /// <param name="totalPages">The total number of pages in the document.</param>
    /// <param name="anchorRegistry">The optional anchor registry for resolving {page:id} tokens.</param>
    /// <returns>The text with all tokens replaced by their numeric values.</returns>
    internal static string ResolveTokens(string text, int currentPage, int totalPages,
        AnchorRegistry? anchorRegistry = null)
    {
        if (text.IndexOf('{') < 0)
            return text;

        // Replace {pages} before {page} to avoid partial match ({page} is a substring of {pages}).
        text = text
            .Replace("{pages}", totalPages.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{page}", currentPage.ToString(), StringComparison.OrdinalIgnoreCase);

        // Resolve {page:id} tokens if anchor registry is available.
        if (anchorRegistry != null)
        {
            text = ResolveAnchorPageTokens(text, anchorRegistry);
        }

        return text;
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Resolves <c>{page:id}</c> tokens by looking up the anchor page number
    /// in the registry. Unresolved tokens are replaced with "?".
    /// </summary>
    private static string ResolveAnchorPageTokens(string text, AnchorRegistry registry)
    {
        int startIdx = 0;
        while (startIdx < text.Length)
        {
            // Find the next {page: token (case-insensitive).
            int tokenStart = text.IndexOf("{page:", startIdx, StringComparison.OrdinalIgnoreCase);
            if (tokenStart < 0)
                break;

            int idStart = tokenStart + 6; // Length of "{page:"
            int tokenEnd = text.IndexOf('}', idStart);
            if (tokenEnd < 0)
                break;

            string anchorId = text.Substring(idStart, tokenEnd - idStart);
            string replacement;

            if (registry.TryGetPage(anchorId, out int page))
            {
                replacement = page.ToString();
            }
            else
            {
                replacement = "?";
            }

            text = string.Concat(text.AsSpan(0, tokenStart), replacement, text.AsSpan(tokenEnd + 1));
            startIdx = tokenStart + replacement.Length;
        }

        return text;
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
