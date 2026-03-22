// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf.Content.Objects;

namespace Middleman.PdfFlex.Pdf.Content
{
    /// <summary>
    /// Represents the functionality for reading PDF content streams.
    /// </summary>
    public static class ContentReader
    {
        /// <summary>
        /// Reads the content stream(s) of the specified page.
        /// </summary>
        /// <param name="page">The page.</param>
        public static CSequence ReadContent(PdfPage page)
        {
            CParser parser = new(page);
            CSequence sequence = parser.ReadContent();

            return sequence;
        }

        /// <summary>
        /// Reads the specified content.
        /// </summary>
        /// <param name="content">The content.</param>
        public static CSequence ReadContent(byte[] content)
        {
            CParser parser = new(content);
            CSequence sequence = parser.ReadContent();
            return sequence;
        }

        /// <summary>
        /// Reads the specified content.
        /// </summary>
        /// <param name="content">The content.</param>
        public static CSequence ReadContent(MemoryStream content)
        {
            CParser parser = new(content);
            CSequence sequence = parser.ReadContent();
            return sequence;
        }
    }
}
