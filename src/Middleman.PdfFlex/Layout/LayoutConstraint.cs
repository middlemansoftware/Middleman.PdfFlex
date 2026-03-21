// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Defines minimum and maximum size constraints for layout calculations.
/// All values are in points.
/// </summary>
public readonly struct LayoutConstraint : IEquatable<LayoutConstraint>
{
    /// <summary>Gets the minimum width in points.</summary>
    public double MinWidth { get; }

    /// <summary>Gets the maximum width in points.</summary>
    public double MaxWidth { get; }

    /// <summary>Gets the minimum height in points.</summary>
    public double MinHeight { get; }

    /// <summary>Gets the maximum height in points.</summary>
    public double MaxHeight { get; }

    /// <summary>Creates a layout constraint with explicit bounds.</summary>
    /// <param name="minWidth">The minimum width in points.</param>
    /// <param name="maxWidth">The maximum width in points.</param>
    /// <param name="minHeight">The minimum height in points.</param>
    /// <param name="maxHeight">The maximum height in points.</param>
    public LayoutConstraint(double minWidth, double maxWidth, double minHeight, double maxHeight)
    {
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }

    /// <summary>Gets an unconstrained layout constraint (zero minimum, infinite maximum).</summary>
    public static LayoutConstraint Unconstrained =>
        new(0, double.PositiveInfinity, 0, double.PositiveInfinity);

    /// <summary>Creates a constraint with a fixed maximum width and unconstrained height.</summary>
    /// <param name="maxWidth">The maximum width in points.</param>
    public static LayoutConstraint FromMaxWidth(double maxWidth) =>
        new(0, maxWidth, 0, double.PositiveInfinity);

    /// <summary>Creates a constraint with fixed maximum dimensions.</summary>
    /// <param name="maxWidth">The maximum width in points.</param>
    /// <param name="maxHeight">The maximum height in points.</param>
    public static LayoutConstraint FromMaxSize(double maxWidth, double maxHeight) =>
        new(0, maxWidth, 0, maxHeight);

    /// <summary>Creates a constraint with an exact fixed size.</summary>
    /// <param name="width">The exact width in points.</param>
    /// <param name="height">The exact height in points.</param>
    public static LayoutConstraint Exact(double width, double height) =>
        new(width, width, height, height);

    /// <summary>Clamps a width value to be within this constraint's bounds.</summary>
    /// <param name="width">The width to clamp.</param>
    public double ClampWidth(double width) =>
        Math.Clamp(width, MinWidth, MaxWidth);

    /// <summary>Clamps a height value to be within this constraint's bounds.</summary>
    /// <param name="height">The height to clamp.</param>
    public double ClampHeight(double height) =>
        Math.Clamp(height, MinHeight, MaxHeight);

    /// <inheritdoc />
    public bool Equals(LayoutConstraint other) =>
        MinWidth.Equals(other.MinWidth) &&
        MaxWidth.Equals(other.MaxWidth) &&
        MinHeight.Equals(other.MinHeight) &&
        MaxHeight.Equals(other.MaxHeight);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is LayoutConstraint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(MinWidth, MaxWidth, MinHeight, MaxHeight);

    /// <summary>Determines whether two <see cref="LayoutConstraint"/> values are equal.</summary>
    public static bool operator ==(LayoutConstraint left, LayoutConstraint right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="LayoutConstraint"/> values are not equal.</summary>
    public static bool operator !=(LayoutConstraint left, LayoutConstraint right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        $"LayoutConstraint(W: {MinWidth}-{MaxWidth}, H: {MinHeight}-{MaxHeight})";
}
