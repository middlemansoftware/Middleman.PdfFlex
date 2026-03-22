// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.Logging;

namespace Middleman.PdfFlex.Pdf.Signatures
{
    /// <summary>
    /// PdfFlex cryptography message.
    /// </summary>
    readonly struct PsCryptoMsg(PsCryptoMsgId id, string message)
    {
        public PsCryptoMsgId Id { get; init; } = id;

        public string Message { get; init; } = message;

        public EventId EventId => new((int)Id, EventName);

        public string EventName => Id.ToString();
    }
}