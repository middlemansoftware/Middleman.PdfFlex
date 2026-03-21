// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies the two-pass Yoga layout pattern: widths resolve top-down in the first pass,
/// then text is word-wrapped at the resolved width and heights cascade back up.
/// </summary>
public class TwoPassLayoutTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.5;

    #region Two-Pass Width/Height Resolution

    [Fact]
    public void Row_WidthResolvedBeforeHeight()
    {
        // A Row with a TextBlock that wraps at the resolved width.
        // The height should reflect the wrapped line count, not the single-line height.
        var longText = "This paragraph of text is intentionally long enough that it will definitely need " +
                       "to wrap across multiple lines when constrained to a two hundred point wide container.";
        var tb = new TextBlock(longText, new FontSpec("Arial", 10));
        var row = new Row(new Element[] { tb }, style: new Style { Width = Length.Pt(200) });

        var result = LayoutEngine.Calculate(row, 200, 1e6);

        var singleLine = TextMeasurer.MeasureSingleLine(tb);
        Assert.True(result.Children[0].Height > singleLine.Height,
            $"TextBlock height {result.Children[0].Height} should exceed single-line height {singleLine.Height}");
    }

    [Fact]
    public void Column_ChildHeights_RecomputedAfterWidthResolution()
    {
        // Column(300pt wide) containing a long TextBlock. The TextBlock wraps at 300pt,
        // so its height is multi-line.
        var longText = "This is another paragraph of text that is long enough to wrap when rendered at ten " +
                       "point Arial inside a three hundred point column container during the layout pass.";
        var tb = new TextBlock(longText, new FontSpec("Arial", 10));
        var col = new Column(new Element[] { tb });

        var result = LayoutEngine.Calculate(col, 300, 1e6);

        var singleLine = TextMeasurer.MeasureSingleLine(tb);

        // If the text wraps at 300pt, the TextBlock node height should exceed a single line.
        if (singleLine.Width > 300)
        {
            Assert.True(result.Children[0].Height > singleLine.Height,
                $"TextBlock height {result.Children[0].Height} should exceed single-line height {singleLine.Height}");
        }
    }

    [Fact]
    public void NestedContainers_TwoPassPropagatesCorrectly()
    {
        // Row > Column > TextBlock chain.
        // Widths cascade down (Row width -> Column width -> TextBlock wrap width).
        // Heights cascade back up (TextBlock wrapped height -> Column height -> Row height).
        var longText = "This text is written specifically to be long enough to word wrap when constrained " +
                       "to a narrow column width of roughly one hundred and fifty points at ten point font.";
        var tb = new TextBlock(longText, new FontSpec("Arial", 10));
        var innerCol = new Column(new Element[] { tb }, style: new Style { Width = Length.Pt(150) });
        var row = new Row(new Element[] { innerCol });

        var result = LayoutEngine.Calculate(row, 400, 1e6);

        var singleLine = TextMeasurer.MeasureSingleLine(tb);

        // The inner Column should be 150pt wide.
        Assert.Equal(150, result.Children[0].Width, Tolerance);

        // The TextBlock should have wrapped, giving multi-line height.
        var tbNode = result.Children[0].Children[0];
        if (singleLine.Width > 150)
        {
            Assert.True(tbNode.Height > singleLine.Height,
                $"Nested TextBlock height {tbNode.Height} should exceed single-line height {singleLine.Height}");
        }
    }

    #endregion Two-Pass Width/Height Resolution
}
