// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents an indirect real value. This type is not used by PdfFlex. If it is imported from
    /// an external PDF file, the value is converted into a direct object.
    /// </summary>
    public sealed class PdfRealObject : PdfNumberObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfRealObject"/> class.
        /// </summary>
        public PdfRealObject()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfRealObject"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public PdfRealObject(double value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfRealObject"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="value">The value.</param>
        public PdfRealObject(PdfDocument document, double value)
            : base(document)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Returns the real as a culture invariant string.
        /// </summary>
        public override string ToString() 
            => Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Writes the real literal.
        /// </summary>
        internal override void WriteObject(PdfWriter writer)
        {
            writer.WriteBeginObject(this);
            writer.Write(Value);
            writer.WriteEndObject();
        }
    }
}
