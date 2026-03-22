// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Internal
{
    // In PdfFlex hybrid build both GDI and WPF is defined.
    // This is for development and testing only.
#if GDI && WPF
#error PdfFlex 6 does not support hybrid builds anymore.
    /// <summary>
    /// Internal switch indicating what context has to be used if both GDI and WPF are defined.
    /// </summary>
    static class TargetContextHelper
    {
        public static XGraphicTargetContext TargetContext = XGraphicTargetContext.WPF;
    }
#endif
}
