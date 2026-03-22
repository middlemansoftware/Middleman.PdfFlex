// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies the direction of the y-axis.
    /// </summary>
    public enum XPageDirection
    {
        /// <summary>
        /// Increasing Y values go downwards. This is the default.
        /// </summary>
        Downwards = 0,

        /// <summary>
        /// Increasing Y values go upwards. This is only possible when drawing on a PDF page.
        /// It is not implemented when drawing on a System.Drawing.Graphics object.
        /// </summary>
        [Obsolete("Not implemeted - yagni")]
        Upwards = 1, // Possible, but needs a lot of case differentiation - postponed.
    }
}
