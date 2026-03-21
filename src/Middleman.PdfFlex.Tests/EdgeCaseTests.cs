// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies cross-platform edge cases: empty documents, unit conversions,
/// hex color parsing, and basic rendering.
/// </summary>
public class EdgeCaseTests
{
    #region Empty Document

    [Fact]
    public void Document_EmptyDocument_ProducesValidPdf()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        // No elements added.

        var bytes = DocumentRenderer.RenderToBytes(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0, "Empty document should produce valid PDF bytes");
    }

    #endregion Empty Document

    #region Unit Conversions

    [Fact]
    public void Length_ToPoints_AllAbsoluteUnits()
    {
        // Pt(72) = 72 points.
        Assert.Equal(72, Length.Pt(72).ToPoints());

        // Mm(25.4) should be approximately 72 points (25.4mm = 1 inch = 72pt).
        Assert.Equal(72, Length.Mm(25.4).ToPoints(), 0.5);

        // In(1) = 72 points.
        Assert.Equal(72, Length.In(1).ToPoints());
    }

    #endregion Unit Conversions

    #region Color Parsing

    [Fact]
    public void Color_FromHex_ValidFormats()
    {
        // Uppercase and lowercase both produce the same color.
        var upper = Color.FromHex("#FF0000");
        var lower = Color.FromHex("#ff0000");

        Assert.Equal(255, upper.R);
        Assert.Equal(0, upper.G);
        Assert.Equal(0, upper.B);

        Assert.Equal(255, lower.R);
        Assert.Equal(0, lower.G);
        Assert.Equal(0, lower.B);

        Assert.Equal(upper, lower);
    }

    #endregion Color Parsing

    #region Basic Rendering

    [Fact]
    public void Document_WithTextBlock_RendersWithoutError()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Add(new TextBlock("Hello, PdfFlex!", new FontSpec("Arial", 14)));

        var bytes = DocumentRenderer.RenderToBytes(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0, "Document with TextBlock should produce valid PDF bytes");
    }

    #endregion Basic Rendering
}
