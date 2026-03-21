// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Pdf.Fonts;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;
using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Provides accurate text measurement using PdfSharp font metrics. Supports single-line
/// width/height measurement and word-wrapping to a constrained width.
/// </summary>
/// <remarks>
/// <para>Thread-safe: uses <see cref="ThreadLocal{T}"/> for the measurement graphics context
/// so no shared mutable state exists between threads.</para>
/// <para>The font resolver must be initialized via <see cref="FontRegistry.EnsureInitialized"/>
/// before calling any measurement method. This is done automatically on first use.</para>
/// </remarks>
internal static class TextMeasurer
{
    #region Constants

    /// <summary>Default line height multiplier when none is specified in the style.</summary>
    private const double DefaultLineHeightMultiplier = 1.2;

    #endregion Constants

    #region Fields

    /// <summary>
    /// Thread-local measurement context. Each thread gets its own <see cref="XGraphics"/>
    /// instance to avoid synchronization overhead on the hot measurement path.
    /// </summary>
    /// <remarks>
    /// Intentionally long-lived (process-lifetime). <see cref="XGraphics.CreateMeasureContext"/>
    /// creates lightweight measurement contexts, and this library is used for batch PDF
    /// generation rather than as a continuously-running service. The instances are cleaned
    /// up when the process exits. <c>trackAllValues</c> is enabled so that all per-thread
    /// instances are discoverable if explicit cleanup is ever needed.
    /// </remarks>
    private static readonly ThreadLocal<XGraphics> MeasureContext = new(
        () =>
        {
            FontRegistry.EnsureInitialized();
            return XGraphics.CreateMeasureContext(
                new XSize(1000, 1000),
                XGraphicsUnit.Point,
                XPageDirection.Downwards);
        },
        trackAllValues: true);

    #endregion Fields

    #region Public Methods

    /// <summary>
    /// Measures the width and height of a text string using the thread-local measurement
    /// context. Use this instead of <c>XGraphics.MeasureString</c> on a render-time graphics
    /// context to ensure layout and rendering use identical measurement.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to measure with.</param>
    /// <returns>The measured size in points.</returns>
    public static XSize MeasureString(string text, XFont font)
    {
        return MeasureContext.Value!.MeasureString(text, font);
    }

    /// <summary>
    /// Measures the single-line (unwrapped) width and height of a <see cref="TextBlock"/>.
    /// </summary>
    /// <param name="textBlock">The text block element to measure.</param>
    /// <returns>The intrinsic width (unwrapped single line) and height (single line) in points.</returns>
    public static (double Width, double Height) MeasureSingleLine(TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(textBlock.Text))
            return (0, 0);

        var font = FontHelper.CreateFontFromElement(textBlock);
        var gfx = MeasureContext.Value!;

        // Handle explicit newlines: the intrinsic width is the widest line,
        // and height accounts for all explicit lines.
        var lines = textBlock.Text.Split('\n');
        double maxWidth = 0;
        double lineHeight = font.GetHeight();

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length == 0)
                continue;

            var size = gfx.MeasureString(lines[i], font);
            if (size.Width > maxWidth)
                maxWidth = size.Width;
        }

        double totalHeight = lineHeight * lines.Length;
        return (maxWidth, totalHeight);
    }

    /// <summary>
    /// Measures the single-line (unwrapped) width and height of a <see cref="RichText"/> element
    /// by summing the measured widths of all spans.
    /// </summary>
    /// <param name="richText">The rich text element to measure.</param>
    /// <returns>The intrinsic width (unwrapped) and height (tallest line) in points.</returns>
    public static (double Width, double Height) MeasureRichTextSingleLine(RichText richText)
    {
        if (richText.Spans.Count == 0)
            return (0, 0);

        var gfx = MeasureContext.Value!;
        double totalWidth = 0;
        double maxLineHeight = 0;

        for (int i = 0; i < richText.Spans.Count; i++)
        {
            var span = richText.Spans[i];
            if (string.IsNullOrEmpty(span.Text))
                continue;

            var spanFont = FontHelper.CreateFontFromSpan(span.Style, richText);
            var size = gfx.MeasureString(span.Text, spanFont);
            totalWidth += size.Width;

            double spanLineHeight = spanFont.GetHeight();
            if (spanLineHeight > maxLineHeight)
                maxLineHeight = spanLineHeight;
        }

        return (totalWidth, maxLineHeight);
    }

    /// <summary>
    /// Word-wraps a <see cref="TextBlock"/>'s text to fit within the specified width
    /// and returns the resulting lines and total height.
    /// </summary>
    /// <param name="textBlock">The text block element to wrap.</param>
    /// <param name="availableWidth">The maximum width in points for each line.</param>
    /// <returns>The wrapped lines and the total height in points.</returns>
    public static TextWrapResult WrapTextBlock(TextBlock textBlock, double availableWidth)
    {
        if (string.IsNullOrEmpty(textBlock.Text))
            return new TextWrapResult(Array.Empty<string>(), 0);

        var font = FontHelper.CreateFontFromElement(textBlock);
        var gfx = MeasureContext.Value!;
        double lineHeight = font.GetHeight();

        // Split on explicit newlines first, then word-wrap each paragraph.
        var paragraphs = textBlock.Text.Split('\n');
        var wrappedLines = new List<string>();

        for (int p = 0; p < paragraphs.Length; p++)
        {
            var paragraph = paragraphs[p];
            if (paragraph.Length == 0)
            {
                wrappedLines.Add(string.Empty);
                continue;
            }

            WrapParagraph(gfx, font, paragraph, availableWidth, wrappedLines);
        }

        double totalHeight = lineHeight * wrappedLines.Count;
        return new TextWrapResult(wrappedLines, totalHeight);
    }

    /// <summary>
    /// Word-wraps a <see cref="RichText"/> element's spans to fit within the specified width
    /// and returns the resulting span runs organized by line, along with the total height.
    /// </summary>
    /// <param name="richText">The rich text element to wrap.</param>
    /// <param name="availableWidth">The maximum width in points for each line.</param>
    /// <returns>The wrapped lines (each containing one or more span segments) and the total height.</returns>
    public static RichTextWrapResult WrapRichText(RichText richText, double availableWidth)
    {
        if (richText.Spans.Count == 0)
            return new RichTextWrapResult(new List<List<SpanSegment>>(), 0);

        var gfx = MeasureContext.Value!;

        // Pre-calculate the line height as the tallest span's line height.
        double maxLineHeight = 0;
        for (int i = 0; i < richText.Spans.Count; i++)
        {
            var spanFont = FontHelper.CreateFontFromSpan(richText.Spans[i].Style, richText);
            double spanLineHeight = spanFont.GetHeight();
            if (spanLineHeight > maxLineHeight)
                maxLineHeight = spanLineHeight;
        }

        var lines = new List<List<SpanSegment>>();
        var currentLine = new List<SpanSegment>();
        double currentLineWidth = 0;

        for (int i = 0; i < richText.Spans.Count; i++)
        {
            var span = richText.Spans[i];
            if (string.IsNullOrEmpty(span.Text))
                continue;

            var spanFont = FontHelper.CreateFontFromSpan(span.Style, richText);

            // Handle explicit newlines within spans.
            var segments = span.Text.Split('\n');
            for (int s = 0; s < segments.Length; s++)
            {
                if (s > 0)
                {
                    // Explicit newline: finalize the current line and start a new one.
                    lines.Add(currentLine);
                    currentLine = new List<SpanSegment>();
                    currentLineWidth = 0;
                }

                string segment = segments[s];
                if (segment.Length == 0)
                    continue;

                // Word-wrap the segment within the available width.
                WrapSpanSegment(gfx, spanFont, span, segment, availableWidth,
                    ref currentLine, ref currentLineWidth, lines);
            }
        }

        // Add the last line if it has content.
        if (currentLine.Count > 0)
            lines.Add(currentLine);

        double totalHeight = maxLineHeight * lines.Count;
        return new RichTextWrapResult(lines, totalHeight);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Word-wraps a single paragraph (no embedded newlines) into lines that fit
    /// within the available width. Words are split on whitespace boundaries.
    /// </summary>
    private static void WrapParagraph(
        XGraphics gfx, XFont font, string paragraph, double availableWidth, List<string> output)
    {
        var words = SplitIntoWords(paragraph);
        if (words.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }

        int lineStart = 0;
        double lineWidth = 0;
        var spaceWidth = gfx.MeasureString(" ", font).Width;

        for (int i = 0; i < words.Length; i++)
        {
            double wordWidth = gfx.MeasureString(words[i], font).Width;
            double widthWithWord = lineWidth == 0
                ? wordWidth
                : lineWidth + spaceWidth + wordWidth;

            if (widthWithWord > availableWidth && lineWidth > 0)
            {
                // Emit the current line and start a new one with the current word.
                output.Add(JoinWords(words, lineStart, i));
                lineStart = i;
                lineWidth = wordWidth;
            }
            else
            {
                lineWidth = widthWithWord;
            }
        }

        // Emit the remaining words.
        if (lineStart < words.Length)
        {
            output.Add(JoinWords(words, lineStart, words.Length));
        }
    }

    /// <summary>
    /// Word-wraps a span segment into the current line and additional lines as needed.
    /// When a word does not fit on the current line, the current line is finalized and
    /// a new line is started.
    /// </summary>
    private static void WrapSpanSegment(
        XGraphics gfx,
        XFont spanFont,
        Span span,
        string text,
        double availableWidth,
        ref List<SpanSegment> currentLine,
        ref double currentLineWidth,
        List<List<SpanSegment>> lines)
    {
        var words = SplitIntoWords(text);
        if (words.Length == 0)
            return;

        var spaceWidth = gfx.MeasureString(" ", spanFont).Width;
        int segmentStart = 0;

        for (int i = 0; i < words.Length; i++)
        {
            double wordWidth = gfx.MeasureString(words[i], spanFont).Width;
            double widthWithWord = currentLineWidth == 0
                ? wordWidth
                : currentLineWidth + spaceWidth + wordWidth;

            if (widthWithWord > availableWidth && currentLineWidth > 0)
            {
                // Emit any accumulated words for this span on the current line.
                if (i > segmentStart)
                {
                    currentLine.Add(new SpanSegment(span, JoinWords(words, segmentStart, i)));
                }

                // Finalize the current line.
                lines.Add(currentLine);
                currentLine = new List<SpanSegment>();
                currentLineWidth = wordWidth;
                segmentStart = i;
            }
            else
            {
                currentLineWidth = widthWithWord;
            }
        }

        // Emit the remaining words for this span on the current line.
        if (segmentStart < words.Length)
        {
            currentLine.Add(new SpanSegment(span, JoinWords(words, segmentStart, words.Length)));
        }
    }

    /// <summary>
    /// Splits text into words on whitespace boundaries, preserving no empty entries.
    /// </summary>
    private static string[] SplitIntoWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Joins a range of words with single spaces.
    /// </summary>
    private static string JoinWords(string[] words, int start, int end)
    {
        if (end - start == 1)
            return words[start];

        return string.Join(' ', words[start..end]);
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
