// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Horizontal flex container that lays out children left-to-right along the main axis.
/// Supports justify (main-axis distribution), align (cross-axis alignment), and gap spacing.
/// </summary>
public class Row : Container
{
    #region Public Properties

    /// <summary>Gets how children are distributed along the horizontal main axis.</summary>
    public Justify Justify { get; }

    /// <summary>Gets how children are aligned along the vertical cross axis.</summary>
    public Align Align { get; }

    /// <summary>Gets the spacing in points between adjacent children.</summary>
    public double Gap { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a horizontal flex container with the specified children and layout options.</summary>
    /// <param name="children">The child elements to lay out horizontally.</param>
    /// <param name="justify">Main-axis distribution. Defaults to <see cref="Layout.Justify.Start"/>.</param>
    /// <param name="align">Cross-axis alignment. Defaults to <see cref="Layout.Align.Stretch"/>.</param>
    /// <param name="gap">Spacing between adjacent children in points. Defaults to 0.</param>
    /// <param name="style">Optional style to apply to this container.</param>
    public Row(
        IEnumerable<Element> children,
        Justify justify = Justify.Start,
        Align align = Align.Stretch,
        double gap = 0,
        Style? style = null)
        : base(children)
    {
        Justify = justify;
        Align = align;
        Gap = gap;
        Style = style;
    }

    /// <summary>Creates a horizontal flex container with default layout options.</summary>
    /// <param name="children">The child elements to lay out horizontally.</param>
    public Row(params Element[] children)
        : this((IEnumerable<Element>)children)
    {
    }

    #endregion Constructors
}
