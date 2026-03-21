// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex;

/// <summary>
/// Represents a page size with width and height in typographic points.
/// Provides standard paper size constants.
/// </summary>
public readonly struct PageSize : IEquatable<PageSize>
{
    /// <summary>Gets the page width in points.</summary>
    public double Width { get; }

    /// <summary>Gets the page height in points.</summary>
    public double Height { get; }

    /// <summary>Creates a page size with the specified dimensions in points.</summary>
    /// <param name="width">The width in points.</param>
    /// <param name="height">The height in points.</param>
    public PageSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>ISO A3 (297 x 420 mm).</summary>
    public static readonly PageSize A3 = new(841.89, 1190.55);

    /// <summary>ISO A4 (210 x 297 mm).</summary>
    public static readonly PageSize A4 = new(595.28, 841.89);

    /// <summary>ISO A5 (148 x 210 mm).</summary>
    public static readonly PageSize A5 = new(419.53, 595.28);

    /// <summary>US Letter (8.5 x 11 inches).</summary>
    public static readonly PageSize Letter = new(612, 792);

    /// <summary>US Legal (8.5 x 14 inches).</summary>
    public static readonly PageSize Legal = new(612, 1008);

    /// <summary>US Tabloid (11 x 17 inches).</summary>
    public static readonly PageSize Tabloid = new(792, 1224);

    /// <summary>Returns a new page size with width and height swapped (landscape orientation).</summary>
    public PageSize Landscape() => new(Height, Width);

    /// <inheritdoc />
    public bool Equals(PageSize other) =>
        Width.Equals(other.Width) && Height.Equals(other.Height);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PageSize other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Width, Height);

    /// <summary>Determines whether two <see cref="PageSize"/> values are equal.</summary>
    public static bool operator ==(PageSize left, PageSize right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="PageSize"/> values are not equal.</summary>
    public static bool operator !=(PageSize left, PageSize right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"{Width} x {Height} pt";
}
