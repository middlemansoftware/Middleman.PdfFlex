// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Carries rendering state through the render pipeline. Provides the graphics surface,
/// current page number, total page count, document conformance profile, anchor registry,
/// and link collection.
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

    /// <summary>
    /// Gets the anchor registry for tracking named destinations across pages,
    /// or null when anchor tracking is not active.
    /// </summary>
    public AnchorRegistry? AnchorRegistry { get; }

    /// <summary>
    /// Gets the current PDF page, or null when not available.
    /// Used for creating link annotations.
    /// </summary>
    public PdfPage? Page { get; }

    /// <summary>
    /// Gets the list of pending internal links to be resolved after all pages are rendered.
    /// Null when link tracking is not active.
    /// </summary>
    public List<PendingLink>? PendingLinks { get; }

    /// <summary>
    /// Gets the page height in points, used for coordinate conversion between
    /// XGraphics (top-left origin) and PDF (bottom-left origin) coordinate systems.
    /// </summary>
    public double PageHeight { get; }

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

    /// <summary>Creates a new render context with full link and anchor support.</summary>
    /// <param name="graphics">The PDF graphics surface.</param>
    /// <param name="currentPage">The 1-based current page number.</param>
    /// <param name="totalPages">The total number of pages.</param>
    /// <param name="conformance">The document's conformance profile.</param>
    /// <param name="structureBuilder">
    /// The structure builder for PDF/UA tagging. Null when the document does not require
    /// tagged structure.
    /// </param>
    /// <param name="anchorRegistry">The anchor registry for named destinations. Null when not tracking.</param>
    /// <param name="page">The current PDF page for annotation creation.</param>
    /// <param name="pendingLinks">The collection for deferred internal link annotations.</param>
    /// <param name="pageHeight">The page height in points for coordinate conversion.</param>
    public RenderContext(XGraphics graphics, int currentPage, int totalPages,
        PdfConformance conformance, StructureBuilder? structureBuilder,
        AnchorRegistry? anchorRegistry, PdfPage? page,
        List<PendingLink>? pendingLinks, double pageHeight)
    {
        Graphics = graphics;
        CurrentPage = currentPage;
        TotalPages = totalPages;
        Conformance = conformance;
        StructureBuilder = structureBuilder;
        AnchorRegistry = anchorRegistry;
        Page = page;
        PendingLinks = pendingLinks;
        PageHeight = pageHeight;
    }

    #endregion Constructors
}
