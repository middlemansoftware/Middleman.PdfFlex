// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Specifies the encoding schema used for an XFont when converting into PDF.
    /// </summary>
    public enum PdfFontEncoding
    {
        /// <summary>
        /// Lets PdfFlex decide which encoding is used when drawing text depending
        /// on the used characters.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Causes a font to use Windows-1252 encoding to encode text rendered with this font.
        /// </summary>
        WinAnsi = 1,

        /// <summary>
        /// Causes a font to use Unicode encoding to encode text rendered with this font.
        /// </summary>
        Unicode = 2,
    }
}
