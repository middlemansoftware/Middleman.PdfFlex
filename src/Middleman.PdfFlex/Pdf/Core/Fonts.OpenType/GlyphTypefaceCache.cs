// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Internal;

namespace Middleman.PdfFlex.Fonts.OpenType
{
    /// <summary>
    /// Global table of all glyph typefaces.
    /// </summary>
    static class GlyphTypefaceCache
    {
        public static bool TryGetGlyphTypeface(string key, [MaybeNullWhen(false)] out XGlyphTypeface glyphTypeface)
        {
            try
            {
                Lock.EnterFontFactory();
                bool result = Globals.Global.Fonts.GlyphTypefacesByKey.TryGetValue(key, out glyphTypeface);
                return result;
            }
            finally { Lock.ExitFontFactory(); }
        }

        public static void AddGlyphTypeface(XGlyphTypeface glyphTypeface)
        {
            try
            {
                Lock.EnterFontFactory();
                Debug.Assert(!Globals.Global.Fonts.GlyphTypefacesByKey.ContainsKey(glyphTypeface.Key));
                Globals.Global.Fonts.GlyphTypefacesByKey.Add(glyphTypeface.Key, glyphTypeface);
            }
            finally { Lock.ExitFontFactory(); }
        }

        internal static void Reset()
        {
            Globals.Global.Fonts.GlyphTypefacesByKey.Clear();
        }

        internal static string GetCacheState()
        {
            var state = new StringBuilder();
            state.Append("====================\n");
            state.Append("Glyph typefaces by name\n");
            Dictionary<string, XGlyphTypeface>.KeyCollection familyKeys = Globals.Global.Fonts.GlyphTypefacesByKey.Keys;
            int count = familyKeys.Count;
            string[] keys = new string[count];
            familyKeys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);
            foreach (string key in keys)
                state.AppendFormat("  {0}: {1}\n", key, Globals.Global.Fonts.GlyphTypefacesByKey[key].DebuggerDisplay);
            state.Append("\n");
            return state.ToString();
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
            /// Maps typeface key to glyph typeface.
            /// </summary>
            public readonly Dictionary<string, XGlyphTypeface> GlyphTypefacesByKey = new(StringComparer.Ordinal);
        }
    }
}
