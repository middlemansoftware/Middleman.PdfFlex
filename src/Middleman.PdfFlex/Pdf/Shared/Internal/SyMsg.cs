// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#pragma warning disable 1591 // Because this is preview code.

using Microsoft.Extensions.Logging;

namespace Middleman.PdfFlex.Internal
{
    /// <summary>
    /// (PdfFlex) System message.
    /// </summary>
    public readonly struct SyMsg(SyMsgId id, string message)
    {
        public SyMsgId Id { get; init; } = id;

        public string Message { get; init; } = message;

        public EventId EventId => new((int)Id, EventName);

        public string EventName => Id.ToString();
    }
}
