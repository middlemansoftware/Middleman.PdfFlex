// Copyright (c) Middleman Software, Inc. All rights reserved.
// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

#pragma warning disable 1591 // Because this is preview code.

using System.Diagnostics.Contracts;

namespace Middleman.PdfFlex.Internal
{
    /// <summary>
    /// (PdfFlex) System message.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    static class SyMsgs
    {
        public static string IndexOutOfRange3
            => "Index out of range.";

        public static SyMsg IndexOutOfRange2<T>(string parameter, T lowerBound, T upperBound)
            => new(SyMsgId.IndexOutOfRange,
            $"The value of '{parameter}' is out of range. " +
                   Invariant($"The value must be between '{lowerBound}' and '{upperBound}'."));
    }
}