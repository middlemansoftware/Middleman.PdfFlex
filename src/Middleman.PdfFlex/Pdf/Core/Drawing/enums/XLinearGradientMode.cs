// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies the direction of a linear gradient.
    /// </summary>
    public enum XLinearGradientMode  // same values as System.Drawing.LinearGradientMode
    {
        /// <summary>
        /// Specifies a gradient from left to right.
        /// </summary>
        Horizontal = 0,

        /// <summary>
        /// Specifies a gradient from top to bottom.
        /// </summary>
        Vertical = 1,

        /// <summary>
        /// Specifies a gradient from upper left to lower right.
        /// </summary>
        ForwardDiagonal = 2,

        /// <summary>
        /// Specifies a gradient from upper right to lower left.
        /// </summary>
        BackwardDiagonal = 3,
    }
}
