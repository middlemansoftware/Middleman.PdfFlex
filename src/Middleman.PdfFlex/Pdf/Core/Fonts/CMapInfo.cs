// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.Logging;
using Middleman.PdfFlex.Fonts.Internal;
using Middleman.PdfFlex.Fonts.OpenType;
using Middleman.PdfFlex.Logging;
using Middleman.PdfFlex.Pdf.Internal;

namespace Middleman.PdfFlex.Fonts
{
    /// <summary>
    /// Helper class that determines the characters used in a particular font.
    /// </summary>
    class CMapInfo
    {
        public CMapInfo(OpenTypeDescriptor descriptor)
        {
            Debug.Assert(descriptor != null);
            _descriptor = descriptor;
        }
        readonly OpenTypeDescriptor _descriptor;

        public void AddChars(CodePointGlyphIndexPair[] codePoints)
        {
            int length = codePoints.Length;

            for (int idx = 0; idx < length; idx++)
            {
                var item = codePoints[idx];
                if (item.GlyphIndex == 0)
                    continue;

                if (CodePointsToGlyphIndices.ContainsKey(item.CodePoint))
                    continue;

                CodePointsToGlyphIndices.Add(item.CodePoint, item.GlyphIndex);
                GlyphIndices[item.GlyphIndex] = default;
                MinCodePoint = Math.Min(MinCodePoint, item.CodePoint);
                MaxCodePoint = Math.Max(MaxCodePoint, item.CodePoint);
            }
        }

        internal bool Contains(char ch)
            => CodePointsToGlyphIndices.ContainsKey(ch);

        public int[] Chars
        {
            get
            {
                var chars = new int[CodePointsToGlyphIndices.Count];
                CodePointsToGlyphIndices.Keys.CopyTo(chars, 0);
                Array.Sort(chars);
                return chars;
            }
        }

        public ushort[] GetGlyphIndices()
        {
            var indices = new ushort[GlyphIndices.Count];
            GlyphIndices.Keys.CopyTo(indices, 0);
            Array.Sort(indices);
            return indices;
        }

        public int MinCodePoint = Int32.MaxValue;  
        public int MaxCodePoint = Int32.MinValue;

        /// <summary>
        /// Maps a Unicode code point to a glyph ID.
        /// </summary>
        public Dictionary<int, ushort> CodePointsToGlyphIndices = [];

        /// <summary>
        /// Collects all used glyph IDs. Value is not used.
        /// </summary>
        public Dictionary<ushort, object?> GlyphIndices = [];
    }
}
