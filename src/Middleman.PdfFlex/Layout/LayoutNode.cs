// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Represents a positioned element in the computed layout tree. Contains the
/// resolved position, size, page assignment, and child nodes.
/// </summary>
public class LayoutNode
{
    /// <summary>Gets the source element that produced this layout node.</summary>
    public Element Source { get; }

    /// <summary>Gets or sets the X position in points, relative to the parent.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the Y position in points, relative to the parent.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the computed width in points.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the computed height in points.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the zero-based page index this node appears on.</summary>
    public int Page { get; set; }

    /// <summary>Gets the child layout nodes.</summary>
    public List<LayoutNode> Children { get; }

    /// <summary>Creates a layout node for the specified source element.</summary>
    /// <param name="source">The element that this layout node represents.</param>
    public LayoutNode(Element source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
        Children = new List<LayoutNode>();
    }
}
