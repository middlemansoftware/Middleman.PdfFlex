// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A paragraph containing inline <see cref="Span"/> children with mixed formatting.
/// Measures and wraps as a single paragraph with word-wrapping across span boundaries.
/// </summary>
public class RichText : Element
{
    #region Public Properties

    /// <summary>Gets the ordered list of inline spans that compose this paragraph.</summary>
    public IReadOnlyList<Span> Spans { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a rich text paragraph from a sequence of spans.</summary>
    /// <param name="children">The inline spans to include in this paragraph.</param>
    /// <param name="style">Optional style to apply to this paragraph.</param>
    public RichText(IEnumerable<Span> children, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(children);
        Spans = children.ToList();
        Style = style;
    }

    /// <summary>Creates a rich text paragraph from one or more spans.</summary>
    /// <param name="children">The inline spans to include in this paragraph.</param>
    public RichText(params Span[] children)
        : this((IEnumerable<Span>)children)
    {
    }

    #endregion Constructors
}
