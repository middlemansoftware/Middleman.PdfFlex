// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.Structure;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Represents an internal link whose target anchor may not yet be rendered.
/// Collected during the render pass and resolved after all pages are laid out.
/// </summary>
internal sealed class PendingLink
{
    /// <summary>Gets the PDF page containing the link annotation.</summary>
    public required PdfPage Page { get; init; }

    /// <summary>Gets the link rectangle in page coordinates (XGraphics top-left origin).</summary>
    public required XRect Rect { get; init; }

    /// <summary>Gets the target element Id that this link points to.</summary>
    public required string TargetId { get; init; }

    /// <summary>Gets the alternative text for accessibility tagging. Null when not applicable.</summary>
    public string? AltText { get; init; }

    /// <summary>Gets the 1-based page number where this link annotation resides.</summary>
    public required int SourcePageNumber { get; init; }

    /// <summary>
    /// Gets the structure builder for PDF/UA link tagging, or null when the document
    /// does not require tagged structure.
    /// </summary>
    public StructureBuilder? StructureBuilder { get; init; }

    /// <summary>
    /// Gets the /Link structure element created during rendering while the element
    /// stack was correctly positioned, or null when no tagged structure is required.
    /// The annotation is associated with this element during deferred resolution.
    /// </summary>
    public PdfStructureElement? LinkStructureElement { get; init; }
}
