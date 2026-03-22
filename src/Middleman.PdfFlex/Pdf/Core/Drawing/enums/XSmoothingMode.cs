// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies whether smoothing (or anti-aliasing) is applied to lines and curves
    /// and the edges of filled areas.
    /// </summary>
    [Flags]
    public enum XSmoothingMode  // same values as System.Drawing.Drawing2D.SmoothingMode
    {
        // Not used in PDF rendering process.

        /// <summary>
        /// Specifies an invalid mode.
        /// </summary>
        Invalid = -1,

        /// <summary>
        /// Specifies the default mode.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Specifies high-speed, low-quality rendering.
        /// </summary>
        HighSpeed = 1,

        /// <summary>
        /// Specifies high-quality, low-speed rendering.
        /// </summary>
        HighQuality = 2,

        /// <summary>
        /// Specifies no anti-aliasing.
        /// </summary>
        None = 3,

        /// <summary>
        /// Specifies anti-aliased rendering.
        /// </summary>
        AntiAlias = 4,
    }
}
