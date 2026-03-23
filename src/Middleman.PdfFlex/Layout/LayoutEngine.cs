// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Layout;

/// <summary>
/// Core layout algorithm that takes an element tree and produces positioned
/// <see cref="LayoutNode"/> instances. Pure computation with no PDF dependencies.
/// Implements a 3-phase layout: Measure (bottom-up intrinsic sizing), Resolve
/// (top-down space distribution), and Position (top-down absolute coordinates).
/// </summary>
/// <remarks>
/// Thread-safe: all state is local to each <see cref="Calculate"/> invocation.
/// No static mutable state is used.
/// </remarks>
public static class LayoutEngine
{
    #region Public Methods

    /// <summary>
    /// Calculates the complete layout for an element tree within the given available space,
    /// with all positions starting at the origin (0, 0).
    /// </summary>
    /// <param name="element">The root element of the tree to lay out.</param>
    /// <param name="availableWidth">The available width in points.</param>
    /// <param name="availableHeight">The available height in points.</param>
    /// <returns>The root <see cref="LayoutNode"/> with resolved positions and sizes for the entire tree.</returns>
    public static LayoutNode Calculate(Element element, double availableWidth, double availableHeight)
    {
        return Calculate(element, availableWidth, availableHeight, 0, 0);
    }

    /// <summary>
    /// Calculates the complete layout for an element tree within the given available space,
    /// with all positions offset by the specified origin. Use this to apply document margins
    /// so the entire coordinate tree starts at the correct page position.
    /// </summary>
    /// <param name="element">The root element of the tree to lay out.</param>
    /// <param name="availableWidth">The available width in points.</param>
    /// <param name="availableHeight">The available height in points.</param>
    /// <param name="originX">The X origin offset in points (e.g., left margin).</param>
    /// <param name="originY">The Y origin offset in points (e.g., top margin).</param>
    /// <returns>The root <see cref="LayoutNode"/> with resolved positions and sizes for the entire tree.</returns>
    public static LayoutNode Calculate(Element element, double availableWidth, double availableHeight,
        double originX, double originY)
    {
        ArgumentNullException.ThrowIfNull(element);

        var node = new LayoutNode(element);
        var intrinsic = Measure(node, element);
        Resolve(node, element, availableWidth, availableHeight, intrinsic);
        Position(node, originX, originY);
        return node;
    }

    #endregion Public Methods

    #region Private Methods

    // ── Phase 1: Measure ─────────────────────────────────────────────────
    // Bottom-up pass. Each element reports its intrinsic (content-based) size.
    // Returns (intrinsicWidth, intrinsicHeight) for the given element.

    /// <summary>
    /// Measures an element's intrinsic size and recursively builds child LayoutNodes.
    /// </summary>
    private static (double Width, double Height) Measure(LayoutNode node, Element element)
    {
        return element switch
        {
            TextBlock tb => MeasureTextBlock(tb),
            RichText rt => MeasureRichText(rt),
            SvgBox svg => MeasureSvgBox(svg),
            ImageBox img => MeasureImageBox(img),
            Table tbl => MeasureTable(tbl),
            Spacer => (0, 0),
            PageBreak => (0, 0),
            Divider d => MeasureDivider(d),
            Box box => MeasureBox(node, box),
            Row row => MeasureRow(node, row),
            Column col => MeasureColumn(node, col),
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Measures a TextBlock using real font metrics via <see cref="TextMeasurer"/>.
    /// Reports the unwrapped single-line intrinsic size; word-wrapping is applied
    /// later during the Resolve phase when the constrained width is known.
    /// </summary>
    private static (double Width, double Height) MeasureTextBlock(TextBlock tb)
    {
        return TextMeasurer.MeasureSingleLine(tb);
    }

    /// <summary>
    /// Measures a Divider. A horizontal divider has thickness as height;
    /// a vertical divider has thickness as width. The cross-axis dimension
    /// is zero (will be stretched by the parent during resolve).
    /// </summary>
    private static (double Width, double Height) MeasureDivider(Divider d)
    {
        return d.IsVertical ? (d.Thickness, 0) : (0, d.Thickness);
    }

    /// <summary>
    /// Measures a Box: child intrinsic size plus padding and border insets.
    /// </summary>
    private static (double Width, double Height) MeasureBox(LayoutNode node, Box box)
    {
        var padding = StyleResolver.GetPadding(box);
        var borderWidths = StyleResolver.GetBorderWidths(box);
        double insetH = padding.HorizontalTotal + borderWidths.HorizontalTotal;
        double insetV = padding.VerticalTotal + borderWidths.VerticalTotal;

        if (box.Child == null)
            return (insetH, insetV);

        var childNode = new LayoutNode(box.Child);
        node.Children.Add(childNode);
        var childIntrinsic = Measure(childNode, box.Child);

        return (childIntrinsic.Width + insetH, childIntrinsic.Height + insetV);
    }

    /// <summary>
    /// Measures a Row: sum of children widths + gaps along horizontal main axis,
    /// max child height along vertical cross axis, plus padding and border.
    /// </summary>
    private static (double Width, double Height) MeasureRow(LayoutNode node, Row row)
    {
        var padding = StyleResolver.GetPadding(row);
        var borderWidths = StyleResolver.GetBorderWidths(row);
        double insetH = padding.HorizontalTotal + borderWidths.HorizontalTotal;
        double insetV = padding.VerticalTotal + borderWidths.VerticalTotal;

        if (row.Children.Count == 0)
            return (insetH, insetV);

        double totalWidth = 0;
        double maxHeight = 0;

        for (int i = 0; i < row.Children.Count; i++)
        {
            var child = row.Children[i];
            var childNode = new LayoutNode(child);
            node.Children.Add(childNode);
            var childIntrinsic = Measure(childNode, child);

            totalWidth += childIntrinsic.Width;
            if (childIntrinsic.Height > maxHeight)
                maxHeight = childIntrinsic.Height;
        }

        double totalGap = row.Gap * Math.Max(0, row.Children.Count - 1);
        return (totalWidth + totalGap + insetH, maxHeight + insetV);
    }

    /// <summary>
    /// Measures a Column: max child width along horizontal cross axis,
    /// sum of children heights + gaps along vertical main axis, plus padding and border.
    /// </summary>
    private static (double Width, double Height) MeasureColumn(LayoutNode node, Column col)
    {
        var padding = StyleResolver.GetPadding(col);
        var borderWidths = StyleResolver.GetBorderWidths(col);
        double insetH = padding.HorizontalTotal + borderWidths.HorizontalTotal;
        double insetV = padding.VerticalTotal + borderWidths.VerticalTotal;

        if (col.Children.Count == 0)
            return (insetH, insetV);

        double maxWidth = 0;
        double totalHeight = 0;

        for (int i = 0; i < col.Children.Count; i++)
        {
            var child = col.Children[i];
            var childNode = new LayoutNode(child);
            node.Children.Add(childNode);
            var childIntrinsic = Measure(childNode, child);

            if (childIntrinsic.Width > maxWidth)
                maxWidth = childIntrinsic.Width;
            totalHeight += childIntrinsic.Height;
        }

        double totalGap = col.Gap * Math.Max(0, col.Children.Count - 1);
        return (maxWidth + insetH, totalHeight + totalGap + insetV);
    }

    /// <summary>
    /// Measures a RichText element using real font metrics via <see cref="TextMeasurer"/>.
    /// Reports the unwrapped single-line intrinsic size; word-wrapping is applied
    /// later during the Resolve phase when the constrained width is known.
    /// </summary>
    private static (double Width, double Height) MeasureRichText(RichText rt)
    {
        return TextMeasurer.MeasureRichTextSingleLine(rt);
    }

    /// <summary>
    /// Measures an SvgBox using the SVG document's intrinsic dimensions.
    /// When the user specifies only one dimension (Width or Height) in the style,
    /// the other is computed from the viewBox aspect ratio so the SVG scales
    /// proportionally without requiring manual width/height calculations.
    /// </summary>
    private static (double Width, double Height) MeasureSvgBox(SvgBox svg)
    {
        var doc = svg.GetDocument();
        double vbW = doc.Width;
        double vbH = doc.Height;

        // Avoid division by zero for degenerate viewBoxes.
        if (vbW <= 0 || vbH <= 0)
            return (vbW, vbH);

        double aspectRatio = vbW / vbH;

        double? styleW = ResolveStyleDimension(svg.Style?.Width);
        double? styleH = ResolveStyleDimension(svg.Style?.Height);

        if (styleW.HasValue && !styleH.HasValue)
        {
            // Width specified, compute height from aspect ratio.
            return (styleW.Value, styleW.Value / aspectRatio);
        }

        if (styleH.HasValue && !styleW.HasValue)
        {
            // Height specified, compute width from aspect ratio.
            return (styleH.Value * aspectRatio, styleH.Value);
        }

        // Both or neither specified — use raw viewBox dimensions (style overrides applied later).
        return (vbW, vbH);
    }

    /// <summary>
    /// Measures an ImageBox. Style dimensions take priority, then intrinsic dimensions,
    /// then a 100x100 default to avoid loading the image during layout.
    /// </summary>
    private static (double Width, double Height) MeasureImageBox(ImageBox img)
    {
        const double defaultSize = 100.0;

        double w = ResolveStyleDimension(img.Style?.Width)
                   ?? (img.IntrinsicWidth > 0 ? img.IntrinsicWidth : defaultSize);
        double h = ResolveStyleDimension(img.Style?.Height)
                   ?? (img.IntrinsicHeight > 0 ? img.IntrinsicHeight : defaultSize);
        return (w, h);
    }

    /// <summary>
    /// Resolves a style dimension to a point value if it is an absolute unit (pt, mm, in, cm).
    /// Returns null for relative units (%, fr, auto) or if the length is null.
    /// </summary>
    private static double? ResolveStyleDimension(Length? length)
    {
        if (length == null)
            return null;
        var len = length.Value;
        return len.IsAbsolute ? len.ToPoints() : null;
    }

    /// <summary>
    /// Measures a Table by estimating width from column definitions and height from row count.
    /// </summary>
    private static (double Width, double Height) MeasureTable(Table tbl)
    {
        const double defaultColumnWidth = 100.0;
        const double rowHeightEstimate = 20.0;

        double totalWidth = 0;
        for (int i = 0; i < tbl.Columns.Count; i++)
        {
            var colWidth = tbl.Columns[i].Width;
            totalWidth += colWidth.IsAbsolute ? colWidth.ToPoints() : defaultColumnWidth;
        }

        // Header row + data rows.
        int rowCount = tbl.Rows.Count + 1;
        double totalHeight = rowCount * rowHeightEstimate;

        return (totalWidth, totalHeight);
    }

    // ── Phase 2: Resolve ────────────────────────────────────────────────
    // Top-down pass. Distribute available space to each node, resolving
    // lengths, flex-grow, flex-shrink, and percentage values.

    /// <summary>
    /// Resolves the final size of a node and its children within the available space.
    /// Containers (Row, Column, Box) fill available space when no explicit size is set,
    /// matching CSS block-level behavior. Leaf elements use their intrinsic size.
    /// </summary>
    private static void Resolve(
        LayoutNode node,
        Element element,
        double availableWidth,
        double availableHeight,
        (double Width, double Height) intrinsic)
    {
        // Containers fill available space by default (like CSS block elements).
        // Leaf elements use their intrinsic size when no explicit size is set.
        bool isContainer = element is Container or Box;
        double defaultWidth = isContainer ? availableWidth : intrinsic.Width;
        double defaultHeight = isContainer ? availableHeight : intrinsic.Height;

        node.Width = StyleResolver.ResolveLength(element.Style?.Width, availableWidth, defaultWidth);
        node.Height = StyleResolver.ResolveLength(element.Style?.Height, availableHeight, defaultHeight);

        // Clamp to min/max constraints if set.
        ClampToMinMax(node, element, availableWidth, availableHeight);

        // After the width is resolved, re-measure text elements at the constrained width
        // to determine the wrapped height. Only override height when no explicit height is set,
        // so user-specified heights are respected.
        if (element.Style?.Height == null)
        {
            ResolveTextWrapping(node, element);
        }

        switch (element)
        {
            case Box box:
                ResolveBox(node, box);
                break;
            case Row row:
                ResolveRow(node, row);
                break;
            case Column col:
                ResolveColumn(node, col);
                break;
            case Divider d:
                // A divider's cross-axis dimension should fill available space when no explicit size is set.
                if (d.IsVertical && element.Style?.Height == null)
                    node.Height = availableHeight;
                else if (!d.IsVertical && element.Style?.Width == null)
                    node.Width = availableWidth;
                break;
        }
    }

    /// <summary>
    /// Re-measures text elements at their resolved constrained width to compute the
    /// wrapped height. Called after the node's width is set but before children are resolved.
    /// Only adjusts height for text elements whose content wraps to multiple lines.
    /// </summary>
    private static void ResolveTextWrapping(LayoutNode node, Element element)
    {
        switch (element)
        {
            case TextBlock tb when !string.IsNullOrEmpty(tb.Text):
            {
                var wrapResult = TextMeasurer.WrapTextBlock(tb, node.Width);
                node.CachedWrapResult = wrapResult;
                if (wrapResult.TotalHeight > 0)
                    node.Height = wrapResult.TotalHeight;
                break;
            }

            case RichText rt when rt.Spans.Count > 0:
            {
                var wrapResult = TextMeasurer.WrapRichText(rt, node.Width);
                node.CachedWrapResult = wrapResult;
                if (wrapResult.TotalHeight > 0)
                    node.Height = wrapResult.TotalHeight;
                break;
            }
        }
    }

    /// <summary>
    /// Returns the wrapped height of a text element at its current resolved width,
    /// or the original height if the element is not a text type.
    /// </summary>
    private static double GetWrappedHeight(LayoutNode node, Element element, double fallbackHeight)
    {
        switch (element)
        {
            case TextBlock tb when !string.IsNullOrEmpty(tb.Text):
            {
                if (node.CachedWrapResult is TextWrapResult cached)
                    return cached.TotalHeight > 0 ? cached.TotalHeight : fallbackHeight;

                var wrapResult = TextMeasurer.WrapTextBlock(tb, node.Width);
                node.CachedWrapResult = wrapResult;
                return wrapResult.TotalHeight > 0 ? wrapResult.TotalHeight : fallbackHeight;
            }

            case RichText rt when rt.Spans.Count > 0:
            {
                if (node.CachedWrapResult is RichTextWrapResult cached)
                    return cached.TotalHeight > 0 ? cached.TotalHeight : fallbackHeight;

                var wrapResult = TextMeasurer.WrapRichText(rt, node.Width);
                node.CachedWrapResult = wrapResult;
                return wrapResult.TotalHeight > 0 ? wrapResult.TotalHeight : fallbackHeight;
            }

            default:
                return fallbackHeight;
        }
    }

    /// <summary>
    /// Applies min/max width and height constraints from the element's style.
    /// </summary>
    private static void ClampToMinMax(LayoutNode node, Element element, double availableWidth, double availableHeight)
    {
        var style = element.Style;
        if (style == null)
            return;

        if (style.MinWidth != null)
        {
            double minW = StyleResolver.ResolveLength(style.MinWidth, availableWidth, 0);
            if (node.Width < minW) node.Width = minW;
        }

        if (style.MaxWidth != null)
        {
            double maxW = StyleResolver.ResolveLength(style.MaxWidth, availableWidth, double.PositiveInfinity);
            if (node.Width > maxW) node.Width = maxW;
        }

        if (style.MinHeight != null)
        {
            double minH = StyleResolver.ResolveLength(style.MinHeight, availableHeight, 0);
            if (node.Height < minH) node.Height = minH;
        }

        if (style.MaxHeight != null)
        {
            double maxH = StyleResolver.ResolveLength(style.MaxHeight, availableHeight, double.PositiveInfinity);
            if (node.Height > maxH) node.Height = maxH;
        }
    }

    /// <summary>
    /// Resolves a Box's single child within the box's content area (after padding/border).
    /// </summary>
    /// <param name="node">The Box's layout node.</param>
    /// <param name="box">The Box element.</param>
    /// <param name="measureOnly">
    /// When <see langword="true"/>, performs a measurement-only pass that skips cross-axis
    /// stretch so the caller can determine content-based height without circular dependency.
    /// </param>
    private static void ResolveBox(LayoutNode node, Box box, bool measureOnly = false)
    {
        if (box.Child == null || node.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(box);
        var borderWidths = StyleResolver.GetBorderWidths(box);
        double contentW = node.Width - padding.HorizontalTotal - borderWidths.HorizontalTotal;
        double contentH = node.Height - padding.VerticalTotal - borderWidths.VerticalTotal;
        contentW = Math.Max(0, contentW);
        contentH = Math.Max(0, contentH);

        var childNode = node.Children[0];
        var childIntrinsic = MeasureIntrinsic(childNode, box.Child);
        Resolve(childNode, box.Child, contentW, contentH, childIntrinsic);
    }

    /// <summary>
    /// Resolves children of a Row within its content area, applying flex-grow/shrink.
    /// </summary>
    /// <param name="node">The Row's layout node.</param>
    /// <param name="row">The Row element.</param>
    /// <param name="measureOnly">
    /// When <see langword="true"/>, performs a measurement-only pass that skips cross-axis
    /// stretch (height) so the caller can determine content-based height without circular
    /// dependency. Width stretch on the main axis still applies.
    /// </param>
    private static void ResolveRow(LayoutNode node, Row row, bool measureOnly = false)
    {
        if (row.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(row);
        var borderWidths = StyleResolver.GetBorderWidths(row);
        double contentW = Math.Max(0, node.Width - padding.HorizontalTotal - borderWidths.HorizontalTotal);
        double contentH = Math.Max(0, node.Height - padding.VerticalTotal - borderWidths.VerticalTotal);
        double totalGap = row.Gap * Math.Max(0, row.Children.Count - 1);

        // First pass: allocate main-axis widths following the Yoga/Flexbox algorithm:
        //   1. Children with explicit width get that exact size.
        //   2. Non-flex children (flex-grow == 0) get their intrinsic measured width.
        //   3. Flex-grow children start at 0 (flex-basis: 0) - they receive space
        //      only from the surplus after non-flex children are allocated.
        // This ensures non-flex children keep their natural width and flex-grow
        // children fill the remaining space, rather than all children competing
        // for intrinsic space and triggering unwanted flex-shrink.
        var elements = new List<Element>(row.Children.Count);
        var intrinsics = new List<(double Width, double Height)>(row.Children.Count);
        double totalChildWidth = 0;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = row.Children[i];
            elements.Add(childElement);

            var childIntrinsic = MeasureIntrinsic(childNode, childElement);
            intrinsics.Add(childIntrinsic);

            double childW;
            if (childElement.Style?.Width != null)
            {
                // Explicit width: use the resolved value.
                childW = StyleResolver.ResolveLength(childElement.Style.Width, contentW, childIntrinsic.Width);
            }
            else if (StyleResolver.GetFlexGrow(childElement) > 0)
            {
                // Flex-grow item with no explicit width: start at 0 (flex-basis: 0).
                // It will receive its width from surplus distribution below.
                childW = 0;
            }
            else
            {
                // Non-flex item with no explicit width: use intrinsic measured width.
                childW = childIntrinsic.Width;
            }

            childNode.Width = childW;
            totalChildWidth += childW;
        }

        // Flex grow/shrink on the main axis (horizontal).
        double spaceForChildren = contentW - totalGap;
        double delta = spaceForChildren - totalChildWidth;

        if (delta > 0)
        {
            FlexResolver.DistributeGrow(node.Children, elements, delta, isHorizontal: true);
        }
        else if (delta < 0)
        {
            FlexResolver.DistributeShrink(node.Children, elements, -delta, isHorizontal: true);
        }

        // Resolve each child's height (cross axis). For text elements, re-measure
        // at the resolved width to get the wrapped height before applying cross-axis sizing.
        // For container children without explicit height, do a measurement resolve to get
        // accurate content-based height (accounts for text wrapping at constrained widths).
        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = elements[i];
            var childIntrinsic = intrinsics[i];

            // Re-measure text at constrained width to get wrapped height.
            double wrappedHeight = childIntrinsic.Height;
            if (childElement.Style?.Height == null)
            {
                wrappedHeight = GetWrappedHeight(childNode, childElement, childIntrinsic.Height);

                // For container children, do a measurement resolve to get accurate
                // content-based height that accounts for text wrapping.
                if (childElement is Container or Box)
                {
                    childNode.Height = 1e6;
                    ResolveChildren(childNode, childElement, measureOnly: true);
                    wrappedHeight = ComputeContentHeight(childNode, childElement);
                    // Invalidate cached wrap results since widths may differ on final pass.
                    InvalidateCachedWrapResults(childNode);
                }
            }

            double childH;
            if (!measureOnly && row.Align == Align.Stretch && childElement.Style?.Height == null)
            {
                childH = contentH;
            }
            else
            {
                childH = StyleResolver.ResolveLength(childElement.Style?.Height, contentH, wrappedHeight);
            }

            childNode.Height = childH;

            // Recursively resolve grandchildren (final layout pass).
            ResolveChildren(childNode, childElement, measureOnly);
        }
    }

    /// <summary>
    /// Resolves children of a Column within its content area, applying flex-grow/shrink.
    /// Container children are resolved in a measurement pass before heights are computed,
    /// following the Yoga/Flexbox pattern of resolving children for measurement before
    /// computing parent heights. This ensures text wrapping at constrained widths produces
    /// accurate heights for flex distribution.
    /// </summary>
    /// <param name="node">The Column's layout node.</param>
    /// <param name="col">The Column element.</param>
    /// <param name="measureOnly">
    /// When <see langword="true"/>, performs a measurement-only pass. Width stretch on
    /// the cross axis still applies (needed for text wrapping), but heights are
    /// content-based rather than stretched.
    /// </param>
    private static void ResolveColumn(LayoutNode node, Column col, bool measureOnly = false)
    {
        if (col.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(col);
        var borderWidths = StyleResolver.GetBorderWidths(col);
        double contentW = Math.Max(0, node.Width - padding.HorizontalTotal - borderWidths.HorizontalTotal);
        double contentH = Math.Max(0, node.Height - padding.VerticalTotal - borderWidths.VerticalTotal);
        double totalGap = col.Gap * Math.Max(0, col.Children.Count - 1);

        var elements = new List<Element>(col.Children.Count);
        var intrinsics = new List<(double Width, double Height)>(col.Children.Count);
        double totalChildHeight = 0;

        // Phase A: Set cross-axis widths and resolve children to get content-based heights.
        // Container children get a measurement resolve so text wrapping at constrained
        // widths produces accurate heights before flex distribution.
        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = col.Children[i];
            elements.Add(childElement);

            var childIntrinsic = MeasureIntrinsic(childNode, childElement);

            // Pre-resolve the child's width (cross axis) so text wrapping can use it.
            double childW;
            if (col.Align == Align.Stretch && childElement.Style?.Width == null)
            {
                childW = contentW;
            }
            else
            {
                childW = StyleResolver.ResolveLength(childElement.Style?.Width, contentW, childIntrinsic.Width);
            }

            childNode.Width = childW;

            // Compute effective height following the Yoga/Flexbox algorithm:
            //   - Children with explicit height get that exact size.
            //   - Flex-grow children (on the main axis) start at 0.
            //   - Non-flex children get their content-based height.
            double effectiveHeight;
            if (childElement.Style?.Height != null)
            {
                effectiveHeight = StyleResolver.ResolveLength(childElement.Style.Height, contentH, childIntrinsic.Height);
            }
            else if (StyleResolver.GetFlexGrow(childElement) > 0)
            {
                // Flex-grow item: start at 0 (flex-basis: 0) on the main axis.
                effectiveHeight = 0;
            }
            else
            {
                // Non-flex item: compute content-based height.
                effectiveHeight = GetWrappedHeight(childNode, childElement, childIntrinsic.Height);

                // For container children, do a measurement resolve to get accurate
                // content-based height that accounts for nested text wrapping.
                if (childElement is Container or Box)
                {
                    childNode.Height = 1e6;
                    ResolveChildren(childNode, childElement, measureOnly: true);
                    effectiveHeight = ComputeContentHeight(childNode, childElement);
                    InvalidateCachedWrapResults(childNode);
                }
            }

            intrinsics.Add((childIntrinsic.Width, effectiveHeight));

            childNode.Height = effectiveHeight;
            totalChildHeight += effectiveHeight;
        }

        // Phase B: Flex grow/shrink on the main axis (vertical).
        double spaceForChildren = contentH - totalGap;
        double delta = spaceForChildren - totalChildHeight;

        if (delta > 0)
        {
            FlexResolver.DistributeGrow(node.Children, elements, delta, isHorizontal: false);
        }
        else if (delta < 0)
        {
            FlexResolver.DistributeShrink(node.Children, elements, -delta, isHorizontal: false);
        }

        // Phase C: Final resolve pass. Widths are already set above.
        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = elements[i];

            // Recursively resolve grandchildren (final layout pass).
            ResolveChildren(childNode, childElement, measureOnly);
        }
    }

    /// <summary>
    /// Re-resolves nested children after a node's size has been set by its parent.
    /// This handles elements like nested Row/Column/Box that need their own child layout pass.
    /// </summary>
    /// <param name="node">The parent layout node whose children need resolving.</param>
    /// <param name="element">The source element for the node.</param>
    /// <param name="measureOnly">
    /// When <see langword="true"/>, performs a measurement-only pass that skips cross-axis
    /// stretch to break circular height dependencies.
    /// </param>
    private static void ResolveChildren(LayoutNode node, Element element, bool measureOnly = false)
    {
        switch (element)
        {
            case Box box:
                ResolveBox(node, box, measureOnly);
                break;
            case Row row:
                ResolveRow(node, row, measureOnly);
                break;
            case Column col:
                ResolveColumn(node, col, measureOnly);
                break;
        }
    }

    /// <summary>
    /// Computes the intrinsic size of an element without creating new LayoutNode children.
    /// Used during the resolve phase when we need intrinsic dimensions but child nodes
    /// are already built.
    /// </summary>
    private static (double Width, double Height) MeasureIntrinsic(LayoutNode node, Element element)
    {
        return element switch
        {
            TextBlock tb => MeasureTextBlock(tb),
            RichText rt => MeasureRichText(rt),
            SvgBox svg => MeasureSvgBox(svg),
            ImageBox img => MeasureImageBox(img),
            Table tbl => MeasureTable(tbl),
            Spacer => (0, 0),
            PageBreak => (0, 0),
            Divider d => MeasureDivider(d),
            Box box => MeasureBoxIntrinsic(node, box),
            Row row => MeasureRowIntrinsic(node, row),
            Column col => MeasureColumnIntrinsic(node, col),
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Computes a Box's intrinsic size from its existing child LayoutNode.
    /// </summary>
    private static (double Width, double Height) MeasureBoxIntrinsic(LayoutNode node, Box box)
    {
        var padding = StyleResolver.GetPadding(box);
        var borderWidths = StyleResolver.GetBorderWidths(box);
        double insetH = padding.HorizontalTotal + borderWidths.HorizontalTotal;
        double insetV = padding.VerticalTotal + borderWidths.VerticalTotal;

        if (box.Child == null || node.Children.Count == 0)
            return (insetH, insetV);

        var childIntrinsic = MeasureIntrinsic(node.Children[0], box.Child);
        return (childIntrinsic.Width + insetH, childIntrinsic.Height + insetV);
    }

    /// <summary>
    /// Computes a Row's intrinsic size from its existing child LayoutNodes.
    /// </summary>
    private static (double Width, double Height) MeasureRowIntrinsic(LayoutNode node, Row row)
    {
        var padding = StyleResolver.GetPadding(row);
        var borderWidths = StyleResolver.GetBorderWidths(row);
        double insetH = padding.HorizontalTotal + borderWidths.HorizontalTotal;
        double insetV = padding.VerticalTotal + borderWidths.VerticalTotal;

        if (row.Children.Count == 0)
            return (insetH, insetV);

        double totalWidth = 0;
        double maxHeight = 0;

        for (int i = 0; i < node.Children.Count && i < row.Children.Count; i++)
        {
            var childIntrinsic = MeasureIntrinsic(node.Children[i], row.Children[i]);
            totalWidth += childIntrinsic.Width;
            if (childIntrinsic.Height > maxHeight)
                maxHeight = childIntrinsic.Height;
        }

        double totalGap = row.Gap * Math.Max(0, row.Children.Count - 1);
        return (totalWidth + totalGap + insetH, maxHeight + insetV);
    }

    /// <summary>
    /// Computes a Column's intrinsic size from its existing child LayoutNodes.
    /// </summary>
    private static (double Width, double Height) MeasureColumnIntrinsic(LayoutNode node, Column col)
    {
        var padding = StyleResolver.GetPadding(col);
        var borderWidths = StyleResolver.GetBorderWidths(col);
        double insetH = padding.HorizontalTotal + borderWidths.HorizontalTotal;
        double insetV = padding.VerticalTotal + borderWidths.VerticalTotal;

        if (col.Children.Count == 0)
            return (insetH, insetV);

        double maxWidth = 0;
        double totalHeight = 0;

        for (int i = 0; i < node.Children.Count && i < col.Children.Count; i++)
        {
            var childIntrinsic = MeasureIntrinsic(node.Children[i], col.Children[i]);
            if (childIntrinsic.Width > maxWidth)
                maxWidth = childIntrinsic.Width;
            totalHeight += childIntrinsic.Height;
        }

        double totalGap = col.Gap * Math.Max(0, col.Children.Count - 1);
        return (maxWidth + insetH, totalHeight + totalGap + insetV);
    }

    /// <summary>
    /// Computes the actual content height of a resolved container from its children's sizes.
    /// Used after a measurement resolve pass to determine how tall a container's content
    /// actually is, independent of any stretch or explicit height.
    /// </summary>
    /// <param name="node">The container's layout node with resolved children.</param>
    /// <param name="element">The container element.</param>
    /// <returns>The content-based height including padding and border.</returns>
    private static double ComputeContentHeight(LayoutNode node, Element element)
    {
        switch (element)
        {
            case Row row:
            {
                var p = StyleResolver.GetPadding(row);
                var b = StyleResolver.GetBorderWidths(row);
                double maxH = 0;
                for (int i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i].Height > maxH)
                        maxH = node.Children[i].Height;
                }

                return maxH + p.VerticalTotal + b.VerticalTotal;
            }

            case Column col:
            {
                var p = StyleResolver.GetPadding(col);
                var b = StyleResolver.GetBorderWidths(col);
                double totalH = 0;
                for (int i = 0; i < node.Children.Count; i++)
                    totalH += node.Children[i].Height;
                double gap = col.Gap * Math.Max(0, node.Children.Count - 1);
                return totalH + gap + p.VerticalTotal + b.VerticalTotal;
            }

            case Box:
            {
                var p = StyleResolver.GetPadding(element);
                var b = StyleResolver.GetBorderWidths(element);
                if (node.Children.Count > 0)
                    return node.Children[0].Height + p.VerticalTotal + b.VerticalTotal;
                return p.VerticalTotal + b.VerticalTotal;
            }

            default:
                return node.Height;
        }
    }

    /// <summary>
    /// Recursively clears cached text wrap results from a node and all its descendants.
    /// Called between the measurement pass and the final layout pass to ensure text is
    /// re-measured if widths change (e.g., due to flex distribution or stretch).
    /// </summary>
    /// <param name="node">The root node to invalidate from.</param>
    private static void InvalidateCachedWrapResults(LayoutNode node)
    {
        node.CachedWrapResult = null;
        for (int i = 0; i < node.Children.Count; i++)
        {
            InvalidateCachedWrapResults(node.Children[i]);
        }
    }

    // ── Phase 3: Position ───────────────────────────────────────────────
    // Top-down pass. Assign absolute (x, y) coordinates to each node.

    /// <summary>
    /// Assigns absolute positions to a node and its children, applying justify/align
    /// for flex containers and padding/border offsets for all containers.
    /// </summary>
    private static void Position(LayoutNode node, double offsetX, double offsetY)
    {
        node.X = offsetX;
        node.Y = offsetY;

        switch (node.Source)
        {
            case Box box:
                PositionBox(node, box);
                break;
            case Row row:
                PositionRow(node, row);
                break;
            case Column col:
                PositionColumn(node, col);
                break;
        }
    }

    /// <summary>
    /// Positions a Box's single child with padding and border offsets.
    /// </summary>
    private static void PositionBox(LayoutNode node, Box box)
    {
        if (box.Child == null || node.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(box);
        var borderWidths = StyleResolver.GetBorderWidths(box);
        double childX = node.X + padding.Left + borderWidths.Left;
        double childY = node.Y + padding.Top + borderWidths.Top;

        Position(node.Children[0], childX, childY);
    }

    /// <summary>
    /// Positions Row children along the horizontal main axis with justify/align.
    /// </summary>
    private static void PositionRow(LayoutNode node, Row row)
    {
        if (node.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(row);
        var borderWidths = StyleResolver.GetBorderWidths(row);
        double contentStartX = node.X + padding.Left + borderWidths.Left;
        double contentStartY = node.Y + padding.Top + borderWidths.Top;
        double contentW = Math.Max(0, node.Width - padding.HorizontalTotal - borderWidths.HorizontalTotal);
        double contentH = Math.Max(0, node.Height - padding.VerticalTotal - borderWidths.VerticalTotal);

        // Calculate total children width.
        double totalChildWidth = 0;
        for (int i = 0; i < node.Children.Count; i++)
        {
            totalChildWidth += node.Children[i].Width;
        }

        double totalGap = row.Gap * Math.Max(0, node.Children.Count - 1);
        double freeSpace = contentW - totalChildWidth - totalGap;

        // Compute main-axis starting offset and inter-item spacing based on justify.
        double mainOffset = 0;
        double extraSpacing = 0;
        ComputeJustify(row.Justify, freeSpace, node.Children.Count, out mainOffset, out extraSpacing);

        double cursorX = contentStartX + mainOffset;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];

            // Cross-axis alignment (vertical).
            double childY = ComputeCrossPosition(
                row.Align, contentStartY, contentH, childNode.Height);

            Position(childNode, cursorX, childY);

            cursorX += childNode.Width + row.Gap + extraSpacing;
        }
    }

    /// <summary>
    /// Positions Column children along the vertical main axis with justify/align.
    /// </summary>
    private static void PositionColumn(LayoutNode node, Column col)
    {
        if (node.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(col);
        var borderWidths = StyleResolver.GetBorderWidths(col);
        double contentStartX = node.X + padding.Left + borderWidths.Left;
        double contentStartY = node.Y + padding.Top + borderWidths.Top;
        double contentW = Math.Max(0, node.Width - padding.HorizontalTotal - borderWidths.HorizontalTotal);
        double contentH = Math.Max(0, node.Height - padding.VerticalTotal - borderWidths.VerticalTotal);

        double totalChildHeight = 0;
        for (int i = 0; i < node.Children.Count; i++)
        {
            totalChildHeight += node.Children[i].Height;
        }

        double totalGap = col.Gap * Math.Max(0, node.Children.Count - 1);
        double freeSpace = contentH - totalChildHeight - totalGap;

        ComputeJustify(col.Justify, freeSpace, node.Children.Count, out double mainOffset, out double extraSpacing);

        double cursorY = contentStartY + mainOffset;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];

            // Cross-axis alignment (horizontal).
            double childX = ComputeCrossPosition(
                col.Align, contentStartX, contentW, childNode.Width);

            Position(childNode, childX, cursorY);

            cursorY += childNode.Height + col.Gap + extraSpacing;
        }
    }

    /// <summary>
    /// Computes the main-axis starting offset and per-item extra spacing for a given
    /// <see cref="Justify"/> mode, free space, and child count.
    /// </summary>
    private static void ComputeJustify(
        Justify justify,
        double freeSpace,
        int childCount,
        out double mainOffset,
        out double extraSpacing)
    {
        mainOffset = 0;
        extraSpacing = 0;

        // When free space is negative (overflow), justify has no effect.
        if (freeSpace <= 0)
            return;

        switch (justify)
        {
            case Justify.Start:
                break;

            case Justify.End:
                mainOffset = freeSpace;
                break;

            case Justify.Center:
                mainOffset = freeSpace / 2.0;
                break;

            case Justify.SpaceBetween:
                if (childCount > 1)
                    extraSpacing = freeSpace / (childCount - 1);
                break;

            case Justify.SpaceAround:
                if (childCount > 0)
                {
                    double perItem = freeSpace / childCount;
                    mainOffset = perItem / 2.0;
                    extraSpacing = perItem;
                }
                break;

            case Justify.SpaceEvenly:
                if (childCount > 0)
                {
                    double slot = freeSpace / (childCount + 1);
                    mainOffset = slot;
                    extraSpacing = slot;
                }
                break;
        }
    }

    /// <summary>
    /// Computes the cross-axis position for a child given the alignment mode.
    /// </summary>
    private static double ComputeCrossPosition(
        Align align,
        double contentStart,
        double contentSize,
        double childSize)
    {
        return align switch
        {
            Align.Start or Align.Stretch => contentStart,
            Align.End => contentStart + contentSize - childSize,
            Align.Center => contentStart + (contentSize - childSize) / 2.0,
            _ => contentStart
        };
    }

    #endregion Private Methods
}
