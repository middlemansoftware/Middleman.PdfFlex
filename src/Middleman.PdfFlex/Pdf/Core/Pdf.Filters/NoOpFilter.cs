// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Filters
{
    /// <summary>
    /// Implements a dummy filter used for not implemented filters.
    /// </summary>
    public abstract class NoOpFiler : Filter
    {
        /// <summary>
        /// Returns a copy of the input data.
        /// </summary>
        public override byte[] Encode(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return (byte[])data.Clone();
        }

        /// <summary>
        /// Returns a copy of the input data.
        /// </summary>
        public override byte[] Decode(byte[] data, FilterParms? parms)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return (byte[])data.Clone();
        }
    }
}
