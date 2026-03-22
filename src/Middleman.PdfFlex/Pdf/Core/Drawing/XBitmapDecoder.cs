// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if GDI
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
#endif
#if WPF
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif
#if WUI
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Provides functionality to load a bitmap image encoded in a specific format.
    /// </summary>
    public class XBitmapDecoder
    {
        internal XBitmapDecoder()
        { }

        /// <summary>
        /// Gets a new instance of the PNG image decoder.
        /// </summary>
        public static XBitmapDecoder GetPngDecoder()
        {
            return new XPngBitmapDecoder();
        }
    }

    sealed class XPngBitmapDecoder : XBitmapDecoder
    {
        internal XPngBitmapDecoder()
        { }
    }
}
