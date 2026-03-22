// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies that watermark rendering works correctly under different conformance profiles.
/// PDF/A-1 profiles prohibit transparency and must use the pre-blending path; PDF/A-2+
/// and None profiles allow transparency. All paths must render without throwing.
/// Tests use conformance profiles that do not require tagged structure (b/u levels and None)
/// because tagged-structure profiles require the UA pipeline which is outside watermark scope.
/// </summary>
public class WatermarkConformanceTests
{
    #region Watermark Pre-Blending (No Transparency)

    [Fact]
    public void Watermark_PdfA1b_RendersWithPreBlending()
    {
        // PDF/A-1b prohibits transparency. The watermark must use pre-blended colors.
        var bytes = RenderWithWatermark(PdfConformance.PdfA1b);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    #endregion Watermark Pre-Blending (No Transparency)

    #region Watermark Transparency Path

    [Fact]
    public void Watermark_PdfA2b_RendersWithTransparency()
    {
        // PDF/A-2b allows transparency. The watermark uses the standard alpha path.
        var bytes = RenderWithWatermark(PdfConformance.PdfA2b);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Watermark_PdfA2u_RendersWithTransparency()
    {
        var bytes = RenderWithWatermark(PdfConformance.PdfA2u);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Watermark_PdfA3b_RendersWithTransparency()
    {
        // PDF/A-3b allows transparency.
        var bytes = RenderWithWatermark(PdfConformance.PdfA3b);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Watermark_None_RendersWithTransparency()
    {
        // No conformance. Standard transparency path.
        var bytes = RenderWithWatermark(PdfConformance.None);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    #endregion Watermark Transparency Path

    #region Helpers

    /// <summary>
    /// Creates a minimal document with a watermark and the given conformance, then renders it.
    /// </summary>
    private static byte[] RenderWithWatermark(PdfConformance conformance)
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50));
        doc.Conformance = conformance;
        doc.Watermark = new Watermark("CONFIDENTIAL", opacity: 0.15);
        doc.Add(new TextBlock("Test content", new FontSpec("Arial", 12)));

        return DocumentRenderer.RenderToBytes(doc);
    }

    #endregion Helpers
}
