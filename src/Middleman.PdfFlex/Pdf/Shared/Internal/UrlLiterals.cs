// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Internal
{
    /// <summary>
    /// URLs used in PdfFlex are maintained only here.
    /// </summary>
    class UrlLiterals  // CHECK_BEFORE_RELEASE
    {
#if DEBUG
        const string DocsPdfFlexUrl = "http://localhost:8094/";
#else
        const string DocsPdfFlexUrl = "https://docs.pdfflex.net/";
#endif

        /// <summary>
        /// URL for index page.
        /// "https://docs.pdfflex.net/"
        /// </summary>
        public const string LinkToRoot = DocsPdfFlexUrl;

        /// <summary>
        /// URL for missing assets error message.
        /// "https://docs.pdfflex.net/link/download-assets.html"
        /// </summary>
        public const string LinkToAssetsDoc = DocsPdfFlexUrl + "link/download-assets.html";

        /// <summary>
        /// URL for missing font resolver.
        /// "https://docs.pdfflex.net/link/font-resolving.html"
        /// </summary>
        public const string LinkToFontResolving = DocsPdfFlexUrl + "link/font-resolving.html";

        /// <summary>
        /// URL for advanced font resolving documentation.
        /// "https://docs.middleman.tv/pdfflex/font-resolving-advanced.html"
        /// </summary>
        public const string LinkToFontResolvingAdvanced = DocsPdfFlexUrl + "link/font-resolving-advanced.html";

        /// <summary>
        /// URL shown when a PDF file cannot be opened/parsed.
        /// "https://docs.pdfflex.net/link/cannot-open-pdf-6.2.html"
        /// </summary>
        public const string LinkToCannotOpenPdfFile = DocsPdfFlexUrl + "link/cannot-open-pdf-6.2.html";
    }
}
