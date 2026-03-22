// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.AcroForms
{
    /// <summary>
    /// Represents the push button field.
    /// </summary>
    public sealed class PdfPushButtonField : PdfButtonField
    {
        /// <summary>
        /// Initializes a new instance of PdfPushButtonField.
        /// </summary>
        internal PdfPushButtonField(PdfDocument document)
            : base(document)
        {
            _document = document;
        }

        internal PdfPushButtonField(PdfDictionary dict)
            : base(dict)
        { }

        /// <summary>
        /// Predefined keys of this dictionary. 
        /// The description comes from PDF 1.4 Reference.
        /// </summary>
        public new class Keys : PdfAcroField.Keys
        {
            internal static DictionaryMeta Meta => _meta ??= CreateMeta(typeof(Keys));

            static DictionaryMeta? _meta;
        }

        /// <summary>
        /// Gets the KeysMeta of this dictionary type.
        /// </summary>
        internal override DictionaryMeta Meta => Keys.Meta;
    }
}
