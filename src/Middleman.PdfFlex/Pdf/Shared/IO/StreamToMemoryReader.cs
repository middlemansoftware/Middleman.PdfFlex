// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace Middleman.PdfFlex.IO_dummy
{
    /// <summary>
    /// Reads any stream into a Memory&lt;byte&gt;.
    /// </summary>
    public class StreamToMemoryReader(Stream stream)
    {
        /// <summary>
        /// Reads a stream to the end.
        /// </summary>
        public Memory<byte> ReadToEnd()
        {
            // Just for testing and integration.

            var memoryStream = new MemoryStream();
            _stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }
}
