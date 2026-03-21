// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Abstract base class for elements that contain child elements. Manages the
/// parent-child relationship and provides read-only access to children.
/// </summary>
public abstract class Container : Element
{
    /// <summary>Gets the child elements of this container.</summary>
    public IReadOnlyList<Element> Children { get; }

    /// <summary>Creates a container with the specified child elements.</summary>
    /// <param name="children">The child elements to add to this container.</param>
    protected Container(IEnumerable<Element> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        var list = children.ToList();
        foreach (var child in list)
        {
            child.Parent = this;
        }
        Children = list;
    }
}
