// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Handles flex-grow and flex-shrink distribution math for flex containers.
/// Distributes surplus or deficit space among children proportionally based
/// on their flex factors.
/// </summary>
internal static class FlexResolver
{
    #region Public Methods

    /// <summary>
    /// Distributes surplus space among children based on their flex-grow values.
    /// Children with flex-grow of 0 are unaffected. Surplus is divided proportionally
    /// among children with positive flex-grow.
    /// </summary>
    /// <param name="children">The child layout nodes to adjust.</param>
    /// <param name="elements">The corresponding source elements (parallel to <paramref name="children"/>).</param>
    /// <param name="surplus">The amount of surplus space in points to distribute.</param>
    /// <param name="isHorizontal">True if distributing along the horizontal axis (Row), false for vertical (Column).</param>
    public static void DistributeGrow(
        List<LayoutNode> children,
        List<Element> elements,
        double surplus,
        bool isHorizontal)
    {
        if (surplus <= 0 || children.Count == 0)
            return;

        double totalGrow = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            totalGrow += StyleResolver.GetFlexGrow(elements[i]);
        }

        if (totalGrow <= 0)
            return;

        for (int i = 0; i < children.Count; i++)
        {
            double grow = StyleResolver.GetFlexGrow(elements[i]);
            if (grow <= 0)
                continue;

            double share = surplus * (grow / totalGrow);

            if (isHorizontal)
                children[i].Width += share;
            else
                children[i].Height += share;
        }
    }

    /// <summary>
    /// Shrinks children to fit within available space based on their flex-shrink values.
    /// Children with flex-shrink of 0 are not shrunk. The deficit is distributed proportionally
    /// based on each child's shrink factor weighted by its current size (per CSS flexbox spec).
    /// </summary>
    /// <param name="children">The child layout nodes to adjust.</param>
    /// <param name="elements">The corresponding source elements (parallel to <paramref name="children"/>).</param>
    /// <param name="deficit">The amount of overflow in points to remove (positive value).</param>
    /// <param name="isHorizontal">True if shrinking along the horizontal axis (Row), false for vertical (Column).</param>
    public static void DistributeShrink(
        List<LayoutNode> children,
        List<Element> elements,
        double deficit,
        bool isHorizontal)
    {
        if (deficit <= 0 || children.Count == 0)
            return;

        // CSS flexbox shrink: weighted by (flex-shrink * item-size).
        double totalWeightedShrink = 0;
        for (int i = 0; i < children.Count; i++)
        {
            double shrink = StyleResolver.GetFlexShrink(elements[i]);
            double size = isHorizontal ? children[i].Width : children[i].Height;
            totalWeightedShrink += shrink * size;
        }

        if (totalWeightedShrink <= 0)
            return;

        for (int i = 0; i < children.Count; i++)
        {
            double shrink = StyleResolver.GetFlexShrink(elements[i]);
            if (shrink <= 0)
                continue;

            double size = isHorizontal ? children[i].Width : children[i].Height;
            double share = deficit * (shrink * size / totalWeightedShrink);

            if (isHorizontal)
                children[i].Width = Math.Max(0, children[i].Width - share);
            else
                children[i].Height = Math.Max(0, children[i].Height - share);
        }
    }

    #endregion Public Methods
}
