// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Specifies how children are distributed along the main axis of a flex container.
/// </summary>
public enum Justify
{
    /// <summary>Pack children toward the start of the main axis.</summary>
    Start,

    /// <summary>Pack children toward the end of the main axis.</summary>
    End,

    /// <summary>Center children along the main axis.</summary>
    Center,

    /// <summary>Distribute children with equal space between them.</summary>
    SpaceBetween,

    /// <summary>Distribute children with equal space around them.</summary>
    SpaceAround,

    /// <summary>Distribute children with equal space between and around them.</summary>
    SpaceEvenly
}
