// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Specifies whether to compress JPEG images with the FlateDecode filter.
    /// </summary>
    public enum PdfUseFlateDecoderForJpegImages
    {
        /// <summary>
        /// PdfFlex will try FlateDecode and use it if it leads to a reduction in PDF file size.
        /// When FlateEncodeMode is set to BestCompression, this is more likely to reduce the file size,
        /// but it takes considerably more time to create the PDF file.
        /// </summary>
        Automatic,

        /// <summary>
        /// PdfFlex will never use FlateDecode - files may be a few bytes larger, but file creation is faster.
        /// </summary>
        Never,

        /// <summary>
        /// PdfFlex will always use FlateDecode, even if this leads to larger files;
        /// this option is meant for testing purposes only and should not be used for production code.
        /// </summary>
        Always,
    }
}