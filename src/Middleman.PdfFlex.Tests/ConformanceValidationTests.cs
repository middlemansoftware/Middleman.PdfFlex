// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.Advanced;
using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies <c>PrepareForConformance()</c> and <c>ValidateConformance()</c> behavior
/// exercised through the <see cref="PdfDocument.Save(Stream, bool)"/> pipeline.
/// Confirms that MarkInfo, OutputIntents, Language, DisplayDocTitle, and StructTreeRoot
/// are correctly handled for each conformance profile.
/// </summary>
public class ConformanceValidationTests
{
    #region PrepareForConformance - MarkInfo

    [Fact]
    public void PrepareForConformance_PdfA1b_AddsMarkInfo()
    {
        var saved = SaveAndReopen(PdfConformance.PdfA1b);

        var markInfo = saved.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.MarkInfo);
        Assert.NotNull(markInfo);
    }

    [Fact]
    public void PrepareForConformance_PdfA2a_AddsMarkInfo()
    {
        var saved = SaveAndReopen(PdfConformance.PdfA2a);

        var markInfo = saved.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.MarkInfo);
        Assert.NotNull(markInfo);
    }

    [Fact]
    public void PrepareForConformance_PdfUA1_AddsMarkInfo()
    {
        var saved = SaveAndReopen(PdfConformance.PdfUA1);

        var markInfo = saved.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.MarkInfo);
        Assert.NotNull(markInfo);
    }

    #endregion PrepareForConformance - MarkInfo

    #region PrepareForConformance - OutputIntent

    [Fact]
    public void PrepareForConformance_PdfA1b_AddsOutputIntent()
    {
        var saved = SaveAndReopen(PdfConformance.PdfA1b);

        var outputIntents = saved.Internals.Catalog.Elements.GetObject(PdfCatalog.Keys.OutputIntents);
        Assert.NotNull(outputIntents);
    }

    [Fact]
    public void PrepareForConformance_PdfA2a_AddsOutputIntent()
    {
        var saved = SaveAndReopen(PdfConformance.PdfA2a);

        var outputIntents = saved.Internals.Catalog.Elements.GetObject(PdfCatalog.Keys.OutputIntents);
        Assert.NotNull(outputIntents);
    }

    [Fact]
    public void PrepareForConformance_PdfUA1_DoesNotAddOutputIntent()
    {
        var saved = SaveAndReopen(PdfConformance.PdfUA1);

        var outputIntents = saved.Internals.Catalog.Elements.GetObject(PdfCatalog.Keys.OutputIntents);
        Assert.Null(outputIntents);
    }

    #endregion PrepareForConformance - OutputIntent

    #region PrepareForConformance - Language

    [Fact]
    public void PrepareForConformance_PdfUA1_SetsDefaultLanguage()
    {
        // Save without explicitly setting Language. PrepareForConformance should default to "en".
        var saved = SaveAndReopen(PdfConformance.PdfUA1);

        var lang = saved.Internals.Catalog.Elements.GetString(PdfCatalog.Keys.Lang);
        Assert.Equal("en", lang);
    }

    [Fact]
    public void PrepareForConformance_PdfUA1_PreservesExplicitLanguage()
    {
        using var ms = new MemoryStream();
        using (var pdfDoc = new PdfDocument())
        {
            pdfDoc.Conformance = PdfConformance.PdfUA1;
            pdfDoc.Language = "fr";
            pdfDoc.AddPage();
            pdfDoc.Save(ms, closeStream: false);
        }

        ms.Position = 0;
        using var saved = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        var lang = saved.Internals.Catalog.Elements.GetString(PdfCatalog.Keys.Lang);
        Assert.Equal("fr", lang);
    }

    #endregion PrepareForConformance - Language

    #region PrepareForConformance - DisplayDocTitle

    [Fact]
    public void PrepareForConformance_PdfUA1_SetsDisplayDocTitle()
    {
        var saved = SaveAndReopen(PdfConformance.PdfUA1);

        var viewerPrefs = saved.Internals.Catalog.Elements.GetDictionary("/ViewerPreferences");
        Assert.NotNull(viewerPrefs);
        Assert.True(viewerPrefs.Elements.GetBoolean("/DisplayDocTitle"));
    }

    #endregion PrepareForConformance - DisplayDocTitle

    #region ValidateConformance - StructTreeRoot

    [Fact]
    public void ValidateConformance_PdfA1b_PassesWithoutStructTreeRoot()
    {
        // PDF/A-1b does not require tagged structure -- save should succeed.
        var bytes = SaveToBytes(PdfConformance.PdfA1b);

        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void ValidateConformance_PdfA2a_PassesWithTaggedStructure()
    {
        // Setting conformance to PdfA2a initializes UAManager which creates StructTreeRoot.
        var bytes = SaveToBytes(PdfConformance.PdfA2a);

        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void ValidateConformance_PdfUA1_PassesWithTaggedStructure()
    {
        // Setting conformance to PdfUA1 initializes UAManager which creates StructTreeRoot.
        var bytes = SaveToBytes(PdfConformance.PdfUA1);

        Assert.True(bytes.Length > 0);
    }

    #endregion ValidateConformance - StructTreeRoot

    #region None Conformance

    [Fact]
    public void None_SkipsPreparationAndValidation()
    {
        // Save with no conformance. No MarkInfo, no OutputIntents, no Language enforcement.
        var saved = SaveAndReopen(PdfConformance.None);

        var markInfo = saved.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.MarkInfo);
        var outputIntents = saved.Internals.Catalog.Elements.GetObject(PdfCatalog.Keys.OutputIntents);

        Assert.Null(markInfo);
        Assert.Null(outputIntents);
    }

    #endregion None Conformance

    #region Helpers

    /// <summary>
    /// Creates a minimal PdfDocument with the given conformance, saves it, and returns the byte array.
    /// </summary>
    private static byte[] SaveToBytes(PdfConformance conformance)
    {
        using var ms = new MemoryStream();
        using (var pdfDoc = new PdfDocument())
        {
            pdfDoc.Conformance = conformance;
            pdfDoc.AddPage();
            pdfDoc.Save(ms, closeStream: false);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal PdfDocument with the given conformance, saves it, then reopens it
    /// for inspection. The caller must dispose the returned document.
    /// </summary>
    private static PdfDocument SaveAndReopen(PdfConformance conformance)
    {
        var ms = new MemoryStream();
        using (var pdfDoc = new PdfDocument())
        {
            pdfDoc.Conformance = conformance;
            pdfDoc.AddPage();
            pdfDoc.Save(ms, closeStream: false);
        }

        ms.Position = 0;
        return PdfReader.Open(ms, PdfDocumentOpenMode.Import);
    }

    #endregion Helpers
}
