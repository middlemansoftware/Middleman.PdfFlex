// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Specifies the order for matrix transform operations.
    /// </summary>
    public enum XMatrixOrder
    {
        /// <summary>
        /// The new operation is applied before the old operation.
        /// </summary>
        Prepend = 0,

        /// <summary>
        /// The new operation is applied after the old operation.
        /// </summary>
        Append = 1,
    }
}
