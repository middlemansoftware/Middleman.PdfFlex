// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    enum XFontStyleValue
    {
        // Values taken from WPF.
        Normal = 0,
        Oblique = 1,
        Italic = 3
    }

    /// <summary>
    /// Provides a set of static predefined font style /> values.
    /// </summary>
    public static class XFontStyles
    {
        /// <summary>
        /// Specifies a normal font style. />
        /// </summary>
        public static XFontStyle Normal => new(XFontStyleValue.Normal);

        /// <summary>
        /// Specifies an oblique font style.
        /// </summary>
        public static XFontStyle Oblique => new(XFontStyleValue.Oblique);

        /// <summary>
        /// Specifies an italic font style. />
        /// </summary>
        public static XFontStyle Italic => new(XFontStyleValue.Italic);

        internal static bool XFontStyleStringToKnownStyle(string style, IFormatProvider provider, ref XFontStyle xFontStyle)
        {
            if (style.Equals("Normal", StringComparison.OrdinalIgnoreCase))
            {
                xFontStyle = Normal;
                return true;
            }
            if (style.Equals("Italic", StringComparison.OrdinalIgnoreCase))
            {
                xFontStyle = Italic;
                return true;
            }
            if (!style.Equals("Oblique", StringComparison.OrdinalIgnoreCase))
                return false;

            xFontStyle = Oblique;
            return true;
        }
    }
}
