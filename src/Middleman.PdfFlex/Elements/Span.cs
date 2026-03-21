// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// An inline styled text run within a <see cref="RichText"/> paragraph.
/// Each span carries its own optional <see cref="SpanStyle"/> for mixed formatting.
/// </summary>
public class Span
{
    #region Public Properties

    /// <summary>Gets the text content of this span.</summary>
    public string Text { get; }

    /// <summary>Gets the optional inline style. When null, formatting is inherited from the parent element.</summary>
    public SpanStyle? Style { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates an inline text span with optional styling.</summary>
    /// <param name="text">The text content.</param>
    /// <param name="style">Optional inline style for this span. Null inherits from the parent.</param>
    public Span(string text, SpanStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        Style = style;
    }

    #endregion Constructors
}
