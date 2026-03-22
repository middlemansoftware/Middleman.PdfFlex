// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Pdf.Images;

/// <summary>
/// Cross-platform image loading supporting JPEG, PNG, BMP, GIF, TGA, PSD, and HDR formats.
/// </summary>
/// <remarks>
/// <para>JPEG images are passed through to the PDF unchanged (no decode/re-encode) to preserve
/// quality and minimize file size. All other supported formats are decoded to raw pixels via
/// the embedded StbImageSharp library and then FLATE-compressed into the PDF.</para>
/// <para>All public methods are thread-safe and may be called concurrently.</para>
/// </remarks>
public static class ImageLoader
{
    /// <summary>
    /// Loads an image from a file path. Supports JPEG, PNG, BMP, GIF, TGA, PSD, and HDR.
    /// </summary>
    /// <param name="path">The absolute or relative path to the image file.</param>
    /// <returns>An <see cref="XImage"/> instance for use with PdfFlex drawing operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the image format is not recognized.</exception>
    public static XImage FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("Image file not found.", path);

        return XImage.FromFile(path);
    }

    /// <summary>
    /// Loads an image from a seekable stream. Supports JPEG, PNG, BMP, GIF, TGA, PSD, and HDR.
    /// </summary>
    /// <param name="stream">A seekable stream containing image data.</param>
    /// <returns>An <see cref="XImage"/> instance for use with PdfFlex drawing operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream"/> is not seekable.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the image format is not recognized.</exception>
    public static XImage FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
            throw new ArgumentException("The stream must be seekable.", nameof(stream));

        return XImage.FromStream(stream);
    }

    /// <summary>
    /// Loads an image from a byte array. Supports JPEG, PNG, BMP, GIF, TGA, PSD, and HDR.
    /// </summary>
    /// <param name="data">A byte array containing image data.</param>
    /// <returns>An <see cref="XImage"/> instance for use with PdfFlex drawing operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the image format is not recognized.</exception>
    public static XImage FromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
            throw new ArgumentException("Image data cannot be empty.", nameof(data));

        using var stream = new MemoryStream(data, writable: false);
        return FromStream(stream);
    }
}
