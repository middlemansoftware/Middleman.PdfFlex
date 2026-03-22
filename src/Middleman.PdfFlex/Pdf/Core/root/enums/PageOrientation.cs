// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Specifies the orientation of a page.
    /// </summary>
    public enum PageOrientation
    {
        /// <summary>
        /// The default page orientation.
        /// The top and bottom width is less than or equal to the
        /// left and right side.
        /// </summary>
        Portrait = 0,

        /// <summary>
        /// The width and height of the page are reversed.
        /// </summary>
        Landscape = 1
    }
}
