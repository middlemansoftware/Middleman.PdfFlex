// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if GDI
using System.Drawing;
using System.Drawing.Drawing2D;
#endif
#if WPF
using System.Windows.Media;
#endif
using Middleman.PdfFlex.Pdf;

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies details about how the font is used in PDF creation.
    /// </summary>
    public class XPdfFontOptions
    {
        internal XPdfFontOptions() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontEncoding encoding, PdfFontEmbedding embedding)
        {
            FontEncoding = encoding;
            FontEmbedding = embedding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontEncoding encoding)
        {
            FontEncoding = encoding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontEmbedding embedding)
        {
            FontEmbedding = embedding;
            FontEncoding = PdfFontEncoding.Automatic;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontEncoding encoding, PdfFontEmbedding embedding, PdfFontColoredGlyphs coloredGlyphs)
        {
            FontEncoding = encoding;
            FontEmbedding = embedding;
            ColoredGlyphs = coloredGlyphs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontEmbedding embedding, PdfFontColoredGlyphs coloredGlyphs)
        {
            FontEmbedding = embedding;
            FontEncoding = PdfFontEncoding.Unicode;
            ColoredGlyphs = coloredGlyphs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontEncoding encoding, PdfFontColoredGlyphs coloredGlyphs)
        {
            FontEmbedding = PdfFontEmbedding.TryComputeSubset;
            FontEncoding = encoding;
            ColoredGlyphs = coloredGlyphs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XPdfFontOptions"/> class.
        /// </summary>
        public XPdfFontOptions(PdfFontColoredGlyphs coloredGlyphs)
        {
            FontEmbedding = PdfFontEmbedding.TryComputeSubset;
            FontEncoding = PdfFontEncoding.Unicode;
            ColoredGlyphs = coloredGlyphs;
        }

        /// <summary>
        /// Gets a value indicating the font embedding.
        /// </summary>
        public PdfFontEmbedding FontEmbedding { get; }

        /// <summary>
        /// Gets a value indicating how the font is encoded.
        /// </summary>
        public PdfFontEncoding FontEncoding { get; }

        /// <summary>
        /// Gets a value indicating how the font is encoded.
        /// </summary>
        public PdfFontColoredGlyphs ColoredGlyphs { get; }

        /// <summary>
        /// Gets the default options with WinAnsi encoding and always font embedding.
        /// </summary>
        public static XPdfFontOptions AutomaticEncoding => new(PdfFontEncoding.Automatic);

        /// <summary>
        /// Gets the default options with WinAnsi encoding and always font embedding.
        /// </summary>
        public static XPdfFontOptions WinAnsiDefault => new(PdfFontEncoding.WinAnsi);

        /// <summary>
        /// Gets the default options with Unicode encoding and always font embedding.
        /// </summary>
        public static XPdfFontOptions UnicodeDefault => new(PdfFontEncoding.Unicode);
    }
}
