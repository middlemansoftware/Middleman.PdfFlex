// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies how the content of an existing PDF page and new content is combined.
    /// </summary>
    public enum XGraphicsPdfPageOptions
    {
        /// <summary>
        /// The new content is inserted behind the old content, and any subsequent drawing is done above the existing graphic.
        /// </summary>
        Append,

        /// <summary>
        /// The new content is inserted before the old content, and any subsequent drawing is done beneath the existing graphic.
        /// </summary>
        Prepend,

        /// <summary>
        /// The new content entirely replaces the old content, and any subsequent drawing is done on a blank page.
        /// </summary>
        Replace,
    }
}
