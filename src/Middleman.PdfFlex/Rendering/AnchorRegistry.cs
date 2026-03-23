// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Tracks named destinations during rendering. Maps element Ids to the page number
/// and Y position where they were rendered, enabling internal links, anchor page tokens,
/// and outline generation.
/// </summary>
internal sealed class AnchorRegistry
{
    #region Public Properties

    /// <summary>Gets the mapping of element Id to 1-based page number.</summary>
    public Dictionary<string, int> PageNumbers { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the mapping of element Id to the Y position (in page coordinates) where the element was rendered.</summary>
    public Dictionary<string, double> YPositions { get; } = new(StringComparer.Ordinal);

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Registers a named destination for an element. If the Id is already registered,
    /// the existing entry is overwritten (last-rendered position wins).
    /// </summary>
    /// <param name="id">The element Id serving as the destination name.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="y">The Y position in page coordinates.</param>
    public void Register(string id, int page, double y)
    {
        PageNumbers[id] = page;
        YPositions[id] = y;
    }

    /// <summary>
    /// Tries to get the page number for a registered anchor.
    /// </summary>
    /// <param name="id">The element Id to look up.</param>
    /// <param name="page">When this method returns, contains the 1-based page number if found.</param>
    /// <returns><c>true</c> if the anchor was found; otherwise, <c>false</c>.</returns>
    public bool TryGetPage(string id, out int page)
    {
        return PageNumbers.TryGetValue(id, out page);
    }

    /// <summary>
    /// Tries to get the Y position for a registered anchor.
    /// </summary>
    /// <param name="id">The element Id to look up.</param>
    /// <param name="y">When this method returns, contains the Y position if found.</param>
    /// <returns><c>true</c> if the anchor was found; otherwise, <c>false</c>.</returns>
    public bool TryGetY(string id, out double y)
    {
        return YPositions.TryGetValue(id, out y);
    }

    #endregion Public Methods
}
