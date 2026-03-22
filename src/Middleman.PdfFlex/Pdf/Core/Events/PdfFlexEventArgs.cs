// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf;

namespace Middleman.PdfFlex.Events
{
    /// <summary>
    /// Base class for EventArgs in PdfFlex.
    /// </summary>
    public abstract class PdfFlexEventArgs(PdfObject source) : EventArgs
    {
        /// <summary>
        /// The source of the event.
        /// </summary>
        public PdfObject Source { get; set; } = source;
    }
}
