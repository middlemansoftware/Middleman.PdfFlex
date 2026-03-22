// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.Logging;
using Middleman.PdfFlex.Pdf;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member because it is for internal use only.

namespace Middleman.PdfFlex.Logging
{
    /// <summary>
    /// Defines the logging event IDs of PdfFlex.
    /// </summary>
    public static class PdfFlexEventId  // draft...
    {
        public const int DocumentCreated = StartId + 1;
        public const int DocumentSaved = StartId + 2;
        public const int PageCreated = StartId + 3;
        public const int PageAdded = StartId + 4;
        public const int GraphicsCreated = StartId + 5;
        public const int FontCreated = StartId + 6;

        // Reading PDFs
        public const int PdfReaderIssue = StartId + 10;
        public const int StreamIssue = StartId + 11;
        public const int EndOfStreamReached = StartId + 12;
        public const int SkippedIllegalBlanksAfterStreamKeyword = StartId + 13;
        public const int StreamKeywordFollowedBySingleCR = StartId + 14;
        public const int StreamKeywordFollowedByIllegalBytes = StartId + 15;

        internal const int Placeholder = StartId + 1234;
        const int StartId = 50000;
    };

    public static class PdfFlexEventName
    {
        public const string DocumentCreated = "Document created";
        public const string DocumentSaved = "Document saved";
        public const string PageCreated = "Page created";
        public const string PageAdded = "Page creation2";
        public const string GraphicsCreated = "Graphics created";
        public const string FontCreated = "Font created";

        public const string PdfReaderIssue = "PDF reader issue";
        public const string StreamIssue = "Stream issue";
        public const string EndOfStreamReached = "End of stream reached";
        public const string SkippedIllegalBlanksAfterStreamKeyword = "Skipped illegal blanks after stream keyword";
        public const string StreamKeywordFollowedBySingleCR = "Stream keyword followed by single CR";
        public const string StreamKeywordFollowedByIllegalBytes = "Stream keyword followed by illegal bytes";
    }

    public static class PdfFlexEvent
    {
        public static EventId DocumentCreate = new(PdfFlexEventId.DocumentCreated, PdfFlexEventName.DocumentCreated);
        public static EventId DocumentSaved = new(PdfFlexEventId.DocumentSaved, PdfFlexEventName.DocumentSaved);
        public static EventId PageCreate = new(PdfFlexEventId.PageCreated, PdfFlexEventName.PageCreated);
        public static EventId PageAdded = new(PdfFlexEventId.PageAdded, PdfFlexEventName.PageAdded);
        public static EventId FontCreate = new(PdfFlexEventId.FontCreated, PdfFlexEventName.FontCreated);

        public static EventId PdfReaderIssue = new(PdfFlexEventId.PdfReaderIssue, PdfFlexEventName.PdfReaderIssue);

        public static EventId Placeholder = new(999999, "Placeholder");
    }

    /// <summary>
    /// Defines the logging high performance messages of PdfFlex.
    /// </summary>
    public static partial class LogMessages
    {
#pragma warning disable SYSLIB1006

        [LoggerMessage(
            Level = LogLevel.Information,
            EventId = PdfFlexEventId.DocumentCreated,
            EventName = PdfFlexEventName.DocumentCreated,
            Message = "New PDF document '{DocumentName}' created.")]
        public static partial void PdfDocumentCreated(this ILogger logger,
            string? documentName);

        [LoggerMessage(
            Level = LogLevel.Information,
            EventId = PdfFlexEventId.DocumentSaved,
            EventName = PdfFlexEventName.DocumentSaved,
            Message = "PDF document '{documentName}' saved.")]
        public static partial void PdfDocumentSaved(this ILogger logger,
            string? documentName);

        [LoggerMessage(
            Level = LogLevel.Information,
            EventId = PdfFlexEventId.PageCreated,
            EventName = PdfFlexEventName.PageCreated,
            Message = "New PDF page added to document '{documentName}'.")]
        public static partial void NewPdfPageCreated(this ILogger logger,
            string? documentName);

        [LoggerMessage(
            Level = LogLevel.Information,
            EventId = PdfFlexEventId.PageAdded,
            EventName = PdfFlexEventName.PageAdded,
            Message = "Existing PDF page added to document '{documentName}'.")]
        public static partial void ExistingPdfPageAdded(this ILogger logger,
            string? documentName);

        [LoggerMessage(
            Level = LogLevel.Information,
            EventId = PdfFlexEventId.GraphicsCreated,
            EventName = PdfFlexEventName.GraphicsCreated,
            Message = "New XGraphics created from '{source}'.")]
        public static partial void XGraphicsCreated(this ILogger logger,
            string? source);

        // Reading PDFs

        [LoggerMessage(
            Level = LogLevel.Error,
            EventId = PdfFlexEventId.StreamIssue,
            EventName = PdfFlexEventName.StreamIssue,
            Message = "{Status} {BytesRead} of {Length} bytes were received. " +
                      "We strongly recommend using streams with PdfReader whose content is fully available. " +
                      "Copy the stream containing the file to a MemoryStream for example.")]
        public static partial void StreamIssue(this ILogger logger,
            string status, int bytesRead, int length);

        [LoggerMessage(
            Level = LogLevel.Warning,
            EventId = PdfFlexEventId.EndOfStreamReached,
            EventName = PdfFlexEventName.EndOfStreamReached,
            Message = "End of stream reached while reading {Length} bytes at position {Position}, but got only {BytesRead} bytes.")]
        public static partial void EndOfStreamReached(this ILogger logger,
             int length, SizeType position, int bytesRead);

        [LoggerMessage(
            Level = LogLevel.Warning,
            EventId = PdfFlexEventId.SkippedIllegalBlanksAfterStreamKeyword,
            EventName = PdfFlexEventName.SkippedIllegalBlanksAfterStreamKeyword,
            Message = "Skipped {BlankCount} illegal blanks behind keyword 'stream' at position {Position} in object {ObjectId}.")]
        public static partial void SkippedIllegalBlanksAfterStreamKeyword(this ILogger logger,
            int blankCount, SizeType position, PdfObjectID objectId);

        [LoggerMessage(
            Level = LogLevel.Warning,
            EventId = PdfFlexEventId.StreamKeywordFollowedBySingleCR,
            EventName = PdfFlexEventName.StreamKeywordFollowedBySingleCR,
            Message = "Keyword 'stream' followed by single CR is illegal at position {Position} in object {ObjectId}.")]
        public static partial void StreamKeywordFollowedBySingleCR(this ILogger logger,
            SizeType position, PdfObjectID objectId);

        [LoggerMessage(
            Level = LogLevel.Warning,
            EventId = PdfFlexEventId.StreamKeywordFollowedByIllegalBytes,
            EventName = PdfFlexEventName.StreamKeywordFollowedByIllegalBytes,
            Message = "Keyword 'stream' followed by illegal bytes at position {Position} in object {ObjectId}.")]
        public static partial void StreamKeywordFollowedByIllegalBytes(this ILogger logger,
            SizeType position, PdfObjectID objectId);

        //[LoggerMessage(EventId = 23, EventName = "hallo", Level = LogLevel.Warning, Message = "This is a warning: `{someText}`")]
        //public static partial void WarningMessage(this ILogger logger, string someText);
    }

#if true_
    class LogTestCode
    {
        void FooBar()
        {
            //var ss = PSEventId.Test1;
            //PdfFlexLogHost.Logger.LogError(ss, "message");
            LoggerMessage.Define<char>(LogLevel.Critical, )
        }
    }
#endif
}
