// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Content
{
    /// <summary>
    /// Exception thrown by page content reader.
    /// </summary>
    public class ContentReaderException : PdfFlexException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentReaderException"/> class.
        /// </summary>
        public ContentReaderException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentReaderException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ContentReaderException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentReaderException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ContentReaderException(string message, Exception innerException) :
            base(message, innerException)
        { }
    }
}
