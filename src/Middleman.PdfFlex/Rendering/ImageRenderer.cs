// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf.Structure;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders <see cref="ImageBox"/> elements by loading raster images and drawing
/// them within the layout node bounds, preserving aspect ratio.
/// </summary>
internal static class ImageRenderer
{
    #region Public Methods

    /// <summary>
    /// Renders an image element within the specified layout node bounds.
    /// Loads the image from a file path or byte array and scales it to fit while
    /// preserving the original aspect ratio.
    /// </summary>
    /// <param name="ctx">The render context carrying the graphics surface and page state.</param>
    /// <param name="node">The layout node positioning the image.</param>
    /// <param name="imageBox">The image element to render.</param>
    public static void Render(RenderContext ctx, LayoutNode node, ImageBox imageBox)
    {
        if (node.Width <= 0 || node.Height <= 0)
            return;

        var gfx = ctx.Graphics;
        var sb = ctx.StructureBuilder;

        XImage? image = null;
        try
        {
            image = LoadImage(imageBox);
            if (image == null)
                return;

            // Calculate scaled dimensions preserving aspect ratio.
            double scaleX = node.Width / image.PixelWidth;
            double scaleY = node.Height / image.PixelHeight;
            double scale = Math.Min(scaleX, scaleY);

            double drawWidth = image.PixelWidth * scale;
            double drawHeight = image.PixelHeight * scale;

            // Center the image within the node bounds.
            double offsetX = node.X + ((node.Width - drawWidth) / 2.0);
            double offsetY = node.Y + ((node.Height - drawHeight) / 2.0);

            if (sb != null)
            {
                var bbox = new XRect(offsetX, offsetY, drawWidth, drawHeight);
                sb.BeginElement(PdfIllustrationElementTag.Figure, imageBox.AltText ?? "", bbox);
            }

            gfx.DrawImage(image, offsetX, offsetY, drawWidth, drawHeight);

            if (sb != null) sb.End();

            // Create link annotation covering the drawn image area.
            string? linkTarget = imageBox.LinkTarget;
            if (!string.IsNullOrEmpty(linkTarget) && ctx.Page != null)
            {
                var linkRect = new XRect(offsetX, offsetY, drawWidth, drawHeight);
                if (DocumentRenderer.IsExternalLink(linkTarget))
                {
                    DocumentRenderer.CreateUriLinkAnnotation(
                        ctx.Page, linkRect, linkTarget, ctx.PageHeight, sb,
                        imageBox.AltText ?? linkTarget);
                }
                else
                {
                    // Create the /Link structure element now while the element
                    // stack is correctly positioned. The annotation will be
                    // associated with this element during deferred resolution.
                    PdfStructureElement? linkSte = sb?.CreateLinkStructureElement();
                    DocumentRenderer.QueueInternalLink(
                        ctx, linkRect, linkTarget, imageBox.AltText, linkSte);
                }
            }
        }
        finally
        {
            image?.Dispose();
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Loads an <see cref="XImage"/> from the image box's file path or byte array data.
    /// Returns null if the image cannot be loaded.
    /// </summary>
    private static XImage? LoadImage(ImageBox imageBox)
    {
        if (imageBox.FilePath != null)
        {
            return XImage.FromFile(imageBox.FilePath);
        }

        if (imageBox.ImageData is { Length: > 0 })
        {
            using var stream = new MemoryStream(imageBox.ImageData, writable: false);
            return XImage.FromStream(stream);
        }

        return null;
    }

    #endregion Private Methods
}
