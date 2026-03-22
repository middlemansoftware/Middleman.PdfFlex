// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies how different clipping regions can be combined.
    /// </summary>
    public enum XCombineMode  // Same values as System.Drawing.Drawing2D.CombineMode.
    {
        /// <summary>
        /// One clipping region is replaced by another.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// Two clipping regions are combined by taking their intersection.
        /// </summary>
        Intersect = 1,

        /// <summary>
        /// Not yet implemented in PdfFlex.
        /// </summary>
        Union = 2,

        /// <summary>
        /// Not yet implemented in PdfFlex.
        /// </summary>
        Xor = 3,

        /// <summary>
        /// Not yet implemented in PdfFlex.
        /// </summary>
        Exclude = 4,

        /// <summary>
        /// Not yet implemented in PdfFlex.
        /// </summary>
        Complement = 5,
    }
}
