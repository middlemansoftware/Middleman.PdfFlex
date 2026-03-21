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
    /// Calculates the complete layout for an element tree within the given available space.
    /// </summary>
    /// <param name="element">The root element of the tree to lay out.</param>
    /// <param name="availableWidth">The available width in points.</param>
    /// <param name="availableHeight">The available height in points.</param>
    /// <returns>The root <see cref="LayoutNode"/> with resolved positions and sizes for the entire tree.</returns>
    public static LayoutNode Calculate(Element element, double availableWidth, double availableHeight)
    {
        ArgumentNullException.ThrowIfNull(element);

        var node = new LayoutNode(element);
        var intrinsic = Measure(node, element);
        Resolve(node, element, availableWidth, availableHeight, intrinsic);
        Position(node, 0, 0);
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
    /// Measures a TextBlock using the character-count approximation.
    /// </summary>
    private static (double Width, double Height) MeasureTextBlock(TextBlock tb)
    {
        double fontSize = StyleResolver.GetFontSize(tb);
        return (tb.EstimateWidth(fontSize), tb.EstimateHeight(fontSize));
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
    /// Measures a RichText element by estimating width from the sum of span text lengths.
    /// </summary>
    private static (double Width, double Height) MeasureRichText(RichText rt)
    {
        double fontSize = StyleResolver.GetFontSize(rt);
        double totalChars = 0;

        for (int i = 0; i < rt.Spans.Count; i++)
        {
            totalChars += rt.Spans[i].Text.Length;
        }

        double width = totalChars * fontSize * 0.5;
        double height = fontSize * 1.2;
        return (width, height);
    }

    /// <summary>
    /// Measures an SvgBox using the SVG document's intrinsic dimensions.
    /// </summary>
    private static (double Width, double Height) MeasureSvgBox(SvgBox svg)
    {
        var doc = svg.GetDocument();
        return (doc.Width, doc.Height);
    }

    /// <summary>
    /// Measures an ImageBox. Uses the element's intrinsic dimensions if available,
    /// otherwise falls back to a 100x100 default to avoid loading the image during layout.
    /// </summary>
    private static (double Width, double Height) MeasureImageBox(ImageBox img)
    {
        const double defaultSize = 100.0;
        double w = img.IntrinsicWidth > 0 ? img.IntrinsicWidth : defaultSize;
        double h = img.IntrinsicHeight > 0 ? img.IntrinsicHeight : defaultSize;
        return (w, h);
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
    private static void ResolveBox(LayoutNode node, Box box)
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
    private static void ResolveRow(LayoutNode node, Row row)
    {
        if (row.Children.Count == 0)
            return;

        var padding = StyleResolver.GetPadding(row);
        var borderWidths = StyleResolver.GetBorderWidths(row);
        double contentW = Math.Max(0, node.Width - padding.HorizontalTotal - borderWidths.HorizontalTotal);
        double contentH = Math.Max(0, node.Height - padding.VerticalTotal - borderWidths.VerticalTotal);
        double totalGap = row.Gap * Math.Max(0, row.Children.Count - 1);

        // First pass: resolve each child's intrinsic width within the content area.
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

            // Resolve child width using style or intrinsic.
            double childW = StyleResolver.ResolveLength(childElement.Style?.Width, contentW, childIntrinsic.Width);
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

        // Resolve each child's height (cross axis).
        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = elements[i];
            var childIntrinsic = intrinsics[i];

            double childH;
            if (row.Align == Align.Stretch && childElement.Style?.Height == null)
            {
                childH = contentH;
            }
            else
            {
                childH = StyleResolver.ResolveLength(childElement.Style?.Height, contentH, childIntrinsic.Height);
            }

            childNode.Height = childH;

            // Recursively resolve grandchildren.
            ResolveChildren(childNode, childElement);
        }
    }

    /// <summary>
    /// Resolves children of a Column within its content area, applying flex-grow/shrink.
    /// </summary>
    private static void ResolveColumn(LayoutNode node, Column col)
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

        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = col.Children[i];
            elements.Add(childElement);

            var childIntrinsic = MeasureIntrinsic(childNode, childElement);
            intrinsics.Add(childIntrinsic);

            double childH = StyleResolver.ResolveLength(childElement.Style?.Height, contentH, childIntrinsic.Height);
            childNode.Height = childH;
            totalChildHeight += childH;
        }

        // Flex grow/shrink on the main axis (vertical).
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

        // Resolve each child's width (cross axis).
        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            var childElement = elements[i];
            var childIntrinsic = intrinsics[i];

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

            // Recursively resolve grandchildren.
            ResolveChildren(childNode, childElement);
        }
    }

    /// <summary>
    /// Re-resolves nested children after a node's size has been set by its parent.
    /// This handles elements like nested Row/Column/Box that need their own child layout pass.
    /// </summary>
    private static void ResolveChildren(LayoutNode node, Element element)
    {
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
