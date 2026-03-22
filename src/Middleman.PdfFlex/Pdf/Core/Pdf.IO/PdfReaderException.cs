// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Pdf.IO
{
    /// <summary>
    /// Exception thrown by PdfReader.
    /// </summary>
    public class PdfReaderException : PdfFlexException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfReaderException"/> class.
        /// </summary>
        public PdfReaderException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfReaderException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public PdfReaderException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfReaderException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public PdfReaderException(string message, Exception innerException)
            :
            base(message, innerException)
        { }
    }
}
