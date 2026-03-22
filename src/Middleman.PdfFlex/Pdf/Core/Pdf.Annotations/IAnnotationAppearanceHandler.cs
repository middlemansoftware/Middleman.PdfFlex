// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Pdf.Annotations
{
    /// <summary>
    /// Draws the visual representation of an AcroForm element.
    /// </summary>
    public interface IAnnotationAppearanceHandler  // kann man Annotation generell selber malen?
    {
        /// <summary>
        /// Draws the visual representation of an AcroForm element.
        /// </summary>
        /// <param name="gfx"></param>
        /// <param name="rect"></param>
        void DrawAppearance(XGraphics gfx, XRect rect);
    }
}
