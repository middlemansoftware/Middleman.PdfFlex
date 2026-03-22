// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

// ReSharper disable InconsistentNaming

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Indicates how to handle the first point of a path.
    /// </summary>
    enum PathStart
    {
        /// <summary>
        /// Set the current position to the first point.
        /// </summary>
        MoveTo1st,

        /// <summary>
        /// Draws a line to the first point.
        /// </summary>
        LineTo1st,

        /// <summary>
        /// Ignores the first point.
        /// </summary>
        Ignore1st,
    }
}
