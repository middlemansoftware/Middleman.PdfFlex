// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

// ReSharper disable InconsistentNaming

namespace Middleman.PdfFlex.Pdf.Signatures
{
    /// <summary>
    /// Specifies the algorithm used to generate the message digest.
    /// </summary>
    public enum PdfMessageDigestType
    {
        /// <summary>
        /// (PDF 1.3)
        /// </summary>
        SHA1 = 0,

        /// <summary>
        /// (PDF 1.6)
        /// </summary>
        SHA256 = 1,

        /// <summary>
        /// (PDF 1.7)
        /// </summary>
        SHA384 = 2,

        /// <summary>
        /// (PDF 1.7)
        /// </summary>
        SHA512 = 3,

        /// <summary>
        /// (PDF 1.7)
        /// </summary>
        RIPEMD160 = 4
    }
}
