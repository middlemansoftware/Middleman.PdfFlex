// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Advanced
{
    /// <summary>
    /// Base class for FontTable, ImageTable, FormXObjectTable etc.
    /// </summary>
    public class PdfResourceTable
    {
        /// <summary>
        /// Base class for document wide resource tables.
        /// </summary>
        public PdfResourceTable(PdfDocument owner)
        {
            if (owner == null!)
                throw new ArgumentNullException(nameof(owner));
            Owner = owner;
        }

        /// <summary>
        /// Gets the owning document of this resource table.
        /// </summary>
        protected PdfDocument Owner { get; }
    }
}
