// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Pdf.Advanced
{
    /// <summary>
    /// Represents a PDF page object.
    /// </summary>
    class PdfPageInheritableObjects : PdfDictionary
    {
        public PdfPageInheritableObjects()
        { }

        // TODO_OLD Inheritable Resources not yet supported

        /// <summary>
        /// 
        /// </summary>
        public PdfRectangle MediaBox
        {
            get => _mediaBox;
            set => _mediaBox = value;
        }
        PdfRectangle _mediaBox = default!;

        public PdfRectangle CropBox
        {
            get => _cropBox;
            set => _cropBox = value;
        }
        PdfRectangle _cropBox = default!;

        public int Rotate
        {
            get => _rotate;
            set
            {
                if (value % 90 != 0)
                    throw new ArgumentException("The value must be a multiple of 90.", nameof(value));
                _rotate = value;
            }
        }
        int _rotate;
    }
}
