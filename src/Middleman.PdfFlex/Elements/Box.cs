// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Generic styled container that holds zero or one child element. Used to apply visual
/// styling (background, border, padding) to content without introducing flex layout semantics.
/// </summary>
public class Box : Element
{
    #region Public Properties

    /// <summary>Gets the optional child element contained within this box.</summary>
    public Element? Child { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a styled box with an optional child element.</summary>
    /// <param name="child">The child element to contain, or null for an empty box.</param>
    /// <param name="style">Optional style to apply to this box.</param>
    public Box(Element? child = null, Style? style = null)
    {
        Child = child;
        Style = style;

        if (child != null)
        {
            child.Parent = this;
        }
    }

    #endregion Constructors
}
