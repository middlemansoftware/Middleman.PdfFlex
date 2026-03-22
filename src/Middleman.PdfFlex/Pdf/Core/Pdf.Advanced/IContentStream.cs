// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Pdf.Advanced
{
    interface IContentStream
    {
        PdfResources Resources { get; }

        string GetFontName(XGlyphTypeface glyphTypeface, FontType fontType, out PdfFont pdfFont);

        string GetFontName(string idName, byte[] fontData, out PdfFont pdfFont);

        string GetImageName(XImage image);

        string GetFormName(XForm form);
    }
}
