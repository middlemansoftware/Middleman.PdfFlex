// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleman.PdfFlex
{
#if !NET6_0_OR_GREATER
    /// <summary>
    /// Extension methods for functionality missing in .NET Framework.
    /// </summary>
    public static class SystemStringExtensions
    {
        /// <summary>
        /// Brings "bool StartsWith(char value)" to String class.
        /// </summary>
        public static bool StartsWith(this string @string, char value) => @string.Length != 0 && @string[0] == value;
    }
#endif
}
