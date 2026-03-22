// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf;

namespace Middleman.PdfFlex.Drawing.Internal
{
    #region ImportedImagePng

    /// <summary>
    /// Data imported from raster images (PNG, BMP, GIF, TGA, PSD, HDR). Used to prepare the
    /// data needed for PDF embedding via <see cref="ImageDataBitmap"/>.
    /// </summary>
    class ImportedImagePng : ImportedImage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportedImagePng"/> class.
        /// </summary>
        public ImportedImagePng()
            : base()
        { }

        /// <summary>
        /// Prepares the image data for PDF embedding by wrapping the private bitmap and
        /// alpha-mask data in an <see cref="ImageDataBitmap"/>.
        /// </summary>
        internal override ImageData PrepareImageData(PdfDocumentOptions options)
        {
            var data = (ImagePrivateDataPng?)Data ?? NRT.ThrowOnNull<ImagePrivateDataPng>();
            ImageDataBitmap imageData = new ImageDataBitmap(data.Bitmap, data.AlphaMask!);

            if (data.PaletteData != null)
            {
                imageData.PaletteData = data.PaletteData;
                imageData.PaletteDataLength = data.PaletteData.Length;
            }
            return imageData;
        }
    }

    #endregion

    #region ImagePrivateDataPng

    /// <summary>
    /// Image data needed for PDF bitmap images. Stores separated RGB (or grayscale/indexed)
    /// pixel data and an optional alpha mask.
    /// </summary>
    class ImagePrivateDataPng : ImagePrivateData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImagePrivateDataPng"/> class.
        /// </summary>
        /// <param name="bitmap">The bitmap pixel data (RGB, grayscale, or palette-indexed).</param>
        /// <param name="alphaMask">Optional per-pixel alpha mask, or <c>null</c> if fully opaque.</param>
        public ImagePrivateDataPng(byte[] bitmap, byte[]? alphaMask)
        {
            Bitmap = bitmap;
            AlphaMask = alphaMask;
        }

        /// <summary>
        /// Gets the bitmap pixel data.
        /// </summary>
        internal readonly byte[] Bitmap;

        /// <summary>
        /// Gets or sets the alpha mask. Set to <c>null</c> when no transparency is present.
        /// </summary>
        internal byte[]? AlphaMask;

        /// <summary>
        /// Gets or sets the palette data for indexed-color images.
        /// </summary>
        internal byte[]? PaletteData { get; set; }
    }

    #endregion

    #region ImageDataBitmap

    /// <summary>
    /// Contains data needed for PDF. Will be prepared when needed.
    /// Bitmap refers to the format used in PDF. Used for BMP, PNG, GIF, TGA, and other
    /// raster formats decoded via StbImageSharp.
    /// </summary>
    class ImageDataBitmap : ImageData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDataBitmap"/> class with document options.
        /// </summary>
        internal ImageDataBitmap(PdfDocumentOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDataBitmap"/> class with pre-split
        /// bitmap and alpha-mask data.
        /// </summary>
        internal ImageDataBitmap(byte[] data, byte[] mask)
        {
            Data = data;
            Length = Data.Length;
            AlphaMask = mask;
            AlphaMaskLength = AlphaMask?.Length ?? 0;
        }

        /// <summary>
        /// Gets the pixel data.
        /// </summary>
        public byte[] Data { get; internal set; } = null!;

        /// <summary>
        /// Gets the length of the pixel data.
        /// </summary>
        public int Length { get; internal set; }

        /// <summary>
        /// Gets the data for the CCITT format.
        /// </summary>
        public byte[]? DataFax { get; internal set; } = null;

        /// <summary>
        /// Gets the length of the fax-encoded data.
        /// </summary>
        public int LengthFax { get; internal set; }

        /// <summary>
        /// Gets the per-pixel alpha mask.
        /// </summary>
        public byte[] AlphaMask { get; internal set; } = null!;

        /// <summary>
        /// Gets the length of the alpha mask.
        /// </summary>
        public int AlphaMaskLength { get; internal set; }

        /// <summary>
        /// Gets the bitmap mask (1-bit per pixel).
        /// </summary>
        public byte[] BitmapMask { get; internal set; } = null!;

        /// <summary>
        /// Gets the length of the bitmap mask.
        /// </summary>
        public int BitmapMaskLength { get; internal set; }

        /// <summary>
        /// Gets or sets the palette data for indexed-color images.
        /// </summary>
        public byte[] PaletteData { get; set; } = null!;

        /// <summary>
        /// Gets or sets the length of the palette data.
        /// </summary>
        public int PaletteDataLength { get; set; }

        /// <summary>
        /// Gets or sets whether a segmented color mask is used.
        /// </summary>
        public bool SegmentedColorMask;

        /// <summary>
        /// Gets or sets the bitonal flag. 0 = false, positive = true, negative = true (inverted).
        /// </summary>
        public int IsBitonal;

        /// <summary>
        /// Gets or sets the CCITT K parameter.
        /// </summary>
        public int K;

        /// <summary>
        /// Gets or sets whether the image is grayscale.
        /// </summary>
        public bool IsGray;

        /// <summary>
        /// Gets the document options.
        /// </summary>
        internal readonly PdfDocumentOptions? Options;
    }

    #endregion
}
