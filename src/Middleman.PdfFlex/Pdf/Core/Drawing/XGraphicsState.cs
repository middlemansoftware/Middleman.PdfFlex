// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if GDI
using System.Drawing;
using System.Drawing.Drawing2D;
#endif
#if WPF
using System.Windows.Media;
#endif

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Represents the internal state of an XGraphics object.
    /// This class is used as a handle for restoring the context.
    /// </summary>
    public sealed class XGraphicsState
    {
        // This class is simply a wrapper of InternalGraphicsState.
#if CORE
        internal XGraphicsState()
        { }
#endif
#if GDI
        internal XGraphicsState(GraphicsState? state)
        {
            GdiState = state;
        }
        internal GraphicsState? GdiState;
#endif
#if WPF
        internal XGraphicsState()
        { }
#endif
        internal InternalGraphicsState InternalState = default!;
    }
}
