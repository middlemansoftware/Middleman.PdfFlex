// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if GDI
using System.Drawing.Drawing2D;
#endif
#if WPF
#endif
#if WUI
using Microsoft.Graphics.Canvas.Brushes;
using UwpColor = Windows.UI.Color;
#endif

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Classes derived from this abstract base class define objects used to fill the 
    /// interiors of paths.
    /// </summary>
    public abstract class XBrush
    {
#if GDI
        internal abstract System.Drawing.Brush RealizeGdiBrush();

        /// <summary>
        /// Converts from a System.Drawing.Brush.
        /// </summary>
        public static implicit operator XBrush(Brush brush)
        {
            // ReSharper disable once IdentifierTypo
            XBrush xbrush;
            if (brush is SolidBrush solidBrush)
            {
                var c = solidBrush.Color;
                xbrush = new XSolidBrush(XColor.FromArgb(c.A, c.R, c.G, c.B));
            }
            else if (brush is LinearGradientBrush lgBrush)
            {
                // xbrush = new LinearGradientBrush(lgBrush.Rectangle, lgBrush.co(solidBrush.Color);
                throw new NotImplementedException("XBrush type not yet supported by PdfFlex.");
            }
            else
            {
                throw new NotImplementedException("XBrush type not supported by PdfFlex.");
            }
            return xbrush;
        }
#endif
#if WPF
        internal abstract System.Windows.Media.Brush RealizeWpfBrush();
#endif
#if WUI
        internal abstract ICanvasBrush RealizeCanvasBrush();
#endif
    }
}
