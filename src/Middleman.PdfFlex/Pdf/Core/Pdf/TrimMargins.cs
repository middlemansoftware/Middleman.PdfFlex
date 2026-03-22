// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Diagnostics;
using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents trim margins added to the page.
    /// </summary>
    [DebuggerDisplay("(Left={Left.Millimeter}mm, Right={Right.Millimeter}mm, Top={Top.Millimeter}mm, Bottom={Bottom.Millimeter}mm)")]
    public sealed class TrimMargins
    {
        /// <summary>
        /// Sets all four crop margins simultaneously.
        /// </summary>
        public XUnit All
        {
            set
            {
                Left = value;
                Right = value;
                Top = value;
                Bottom = value;
            }
        }

        /// <summary>
        /// Gets or sets the left crop margin.
        /// </summary>
        public XUnit Left { get; set; }

        /// <summary>
        /// Gets or sets the right crop margin.
        /// </summary>
        public XUnit Right { get; set; }

        /// <summary>
        /// Gets or sets the top crop margin.
        /// </summary>
        public XUnit Top { get; set; }

        /// <summary>
        /// Gets or sets the bottom crop margin.
        /// </summary>
        public XUnit Bottom { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has at least one margin with a value other than zero.
        /// </summary>
        public bool AreSet => Left.Value != 0 || Right.Value != 0 || Top.Value != 0 || Bottom.Value != 0;
    }
}
