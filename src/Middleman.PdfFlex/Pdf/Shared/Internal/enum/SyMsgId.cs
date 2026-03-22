// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#pragma warning disable 1591 // Because this is preview code.

namespace Middleman.PdfFlex.Internal
{
    /// <summary>
    /// System message ID.
    /// </summary>
    public enum SyMsgId
    {
        None = 0,

        // ----- General Messages ---------------------------------------------------------------------

        IndexOutOfRange = MessageIdOffset.Sy,
        IndexOutOfRange2,
    }

    /// <summary>
    /// Offsets to ensure that all message IDs are pairwise distinct
    /// within PdfFlex foundation.
    /// </summary>
    public enum MessageIdOffset
    {
        Sy = 1000,
        Ps = 3000,
        PsCrypto = 4000,
        MdDom = 5000,
        MdPdf = 6000,
        MdRtf = 7000,
    }
}
