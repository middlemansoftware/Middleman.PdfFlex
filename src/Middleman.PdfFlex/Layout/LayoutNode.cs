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

    /// <summary>Gets or sets the absolute X position in points.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the absolute Y position in points.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the computed width in points.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the computed height in points.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the zero-based page index this node appears on.</summary>
    public int Page { get; set; }

    /// <summary>Gets the child layout nodes.</summary>
    public List<LayoutNode> Children { get; }

    /// <summary>
    /// Cached text wrap result from <see cref="TextMeasurer"/>. Populated during
    /// <c>ResolveTextWrapping</c> and reused by <c>GetWrappedHeight</c> to avoid
    /// redundant text measurement at the same width.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="object"/> because it may hold either a <see cref="TextWrapResult"/>
    /// or a <see cref="RichTextWrapResult"/> depending on the source element type.
    /// </remarks>
    internal object? CachedWrapResult { get; set; }

    /// <summary>Creates a layout node for the specified source element.</summary>
    /// <param name="source">The element that this layout node represents.</param>
    public LayoutNode(Element source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
        Children = new List<LayoutNode>();
    }
}
