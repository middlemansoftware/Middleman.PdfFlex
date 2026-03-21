// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;

namespace Middleman.PdfFlex.Styling;

/// <summary>
/// Resolves cascading style properties by walking up the element tree. When a property
/// is null on the current element, the resolver checks ancestors until a value is found
/// or the default is returned.
/// </summary>
internal static class StyleResolver
{
    /// <summary>Default font size in points when no ancestor specifies one.</summary>
    private const double DefaultFontSize = 10.0;

    #region Public Methods

    /// <summary>
    /// Resolves the effective font size by walking up the parent chain.
    /// Returns <see cref="DefaultFontSize"/> if no ancestor defines a font size.
    /// </summary>
    /// <param name="element">The element to resolve from.</param>
    /// <returns>The resolved font size in points.</returns>
    public static double GetFontSize(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.Style?.FontSize is { } size)
                return size;

            // Also check TextBlock's FontSpec as a source of font size.
            if (current is TextBlock textBlock && textBlock.Font != null)
                return textBlock.Font.Size;

            current = current.Parent;
        }

        return DefaultFontSize;
    }

    /// <summary>
    /// Resolves the effective font weight by walking up the parent chain.
    /// Returns <see cref="FontWeight.Normal"/> if no ancestor defines a font weight.
    /// </summary>
    /// <param name="element">The element to resolve from.</param>
    /// <returns>The resolved font weight.</returns>
    public static FontWeight GetFontWeight(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.Style?.FontWeight is { } weight)
                return weight;

            current = current.Parent;
        }

        return FontWeight.Normal;
    }

    /// <summary>
    /// Resolves the effective font color by walking up the parent chain.
    /// Returns <see cref="Colors.Black"/> if no ancestor defines a font color.
    /// </summary>
    /// <param name="element">The element to resolve from.</param>
    /// <returns>The resolved font color.</returns>
    public static Color GetFontColor(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.Style?.FontColor is { } color)
                return color;

            current = current.Parent;
        }

        return Colors.Black;
    }

    /// <summary>
    /// Gets the padding for an element. Padding does not cascade; returns
    /// <see cref="EdgeInsets.Zero"/> if the element has no padding set.
    /// </summary>
    /// <param name="element">The element to read padding from.</param>
    /// <returns>The element's padding or zero.</returns>
    public static EdgeInsets GetPadding(Element element)
    {
        return element.Style?.Padding ?? EdgeInsets.Zero;
    }

    /// <summary>
    /// Gets the margin for an element. Margin does not cascade; returns
    /// <see cref="EdgeInsets.Zero"/> if the element has no margin set.
    /// </summary>
    /// <param name="element">The element to read margin from.</param>
    /// <returns>The element's margin or zero.</returns>
    public static EdgeInsets GetMargin(Element element)
    {
        return element.Style?.Margin ?? EdgeInsets.Zero;
    }

    /// <summary>
    /// Gets the flex-grow factor for an element. Defaults to 0 (no growth).
    /// </summary>
    /// <param name="element">The element to read flex-grow from.</param>
    /// <returns>The flex-grow factor.</returns>
    public static double GetFlexGrow(Element element)
    {
        return element.Style?.FlexGrow ?? 0;
    }

    /// <summary>
    /// Gets the flex-shrink factor for an element. Defaults to 1.
    /// </summary>
    /// <param name="element">The element to read flex-shrink from.</param>
    /// <returns>The flex-shrink factor.</returns>
    public static double GetFlexShrink(Element element)
    {
        return element.Style?.FlexShrink ?? 1;
    }

    /// <summary>
    /// Gets the total border width contribution for each edge. Returns zero widths
    /// if the element has no border defined.
    /// </summary>
    /// <param name="element">The element to read border widths from.</param>
    /// <returns>Edge insets representing border widths on each side.</returns>
    public static EdgeInsets GetBorderWidths(Element element)
    {
        var border = element.Style?.Border;
        if (border == null)
            return EdgeInsets.Zero;

        return new EdgeInsets(border.Top.Width, border.Right.Width, border.Bottom.Width, border.Left.Width);
    }

    /// <summary>
    /// Resolves a <see cref="Length"/> value to an absolute point value, using the parent
    /// dimension for percentage calculations and the intrinsic size as the auto fallback.
    /// </summary>
    /// <param name="length">The length to resolve. Null returns <paramref name="intrinsicSize"/>.</param>
    /// <param name="parentDimension">The parent's resolved size in the same axis, used for percentage calculations.</param>
    /// <param name="intrinsicSize">The element's intrinsic (content-based) size, used as the auto fallback.</param>
    /// <returns>The resolved size in points.</returns>
    public static double ResolveLength(Length? length, double parentDimension, double intrinsicSize)
    {
        if (length == null)
            return intrinsicSize;

        return length.Value.Type switch
        {
            Length.Unit.Percent => parentDimension * length.Value.Value / 100.0,
            Length.Unit.Auto => intrinsicSize,
            _ when length.Value.IsAbsolute => length.Value.ToPoints(),
            // Fr units are handled separately by the flex resolver.
            _ => intrinsicSize
        };
    }

    #endregion Public Methods
}
