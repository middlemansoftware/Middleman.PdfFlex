// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.Logging;

namespace Middleman.PdfFlex.Logging
{
    /// <summary>
    /// Defines the logging categories of PdfFlex.
    /// </summary>
    public static class PdfFlexLogCategory
    {
        /// <summary>
        /// Logger category for creating or saving documents, adding or removing pages,
        /// and other document level specific action.s
        /// </summary>
        public const string DocumentProcessing = "Document processing";

        /// <summary>
        /// Logger category for processing bitmap images.
        /// </summary>
        public const string ImageProcessing = "Image processing";

        /// <summary>
        /// Logger category for creating XFont objects.
        /// </summary>
        public const string FontManagement = "Font management";

        /// <summary>
        /// Logger category for reading PDF documents.
        /// </summary>
        public const string PdfReading = "PDF reading";
    }

    /// <summary>
    /// Provides a single host for logging in PdfFlex.
    /// The logger factory is taken from LogHost.
    /// </summary>
    public static class PdfFlexLogHost
    {
        /// <summary>
        /// Gets the general PdfFlex logger.
        /// This the same you get from LogHost.Logger.
        /// </summary>
        public static ILogger Logger => LogHost.Logger;

        #region Specific logger

        /// <summary>
        /// Gets the global PdfFlex font management logger.
        /// </summary>
        public static ILogger DocumentProcessingLogger
        {
            get
            {
                // We do not need lock, because even creating two loggers has no negative effects.
                {
                    CheckFactoryHasChanged();
                    return _documentProcessingLogger ??= LogHost.CreateLogger(PdfFlexLogCategory.DocumentProcessing);
                }
            }
        }
        static ILogger? _documentProcessingLogger;

        /// <summary>
        /// Gets the global PdfFlex image processing logger.
        /// </summary>
        public static ILogger ImageProcessingLogger
        {
            get
            {
                {
                    CheckFactoryHasChanged();
                    return _imageProcessingLogger ??= LogHost.CreateLogger(PdfFlexLogCategory.ImageProcessing);
                }
            }
        }
        static ILogger? _imageProcessingLogger;

        /// <summary>
        /// Gets the global PdfFlex font management logger.
        /// </summary>
        public static ILogger FontManagementLogger
        {
            get
            {
                {
                    CheckFactoryHasChanged();
                    return _fontManagementLogger ??= LogHost.CreateLogger(PdfFlexLogCategory.FontManagement);
                }
            }
        }
        static ILogger? _fontManagementLogger;

        /// <summary>
        /// Gets the global PdfFlex document reading logger.
        /// </summary>
        public static ILogger PdfReadingLogger
        {
            get
            {
                {
                    CheckFactoryHasChanged();
                    return _pdfReadingLogger ??= LogHost.CreateLogger(PdfFlexLogCategory.PdfReading);
                }
            }
        }
        static ILogger? _pdfReadingLogger;
        #endregion

        static void CheckFactoryHasChanged()
        {
            // Sync with LogHost factory.
            if (!ReferenceEquals(_factory, LogHost.Factory))
            {
                ResetLogging();
                _factory = LogHost.Factory;
            }
        }
        static ILoggerFactory? _factory;

        /// <summary>
        /// Resets all loggers after an update of global logging factory.
        /// </summary>
        internal static void ResetLogging()
        {
            _factory = default;
            _documentProcessingLogger = default;
            _imageProcessingLogger = default;
            _fontManagementLogger = default;
            _pdfReadingLogger = default;
        }
    }
}
