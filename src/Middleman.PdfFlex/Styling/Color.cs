// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Globalization;

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Represents an RGBA color value. Each component is stored as a byte (0-255).
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    /// <summary>Gets the red component (0-255).</summary>
    public byte R { get; }

    /// <summary>Gets the green component (0-255).</summary>
    public byte G { get; }

    /// <summary>Gets the blue component (0-255).</summary>
    public byte B { get; }

    /// <summary>Gets the alpha component (0-255), where 255 is fully opaque.</summary>
    public byte A { get; }

    /// <summary>Creates a color from individual RGBA components.</summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255). Defaults to 255 (fully opaque).</param>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates a color from a hexadecimal string. Supports formats:
    /// "#RRGGBB", "#AARRGGBB", "RRGGBB", and "AARRGGBB".
    /// </summary>
    /// <param name="hex">The hexadecimal color string.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="ArgumentException">Thrown when the hex string format is invalid.</exception>
    public static Color FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        ReadOnlySpan<char> span = hex.AsSpan();
        if (span.Length > 0 && span[0] == '#')
        {
            span = span[1..];
        }

        return span.Length switch
        {
            6 => new Color(
                ParseHexByte(span[0..2]),
                ParseHexByte(span[2..4]),
                ParseHexByte(span[4..6])),
            8 => new Color(
                ParseHexByte(span[2..4]),
                ParseHexByte(span[4..6]),
                ParseHexByte(span[6..8]),
                ParseHexByte(span[0..2])),
            _ => throw new ArgumentException(
                $"Invalid hex color format: '{hex}'. Expected #RRGGBB, #AARRGGBB, RRGGBB, or AARRGGBB.",
                nameof(hex))
        };
    }

    /// <summary>Creates an opaque color from RGB components.</summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b);

    /// <summary>Creates a color from RGBA components.</summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    public static Color FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);

    /// <summary>Returns a new color with the specified alpha component.</summary>
    /// <param name="a">The new alpha component (0-255).</param>
    public Color WithAlpha(byte a) => new(R, G, B, a);

    /// <summary>Returns a new color with the specified opacity.</summary>
    /// <param name="opacity">The opacity value (0.0 to 1.0).</param>
    public Color WithOpacity(double opacity) => new(R, G, B, (byte)(Math.Clamp(opacity, 0.0, 1.0) * 255));

    /// <inheritdoc />
    public bool Equals(Color other) =>
        R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Color other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(R, G, B, A);

    /// <summary>Determines whether two <see cref="Color"/> values are equal.</summary>
    public static bool operator ==(Color left, Color right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Color"/> values are not equal.</summary>
    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        A == 255
            ? $"#{R:X2}{G:X2}{B:X2}"
            : $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    private static byte ParseHexByte(ReadOnlySpan<char> hex) =>
        byte.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
