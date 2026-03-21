// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A raster image element (PNG, JPEG, BMP, GIF). The image source can be
/// a file path or a byte array. Intrinsic dimensions are used for aspect-ratio
/// preservation during layout.
/// </summary>
public class ImageBox : Element
{
    #region Public Properties

    /// <summary>Gets the file path to the image, or null if the image is provided as bytes.</summary>
    public string? FilePath { get; }

    /// <summary>Gets the raw image data, or null if the image is provided as a file path.</summary>
    public byte[]? ImageData { get; }

    /// <summary>Gets the intrinsic width of the image in points. Zero means unknown.</summary>
    public double IntrinsicWidth { get; }

    /// <summary>Gets the intrinsic height of the image in points. Zero means unknown.</summary>
    public double IntrinsicHeight { get; }

    /// <summary>Gets or sets the alt text for PDF/UA accessibility tagging.</summary>
    public string? AltText { get; set; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates an image element from a file path.</summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="width">The intrinsic width in points. Zero means unknown.</param>
    /// <param name="height">The intrinsic height in points. Zero means unknown.</param>
    /// <param name="style">Optional style to apply to this image.</param>
    public ImageBox(string filePath, double width = 0, double height = 0, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        FilePath = filePath;
        IntrinsicWidth = width;
        IntrinsicHeight = height;
        Style = style;
    }

    /// <summary>Creates an image element from raw image data.</summary>
    /// <param name="imageData">The raw image bytes.</param>
    /// <param name="width">The intrinsic width in points.</param>
    /// <param name="height">The intrinsic height in points.</param>
    /// <param name="style">Optional style to apply to this image.</param>
    public ImageBox(byte[] imageData, double width, double height, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ImageData = imageData;
        IntrinsicWidth = width;
        IntrinsicHeight = height;
        Style = style;
    }

    #endregion Constructors
}
