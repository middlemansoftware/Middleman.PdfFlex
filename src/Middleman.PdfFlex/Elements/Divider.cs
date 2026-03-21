// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A horizontal or vertical line element used as a visual separator between content.
/// </summary>
public class Divider : Element
{
    #region Public Properties

    /// <summary>Gets the line thickness in points.</summary>
    public double Thickness { get; }

    /// <summary>Gets the line color.</summary>
    public Color Color { get; }

    /// <summary>Gets whether this divider is vertical. When false, the divider is horizontal.</summary>
    public bool IsVertical { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a divider line with the specified orientation and appearance.</summary>
    /// <param name="thickness">The line thickness in points. Defaults to 1.</param>
    /// <param name="color">The line color. Defaults to <see cref="Colors.Black"/>.</param>
    /// <param name="isVertical">True for a vertical divider, false for horizontal. Defaults to false.</param>
    /// <param name="style">Optional style to apply to this divider.</param>
    public Divider(double thickness = 1, Color? color = null, bool isVertical = false, Style? style = null)
    {
        Thickness = thickness;
        Color = color ?? Colors.Black;
        IsVertical = isVertical;
        Style = style;
    }

    #endregion Constructors
}
