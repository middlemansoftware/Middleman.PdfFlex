// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Sets the mode for the Deflater (FlateEncoder).
    /// </summary>
    public enum PdfFlateEncodeMode
    {
        /// <summary>
        /// The default mode.
        /// </summary>
        Default,

        /// <summary>
        /// Fast encoding, but larger PDF files.
        /// </summary>
        BestSpeed,

        /// <summary>
        /// Best compression, but takes more time.
        /// </summary>
        BestCompression,
    }
}