// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Represents insets (padding or margin) for each edge of a rectangular element.
/// All values are in points.
/// </summary>
public readonly struct EdgeInsets : IEquatable<EdgeInsets>
{
    /// <summary>Gets the top inset in points.</summary>
    public double Top { get; }

    /// <summary>Gets the right inset in points.</summary>
    public double Right { get; }

    /// <summary>Gets the bottom inset in points.</summary>
    public double Bottom { get; }

    /// <summary>Gets the left inset in points.</summary>
    public double Left { get; }

    /// <summary>Creates edge insets with the same value on all four sides.</summary>
    /// <param name="all">The uniform inset value in points.</param>
    public EdgeInsets(double all) : this(all, all, all, all) { }

    /// <summary>Creates edge insets with symmetric vertical and horizontal values.</summary>
    /// <param name="vertical">The top and bottom inset in points.</param>
    /// <param name="horizontal">The left and right inset in points.</param>
    public EdgeInsets(double vertical, double horizontal) : this(vertical, horizontal, vertical, horizontal) { }

    /// <summary>Creates edge insets with individual values for each side.</summary>
    /// <param name="top">The top inset in points.</param>
    /// <param name="right">The right inset in points.</param>
    /// <param name="bottom">The bottom inset in points.</param>
    /// <param name="left">The left inset in points.</param>
    public EdgeInsets(double top, double right, double bottom, double left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>Gets the total horizontal inset (Left + Right).</summary>
    public double HorizontalTotal => Left + Right;

    /// <summary>Gets the total vertical inset (Top + Bottom).</summary>
    public double VerticalTotal => Top + Bottom;

    /// <summary>Edge insets with all sides set to zero.</summary>
    public static readonly EdgeInsets Zero = new(0);

    /// <inheritdoc />
    public bool Equals(EdgeInsets other) =>
        Top.Equals(other.Top) &&
        Right.Equals(other.Right) &&
        Bottom.Equals(other.Bottom) &&
        Left.Equals(other.Left);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is EdgeInsets other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Top, Right, Bottom, Left);

    /// <summary>Determines whether two <see cref="EdgeInsets"/> values are equal.</summary>
    public static bool operator ==(EdgeInsets left, EdgeInsets right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="EdgeInsets"/> values are not equal.</summary>
    public static bool operator !=(EdgeInsets left, EdgeInsets right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        Top == Right && Right == Bottom && Bottom == Left
            ? $"EdgeInsets({Top})"
            : $"EdgeInsets({Top}, {Right}, {Bottom}, {Left})";
}
