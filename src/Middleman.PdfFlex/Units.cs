// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex;

/// <summary>
/// Provides unit conversion constants and helper methods for converting between
/// typographic points and common measurement units.
/// </summary>
public static class Units
{
    /// <summary>The number of typographic points in one inch (72).</summary>
    public const double PointsPerInch = 72.0;

    /// <summary>The number of typographic points in one millimeter.</summary>
    public const double PointsPerMm = 2.8346456693;

    /// <summary>The number of typographic points in one centimeter.</summary>
    public const double PointsPerCm = 28.346456693;

    /// <summary>Converts millimeters to points.</summary>
    /// <param name="mm">The value in millimeters.</param>
    public static double MmToPoints(double mm) => mm * PointsPerMm;

    /// <summary>Converts inches to points.</summary>
    /// <param name="inches">The value in inches.</param>
    public static double InchToPoints(double inches) => inches * PointsPerInch;

    /// <summary>Converts centimeters to points.</summary>
    /// <param name="cm">The value in centimeters.</param>
    public static double CmToPoints(double cm) => cm * PointsPerCm;

    /// <summary>Converts points to millimeters.</summary>
    /// <param name="points">The value in points.</param>
    public static double PointsToMm(double points) => points / PointsPerMm;

    /// <summary>Converts points to inches.</summary>
    /// <param name="points">The value in points.</param>
    public static double PointsToInch(double points) => points / PointsPerInch;

    /// <summary>Converts points to centimeters.</summary>
    /// <param name="points">The value in points.</param>
    public static double PointsToCm(double points) => points / PointsPerCm;
}
