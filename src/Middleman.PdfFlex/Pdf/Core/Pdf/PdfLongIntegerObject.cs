// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents an indirect 64-bit signed integer value. This type is not used by PdfFlex. If it is imported from
    /// an external PDF file, the value is converted into a direct object.
    /// </summary>
    [DebuggerDisplay("({" + nameof(Value) + "})")]
    public sealed class PdfLongIntegerObject : PdfNumberObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfLongIntegerObject"/> class.
        /// </summary>
        public PdfLongIntegerObject()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfLongIntegerObject"/> class.
        /// </summary>
        public PdfLongIntegerObject(long value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfLongIntegerObject"/> class.
        /// </summary>
        public PdfLongIntegerObject(PdfDocument document, long value)
            : base(document)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value as 64-bit integer.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Returns the integer as string.
        /// </summary>
        public override string ToString() 
            => Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Writes the integer literal.
        /// </summary>
        internal override void WriteObject(PdfWriter writer)
        {
            writer.WriteBeginObject(this);
            writer.Write(Value);
            writer.WriteEndObject();
        }
    }
}
