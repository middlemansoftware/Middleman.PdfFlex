// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies style cascade, border construction, padding effects on layout,
/// and EdgeInsets value construction.
/// </summary>
public class StylingTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.5;

    #region Style Cascade

    [Fact]
    public void Style_Cascade_ChildOverridesParent()
    {
        // Parent Column with FontSize=12, child TextBlock with FontSize=10.
        // Measure the child at FontSize=10 and at FontSize=12 to confirm they differ.
        var childTb = new TextBlock("Hello World", style: new Style { FontSize = 10 });
        var parentTb = new TextBlock("Hello World", style: new Style { FontSize = 12 });

        var childSize = TextMeasurer.MeasureSingleLine(childTb);
        var parentSize = TextMeasurer.MeasureSingleLine(parentTb);

        // The child at 10pt should have a smaller height than the parent at 12pt.
        Assert.True(childSize.Height < parentSize.Height,
            $"Child at 10pt ({childSize.Height}) should be shorter than parent at 12pt ({parentSize.Height})");
    }

    #endregion Style Cascade

    #region Border Construction

    [Fact]
    public void Border_All_SetsAllFourSides()
    {
        var border = Border.All(1, Colors.Black);

        Assert.Equal(1, border.Top.Width);
        Assert.Equal(1, border.Right.Width);
        Assert.Equal(1, border.Bottom.Width);
        Assert.Equal(1, border.Left.Width);
        Assert.Equal(Colors.Black, border.Top.Color);
        Assert.Equal(Colors.Black, border.Right.Color);
        Assert.Equal(Colors.Black, border.Bottom.Color);
        Assert.Equal(Colors.Black, border.Left.Color);
    }

    #endregion Border Construction

    #region Padding Layout Effects

    [Fact]
    public void Padding_AffectsChildPosition()
    {
        // Box(padding=10) with child. The child X should be offset by 10 from the Box X.
        var child = new Box();
        var box = new Box(child, new Style { Padding = new EdgeInsets(10), Width = Length.Pt(100), Height = Length.Pt(100) });

        var result = LayoutEngine.Calculate(box, 200, 200);

        Assert.Single(result.Children);
        Assert.Equal(10, result.Children[0].X, Tolerance);
        Assert.Equal(10, result.Children[0].Y, Tolerance);
    }

    #endregion Padding Layout Effects

    #region EdgeInsets Construction

    [Fact]
    public void EdgeInsets_SingleValue_SetsAllSides()
    {
        var insets = new EdgeInsets(10);

        Assert.Equal(10, insets.Top);
        Assert.Equal(10, insets.Right);
        Assert.Equal(10, insets.Bottom);
        Assert.Equal(10, insets.Left);
    }

    [Fact]
    public void EdgeInsets_FourValues_SetsIndividually()
    {
        var insets = new EdgeInsets(1, 2, 3, 4);

        Assert.Equal(1, insets.Top);
        Assert.Equal(2, insets.Right);
        Assert.Equal(3, insets.Bottom);
        Assert.Equal(4, insets.Left);
    }

    #endregion EdgeInsets Construction
}
