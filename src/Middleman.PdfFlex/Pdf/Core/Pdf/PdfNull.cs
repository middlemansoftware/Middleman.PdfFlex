// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents an indirect reference that is not in the cross-reference table.
    /// </summary>
    public sealed class PdfNull : PdfItem
    {
        // Reference: 3.2.8  Null Object / Page 63

        PdfNull()
        { }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() => "null";

        internal override void WriteObject(PdfWriter writer)
        {
            // Implemented because it must be overridden.
            writer.WriteRaw(" null ");
        }

        /// <summary>
        /// The only instance of this class.
        /// </summary>
        public static readonly PdfNull Value = new();
    }
}
