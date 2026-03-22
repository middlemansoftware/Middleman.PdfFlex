// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.Advanced;
using Middleman.PdfFlex.Pdf.IO;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies that the PDF/UA structure tagging implementation emits the correct
/// structure tree elements when <see cref="PdfConformance.PdfUA1"/> (or any
/// conformance with <see cref="PdfConformance.RequiresTaggedStructure"/>) is active.
/// Tests cover structure tree basics, text tagging, table tagging, figure tagging,
/// validation enforcement, and combined conformance profiles.
/// </summary>
public class PdfUaTaggingTests
{
    #region Helpers

    /// <summary>
    /// Creates a temporary JPEG file containing a 1x1 white pixel and returns the file path.
    /// The caller is responsible for deleting the file after use.
    /// </summary>
    private static string CreateTempJpegFile()
    {
        // Minimal valid JFIF JPEG: SOI + APP0 + DQT + SOF0 + DHT + SOS + scan data + EOI.
        // Verified to be accepted by PdfSharp's JPEG importer.
        byte[] jpeg =
        {
            0xFF, 0xD8,                         // SOI
            0xFF, 0xE0, 0x00, 0x10,             // APP0 length=16
            0x4A, 0x46, 0x49, 0x46, 0x00,       // "JFIF\0"
            0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0xFF, 0xDB, 0x00, 0x43, 0x00,       // DQT length=67, table 0
            0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07,
            0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14,
            0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12, 0x13,
            0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A,
            0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20, 0x22,
            0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C,
            0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39,
            0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32,
            0xFF, 0xC0, 0x00, 0x0B, 0x08,       // SOF0 length=11, 8-bit
            0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00,  // 1x1, 1 component
            0xFF, 0xC4, 0x00, 0x1F, 0x00,       // DHT length=31, DC table 0
            0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B,
            0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, // SOS
            0x7B, 0x40, 0x1B,                   // Scan data (white pixel)
            0xFF, 0xD9                          // EOI
        };

        string path = Path.Combine(Path.GetTempPath(), $"pdfflex_test_{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(path, jpeg);
        return path;
    }

    /// <summary>
    /// Minimal SVG content string for SvgBox tests.
    /// </summary>
    private const string MinimalSvg =
        """<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><rect width="10" height="10" fill="red"/></svg>""";

    /// <summary>
    /// Builds a PDF/UA-1 document with the given elements, renders it, and reopens
    /// the PDF for structure inspection. The caller must dispose the returned document.
    /// </summary>
    private static PdfDocument RenderAndReopen(params Element[] elements)
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfUA1,
            Language = "en"
        };

        foreach (var element in elements)
            doc.Add(element);

        var bytes = DocumentRenderer.RenderToBytes(doc);
        return PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Modify);
    }

    /// <summary>
    /// Finds all structure elements with the specified tag name (e.g. "P", "H1", "Table")
    /// within a structure tree rooted at <paramref name="root"/>.
    /// </summary>
    private static List<PdfDictionary> FindStructureElements(PdfDictionary root, string tagName)
    {
        var results = new List<PdfDictionary>();
        FindStructureElementsRecursive(root, tagName, results);
        return results;
    }

    /// <summary>
    /// Recursively traverses a PDF structure tree dictionary, collecting elements
    /// whose /S (structure type) key matches <paramref name="tagName"/>.
    /// PdfReference dereferencing is handled by <c>GetObject</c> / <c>GetDictionary</c>.
    /// </summary>
    private static void FindStructureElementsRecursive(
        PdfDictionary element,
        string tagName,
        List<PdfDictionary> results)
    {
        var tag = element.Elements.GetName("/S");
        if (tag == "/" + tagName || tag == tagName)
            results.Add(element);

        var kids = element.Elements.GetObject("/K");
        if (kids is PdfArray kidsArray)
        {
            for (int i = 0; i < kidsArray.Elements.Count; i++)
            {
                if (kidsArray.Elements.GetDictionary(i) is { } childDict)
                    FindStructureElementsRecursive(childDict, tagName, results);
            }
        }
        else if (kids is PdfDictionary kidsDict)
        {
            FindStructureElementsRecursive(kidsDict, tagName, results);
        }
    }

    /// <summary>
    /// Collects all distinct /S tag names found in the structure tree rooted at
    /// <paramref name="root"/>.
    /// </summary>
    private static HashSet<string> CollectAllTags(PdfDictionary root)
    {
        var tags = new HashSet<string>();
        CollectAllTagsRecursive(root, tags);
        return tags;
    }

    /// <summary>
    /// Recursively collects /S tag names from a structure tree.
    /// PdfReference dereferencing is handled by <c>GetObject</c> / <c>GetDictionary</c>.
    /// </summary>
    private static void CollectAllTagsRecursive(PdfDictionary element, HashSet<string> tags)
    {
        var tag = element.Elements.GetName("/S");
        if (!string.IsNullOrEmpty(tag))
            tags.Add(tag);

        var kids = element.Elements.GetObject("/K");
        if (kids is PdfArray kidsArray)
        {
            for (int i = 0; i < kidsArray.Elements.Count; i++)
            {
                if (kidsArray.Elements.GetDictionary(i) is { } childDict)
                    CollectAllTagsRecursive(childDict, tags);
            }
        }
        else if (kids is PdfDictionary kidsDict)
        {
            CollectAllTagsRecursive(kidsDict, tags);
        }
    }

    #endregion Helpers

    #region Group 1: Structure Tree Basics

    [Fact]
    public void PdfUA_HasStructTreeRoot()
    {
        using var pdf = RenderAndReopen(
            new TextBlock("Hello", new FontSpec("NotoSans", 12)));

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);

        Assert.NotNull(structTreeRoot);
    }

    [Fact]
    public void PdfUA_HasMarkInfo()
    {
        using var pdf = RenderAndReopen(
            new TextBlock("Hello", new FontSpec("NotoSans", 12)));

        var markInfo = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.MarkInfo);

        Assert.NotNull(markInfo);
        Assert.True(markInfo.Elements.GetBoolean("/Marked"));
    }

    [Fact]
    public void PdfUA_HasDocumentLanguage()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfUA1,
            Language = "fr-FR"
        };
        doc.Add(new TextBlock("Bonjour", new FontSpec("NotoSans", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Modify);

        var lang = pdf.Internals.Catalog.Elements.GetString(PdfCatalog.Keys.Lang);

        Assert.Equal("fr-FR", lang);
    }

    [Fact]
    public void NonPdfUA_NoStructTreeRoot()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.None
        };
        doc.Add(new TextBlock("No tagging", new FontSpec("NotoSans", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Modify);

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);

        Assert.Null(structTreeRoot);
    }

    #endregion Group 1: Structure Tree Basics

    #region Group 2: Text Tagging

    [Fact]
    public void PdfUA_TextBlock_TaggedAsParagraph()
    {
        using var pdf = RenderAndReopen(
            new TextBlock("Simple paragraph", new FontSpec("NotoSans", 12)));

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var paragraphs = FindStructureElements(structTreeRoot, "P");

        Assert.NotEmpty(paragraphs);
    }

    [Fact]
    public void PdfUA_TextBlock_HeadingLevel1_TaggedAsH1()
    {
        using var pdf = RenderAndReopen(
            new TextBlock("Main Heading", new FontSpec("NotoSans", 18), headingLevel: 1));

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var headings = FindStructureElements(structTreeRoot, "H1");

        Assert.NotEmpty(headings);
    }

    [Fact]
    public void PdfUA_TextBlock_HeadingLevel3_TaggedAsH3()
    {
        using var pdf = RenderAndReopen(
            new TextBlock("Sub-heading", new FontSpec("NotoSans", 14), headingLevel: 3));

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var headings = FindStructureElements(structTreeRoot, "H3");

        Assert.NotEmpty(headings);
    }

    [Fact]
    public void PdfUA_RichText_TaggedAsParagraphWithSpans()
    {
        var richText = new RichText(
            new Span("Bold text", new SpanStyle { FontFamily = "NotoSans", FontSize = 12, FontWeight = FontWeight.Bold }),
            new Span(" normal text", new SpanStyle { FontFamily = "NotoSans", FontSize = 12 }));

        using var pdf = RenderAndReopen(richText);

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var paragraphs = FindStructureElements(structTreeRoot, "P");

        Assert.NotEmpty(paragraphs);

        // Look for /Span children within the paragraph.
        var spans = FindStructureElements(structTreeRoot, "Span");

        Assert.NotEmpty(spans);
    }

    #endregion Group 2: Text Tagging

    #region Group 3: Table Tagging

    [Fact]
    public void PdfUA_Table_HasCorrectStructure()
    {
        var table = new Table(
            new[] { new TableColumn("Name", Length.Fr(1)), new TableColumn("Value", Length.Fr(1)) },
            new[] { new object[] { "A", "1" }, new object[] { "B", "2" } });

        using var pdf = RenderAndReopen(table);

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var tables = FindStructureElements(structTreeRoot, "Table");
        Assert.NotEmpty(tables);

        // Verify the table contains THead and TBody sub-structures.
        var allTags = CollectAllTags(structTreeRoot);
        Assert.Contains("/THead", allTags);
        Assert.Contains("/TBody", allTags);
    }

    [Fact]
    public void PdfUA_Table_HeaderCells_TaggedAsTH()
    {
        var table = new Table(
            new[] { new TableColumn("Name", Length.Fr(1)), new TableColumn("Value", Length.Fr(1)) },
            new[] { new object[] { "A", "1" } });

        using var pdf = RenderAndReopen(table);

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var headerCells = FindStructureElements(structTreeRoot, "TH");

        Assert.NotEmpty(headerCells);
    }

    [Fact]
    public void PdfUA_Table_DataCells_TaggedAsTD()
    {
        var table = new Table(
            new[] { new TableColumn("Name", Length.Fr(1)), new TableColumn("Value", Length.Fr(1)) },
            new[] { new object[] { "A", "1" } });

        using var pdf = RenderAndReopen(table);

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var dataCells = FindStructureElements(structTreeRoot, "TD");

        Assert.NotEmpty(dataCells);
    }

    #endregion Group 3: Table Tagging

    #region Group 4: Figure Tagging

    [Fact]
    public void PdfUA_ImageBox_TaggedAsFigure()
    {
        string jpegPath = CreateTempJpegFile();
        try
        {
            var image = new ImageBox(jpegPath, width: 1, height: 1) { AltText = "Test image" };

            using var pdf = RenderAndReopen(image);

            var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
            Assert.NotNull(structTreeRoot);

            var figures = FindStructureElements(structTreeRoot, "Figure");

            Assert.NotEmpty(figures);
        }
        finally
        {
            if (File.Exists(jpegPath))
                File.Delete(jpegPath);
        }
    }

    [Fact]
    public void PdfUA_SvgBox_TaggedAsFigure()
    {
        var svg = SvgBox.FromContent(MinimalSvg);
        svg.AltText = "Test SVG";

        using var pdf = RenderAndReopen(svg);

        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        var figures = FindStructureElements(structTreeRoot, "Figure");

        Assert.NotEmpty(figures);
    }

    #endregion Group 4: Figure Tagging

    #region Group 5: Validation

    [Fact]
    public void PdfUA_MissingLanguage_Throws()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfUA1
            // Language intentionally not set.
        };
        doc.Add(new TextBlock("No language", new FontSpec("NotoSans", 12)));

        Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
    }

    [Fact]
    public void PdfUA_MissingImageAltText_Throws()
    {
        string jpegPath = CreateTempJpegFile();
        try
        {
            var doc = new Document(PageSize.Letter, new EdgeInsets(50))
            {
                Conformance = PdfConformance.PdfUA1,
                Language = "en"
            };
            doc.Add(new ImageBox(jpegPath, width: 1, height: 1));
            // AltText intentionally not set.

            Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
        }
        finally
        {
            if (File.Exists(jpegPath))
                File.Delete(jpegPath);
        }
    }

    [Fact]
    public void PdfUA_MissingSvgAltText_Throws()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfUA1,
            Language = "en"
        };
        doc.Add(SvgBox.FromContent(MinimalSvg));
        // AltText intentionally not set.

        Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
    }

    [Fact]
    public void PdfUA_WithLanguageAndAltText_Succeeds()
    {
        string jpegPath = CreateTempJpegFile();
        try
        {
            var doc = new Document(PageSize.Letter, new EdgeInsets(50))
            {
                Conformance = PdfConformance.PdfUA1,
                Language = "en"
            };
            var image = new ImageBox(jpegPath, width: 1, height: 1) { AltText = "Test" };
            var svg = SvgBox.FromContent(MinimalSvg);
            svg.AltText = "Test SVG";
            doc.Add(new TextBlock("Content", new FontSpec("NotoSans", 12)));
            doc.Add(image);
            doc.Add(svg);

            var bytes = DocumentRenderer.RenderToBytes(doc);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }
        finally
        {
            if (File.Exists(jpegPath))
                File.Delete(jpegPath);
        }
    }

    #endregion Group 5: Validation

    #region Group 6: Combined Conformance

    [Fact]
    public void PdfA2a_WithPdfUA1_HasBothMetadata()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1),
            Language = "en"
        };
        doc.Add(new TextBlock("Combined conformance", new FontSpec("NotoSans", 12)));

        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Modify);

        // PDF/A requires OutputIntents.
        var outputIntents = pdf.Internals.Catalog.Elements.GetObject(PdfCatalog.Keys.OutputIntents);
        Assert.NotNull(outputIntents);

        // PDF/UA requires StructTreeRoot.
        var structTreeRoot = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.StructTreeRoot);
        Assert.NotNull(structTreeRoot);

        // PDF/UA requires MarkInfo with Marked=true.
        var markInfo = pdf.Internals.Catalog.Elements.GetDictionary(PdfCatalog.Keys.MarkInfo);
        Assert.NotNull(markInfo);
        Assert.True(markInfo.Elements.GetBoolean("/Marked"));

        // PDF/UA requires document language.
        var lang = pdf.Internals.Catalog.Elements.GetString(PdfCatalog.Keys.Lang);
        Assert.Equal("en", lang);
    }

    #endregion Group 6: Combined Conformance
}
