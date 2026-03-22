// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Fonts.OpenType
{
    /// <summary>
    /// Identifies the technology of an OpenType font file.
    /// </summary>
    enum FontTechnology
    {
        /// <summary>
        /// Font is Adobe Postscript font in CFF.
        /// </summary>
        PostscriptOutlines,

        /// <summary>
        /// Font is a TrueType font.
        /// </summary>
        TrueTypeOutlines,

        /// <summary>
        /// Font is a TrueType font collection.
        /// </summary>
        TrueTypeCollection
    }
}
