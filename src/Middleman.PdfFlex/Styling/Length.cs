// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Represents a measurement value with a specific unit. Supports absolute units
/// (points, millimeters, inches, centimeters), relative units (percent, fractional),
/// and auto sizing.
/// </summary>
public readonly struct Length : IEquatable<Length>
{
    /// <summary>
    /// The unit of measurement for a <see cref="Length"/> value.
    /// </summary>
    public enum Unit
    {
        /// <summary>Typographic points (1/72 inch).</summary>
        Point,

        /// <summary>Millimeters.</summary>
        Millimeter,

        /// <summary>Inches.</summary>
        Inch,

        /// <summary>Centimeters.</summary>
        Centimeter,

        /// <summary>Percentage of the parent container's corresponding dimension.</summary>
        Percent,

        /// <summary>Automatic sizing based on content.</summary>
        Auto,

        /// <summary>Fractional unit for distributing remaining space.</summary>
        Fr
    }

    /// <summary>Gets the numeric value of this length.</summary>
    public double Value { get; }

    /// <summary>Gets the unit of measurement.</summary>
    public Unit Type { get; }

    private Length(double value, Unit type)
    {
        Value = value;
        Type = type;
    }

    /// <summary>Creates a length in typographic points.</summary>
    /// <param name="v">The value in points.</param>
    public static Length Pt(double v) => new(v, Unit.Point);

    /// <summary>Creates a length in millimeters.</summary>
    /// <param name="v">The value in millimeters.</param>
    public static Length Mm(double v) => new(v, Unit.Millimeter);

    /// <summary>Creates a length in inches.</summary>
    /// <param name="v">The value in inches.</param>
    public static Length In(double v) => new(v, Unit.Inch);

    /// <summary>Creates a length in centimeters.</summary>
    /// <param name="v">The value in centimeters.</param>
    public static Length Cm(double v) => new(v, Unit.Centimeter);

    /// <summary>Creates a percentage length.</summary>
    /// <param name="v">The percentage value (e.g., 50 for 50%).</param>
    public static Length Pct(double v) => new(v, Unit.Percent);

    /// <summary>Gets an auto-sized length.</summary>
    public static Length Auto => new(0, Unit.Auto);

    /// <summary>Creates a fractional length for distributing remaining space.</summary>
    /// <param name="v">The fractional value.</param>
    public static Length Fr(double v) => new(v, Unit.Fr);

    /// <summary>A zero-point length.</summary>
    public static readonly Length Zero = new(0, Unit.Point);

    /// <summary>
    /// Converts this length to an absolute value in points. Only absolute units
    /// (Point, Millimeter, Inch, Centimeter) can be converted without context.
    /// </summary>
    /// <returns>The equivalent value in points.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the unit is relative (Percent, Fr) or Auto.
    /// </exception>
    public double ToPoints() => Type switch
    {
        Unit.Point => Value,
        Unit.Millimeter => Value * Units.PointsPerMm,
        Unit.Inch => Value * Units.PointsPerInch,
        Unit.Centimeter => Value * Units.PointsPerCm,
        _ => throw new InvalidOperationException(
            $"Cannot convert {Type} to absolute points without context.")
    };

    /// <summary>Gets whether this length uses an absolute unit that can be converted to points directly.</summary>
    public bool IsAbsolute => Type is Unit.Point or Unit.Millimeter or Unit.Inch or Unit.Centimeter;

    /// <summary>Gets whether this length uses a relative unit (Percent or Fr).</summary>
    public bool IsRelative => Type is Unit.Percent or Unit.Fr;

    /// <summary>Gets whether this length is auto-sized.</summary>
    public bool IsAuto => Type is Unit.Auto;

    /// <inheritdoc />
    public bool Equals(Length other) =>
        Type == other.Type && Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Length other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Value, Type);

    /// <summary>Determines whether two <see cref="Length"/> values are equal.</summary>
    public static bool operator ==(Length left, Length right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Length"/> values are not equal.</summary>
    public static bool operator !=(Length left, Length right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => Type switch
    {
        Unit.Auto => "auto",
        Unit.Point => $"{Value}pt",
        Unit.Millimeter => $"{Value}mm",
        Unit.Inch => $"{Value}in",
        Unit.Centimeter => $"{Value}cm",
        Unit.Percent => $"{Value}%",
        Unit.Fr => $"{Value}fr",
        _ => $"{Value} {Type}"
    };
}
