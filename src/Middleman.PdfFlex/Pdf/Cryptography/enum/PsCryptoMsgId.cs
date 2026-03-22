// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Internal;

namespace Middleman.PdfFlex.Pdf.Signatures
{
    /// <summary>
    /// PdfFlex cryptography message IDs.
    /// </summary>
    // GPT 4 recommends to use Crypto instead of Cry as abbreviation,
    // because Cry is too ambiguous.
    enum PsCryptoMsgId
    {
        None = 0,

        // ----- Signature messages ----------------------------------------------------------------

        SampleMessage1 = MessageIdOffset.PsCrypto,
    }
}
