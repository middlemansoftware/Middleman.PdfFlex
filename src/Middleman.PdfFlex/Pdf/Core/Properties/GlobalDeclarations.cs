// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

global using System.IO;
global using Middleman.PdfFlex.Internal;


#if USE_LONG_SIZE
global using SizeType = System.Int64;
#else
global using SizeType = System.Int32;
#endif

global using static System.FormattableString;

using System.Diagnostics.CodeAnalysis;
//using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]
[assembly: SuppressMessage("LoggingGenerator", "SYSLIB1006:Multiple logging methods cannot use the same event ID within a class",
    Justification = "We use logging event IDs as documented, i.e. multiple times", Scope = "member"/*, Target = "~M:Middleman.PdfFlex.Internal.Logging.LogMessages.XGraphicsCreated(Microsoft.Extensions.Logging.ILogger,System.String)"*/)]

