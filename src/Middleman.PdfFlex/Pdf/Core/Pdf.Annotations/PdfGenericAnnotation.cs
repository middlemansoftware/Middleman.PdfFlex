// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Annotations
{
    /// <summary>
    /// Represents a generic annotation. Used for annotation dictionaries unknown to PdfFlex.
    /// </summary>
    sealed class PdfGenericAnnotation : PdfAnnotation
    {
        // DMH 6/7/06
        // Make this public so we can use it in PdfAnnotations to
        // get the metadata from existing annotations.
        public PdfGenericAnnotation(PdfDictionary dict)
            : base(dict)
        { }

        /// <summary>
        /// Predefined keys of this dictionary.
        /// </summary>
        internal new class Keys : PdfAnnotation.Keys
        {
            public static DictionaryMeta Meta => _meta ??= CreateMeta(typeof(Keys));

            static DictionaryMeta? _meta;
        }

        /// <summary>
        /// Gets the KeysMeta of this dictionary type.
        /// </summary>
        internal override DictionaryMeta Meta => Keys.Meta;
    }
}
