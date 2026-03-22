// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

// ReSharper disable InconsistentNaming

namespace Middleman.PdfFlex.Drawing
{
    ///<summary>
    /// Determines whether rendering based on GDI+ or WPF.
    /// For internal use in hybrid build only.
    /// </summary>
    enum XGraphicTargetContext
    {
        NONE = 0,

        /// <summary>
        /// Rendering does not depend on a particular technology.
        /// </summary>
        CORE = 1,

        /// <summary>
        /// Renders using GDI+.
        /// </summary>
        GDI = 2,

        /// <summary>
        /// Renders using WPF.
        /// </summary>
        WPF = 3,

        /// <summary>
        /// Universal Windows Platform.
        /// </summary>
        WUI = 10,
    }
}
