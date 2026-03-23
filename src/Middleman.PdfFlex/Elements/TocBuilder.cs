// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Helpers;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Builds a composable table of contents from heading elements in a document.
/// Walks the document's element tree, finds all <see cref="TextBlock"/> elements
/// with a <see cref="TextBlock.HeadingLevel"/>, and generates TOC entries with
/// clickable links and <c>{page:id}</c> tokens for page number resolution.
/// </summary>
public static class TocBuilder
{
    #region Public Methods

    /// <summary>
    /// Builds a table of contents from the headings in the specified document.
    /// Does not mutate the source document or its elements. For headings without an Id,
    /// generates GFM-compatible slugs using the same algorithm as the renderer's
    /// auto-assignment, ensuring TOC references match at render time.
    /// Each TOC entry is a <see cref="Row"/> containing the heading text (as a link),
    /// a spacer, and a <c>{page:id}</c> token.
    /// </summary>
    /// <param name="doc">The document to scan for headings.</param>
    /// <param name="style">Optional TOC style configuration. Null uses defaults.</param>
    /// <returns>A list of elements representing the TOC entries.</returns>
    public static List<Element> Build(Document doc, TocStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(doc);

        style ??= new TocStyle();
        var entries = new List<Element>();
        var existingSlugs = new HashSet<string>(StringComparer.Ordinal);

        // First pass: collect all existing Ids to avoid slug collisions.
        CollectExistingIds(doc.Children, existingSlugs);

        // Second pass: find headings and build TOC entries.
        BuildEntries(doc.Children, style, entries, existingSlugs);

        return entries;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Recursively collects all non-null element Ids from the tree into the slug set.
    /// </summary>
    private static void CollectExistingIds(IEnumerable<Element> elements, HashSet<string> slugs)
    {
        foreach (var element in elements)
        {
            if (!string.IsNullOrEmpty(element.Id))
                slugs.Add(element.Id);

            if (element is Container container)
                CollectExistingIds(container.Children, slugs);
        }
    }

    /// <summary>
    /// Recursively walks the element tree, building TOC entries for each heading found.
    /// </summary>
    private static void BuildEntries(
        IEnumerable<Element> elements,
        TocStyle style,
        List<Element> entries,
        HashSet<string> existingSlugs)
    {
        foreach (var element in elements)
        {
            if (element is TextBlock tb && tb.HeadingLevel.HasValue)
            {
                // Determine the Id to reference without mutating the source heading.
                // If the heading already has an Id, use it. Otherwise generate a slug
                // using the same algorithm that DocumentRenderer.AutoAssignHeadingIds
                // will use during rendering, so the references match.
                string headingId;
                if (!string.IsNullOrEmpty(tb.Id))
                {
                    headingId = tb.Id;
                }
                else
                {
                    headingId = HeadingSlugGenerator.Generate(tb.Text, existingSlugs);
                }

                int level = tb.HeadingLevel.Value;
                double indent = (level - 1) * style.IndentPerLevel;
                FontSpec? font = null;
                style.FontPerLevel?.TryGetValue(level, out font);
                string pageToken = $"{{page:{headingId}}}";

                // Heading text with link to the heading anchor.
                var textElement = new TextBlock(tb.Text, font)
                {
                    LinkTarget = headingId
                };

                // Spacer pushes the page number to the right edge.
                var spacer = new Spacer();

                // Page number token, right-aligned with a fixed width so numbers
                // line up vertically across all TOC entries.
                var pageElement = new TextBlock(pageToken, font, new Style
                {
                    TextAlign = Styling.TextAlign.Right,
                    Width = Length.Pt(30)
                });

                // Build the row with optional indent via left margin.
                // Small vertical padding for line spacing between entries.
                var rowStyle = new Style
                {
                    Padding = new EdgeInsets(2, 0, 2, indent)
                };

                var row = new Row(new Element[] { textElement, spacer, pageElement })
                {
                    Style = rowStyle
                };

                entries.Add(row);
            }

            if (element is Container container)
            {
                BuildEntries(container.Children, style, entries, existingSlugs);
            }
        }
    }

    #endregion Private Methods
}
