// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex
{
    /// <summary>
    /// Base class of all exceptions in the PdfFlex library.
    /// </summary>
    public class PdfFlexException : Exception
    {
        // The class is not yet used

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfFlexException"/> class.
        /// </summary>
        public PdfFlexException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfFlexException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public PdfFlexException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfFlexException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public PdfFlexException(string message, Exception innerException) :
            base(message, innerException)
        { }
    }
}
