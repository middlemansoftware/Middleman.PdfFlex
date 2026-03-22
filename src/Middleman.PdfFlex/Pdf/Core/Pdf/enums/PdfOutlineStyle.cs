// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

// Review: OK - StL/14-10-05

using System;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Specifies the font style for the outline (bookmark) text.
    ///  </summary>
    [Flags]
    public enum PdfOutlineStyle  // Reference:  TABLE 8.5 Outline Item flags / Page 587
    {
        /// <summary>
        /// Outline text is displayed using a regular font.
        /// </summary>
        Regular = 0,

        /// <summary>
        /// Outline text is displayed using an italic font.
        /// </summary>
        Italic = 1,

        /// <summary>
        /// Outline text is displayed using a bold font.
        /// </summary>
        Bold = 2,

        /// <summary>
        /// Outline text is displayed using a bold and italic font.
        /// </summary>
        BoldItalic = 3,
    }
}
