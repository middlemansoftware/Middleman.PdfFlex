// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Text;

namespace Middleman.PdfFlex.Pdf.Internal
{
    /// <summary>
    /// An encoder for Unicode strings. 
    /// (That means, a character represents a glyph index.)
    /// </summary>
    sealed class RawUnicodeEncoding : Encoding
    {
        public RawUnicodeEncoding()
        { }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            // Each character represents exactly an ushort value, which is a glyph index.
            return 2 * count;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int count = charCount; count > 0; charIndex++, count--)
            {
                char ch = chars[charIndex];
                bytes[byteIndex++] = (byte)(ch >> 8);
                bytes[byteIndex++] = (byte)ch;
            }
            return charCount * 2;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return count / 2;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int count = byteCount; count > 0; byteIndex += 2, charIndex++, count--)
            {
                chars[charIndex] = (char)((int)bytes[byteIndex] << 8 + (int)bytes[byteIndex + 1]);
            }
            return byteCount;
        }

        public override int GetMaxByteCount(int charCount) => charCount * 2;

        public override int GetMaxCharCount(int byteCount) => byteCount / 2;
    }
}
