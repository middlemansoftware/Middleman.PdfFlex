// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Top-level document definition. Contains the element tree, page settings,
/// watermark, and accessibility options.
/// </summary>
public class Document
{
    #region Public Properties

    /// <summary>Gets the page size for the document.</summary>
    public PageSize PageSize { get; }

    /// <summary>Gets the page margins.</summary>
    public EdgeInsets Margins { get; }

    /// <summary>Gets the top-level child elements that compose the document body.</summary>
    public List<Element> Children { get; } = new();

    /// <summary>Gets or sets the optional watermark rendered behind content on every page.</summary>
    public Watermark? Watermark { get; set; }

    /// <summary>Gets or sets whether PDF/UA accessibility tagging is enabled.</summary>
    public bool Accessibility { get; set; }

    /// <summary>Gets or sets the default style applied to all elements unless overridden.</summary>
    public Style? DefaultStyle { get; set; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a document with the specified page size and optional margins.</summary>
    /// <param name="pageSize">The page dimensions.</param>
    /// <param name="margins">The page margins. Defaults to 40pt (~14mm) on all sides.</param>
    public Document(PageSize pageSize, EdgeInsets? margins = null)
    {
        PageSize = pageSize;
        Margins = margins ?? new EdgeInsets(40);
    }

    #endregion Constructors

    #region Public Methods

    /// <summary>Adds one or more elements to the document body.</summary>
    /// <param name="elements">The elements to add.</param>
    /// <returns>This document instance for fluent chaining.</returns>
    public Document Add(params Element[] elements)
    {
        foreach (var element in elements)
        {
            element.Parent = null;
            Children.Add(element);
        }

        return this;
    }

    // Save/ToBytes methods will be added when DocumentRenderer is implemented.

    #endregion Public Methods
}
