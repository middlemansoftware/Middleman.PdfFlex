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
#if true_ // unused
    /// <summary>
    /// Represents a segment of a path defined by a type and a set of points.
    /// </summary>
    internal sealed class XGraphicsPathItem
    {
        public XGraphicsPathItem(XGraphicsPathItemType type)
        {
            Type = type;
            Points = null;
        }

#if GDI
        public XGraphicsPathItem(XGraphicsPathItemType type, params PointF[] points)
        {
            Type = type;
            Points = XGraphics.MakeXPointArray(points, 0, points.Length);
        }
#endif

        public XGraphicsPathItem(XGraphicsPathItemType type, params XPoint[] points)
        {
            Type = type;
            Points = (XPoint[])points.Clone();
        }

        public XGraphicsPathItem Clone()
        {
            XGraphicsPathItem item = (XGraphicsPathItem)MemberwiseClone();
            item.Points = (XPoint[])Points.Clone();
            return item;
        }

        public XGraphicsPathItemType Type;
        public XPoint[] Points;
    }
#endif
}