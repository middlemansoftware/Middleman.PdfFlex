// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Holds PDF specific information of the document.
    /// </summary>
    public sealed class PdfDocumentSettings
    {
        internal PdfDocumentSettings(PdfDocument document)
        { }

        /// <summary>
        /// Gets or sets the default trim margins.
        /// </summary>
        public TrimMargins TrimMargins
        {
            get
            {
                if (_trimMargins == null)
                    _trimMargins = new();
                return _trimMargins;
            }
            set
            {
                if (_trimMargins == null)
                    _trimMargins = new();
                if (value != null)
                {
                    _trimMargins.Left = value.Left;
                    _trimMargins.Right = value.Right;
                    _trimMargins.Top = value.Top;
                    _trimMargins.Bottom = value.Bottom;
                }
                else
                    _trimMargins.All = XUnit.Zero;
            }
        }

        TrimMargins _trimMargins = new();
    }
}