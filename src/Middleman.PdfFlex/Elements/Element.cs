// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Abstract base class for all document elements. Provides style and parent-chain
/// support for cascading style resolution.
/// </summary>
public abstract class Element
{
    /// <summary>Gets or sets the style applied to this element. Null uses inherited/default styles.</summary>
    public Style? Style { get; set; }

    /// <summary>Gets or sets an optional identifier for this element.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the parent element. Set internally when added to a container.</summary>
    public Element? Parent { get; internal set; }
}
