// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.Logging;
using Middleman.PdfFlex.Logging;
using StbImageSharp;

namespace Middleman.PdfFlex.Drawing.Internal
{
    /// <summary>
    /// Universal image importer using StbImageSharp. Decodes PNG, BMP, GIF, TGA, PSD, and HDR
    /// formats to raw pixel data for PDF embedding.
    /// </summary>
    /// <remarks>
    /// <para>This importer is registered after <see cref="ImageImporterJpeg"/> in the importer
    /// chain. JPEG images are handled by the JPEG importer via passthrough (no decode/re-encode),
    /// while all other supported formats fall through to this importer for decoding.</para>
    /// <para>StbImageSharp decodes images to an interleaved RGBA byte array. This importer
    /// splits the RGBA data into separate RGB and alpha-mask arrays, matching the format
    /// expected by <see cref="ImportedImagePng"/> and <see cref="ImagePrivateDataPng"/>.</para>
    /// </remarks>
    class ImageImporterStb : ImageImporterRoot, IImageImporter
    {
        #region IImageImporter

        /// <summary>
        /// Attempts to decode the image using StbImageSharp. Returns <c>null</c> if the format
        /// is not recognized or decoding fails.
        /// </summary>
        /// <param name="stream">The stream reader helper containing the raw image bytes.</param>
        /// <returns>An <see cref="ImportedImage"/> on success; <c>null</c> if the format is not supported.</returns>
        public ImportedImage? ImportImage(StreamReaderHelper stream)
        {
            try
            {
                stream.CurrentOffset = 0;

                // Build a MemoryStream over the raw byte data for StbImageSharp.
                using var memoryStream = new MemoryStream(stream.Data, 0, stream.Length, writable: false);

                // Decode to RGBA so we always get a consistent 4-component output.
                var result = ImageResult.FromStream(memoryStream, ColorComponents.RedGreenBlueAlpha);

                if (result.Data == null || result.Width <= 0 || result.Height <= 0)
                    return null;

                return BuildImportedImage(result);
            }
            catch (Exception ex)
            {
                // Eat exceptions so the importer chain can continue. If no importer succeeds,
                // the caller will report "unsupported format" to the user.
                PdfFlexLogHost.Logger.LogDebug(ex, "StbImageSharp could not decode image; skipping to next importer.");
                return null;
            }
        }

        /// <summary>
        /// Not used. Image data is prepared via <see cref="ImportedImagePng.PrepareImageData"/>.
        /// </summary>
        public ImageData PrepareImage(ImagePrivateData data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Builds an <see cref="ImportedImagePng"/> from the StbImageSharp decode result by
        /// splitting interleaved RGBA data into separate RGB and alpha-mask arrays.
        /// </summary>
        /// <param name="result">The decoded image result from StbImageSharp.</param>
        /// <returns>A fully initialized <see cref="ImportedImage"/> ready for PDF embedding.</returns>
        private static ImportedImage BuildImportedImage(ImageResult result)
        {
            int width = result.Width;
            int height = result.Height;
            int pixelCount = width * height;
            byte[] rgba = result.Data;

            // Split interleaved RGBA into separate RGB and alpha arrays.
            var rgb = new byte[pixelCount * 3];
            var alpha = new byte[pixelCount];
            bool alphaUsed = false;

            int rgbaOffset = 0;
            int rgbOffset = 0;

            for (int i = 0; i < pixelCount; i++)
            {
                rgb[rgbOffset] = rgba[rgbaOffset];         // R
                rgb[rgbOffset + 1] = rgba[rgbaOffset + 1]; // G
                rgb[rgbOffset + 2] = rgba[rgbaOffset + 2]; // B

                byte a = rgba[rgbaOffset + 3];
                alpha[i] = a;
                alphaUsed |= a != 255;

                rgbaOffset += 4;
                rgbOffset += 3;
            }

            // If no pixel has transparency, discard the alpha mask entirely.
            byte[]? alphaMask = alphaUsed ? alpha : null;

            var privateData = new ImagePrivateDataPng(rgb, alphaMask);
            var image = new ImportedImagePng();
            image.Data = privateData;
            privateData.Image = image;

            // Fill image information.
            image.Information.Width = (uint)width;
            image.Information.Height = (uint)height;
            image.Information.ImageFormat = alphaUsed
                ? ImageInformation.ImageFormats.ARGB32
                : ImageInformation.ImageFormats.RGB24;
            image.Information.DefaultDPI = 96;
            image.Information.HorizontalDPI = 0;
            image.Information.VerticalDPI = 0;
            image.Information.HorizontalDPM = 0;
            image.Information.VerticalDPM = 0;
            image.Information.HorizontalAspectRatio = 0;
            image.Information.VerticalAspectRatio = 0;
            image.Information.ColorsUsed = 0;

            return image;
        }

        #endregion
    }
}
