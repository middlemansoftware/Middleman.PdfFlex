// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Describes a complete font specification including family, size, weight, and style.
/// </summary>
public class FontSpec
{
    /// <summary>Gets the font family name.</summary>
    public string Family { get; }

    /// <summary>Gets the font size in points.</summary>
    public double Size { get; }

    /// <summary>Gets the font weight.</summary>
    public FontWeight Weight { get; }

    /// <summary>Gets the font style.</summary>
    public FontStyle Style { get; }

    /// <summary>Creates a new font specification.</summary>
    /// <param name="family">The font family name.</param>
    /// <param name="size">The font size in points.</param>
    /// <param name="weight">The font weight. Defaults to <see cref="FontWeight.Normal"/>.</param>
    /// <param name="style">The font style. Defaults to <see cref="FontStyle.Normal"/>.</param>
    public FontSpec(
        string family,
        double size,
        FontWeight weight = FontWeight.Normal,
        FontStyle style = FontStyle.Normal)
    {
        ArgumentNullException.ThrowIfNull(family);
        Family = family;
        Size = size;
        Weight = weight;
        Style = style;
    }

    /// <summary>Gets the default font specification (NotoSans, 10pt, Normal weight, Normal style).</summary>
    public static FontSpec Default { get; } = new("NotoSans", 10, FontWeight.Normal, FontStyle.Normal);

    /// <inheritdoc />
    public override string ToString() => $"{Family} {Size}pt {Weight} {Style}";
}
