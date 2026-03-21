// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies the Yoga/Flexbox-pattern flex distribution in Row and Column containers.
/// Flex-grow children start at flex-basis 0 and receive surplus space; non-flex children
/// retain their natural or explicit width.
/// </summary>
public class FlexDistributionTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.5;

    #region Row Flex Distribution

    [Fact]
    public void Row_FlexGrowChild_GetsRemainingSpaceAfterNonFlexChildren()
    {
        // Row(512pt) with Child1(FlexGrow=1) and Child2(explicit width 155pt).
        // Child2 gets 155pt, Child1 gets 512 - 155 = 357pt.
        var child1 = new Box(style: new Style { FlexGrow = 1 });
        var child2 = new Box(style: new Style { Width = Length.Pt(155) });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 512, 100);

        Assert.Equal(357, result.Children[0].Width, Tolerance);
        Assert.Equal(155, result.Children[1].Width, Tolerance);
    }

    [Fact]
    public void Row_NonFlexChild_GetsNaturalWidth_NotShrunk()
    {
        // Row(500pt) with Child1(FlexGrow=1) and Child2(explicit width 150pt).
        // Child2 keeps 150pt regardless of Child1.
        var child1 = new Box(style: new Style { FlexGrow = 1 });
        var child2 = new Box(style: new Style { Width = Length.Pt(150) });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 500, 100);

        Assert.Equal(150, result.Children[1].Width, Tolerance);
        Assert.Equal(350, result.Children[0].Width, Tolerance);
    }

    [Fact]
    public void Row_MultipleFlexGrow_DistributesProportionally()
    {
        // Row(600pt) with Child1(FlexGrow=1), Child2(FlexGrow=2), Child3(explicit width 100pt).
        // Remaining space: 600 - 100 = 500. Child1 = 500/3 = 166.67, Child2 = 1000/3 = 333.33.
        var child1 = new Box(style: new Style { FlexGrow = 1 });
        var child2 = new Box(style: new Style { FlexGrow = 2 });
        var child3 = new Box(style: new Style { Width = Length.Pt(100) });
        var row = new Row(new Element[] { child1, child2, child3 });

        var result = LayoutEngine.Calculate(row, 600, 100);

        Assert.Equal(100, result.Children[2].Width, Tolerance);
        Assert.Equal(166.67, result.Children[0].Width, Tolerance);
        Assert.Equal(333.33, result.Children[1].Width, Tolerance);
    }

    [Fact]
    public void Row_AllFlexGrow_SplitsEvenly()
    {
        // Row(400pt) with Child1(FlexGrow=1) and Child2(FlexGrow=1).
        // Each gets 200pt.
        var child1 = new Box(style: new Style { FlexGrow = 1 });
        var child2 = new Box(style: new Style { FlexGrow = 1 });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 400, 100);

        Assert.Equal(200, result.Children[0].Width, Tolerance);
        Assert.Equal(200, result.Children[1].Width, Tolerance);
    }

    [Fact]
    public void Row_NoFlexGrow_ChildrenGetNaturalWidth()
    {
        // Row(600pt) with Child1(explicit width 100pt) and Child2(explicit width 150pt).
        var child1 = new Box(style: new Style { Width = Length.Pt(100) });
        var child2 = new Box(style: new Style { Width = Length.Pt(150) });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 600, 100);

        Assert.Equal(100, result.Children[0].Width, Tolerance);
        Assert.Equal(150, result.Children[1].Width, Tolerance);
    }

    #endregion Row Flex Distribution

    #region Column Flex Distribution

    [Fact]
    public void Column_FlexGrow_DistributesVerticalSpace()
    {
        // Column(400pt height) with Child1(FlexGrow=1) and Child2(explicit height 100pt).
        // Child2 = 100pt, Child1 = 300pt.
        var child1 = new Box(style: new Style { FlexGrow = 1 });
        var child2 = new Box(style: new Style { Height = Length.Pt(100) });
        var col = new Column(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(col, 200, 400);

        Assert.Equal(100, result.Children[1].Height, Tolerance);
        Assert.Equal(300, result.Children[0].Height, Tolerance);
    }

    #endregion Column Flex Distribution
}
