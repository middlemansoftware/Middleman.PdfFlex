// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Defines the direction an elliptical arc is drawn.
    /// </summary>
    public enum XSweepDirection // Same values as System.Windows.Media.SweepDirection.
    {
        /// <summary>
        /// Specifies that arcs are drawn in a counterclockwise (negative-angle) direction.
        /// </summary>
        Counterclockwise = 0,

        /// <summary>
        /// Specifies that arcs are drawn in a clockwise (positive-angle) direction.
        /// </summary>
        Clockwise = 1,
    }
}
