// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Pdf.Images;

/// <summary>
/// Cross-platform image loading supporting JPEG, PNG, BMP, TGA, and GIF formats.
/// </summary>
/// <remarks>
/// <para>Currently delegates to PdfSharp's built-in <see cref="XImage"/> factory methods, which
/// support PNG and JPEG in the Core build. When StbImageSharp (MIT) source is integrated, this
/// class will use it as a fallback decoder for BMP, TGA, GIF, and any format PdfSharp cannot
/// handle natively.</para>
/// <para>All public methods are thread-safe and may be called concurrently.</para>
/// </remarks>
public static class ImageLoader
{
    /// <summary>
    /// Loads an image from a file path. Supports JPEG and PNG natively; other formats
    /// (BMP, TGA, GIF) will be supported after StbImageSharp integration.
    /// </summary>
    /// <param name="path">The absolute or relative path to the image file.</param>
    /// <returns>An <see cref="XImage"/> instance for use with PdfSharp drawing operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the image format is not supported by PdfSharp's built-in decoder and
    /// StbImageSharp is not yet integrated.
    /// </exception>
    public static XImage FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("Image file not found.", path);

        try
        {
            return XImage.FromFile(path);
        }
        catch (InvalidOperationException ex) when (IsUnsupportedFormatError(ex))
        {
            throw new NotSupportedException(
                $"Image format not supported natively by PdfSharp. " +
                $"StbImageSharp integration pending -- currently only PNG and JPEG are supported. File: {path}",
                ex);
        }
    }

    /// <summary>
    /// Loads an image from a seekable stream. Supports JPEG and PNG natively; other formats
    /// (BMP, TGA, GIF) will be supported after StbImageSharp integration.
    /// </summary>
    /// <param name="stream">A seekable stream containing image data.</param>
    /// <returns>An <see cref="XImage"/> instance for use with PdfSharp drawing operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream"/> is not seekable.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the image format is not supported by PdfSharp's built-in decoder and
    /// StbImageSharp is not yet integrated.
    /// </exception>
    public static XImage FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
            throw new ArgumentException("The stream must be seekable.", nameof(stream));

        try
        {
            return XImage.FromStream(stream);
        }
        catch (InvalidOperationException ex) when (IsUnsupportedFormatError(ex))
        {
            throw new NotSupportedException(
                "Image format not supported natively by PdfSharp. " +
                "StbImageSharp integration pending -- currently only PNG and JPEG are supported.",
                ex);
        }
    }

    /// <summary>
    /// Loads an image from a byte array. Supports JPEG and PNG natively; other formats
    /// (BMP, TGA, GIF) will be supported after StbImageSharp integration.
    /// </summary>
    /// <param name="data">A byte array containing image data.</param>
    /// <returns>An <see cref="XImage"/> instance for use with PdfSharp drawing operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the image format is not supported by PdfSharp's built-in decoder and
    /// StbImageSharp is not yet integrated.
    /// </exception>
    public static XImage FromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
            throw new ArgumentException("Image data cannot be empty.", nameof(data));

        using var stream = new MemoryStream(data, writable: false);
        return FromStream(stream);
    }

    /// <summary>
    /// Determines whether an <see cref="InvalidOperationException"/> from PdfSharp indicates
    /// an unsupported image format (as opposed to a different operational error).
    /// </summary>
    private static bool IsUnsupportedFormatError(InvalidOperationException ex)
    {
        // PdfSharp's Core build throws InvalidOperationException with "Unsupported image format"
        // when the image importer cannot handle the input.
        return ex.Message.Contains("Unsupported", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("image format", StringComparison.OrdinalIgnoreCase);
    }
}
