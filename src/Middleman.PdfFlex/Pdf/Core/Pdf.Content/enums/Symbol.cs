// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Content
{
    /// <summary>
    /// Terminal symbols recognized by PDF content stream lexer.
    /// </summary>
    public enum CSymbol
    {
#pragma warning disable 1591
        None,
        Comment,
        Integer,
        Real,
        /*Boolean?,*/
        String,
        HexString,
        UnicodeString,
        UnicodeHexString,
        Name,
        Operator,
        BeginArray,
        EndArray,
        // IMPROVE
        // Content dictionary << … >> is scanned as string literal.
        // Scan as an object tree.
        Dictionary,
        Eof,
        Error = -1,
    }
}
