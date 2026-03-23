// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Pdf;

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

    /// <summary>
    /// Gets or sets the conformance profile for the document (e.g. PDF/A-1b, PDF/UA-1).
    /// Defaults to <see cref="PdfConformance.None"/>.
    /// </summary>
    public PdfConformance Conformance { get; set; } = PdfConformance.None;

    /// <summary>Gets or sets whether PDF/UA accessibility tagging is enabled.</summary>
    [Obsolete("Use Conformance property. Set Conformance = PdfConformance.PdfUA1")]
    public bool Accessibility
    {
        get => Conformance.RequiresTaggedStructure;
        set
        {
            if (value && !Conformance.RequiresTaggedStructure)
                Conformance = Conformance.IsNone
                    ? PdfConformance.PdfUA1
                    : Conformance.With(PdfConformance.PdfUA1);
            else if (!value)
                throw new NotSupportedException(
                    "Cannot remove PDF/UA via the Accessibility property. " +
                    "Set the Conformance property directly.");
        }
    }

    /// <summary>
    /// Gets or sets the document language (e.g. "en-US"). Required by PDF/UA-1 for
    /// screen reader language identification. Forwarded to the PDF metadata layer
    /// when <see cref="Conformance"/> requires a document language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the default style applied to all elements unless overridden.</summary>
    public Style? DefaultStyle { get; set; }

    /// <summary>Gets or sets the optional header rendered at the top of every page.</summary>
    public Element? Header { get; set; }

    /// <summary>Gets or sets the optional footer rendered at the bottom of every page.</summary>
    public Element? Footer { get; set; }

    /// <summary>
    /// Gets or sets the optional first-page header override. When set, replaces <see cref="Header"/>
    /// on page 1. Set to an empty element to suppress the header on page 1. When null, uses <see cref="Header"/>.
    /// </summary>
    public Element? FirstPageHeader { get; set; }

    /// <summary>
    /// Gets or sets the optional first-page footer override. When set, replaces <see cref="Footer"/>
    /// on page 1. Set to an empty element to suppress the footer on page 1. When null, uses <see cref="Footer"/>.
    /// </summary>
    public Element? FirstPageFooter { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-generate PDF outlines (bookmarks) from heading elements.
    /// When true, all <see cref="TextBlock"/> elements with a <see cref="TextBlock.HeadingLevel"/>
    /// are added to the PDF outline tree with proper nesting. Defaults to true.
    /// </summary>
    public bool AutoGenerateOutlines { get; set; } = true;

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

    /// <summary>
    /// Pre-fills form field values by matching field names. Walks the entire element
    /// tree and sets <see cref="FormField.Value"/> on any <see cref="FormField"/> whose
    /// <see cref="FormField.Name"/> matches a key in the provided dictionary.
    /// For <see cref="FormCheckbox"/> fields, the value "true" (case-insensitive) sets
    /// <see cref="FormCheckbox.Checked"/> to true. For <see cref="FormDropdown"/> fields,
    /// the value is applied to <see cref="FormDropdown.SelectedOption"/>.
    /// Unknown field names are silently ignored.
    /// </summary>
    /// <param name="values">A dictionary of field name to value mappings.</param>
    /// <returns>This document instance for fluent chaining.</returns>
    public Document SetFieldValues(Dictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var child in Children)
            SetFieldValuesRecursive(child, values);

        return this;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>Recursively walks the element tree to set form field values.</summary>
    private static void SetFieldValuesRecursive(Element element, Dictionary<string, string> values)
    {
        if (element is FormField field && values.TryGetValue(field.Name, out var value))
        {
            field.Value = value;

            if (field is FormCheckbox checkbox)
                checkbox.Checked = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

            if (field is FormDropdown dropdown)
                dropdown.SelectedOption = value;
        }

        if (element is Container container)
        {
            foreach (var child in container.Children)
                SetFieldValuesRecursive(child, values);
        }
    }

    #endregion Private Methods
}
