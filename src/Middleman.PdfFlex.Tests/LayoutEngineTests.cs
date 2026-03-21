// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies the 3-phase layout engine: measure, resolve, and position.
/// Uses xUnit with a tolerance constant for floating-point comparisons.
/// </summary>
public class LayoutEngineTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.001;

    #region Row Layout

    [Fact]
    public void Row_TwoChildren_EqualFlexGrow_SplitsEvenly()
    {
        // Two children with equal flex-grow should each get half the available width.
        var child1 = new Box(style: new Style { FlexGrow = 1 });
        var child2 = new Box(style: new Style { FlexGrow = 1 });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(2, result.Children.Count);
        Assert.Equal(100, result.Children[0].Width, Tolerance);
        Assert.Equal(100, result.Children[1].Width, Tolerance);
    }

    [Fact]
    public void Row_FixedWidthChildren_PositionedCorrectly()
    {
        // Two fixed-width children should be placed at x=0 and x=80.
        var child1 = new Box(style: new Style { Width = Length.Pt(80) });
        var child2 = new Box(style: new Style { Width = Length.Pt(60) });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 300, 100);

        Assert.Equal(0, result.Children[0].X, Tolerance);
        Assert.Equal(80, result.Children[1].X, Tolerance);
        Assert.Equal(80, result.Children[0].Width, Tolerance);
        Assert.Equal(60, result.Children[1].Width, Tolerance);
    }

    [Fact]
    public void Row_WithGap_SpacingApplied()
    {
        // Children separated by a 10pt gap.
        var child1 = new Box(style: new Style { Width = Length.Pt(50) });
        var child2 = new Box(style: new Style { Width = Length.Pt(50) });
        var child3 = new Box(style: new Style { Width = Length.Pt(50) });
        var row = new Row(new Element[] { child1, child2, child3 }, gap: 10);

        var result = LayoutEngine.Calculate(row, 300, 100);

        Assert.Equal(0, result.Children[0].X, Tolerance);
        Assert.Equal(60, result.Children[1].X, Tolerance);  // 50 + 10
        Assert.Equal(120, result.Children[2].X, Tolerance); // 50 + 10 + 50 + 10
    }

    [Fact]
    public void Row_JustifySpaceBetween_DistributesEvenly()
    {
        // Three 40pt children in 200pt width with SpaceBetween.
        // Free space = 200 - 120 = 80. Between 3 items = 40 each.
        var child1 = new Box(style: new Style { Width = Length.Pt(40) });
        var child2 = new Box(style: new Style { Width = Length.Pt(40) });
        var child3 = new Box(style: new Style { Width = Length.Pt(40) });
        var row = new Row(new Element[] { child1, child2, child3 }, justify: Justify.SpaceBetween);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(0, result.Children[0].X, Tolerance);
        Assert.Equal(80, result.Children[1].X, Tolerance);   // 40 + 40
        Assert.Equal(160, result.Children[2].X, Tolerance);  // 40 + 40 + 40 + 40
    }

    [Fact]
    public void Row_JustifyCenter_CenteredChildren()
    {
        // One 60pt child centered in 200pt.
        // Free space = 200 - 60 = 140. Offset = 70.
        var child = new Box(style: new Style { Width = Length.Pt(60) });
        var row = new Row(new Element[] { child }, justify: Justify.Center);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(70, result.Children[0].X, Tolerance);
    }

    #endregion Row Layout

    #region Column Layout

    [Fact]
    public void Column_ThreeChildren_StackedVertically()
    {
        // Three fixed-height children should stack top-to-bottom.
        var child1 = new Box(style: new Style { Height = Length.Pt(30) });
        var child2 = new Box(style: new Style { Height = Length.Pt(40) });
        var child3 = new Box(style: new Style { Height = Length.Pt(50) });
        var col = new Column(new Element[] { child1, child2, child3 });

        var result = LayoutEngine.Calculate(col, 200, 300);

        Assert.Equal(3, result.Children.Count);
        Assert.Equal(0, result.Children[0].Y, Tolerance);
        Assert.Equal(30, result.Children[1].Y, Tolerance);
        Assert.Equal(70, result.Children[2].Y, Tolerance); // 30 + 40
    }

    [Fact]
    public void Column_AlignCenter_ChildrenCentered()
    {
        // A 60pt-wide child centered horizontally in 200pt column.
        var child = new Box(style: new Style { Width = Length.Pt(60), Height = Length.Pt(30) });
        var col = new Column(new Element[] { child }, align: Align.Center);

        var result = LayoutEngine.Calculate(col, 200, 300);

        Assert.Equal(70, result.Children[0].X, Tolerance); // (200 - 60) / 2
    }

    #endregion Column Layout

    #region Box Layout

    [Fact]
    public void Box_WithPadding_ChildInsetCorrectly()
    {
        // A box with 10pt padding on all sides containing a child.
        var child = new Box();
        var box = new Box(child, new Style { Padding = new EdgeInsets(10), Width = Length.Pt(100), Height = Length.Pt(100) });

        var result = LayoutEngine.Calculate(box, 200, 200);

        Assert.Single(result.Children);
        Assert.Equal(10, result.Children[0].X, Tolerance);
        Assert.Equal(10, result.Children[0].Y, Tolerance);
    }

    #endregion Box Layout

    #region Percentage and Flex

    [Fact]
    public void PercentWidth_ResolvesAgainstParent()
    {
        // A child with 50% width in a 200pt row should be 100pt.
        var child = new Box(style: new Style { Width = Length.Pct(50) });
        var row = new Row(new Element[] { child });

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(100, result.Children[0].Width, Tolerance);
    }

    [Fact]
    public void FlexShrink_ChildrenOverflow_ShrinkProportionally()
    {
        // Two 120pt children in a 200pt row. Both default flex-shrink=1.
        // Total = 240, surplus = -40. Equal sizes, so each shrinks by 20.
        var child1 = new Box(style: new Style { Width = Length.Pt(120) });
        var child2 = new Box(style: new Style { Width = Length.Pt(120) });
        var row = new Row(new Element[] { child1, child2 });

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(100, result.Children[0].Width, Tolerance);
        Assert.Equal(100, result.Children[1].Width, Tolerance);
    }

    #endregion Percentage and Flex

    #region Nested Layout

    [Fact]
    public void Nested_RowInColumn_LayoutsCorrectly()
    {
        // A column with a row inside. The row has two flex-grow children.
        var rowChild1 = new Box(style: new Style { FlexGrow = 1 });
        var rowChild2 = new Box(style: new Style { FlexGrow = 1 });
        var innerRow = new Row(new Element[] { rowChild1, rowChild2 }, style: new Style { Height = Length.Pt(50) });
        var col = new Column(new Element[] { innerRow });

        var result = LayoutEngine.Calculate(col, 200, 300);

        // The row should be 200pt wide (stretched by column's Align.Stretch default).
        var rowNode = result.Children[0];
        Assert.Equal(200, rowNode.Width, Tolerance);
        Assert.Equal(50, rowNode.Height, Tolerance);

        // Each row child should be 100pt wide (half of 200).
        Assert.Equal(2, rowNode.Children.Count);
        Assert.Equal(100, rowNode.Children[0].Width, Tolerance);
        Assert.Equal(100, rowNode.Children[1].Width, Tolerance);
    }

    #endregion Nested Layout

    #region Spacer

    [Fact]
    public void Spacer_PushesContentApart()
    {
        // A row with a 50pt box, a spacer, and another 50pt box in 200pt.
        // The spacer should consume the remaining 100pt, pushing the second box to x=150.
        var left = new Box(style: new Style { Width = Length.Pt(50) });
        var spacer = new Spacer();
        var right = new Box(style: new Style { Width = Length.Pt(50) });
        var row = new Row(new Element[] { left, spacer, right });

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(0, result.Children[0].X, Tolerance);
        Assert.Equal(50, result.Children[0].Width, Tolerance);

        // Spacer gets the remaining 100pt.
        Assert.Equal(100, result.Children[1].Width, Tolerance);

        // Right box at 50 + 100 = 150.
        Assert.Equal(150, result.Children[2].X, Tolerance);
        Assert.Equal(50, result.Children[2].Width, Tolerance);
    }

    #endregion Spacer

    #region Edge Cases

    [Fact]
    public void EmptyRow_ReturnsZeroSizeChildren()
    {
        var row = new Row(Array.Empty<Element>());
        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Empty(result.Children);
        Assert.Equal(200, result.Width, Tolerance);
        Assert.Equal(100, result.Height, Tolerance);
    }

    [Fact]
    public void EmptyColumn_ReturnsZeroSizeChildren()
    {
        var col = new Column(Array.Empty<Element>());
        var result = LayoutEngine.Calculate(col, 200, 100);

        Assert.Empty(result.Children);
        Assert.Equal(200, result.Width, Tolerance);
        Assert.Equal(100, result.Height, Tolerance);
    }

    [Fact]
    public void EmptyBox_HasZeroIntrinsicSize()
    {
        var box = new Box();
        var result = LayoutEngine.Calculate(box, 200, 100);

        Assert.Empty(result.Children);
    }

    [Fact]
    public void Row_JustifyEnd_ChildrenAtEnd()
    {
        var child = new Box(style: new Style { Width = Length.Pt(60) });
        var row = new Row(new Element[] { child }, justify: Justify.End);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(140, result.Children[0].X, Tolerance); // 200 - 60
    }

    [Fact]
    public void Row_JustifySpaceAround_EqualSpaceAroundItems()
    {
        // Two 40pt children in 200pt. Free = 120. Per item = 60. Half = 30 at start.
        var child1 = new Box(style: new Style { Width = Length.Pt(40) });
        var child2 = new Box(style: new Style { Width = Length.Pt(40) });
        var row = new Row(new Element[] { child1, child2 }, justify: Justify.SpaceAround);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(30, result.Children[0].X, Tolerance);
        Assert.Equal(130, result.Children[1].X, Tolerance); // 30 + 40 + 60
    }

    [Fact]
    public void Row_JustifySpaceEvenly_EqualSpaceBetweenAndAround()
    {
        // Two 40pt children in 200pt. Free = 120. Slots = 3. Each = 40.
        var child1 = new Box(style: new Style { Width = Length.Pt(40) });
        var child2 = new Box(style: new Style { Width = Length.Pt(40) });
        var row = new Row(new Element[] { child1, child2 }, justify: Justify.SpaceEvenly);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(40, result.Children[0].X, Tolerance);
        Assert.Equal(120, result.Children[1].X, Tolerance); // 40 + 40 + 40
    }

    [Fact]
    public void Row_AlignEnd_ChildAtBottom()
    {
        // A 30pt-tall child in a 100pt-tall row aligned to end.
        var child = new Box(style: new Style { Width = Length.Pt(50), Height = Length.Pt(30) });
        var row = new Row(new Element[] { child }, align: Align.End);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(70, result.Children[0].Y, Tolerance); // 100 - 30
    }

    [Fact]
    public void Row_AlignCenter_ChildVerticallyCentered()
    {
        var child = new Box(style: new Style { Width = Length.Pt(50), Height = Length.Pt(40) });
        var row = new Row(new Element[] { child }, align: Align.Center);

        var result = LayoutEngine.Calculate(row, 200, 100);

        Assert.Equal(30, result.Children[0].Y, Tolerance); // (100 - 40) / 2
    }

    #endregion Edge Cases
}
