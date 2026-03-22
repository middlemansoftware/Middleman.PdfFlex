// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Drawing.Layout;
using Middleman.PdfFlex.Pdf.Annotations;

namespace Middleman.PdfFlex.Pdf.Signatures
{
    /// <summary>
    /// This is only a sample of an appearance handler that draws the visual representation of the signature in the PDF.
    /// You should write your own implementation of IAnnotationAppearanceHandler and ensure that the used font is available.
    /// </summary>
    class DefaultSignatureAppearanceHandler : IAnnotationAppearanceHandler
    {
        public string? Location { get; set; }

        public string? Reason { get; set; }

        public string? Signer { get; set; }

        public void DrawAppearance(XGraphics gfx, XRect rect)
        {
            var defaultText = $"Signed by: {Signer}\nLocation: {Location}\nReason: {Reason}\nDate: {DateTime.Now}";

            // You should write your own implementation of IAnnotationAppearanceHandler and ensure that the used font is available.
            var font = new XFont("Verdana", 7, XFontStyleEx.Regular);

            var txtFormat = new XTextFormatter(gfx);

            var currentPosition = new XPoint(0, 0);
            double width = rect.Width;
            double height = rect.Height;

            // Leave 5% space on each side.
            txtFormat.DrawString(defaultText, font,
                new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black)),
                new XRect(currentPosition.X + width * .05, currentPosition.Y + height * .05, 
                    width * .9, height * .9),
                XStringFormats.TopLeft);
        }
    }
}
