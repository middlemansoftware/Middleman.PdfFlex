// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Specifies how a child is aligned along the cross axis of a flex container.
/// </summary>
public enum Align
{
    /// <summary>Align to the start of the cross axis.</summary>
    Start,

    /// <summary>Align to the end of the cross axis.</summary>
    End,

    /// <summary>Center along the cross axis.</summary>
    Center,

    /// <summary>Stretch to fill the cross axis.</summary>
    Stretch
}
