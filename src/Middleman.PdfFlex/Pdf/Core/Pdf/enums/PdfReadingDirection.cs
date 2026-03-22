// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Specifies how the document should be displayed by a viewer when opened.
    /// </summary>
    public enum PdfReadingDirection
    {
        /// <summary>
        /// Left to right.
        /// </summary>
        LeftToRight,

        /// <summary>
        /// Right to left (including vertical writing systems, such as Chinese, Japanese, and Korean)
        /// </summary>
        RightToLeft,
    }
}