// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Pdf.Signatures
{
    /// <summary>
    /// Internal PDF array used for digital signatures.
    /// For digital signatures, we have to add an array with four integers,
    /// but at the time we add the array we cannot yet determine
    /// how many digits those integers will have.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="paddingRight">The count of spaces added after the array.</param>
    /// <param name="items">The contents of the array.</param>
    sealed class PdfArrayWithPadding(PdfDocument document, int paddingRight, params PdfItem[] items)
        : PdfArray(document, items)
    {
        public int PaddingRight { get; init; } = paddingRight;

        internal override void WriteObject(PdfWriter writer)
        {
            StartPosition = writer.Position;

            base.WriteObject(writer);
            writer.WriteRaw(new String(' ', PaddingRight));
        }

        /// <summary>
        /// Position of the first byte of this string in PdfWriter’s stream.
        /// </summary>
        public SizeType StartPosition { get; internal set; }
    }
}
