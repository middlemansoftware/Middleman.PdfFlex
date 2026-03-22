// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies how the interior of a closed path is filled.
    /// </summary>
    public enum XFillMode  // Same values as System.Drawing.FillMode.
    {
        /// <summary>
        /// Specifies the alternate fill mode. Called the 'odd-even rule' in PDF terminology.
        /// </summary>
        Alternate = 0,

        /// <summary>
        /// Specifies the winding fill mode. Called the 'nonzero winding number rule' in PDF terminology.
        /// </summary>
        Winding = 1,
    }
}
