// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents an indirect null value. This type is not used by PdfFlex, but at least
    /// one tool from Adobe creates PDF files with a null object.
    /// </summary>
    public sealed class PdfNullObject : PdfObject
    {
        // Reference: 3.2.8  Null Object / Page 63

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfNullObject"/> class.
        /// </summary>
        public PdfNullObject()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfNullObject"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        public PdfNullObject(PdfDocument document)
            : base(document)
        { }

        /// <summary>
        /// Returns the string "null".
        /// </summary>
        public override string ToString() => "null";

        /// <summary>
        /// Writes the keyword «null».
        /// </summary>
        internal override void WriteObject(PdfWriter writer)
        {
            writer.WriteBeginObject(this);
            writer.WriteRaw(" null ");
            writer.WriteEndObject();
        }
    }
}
