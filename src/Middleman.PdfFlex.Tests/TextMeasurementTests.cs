// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies text measurement using real PdfFlex font metrics via <see cref="TextMeasurer"/>.
/// No mocks: the TextMeasurer auto-initializes its thread-local XGraphics context.
/// </summary>
public class TextMeasurementTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.5;

    #region TextBlock Measurement

    [Fact]
    public void TextBlock_MeasuredWidth_IsPositive()
    {
        var tb = new TextBlock("Hello World", new FontSpec("Arial", 10));

        var (width, height) = TextMeasurer.MeasureSingleLine(tb);

        Assert.True(width > 0, $"Expected positive width, got {width}");
        Assert.True(height > 0, $"Expected positive height, got {height}");
    }

    [Fact]
    public void TextBlock_WordWraps_WhenExceedingContainerWidth()
    {
        // A long text string that exceeds 200pt at Arial 10pt should wrap.
        var tb = new TextBlock(
            "This is a fairly long sentence that should definitely exceed two hundred points of width when rendered at ten point Arial font size.",
            new FontSpec("Arial", 10));

        var singleLine = TextMeasurer.MeasureSingleLine(tb);
        var wrapResult = TextMeasurer.WrapTextBlock(tb, 200);

        Assert.True(singleLine.Width > 200, "Test text must be wider than 200pt unwrapped");
        Assert.True(wrapResult.TotalHeight > singleLine.Height,
            $"Wrapped height {wrapResult.TotalHeight} should exceed single-line height {singleLine.Height}");
    }

    [Fact]
    public void TextBlock_WrappedHeight_FeedsBackToParentLayout()
    {
        // A Column containing a long TextBlock with explicit width 200pt.
        // The TextBlock should wrap, and the Column height should reflect the full wrapped height.
        var longText = "This is a fairly long paragraph of text that should definitely wrap to multiple lines " +
                       "when constrained to only two hundred points of width at ten point Arial font size.";
        var tb = new TextBlock(longText, new FontSpec("Arial", 10));
        var col = new Column(new Element[] { tb }, style: new Style { Width = Length.Pt(200) });

        var result = LayoutEngine.Calculate(col, 200, 1e6);

        var singleLineResult = TextMeasurer.MeasureSingleLine(tb);
        Assert.True(result.Children[0].Height > singleLineResult.Height,
            $"TextBlock height {result.Children[0].Height} should exceed single-line height {singleLineResult.Height}");
    }

    [Fact]
    public void TextBlock_SingleWord_DoesNotWrap()
    {
        // "INVOICE" at 28pt Bold in 200pt container should stay on one line.
        var tb = new TextBlock("INVOICE", style: new Style { FontSize = 28, FontWeight = FontWeight.Bold });

        var singleLine = TextMeasurer.MeasureSingleLine(tb);
        var wrapResult = TextMeasurer.WrapTextBlock(tb, 200);

        Assert.Equal(singleLine.Height, wrapResult.TotalHeight, Tolerance);
    }

    [Fact]
    public void TextBlock_EmptyString_HasZeroSize()
    {
        var tb = new TextBlock("", new FontSpec("Arial", 10));

        var (width, height) = TextMeasurer.MeasureSingleLine(tb);

        Assert.Equal(0, width);
        Assert.Equal(0, height);
    }

    #endregion TextBlock Measurement

    #region RichText Measurement

    [Fact]
    public void RichText_SpansWrapAcrossBoundaries()
    {
        // Two spans that together exceed 200pt container width should wrap to multiple lines.
        var span1 = new Span("This is the first part of a long rich text paragraph ",
            new SpanStyle { FontSize = 10 });
        var span2 = new Span("and this is the second part that continues the sentence even further.",
            new SpanStyle { FontSize = 10 });
        var rt = new RichText(span1, span2);

        var singleLine = TextMeasurer.MeasureRichTextSingleLine(rt);
        var wrapResult = TextMeasurer.WrapRichText(rt, 200);

        Assert.True(singleLine.Width > 200, "Test text must be wider than 200pt unwrapped");
        Assert.True(wrapResult.TotalHeight > singleLine.Height,
            $"Wrapped height {wrapResult.TotalHeight} should exceed single-line height {singleLine.Height}");
    }

    [Fact]
    public void RichText_MixedFontSizes_LineHeightIsMaximum()
    {
        // Span at 10pt + Span at 20pt. Line height should be based on the tallest span.
        var smallSpan = new Span("Small text ", new SpanStyle { FontSize = 10 });
        var largeSpan = new Span("Large text", new SpanStyle { FontSize = 20 });
        var rt = new RichText(smallSpan, largeSpan);

        var (_, height) = TextMeasurer.MeasureRichTextSingleLine(rt);

        // The height should be close to the 20pt span's line height, not the 10pt span's.
        var smallOnly = new RichText(new Span("Small text", new SpanStyle { FontSize = 10 }));
        var (_, smallHeight) = TextMeasurer.MeasureRichTextSingleLine(smallOnly);

        Assert.True(height > smallHeight,
            $"Mixed-size line height {height} should exceed small-only height {smallHeight}");
    }

    #endregion RichText Measurement
}
