// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Styling;
using PdfSharp.Drawing;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Creates PdfSharp <see cref="XFont"/> instances from PdfFlex font specifications
/// and cascading style properties.
/// </summary>
internal static class FontHelper
{
    #region Constants

    /// <summary>Default font family when none is specified.</summary>
    private const string DefaultFontFamily = "Arial";

    /// <summary>Default font size in points when none is specified.</summary>
    private const double DefaultFontSize = 10.0;

    #endregion Constants

    #region Public Methods

    /// <summary>
    /// Creates an <see cref="XFont"/> from a <see cref="FontSpec"/>. Falls back to
    /// <see cref="FontSpec.Default"/> if the spec is null.
    /// </summary>
    /// <param name="fontSpec">The font specification, or null for the default.</param>
    /// <returns>A configured <see cref="XFont"/> instance.</returns>
    public static XFont CreateFont(FontSpec? fontSpec)
    {
        var spec = fontSpec ?? FontSpec.Default;
        var xStyle = MapFontStyle(spec.Weight, spec.Style);
        return new XFont(spec.Family, spec.Size, xStyle);
    }

    /// <summary>
    /// Creates an <see cref="XFont"/> by resolving cascading style properties from an element
    /// and its parent chain. The element's <see cref="TextBlock.Font"/> takes priority,
    /// followed by style cascade values, then defaults.
    /// </summary>
    /// <param name="element">The element to resolve font properties from.</param>
    /// <returns>A configured <see cref="XFont"/> instance.</returns>
    public static XFont CreateFontFromElement(Element element)
    {
        // TextBlock's FontSpec takes priority when available.
        if (element is TextBlock tb && tb.Font != null)
            return CreateFont(tb.Font);

        string family = ResolveFamily(element);
        double size = StyleResolver.GetFontSize(element);
        var weight = StyleResolver.GetFontWeight(element);
        var fontStyle = ResolveFontStyle(element);
        var xStyle = MapFontStyle(weight, fontStyle);

        return new XFont(family, size, xStyle);
    }

    /// <summary>
    /// Creates an <see cref="XFont"/> from a <see cref="SpanStyle"/>, falling back to
    /// the parent element's style cascade for any null properties.
    /// </summary>
    /// <param name="spanStyle">The span-level style, or null to use element defaults entirely.</param>
    /// <param name="parentElement">The parent element for cascade fallback.</param>
    /// <returns>A configured <see cref="XFont"/> instance.</returns>
    public static XFont CreateFontFromSpan(SpanStyle? spanStyle, Element parentElement)
    {
        if (spanStyle == null)
            return CreateFontFromElement(parentElement);

        string family = spanStyle.FontFamily ?? ResolveFamily(parentElement);
        double size = spanStyle.FontSize ?? StyleResolver.GetFontSize(parentElement);
        var weight = spanStyle.FontWeight ?? StyleResolver.GetFontWeight(parentElement);
        var fontStyle = spanStyle.FontStyle ?? ResolveFontStyle(parentElement);
        var xStyle = MapFontStyle(weight, fontStyle);

        return new XFont(family, size, xStyle);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Resolves the font family name by walking the element's parent chain.
    /// </summary>
    private static string ResolveFamily(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.Style?.FontFamily is { } family)
                return family;
            current = current.Parent;
        }
        return DefaultFontFamily;
    }

    /// <summary>
    /// Resolves the font style (normal, italic, oblique) by walking the element's parent chain.
    /// </summary>
    private static Styling.FontStyle ResolveFontStyle(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.Style?.FontStyle is { } style)
                return style;
            current = current.Parent;
        }
        return Styling.FontStyle.Normal;
    }

    /// <summary>
    /// Maps PdfFlex font weight and style to a PdfSharp <see cref="XFontStyleEx"/> value.
    /// </summary>
    private static XFontStyleEx MapFontStyle(FontWeight weight, Styling.FontStyle style)
    {
        bool bold = weight >= FontWeight.Bold;
        bool italic = style is Styling.FontStyle.Italic or Styling.FontStyle.Oblique;

        return (bold, italic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular
        };
    }

    #endregion Private Methods
}
