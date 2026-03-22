// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Describes the border of a rectangular element with independent sides and optional corner radius.
/// </summary>
public class Border
{
    #region Public Properties

    /// <summary>Gets the top border side.</summary>
    public BorderSide Top { get; }

    /// <summary>Gets the right border side.</summary>
    public BorderSide Right { get; }

    /// <summary>Gets the bottom border side.</summary>
    public BorderSide Bottom { get; }

    /// <summary>Gets the left border side.</summary>
    public BorderSide Left { get; }

    /// <summary>Gets the corner radius in points. Zero means no rounding.</summary>
    public double CornerRadius { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a border with individually specified sides.</summary>
    /// <param name="top">The top border side.</param>
    /// <param name="right">The right border side.</param>
    /// <param name="bottom">The bottom border side.</param>
    /// <param name="left">The left border side.</param>
    /// <param name="cornerRadius">The corner radius in points.</param>
    public Border(BorderSide top, BorderSide right, BorderSide bottom, BorderSide left, double cornerRadius = 0)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
        CornerRadius = cornerRadius;
    }

    #endregion Constructors

    #region Static Factories

    /// <summary>Creates a uniform border on all four sides.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    /// <param name="cornerRadius">The corner radius in points.</param>
    public static Border All(double width, Color color, BorderStyle style = BorderStyle.Solid, double cornerRadius = 0)
    {
        var side = new BorderSide(width, color, style);
        return new Border(side, side, side, side, cornerRadius);
    }

    /// <summary>Creates a border on the top and bottom sides only.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    public static Border Horizontal(double width, Color color, BorderStyle style = BorderStyle.Solid)
    {
        var side = new BorderSide(width, color, style);
        return new Border(side, BorderSide.None, side, BorderSide.None);
    }

    /// <summary>Creates a border on the left and right sides only.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    public static Border Vertical(double width, Color color, BorderStyle style = BorderStyle.Solid)
    {
        var side = new BorderSide(width, color, style);
        return new Border(BorderSide.None, side, BorderSide.None, side);
    }

    /// <summary>Creates a border on the bottom side only.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    public static Border BottomOnly(double width, Color color, BorderStyle style = BorderStyle.Solid) =>
        new(BorderSide.None, BorderSide.None, new BorderSide(width, color, style), BorderSide.None);

    /// <summary>Creates a border on the top side only.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    public static Border TopOnly(double width, Color color, BorderStyle style = BorderStyle.Solid) =>
        new(new BorderSide(width, color, style), BorderSide.None, BorderSide.None, BorderSide.None);

    /// <summary>Creates a border on the left side only.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    public static Border LeftOnly(double width, Color color, BorderStyle style = BorderStyle.Solid) =>
        new(BorderSide.None, BorderSide.None, BorderSide.None, new BorderSide(width, color, style));

    /// <summary>Creates a border on the right side only.</summary>
    /// <param name="width">The border width in points.</param>
    /// <param name="color">The border color.</param>
    /// <param name="style">The border line style.</param>
    public static Border RightOnly(double width, Color color, BorderStyle style = BorderStyle.Solid) =>
        new(BorderSide.None, new BorderSide(width, color, style), BorderSide.None, BorderSide.None);

    /// <summary>Gets a border with no visible sides.</summary>
    public static Border None { get; } = new(BorderSide.None, BorderSide.None, BorderSide.None, BorderSide.None);

    #endregion Static Factories
}
