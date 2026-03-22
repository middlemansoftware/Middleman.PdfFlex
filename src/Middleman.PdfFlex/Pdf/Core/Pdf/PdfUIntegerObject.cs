// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#if false // DELETE 2025-12-31 - PDF has no explicit unsigned number type.
using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents an indirect 32-bit unsigned integer value. This type is not used by PdfFlex. If it is imported from
    /// an external PDF file, the value is converted into a direct object.
    /// </summary>
    [DebuggerDisplay("({" + nameof(Value) + "})")]
    [Obsolete("This class is deprecated and will be removed.")]
    public sealed class PdfUIntegerObject : PdfNumberObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfUIntegerObject"/> class.
        /// </summary>
        public PdfUIntegerObject()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfUIntegerObject"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public PdfUIntegerObject(uint value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfUIntegerObject"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="value">The value.</param>
        public PdfUIntegerObject(PdfDocument document, uint value)
            : base(document)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value as unsigned integer.
        /// </summary>
        public uint Value { get; }

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
#endif
