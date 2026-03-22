// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Text;
using Middleman.PdfFlex.Fonts;
using Middleman.PdfFlex.Fonts.OpenType;
using Middleman.PdfFlex.Pdf.Filters;

namespace Middleman.PdfFlex.Pdf.Advanced
{
    /// <summary>
    /// Represents the base class of a PDF font.
    /// </summary>
    public sealed class PdfFontProgram : PdfDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfFontProgram"/> class.
        /// </summary>
        internal PdfFontProgram(PdfDocument document)
            : base(document)
        { }

        internal void CreateFontFileAndAddToDescriptor(PdfFontDescriptor pdfFontDescriptor, CMapInfo cmapInfo, bool cidFont)
        {
            var x = pdfFontDescriptor.Elements[PdfFontDescriptor.Keys.FontFile2];

            OpenTypeFontFace subSet = pdfFontDescriptor.Descriptor.FontFace.CreateFontSubset(cmapInfo.GlyphIndices, cidFont);
            byte[] fontData = subSet.FontSource.Bytes;

            Owner.Internals.AddObject(this);
            pdfFontDescriptor.Elements[PdfFontDescriptor.Keys.FontFile2] = Reference;

            Elements["/Length1"] = new PdfInteger(fontData.Length);
            if (!Owner.Options.NoCompression)
            {
                fontData = Filtering.FlateDecode.Encode(fontData, _document.Options.FlateEncodeMode);
                Elements["/Filter"] = new PdfName("/FlateDecode");
            }
            Elements["/Length"] = new PdfInteger(fontData.Length);
            CreateStream(fontData);
        }
    }
}
