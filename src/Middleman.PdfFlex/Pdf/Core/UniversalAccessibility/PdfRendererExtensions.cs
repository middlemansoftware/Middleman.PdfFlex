// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Drawing.Pdf;

namespace Middleman.PdfFlex.UniversalAccessibility
{
    /// <summary>
    /// Helper class containing methods that are called on XGraphics object’s XGraphicsPdfRenderer.
    /// </summary>
    public static class PdfRendererExtensions
    {
        /// <summary>
        /// Activate Text mode for Universal Accessibility.
        /// </summary>
        public static void BeginTextMode(XGraphics gfx)
        {
            if (gfx._renderer is not XGraphicsPdfRenderer renderer)
                throw new InvalidOperationException("Current renderer must be an XGraphicsPdfRenderer.");

            // BeginPage() must be executed before first BeginTextMode.
            renderer.BeginPage();
            renderer.BeginTextMode();
        }

        /// <summary>
        /// Activate Graphic mode for Universal Accessibility.
        /// </summary>
        public static void BeginGraphicMode(XGraphics gfx)
        {
            if (gfx._renderer is not XGraphicsPdfRenderer renderer)
                throw new InvalidOperationException("Current renderer must be an XGraphicsPdfRenderer.");

            // BeginPage must be executed before first BeginGraphicMode.
            renderer.BeginPage();
            renderer.BeginGraphicMode();
        }

        /// <summary>
        /// Determine if renderer is in Text mode or Graphic mode.
        /// </summary>
        public static bool IsInTextMode(XGraphics gfx)
        {
            if (gfx._renderer is not XGraphicsPdfRenderer renderer)
                throw new InvalidOperationException("Current renderer must be an XGraphicsPdfRenderer.");

            return renderer.IsInTextMode();
        }
    }
}
