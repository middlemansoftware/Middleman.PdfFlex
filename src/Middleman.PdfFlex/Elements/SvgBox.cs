// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;
using Middleman.Svg.Model;
using Middleman.Svg.Parsing;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// An SVG vector graphic element. Renders as native PDF vector operators, not rasterized.
/// The SVG source can be a file path, raw SVG content string, or a pre-parsed
/// <see cref="SvgDocument"/>.
/// </summary>
public class SvgBox : Element
{
    #region Public Properties

    /// <summary>Gets the file path to the SVG file, or null if the SVG is provided inline.</summary>
    public string? FilePath { get; }

    /// <summary>Gets the raw SVG content string, or null if the SVG is provided by file or document.</summary>
    public string? SvgContent { get; private init; }

    /// <summary>Gets the pre-parsed SVG document, or null if the SVG is provided by file or string.</summary>
    public SvgDocument? Document { get; private init; }

    /// <summary>Gets or sets the alt text for PDF/UA accessibility tagging.</summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Gets or sets the link target. URLs starting with http:// or https:// create URI links.
    /// Other values create internal GoTo links to the element with a matching Id.
    /// </summary>
    public string? LinkTarget { get; set; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates an SVG element from a file path.</summary>
    /// <param name="filePath">The path to the SVG file.</param>
    /// <param name="style">Optional style to apply to this element.</param>
    public SvgBox(string filePath, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        FilePath = filePath;
        Style = style;
    }

    /// <summary>Private constructor for factory methods.</summary>
    private SvgBox(Style? style)
    {
        Style = style;
    }

    #endregion Constructors

    #region Factory Methods

    /// <summary>Creates an SVG element from raw SVG content.</summary>
    /// <param name="svgContent">The SVG XML content string.</param>
    /// <param name="style">Optional style to apply to this element.</param>
    /// <returns>A new <see cref="SvgBox"/> instance.</returns>
    public static SvgBox FromContent(string svgContent, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(svgContent);
        return new SvgBox(style) { SvgContent = svgContent };
    }

    /// <summary>Creates an SVG element from a pre-parsed document.</summary>
    /// <param name="document">The parsed SVG document.</param>
    /// <param name="style">Optional style to apply to this element.</param>
    /// <returns>A new <see cref="SvgBox"/> instance.</returns>
    public static SvgBox FromDocument(SvgDocument document, Style? style = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new SvgBox(style) { Document = document };
    }

    #endregion Factory Methods

    #region Public Methods

    /// <summary>
    /// Gets the parsed SVG document, parsing lazily from content or file if needed.
    /// The parsed result is cached for subsequent calls.
    /// </summary>
    /// <returns>The parsed <see cref="SvgDocument"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no SVG source (file path, content, or document) has been provided.
    /// </exception>
    public SvgDocument GetDocument()
    {
        if (_parsedDocument != null)
        {
            return _parsedDocument;
        }

        if (Document != null)
        {
            _parsedDocument = Document;
            return _parsedDocument;
        }

        if (SvgContent != null)
        {
            _parsedDocument = SvgParser.Parse(SvgContent);
            return _parsedDocument;
        }

        if (FilePath != null)
        {
            _parsedDocument = SvgParser.ParseFile(FilePath);
            return _parsedDocument;
        }

        throw new InvalidOperationException("SvgBox has no SVG source.");
    }

    #endregion Public Methods

    #region Private Fields

    private SvgDocument? _parsedDocument;

    #endregion Private Fields
}
