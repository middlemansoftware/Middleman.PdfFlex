// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Describes a single side of a border, including its width, color, and line style.
/// </summary>
/// <param name="Width">The border width in points.</param>
/// <param name="Color">The border color.</param>
/// <param name="Style">The border line style.</param>
public record BorderSide(double Width, Color Color, BorderStyle Style)
{
    /// <summary>A border side with no visible border.</summary>
    public static readonly BorderSide None = new(0, Colors.Transparent, BorderStyle.None);
}
