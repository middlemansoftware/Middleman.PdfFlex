// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if GDI
using Middleman.PdfFlex.Internal;
#endif
#if WPF
using Middleman.PdfFlex.Internal;
#endif
#if WUI
using Windows.UI.Xaml.Media.Imaging;
using Middleman.PdfFlex.Internal;
#endif

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Defines a pixel-based bitmap image.
    /// </summary>
    public sealed class XBitmapImage : XBitmapSource
    {
        // TODO_OLD: Move code from XImage to this class.

        /// <summary>
        /// Initializes a new instance of the <see cref="XBitmapImage"/> class.
        /// </summary>
        internal XBitmapImage(int width, int height)
        {
#if GDI
            try
            {
                Lock.EnterGdiPlus();
                // Create a default 24-bit ARGB bitmap.
                _gdiImage = new Bitmap(width, height);
            }
            finally { Lock.ExitGdiPlus(); }
#endif
#if WPF
            DiagnosticsHelper.ThrowNotImplementedException("CreateBitmap");
#endif
#if WUI
            DiagnosticsHelper.ThrowNotImplementedException("CreateBitmap");
#endif
#if CORE || GDI && !WPF // Prevent unreachable code error
            Initialize();
#endif
        }

        /// <summary>
        /// Creates a default 24-bit ARGB bitmap with the specified pixel size.
        /// </summary>
        public static XBitmapSource CreateBitmap(int width, int height)
        {
            // Create a default 24-bit ARGB bitmap.
            return new XBitmapImage(width, height);
        }
    }
}
