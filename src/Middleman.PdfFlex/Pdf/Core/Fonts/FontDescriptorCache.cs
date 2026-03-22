// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Fonts.OpenType;
using Middleman.PdfFlex.Internal;

namespace Middleman.PdfFlex.Fonts
{
    /// <summary>
    /// Global table of OpenType font descriptor objects.
    /// </summary>
    static class FontDescriptorCache
    {
        /// <summary>
        /// Gets the FontDescriptor identified by the specified XFont. If no such object 
        /// exists, a new FontDescriptor is created and added to the cache.
        /// </summary>
        public static FontDescriptor GetOrCreateDescriptorFor(XFont font)
        {
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            font.GlyphTypeface.CheckVersion();

            //FontSelector1 selector = new FontSelector1(font);
            string fontDescriptorKey = font.GlyphTypeface.Key;
            try
            {
                var cache = Globals.Global.Fonts.FontDescriptorCache;
                Lock.EnterFontFactory();
                if (cache.TryGetValue(fontDescriptorKey, out var descriptor))
                    return descriptor;

                descriptor = new OpenTypeDescriptor(fontDescriptorKey, font);
                cache.Add(fontDescriptorKey, descriptor);
                return descriptor;
            }
            finally { Lock.ExitFontFactory(); }
        }

        public static FontDescriptor GetOrCreateDescriptorFor(XGlyphTypeface glyphTypeface)
        {
            glyphTypeface.CheckVersion();

            string fontDescriptorKey = glyphTypeface.Key;
            try
            {
                var cache = Globals.Global.Fonts.FontDescriptorCache;
                Lock.EnterFontFactory();
                if (cache.TryGetValue(fontDescriptorKey, out var descriptor))
                    return descriptor;

                descriptor = new OpenTypeDescriptor(fontDescriptorKey, glyphTypeface);
                cache.Add(fontDescriptorKey, descriptor);
                return descriptor;
            }
            finally { Lock.ExitFontFactory(); }
        }

        /// <summary>
        /// Gets the FontDescriptor identified by the specified FontSelector. If no such object 
        /// exists, a new FontDescriptor is created and added to the stock.
        /// </summary>
        public static FontDescriptor GetOrCreateDescriptor(string fontFamilyName, XFontStyleEx style)
        {
            if (String.IsNullOrEmpty(fontFamilyName))
                throw new ArgumentNullException(nameof(fontFamilyName));

            //FontSelector1 selector = new FontSelector1(fontFamilyName, style);
            string fontDescriptorKey = FontDescriptor.ComputeFdKey(fontFamilyName, style);
            try
            {
                var cache = Globals.Global.Fonts.FontDescriptorCache;
                Lock.EnterFontFactory();
                if (!cache.TryGetValue(fontDescriptorKey, out var descriptor))
                {
                    var font = new XFont(fontFamilyName, 10, style);
                    descriptor = GetOrCreateDescriptorFor(font);
                    // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd because there is not TryAdd in .NET Framework
                    if (cache.ContainsKey(fontDescriptorKey))
                        _ = typeof(int);  // Just a NOP for a break point.
                    else
                        cache.Add(fontDescriptorKey, descriptor);
                }
                return descriptor;
            }
            finally { Lock.ExitFontFactory(); }
        }

        internal static void Reset()
        {
            Globals.Global.Fonts.FontDescriptorCache.Clear();
        }
    }
}

namespace Middleman.PdfFlex.Internal
{
    partial class Globals
    {
        partial class FontStorage
        {
            /// <summary>
            /// Maps font descriptor key to font descriptor which is currently only an OpenTypeFontDescriptor.
            /// </summary>
            public readonly Dictionary<string, FontDescriptor> FontDescriptorCache = [];
        }
    }
}
