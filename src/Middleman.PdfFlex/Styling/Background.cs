// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Describes the background fill of an element. Currently supports solid color backgrounds.
/// </summary>
public class Background
{
    /// <summary>Gets the background color.</summary>
    public Color Color { get; }

    /// <summary>Creates a background with the specified solid color.</summary>
    /// <param name="color">The background color.</param>
    public Background(Color color)
    {
        Color = color;
    }

    /// <summary>Creates a solid color background.</summary>
    /// <param name="color">The background color.</param>
    public static Background FromColor(Color color) => new(color);
}
