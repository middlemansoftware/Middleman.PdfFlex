// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Text;
using System.Text.RegularExpressions;

namespace Middleman.PdfFlex.Helpers;

/// <summary>
/// Generates GFM-compatible URL slugs from heading text for use as element Ids.
/// Converts to lowercase, replaces spaces with hyphens, strips non-alphanumeric
/// characters (except hyphens), and deduplicates with -1, -2 suffixes.
/// </summary>
internal static partial class HeadingSlugGenerator
{
    #region Public Methods

    /// <summary>
    /// Generates a GFM-compatible slug from heading text. The slug is guaranteed
    /// to be unique within the provided set of existing slugs.
    /// </summary>
    /// <param name="text">The heading text to slugify.</param>
    /// <param name="existingSlugs">
    /// The set of previously generated slugs. The new slug (including any
    /// deduplication suffix) is added to this set before returning.
    /// </param>
    /// <returns>A unique, GFM-compatible slug string.</returns>
    public static string Generate(string text, HashSet<string> existingSlugs)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(existingSlugs);

        // Lowercase the text.
        string slug = text.ToLowerInvariant();

        // Replace spaces with hyphens.
        slug = slug.Replace(' ', '-');

        // Strip non-alphanumeric characters except hyphens.
        slug = NonAlphanumericRegex().Replace(slug, "");

        // Collapse multiple consecutive hyphens.
        slug = MultipleHyphenRegex().Replace(slug, "-");

        // Trim leading/trailing hyphens.
        slug = slug.Trim('-');

        // Ensure non-empty slug.
        if (string.IsNullOrEmpty(slug))
            slug = "heading";

        // Deduplicate: if slug already exists, append -1, -2, etc.
        string candidate = slug;
        int counter = 1;
        while (existingSlugs.Contains(candidate))
        {
            candidate = $"{slug}-{counter}";
            counter++;
        }

        existingSlugs.Add(candidate);
        return candidate;
    }

    #endregion Public Methods

    #region Private Methods

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleHyphenRegex();

    #endregion Private Methods
}
