// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if GDI
using System.Drawing;
using System.Drawing.Drawing2D;
#endif
#if WPF
using System.Windows;
using System.Windows.Media;
#endif
#if WUI
using Windows.UI.Xaml.Media;
#endif

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Provides access to the internal data structures of XGraphicsPath.
    /// This class prevents the public interface from pollution with internal functions.
    /// </summary>
    public sealed class XGraphicsPathInternals
    {
        internal XGraphicsPathInternals(XGraphicsPath path)
        {
            _path = path;
        }

        XGraphicsPath _path;

#if GDI
        /// <summary>
        /// Gets the underlying GDI+ path object.
        /// </summary>
        public GraphicsPath GdiPath
        {
            get { return _path.GdipPath; }
        }
#endif

#if WPF || WUI
        /// <summary>
        /// Gets the underlying WPF path geometry object.
        /// </summary>
        public PathGeometry WpfPath
        {
            get { return _path.PathGeometry; }
        }
#endif
    }
}