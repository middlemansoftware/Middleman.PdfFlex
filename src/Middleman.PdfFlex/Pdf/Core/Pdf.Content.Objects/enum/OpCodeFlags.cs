// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Pdf.Content.Objects
{
    /// <summary>
    /// Specifies the group of operations the op-code belongs to.
    /// </summary>
    [Flags]
    public enum OpCodeFlags
    {
        /// <summary>
        /// 
        /// </summary>
        None,

        /// <summary>
        /// 
        /// </summary>
        TextOut = 0x0001,
        //Color, Pattern, Images,...
    }
}
