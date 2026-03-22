// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing.Pdf
{
    /// <summary>
    /// Indicates whether we are within a BT/ET block.
    /// </summary>
    enum StreamMode
    {
        /// <summary>
        /// Graphic mode. This is default.
        /// </summary>
        Graphic,

        /// <summary>
        /// Text mode.
        /// </summary>
        Text,
    }
}
