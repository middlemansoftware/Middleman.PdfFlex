// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Carries rendering state through the render pipeline. Provides the graphics surface,
/// current page number, total page count, and document conformance profile.
/// </summary>
internal sealed class RenderContext
{
    #region Public Properties

    /// <summary>Gets the PDF graphics surface to draw on.</summary>
    public XGraphics Graphics { get; }

    /// <summary>Gets the 1-based current page number.</summary>
    public int CurrentPage { get; }

    /// <summary>Gets the total number of pages in the document.</summary>
    public int TotalPages { get; }

    /// <summary>Gets the document's conformance profile.</summary>
    public PdfConformance Conformance { get; }

    /// <summary>
    /// Gets the structure builder for PDF/UA tagging, or null when the document
    /// does not require tagged structure.
    /// </summary>
    public StructureBuilder? StructureBuilder { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a new render context.</summary>
    /// <param name="graphics">The PDF graphics surface.</param>
    /// <param name="currentPage">The 1-based current page number.</param>
    /// <param name="totalPages">The total number of pages.</param>
    /// <param name="conformance">The document's conformance profile.</param>
    public RenderContext(XGraphics graphics, int currentPage, int totalPages, PdfConformance conformance)
        : this(graphics, currentPage, totalPages, conformance, structureBuilder: null)
    {
    }

    /// <summary>Creates a new render context with optional PDF/UA structure builder.</summary>
    /// <param name="graphics">The PDF graphics surface.</param>
    /// <param name="currentPage">The 1-based current page number.</param>
    /// <param name="totalPages">The total number of pages.</param>
    /// <param name="conformance">The document's conformance profile.</param>
    /// <param name="structureBuilder">
    /// The structure builder for PDF/UA tagging. Null when the document does not require
    /// tagged structure.
    /// </param>
    public RenderContext(XGraphics graphics, int currentPage, int totalPages,
        PdfConformance conformance, StructureBuilder? structureBuilder)
    {
        Graphics = graphics;
        CurrentPage = currentPage;
        TotalPages = totalPages;
        Conformance = conformance;
        StructureBuilder = structureBuilder;
    }

    #endregion Constructors
}
