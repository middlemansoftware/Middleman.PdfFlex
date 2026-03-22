// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies the style of dashed lines drawn with an XPen object.
    /// </summary>
    public enum XDashStyle  // Same values as System.Drawing.Drawing2D.DashStyle.
    {
        /// <summary>
        /// Specifies a solid line.
        /// </summary>
        Solid = 0,

        /// <summary>
        /// Specifies a line consisting of dashes.
        /// </summary>
        Dash = 1,

        /// <summary>
        /// Specifies a line consisting of dots.
        /// </summary>
        Dot = 2,

        /// <summary>
        /// Specifies a line consisting of a repeating pattern of dash-dot.
        /// </summary>
        DashDot = 3,

        /// <summary>
        /// Specifies a line consisting of a repeating pattern of dash-dot-dot.
        /// </summary>
        DashDotDot = 4,

        /// <summary>
        /// Specifies a user-defined custom dash style.
        /// </summary>
        Custom = 5,
    }
}
