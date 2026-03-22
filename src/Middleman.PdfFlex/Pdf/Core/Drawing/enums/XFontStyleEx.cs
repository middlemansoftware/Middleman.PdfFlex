// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies style information applied to text.
    /// Note that this enum was named XFontStyle in PdfFlex versions prior to 6.0.
    /// </summary>
    [Flags]
    public enum XFontStyleEx  // Same values as System.Drawing.FontStyle.
    {
        // Will be renamed to XGdiFontStyle or XWinFontStyle.

        /// <summary>
        /// Normal text.
        /// </summary>
        Regular = 0,

        /// <summary>
        /// Bold text.
        /// </summary>
        Bold = 1,

        /// <summary>
        /// Italic text.
        /// </summary>
        Italic = 2,

        /// <summary>
        /// Bold and italic text.
        /// </summary>
        BoldItalic = 3,

        /// <summary>
        /// Underlined text.
        /// </summary>
        Underline = 4,

        /// <summary>
        /// Text with a line through the middle.
        /// </summary>
        Strikeout = 8,
    }
}
