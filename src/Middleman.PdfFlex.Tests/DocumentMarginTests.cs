// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies that document margins correctly reduce the available content area and
/// offset child positions. These tests reproduce the layout path that DocumentRenderer
/// uses: wrapping children in a Column and calling LayoutEngine.Calculate with the
/// content width, unlimited height, and margin offsets as the origin.
/// </summary>
public class DocumentMarginTests
{
    /// <summary>Floating-point comparison tolerance in points.</summary>
    private const double Tolerance = 0.5;

    #region Document Margin Layout

    [Fact]
    public void Document_Margins_ReduceAvailableContentWidth()
    {
        // Letter = 612 x 792. Margins = 50 on all sides.
        // Content width = 612 - 50 - 50 = 512.
        double pageWidth = PageSize.Letter.Width;
        var margins = new EdgeInsets(50);
        double contentWidth = pageWidth - margins.HorizontalTotal;

        Assert.Equal(512, contentWidth, Tolerance);
    }

    [Fact]
    public void Document_Margins_ReduceAvailableContentHeight()
    {
        // Letter = 612 x 792. Margins = 50 on all sides.
        // Content height = 792 - 50 - 50 = 692.
        double pageHeight = PageSize.Letter.Height;
        var margins = new EdgeInsets(50);
        double contentHeight = pageHeight - margins.VerticalTotal;

        Assert.Equal(692, contentHeight, Tolerance);
    }

    [Fact]
    public void Document_Margins_OffsetContentOrigin()
    {
        // With 50pt margins, the first child should start at (50, 50).
        var margins = new EdgeInsets(50);
        double contentWidth = PageSize.Letter.Width - margins.HorizontalTotal;
        var child = new Box(style: new Style { Height = Length.Pt(30) });
        var wrapper = new Column(new Element[] { child });

        var rootNode = LayoutEngine.Calculate(wrapper, contentWidth, 1e6, margins.Left, margins.Top);

        Assert.Equal(50, rootNode.Children[0].X, Tolerance);
        Assert.Equal(50, rootNode.Children[0].Y, Tolerance);
    }

    [Fact]
    public void Document_Margins_ContentDoesNotExceedRightMargin()
    {
        // A FlexGrow child in a Row should not extend past pageWidth - marginRight.
        var margins = new EdgeInsets(50);
        double pageWidth = PageSize.Letter.Width;
        double contentWidth = pageWidth - margins.HorizontalTotal;
        var child = new Box(style: new Style { FlexGrow = 1 });
        var row = new Row(new Element[] { child });
        var wrapper = new Column(new Element[] { row });

        var rootNode = LayoutEngine.Calculate(wrapper, contentWidth, 1e6, margins.Left, margins.Top);

        var rowNode = rootNode.Children[0];
        var childNode = rowNode.Children[0];
        double rightEdge = childNode.X + childNode.Width;

        Assert.True(rightEdge <= pageWidth - margins.Right + Tolerance,
            $"Child right edge {rightEdge} exceeds right margin boundary {pageWidth - margins.Right}");
    }

    [Fact]
    public void Document_AsymmetricMargins_Respected()
    {
        // Asymmetric margins: top=30, right=40, bottom=50, left=60.
        var margins = new EdgeInsets(30, 40, 50, 60);
        double pageWidth = PageSize.Letter.Width;
        double contentWidth = pageWidth - margins.HorizontalTotal; // 612 - 100 = 512
        var child = new Box(style: new Style { Height = Length.Pt(30) });
        var wrapper = new Column(new Element[] { child });

        var rootNode = LayoutEngine.Calculate(wrapper, contentWidth, 1e6, margins.Left, margins.Top);

        // Child X should start at left margin (60), Y at top margin (30).
        Assert.Equal(60, rootNode.Children[0].X, Tolerance);
        Assert.Equal(30, rootNode.Children[0].Y, Tolerance);

        // Content width should be page width minus left and right margins.
        Assert.Equal(512, contentWidth, Tolerance);

        // Content height should be page height minus top and bottom margins.
        double contentHeight = PageSize.Letter.Height - margins.VerticalTotal;
        Assert.Equal(712, contentHeight, Tolerance);
    }

    #endregion Document Margin Layout
}
