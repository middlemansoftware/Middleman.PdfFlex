// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Internal
{
    class PdfDiagnostics
    {
        public static bool TraceCompressedObjects
        {
            get => _traceCompressedObjects;
            set => _traceCompressedObjects = value;
        }
        static bool _traceCompressedObjects = true;

        public static bool TraceXrefStreams
        {
            get => _traceXrefStreams && TraceCompressedObjects;
            set => _traceXrefStreams = value;
        }
        static bool _traceXrefStreams = true;

        public static bool TraceObjectStreams
        {
            get => _traceObjectStreams && TraceCompressedObjects;
            set => _traceObjectStreams = value;
        }
        static bool _traceObjectStreams = true;
    }
}
