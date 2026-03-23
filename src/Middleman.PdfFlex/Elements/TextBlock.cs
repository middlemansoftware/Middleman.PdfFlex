// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Single-style text element that renders a string with a given font specification.
/// Text measurement currently uses a rough character-count approximation; real font metrics
/// will be integrated when the PDF rendering engine is connected.
/// </summary>
public class TextBlock : Element
{
    /// <summary>Average character width as a fraction of font size, used for estimation.</summary>
    internal const double AverageCharWidthRatio = 0.5;

    /// <summary>Line height as a multiplier of font size, used for estimation.</summary>
    internal const double LineHeightMultiplier = 1.2;

    #region Public Properties

    /// <summary>Gets the text content to render.</summary>
    public string Text { get; }

    /// <summary>Gets the optional font specification. When null, font properties are inherited from the parent chain.</summary>
    public FontSpec? Font { get; }

    /// <summary>
    /// Gets the optional heading level for PDF/UA accessibility tagging.
    /// Null renders as /P (paragraph). Values 1-6 render as /H1 through /H6
    /// for screen reader heading navigation.
    /// </summary>
    public int? HeadingLevel { get; }

    /// <summary>
    /// Gets or sets the link target. URLs starting with http:// or https:// create URI links.
    /// Other values create internal GoTo links to the element with a matching Id.
    /// </summary>
    public string? LinkTarget { get; set; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a text element with the specified content and optional font.</summary>
    /// <param name="text">The text content to render.</param>
    /// <param name="font">Optional font specification. Null inherits from parent chain.</param>
    /// <param name="style">Optional style to apply to this text element.</param>
    /// <param name="headingLevel">Optional heading level (1-6) for PDF/UA tagging. Null renders as paragraph.</param>
    public TextBlock(string text, FontSpec? font = null, Style? style = null, int? headingLevel = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (headingLevel.HasValue && (headingLevel.Value < 1 || headingLevel.Value > 6))
            throw new ArgumentOutOfRangeException(nameof(headingLevel), "Heading level must be between 1 and 6.");
        Text = text;
        Font = font;
        Style = style;
        HeadingLevel = headingLevel;
    }

    #endregion Constructors

    #region Public Methods

    /// <summary>
    /// Estimates the rendered width of this text block in points. Uses a rough approximation
    /// based on character count and font size until real font metrics are available.
    /// </summary>
    /// <param name="fontSize">The resolved font size in points.</param>
    /// <returns>The estimated width in points.</returns>
    public double EstimateWidth(double fontSize)
    {
        return Text.Length * fontSize * AverageCharWidthRatio;
    }

    /// <summary>
    /// Estimates the rendered height of this text block in points.
    /// </summary>
    /// <param name="fontSize">The resolved font size in points.</param>
    /// <returns>The estimated height in points.</returns>
    public double EstimateHeight(double fontSize)
    {
        return fontSize * LineHeightMultiplier;
    }

    #endregion Public Methods
}
