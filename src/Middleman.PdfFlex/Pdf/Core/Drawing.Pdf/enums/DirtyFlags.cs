// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Drawing.Pdf
{
    [Flags]
    enum DirtyFlags
    {
        Ctm = 0x00000001,
        ClipPath = 0x00000002,
        LineWidth = 0x00000010,
        LineJoin = 0x00000020,
        MiterLimit = 0x00000040,
        StrokeFill = 0x00000070,
    }
}
