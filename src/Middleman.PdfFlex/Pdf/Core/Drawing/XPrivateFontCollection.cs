// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if false  // DELETE 2025-12-31
#if GDI
using System.Runtime.InteropServices;
using Middleman.PdfFlex.Logging;
using GdiFontFamily = System.Drawing.FontFamily;
using GdiFont = System.Drawing.Font;
using GdiFontStyle = System.Drawing.FontStyle;
using GdiPrivateFontCollection = System.Drawing.Text.PrivateFontCollection;
#endif
#if WPF
using WpfFonts = System.Windows.Media.Fonts;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfTypeface = System.Windows.Media.Typeface;
using WpfGlyphTypeface = System.Windows.Media.GlyphTypeface;
#endif
using Microsoft.Extensions.Logging;
using Middleman.PdfFlex.Fonts;
using Middleman.PdfFlex.Fonts.Internal;

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// This class is out of order. Use a font resolver instead.
    /// </summary>
    [Obsolete("XPrivateFontCollection is out of order now. Use a font resolver instead.")]
    public sealed class XPrivateFontCollection
    { }
}
#endif
