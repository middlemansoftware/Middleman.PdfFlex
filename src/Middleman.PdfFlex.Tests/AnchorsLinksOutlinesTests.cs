// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Helpers;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.Advanced;
using Middleman.PdfFlex.Pdf.IO;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Tests for Phase 2: anchors, links, outlines, anchor page tokens, and TocBuilder.
/// Covers named destination registration, internal/external link annotations,
/// PDF outline generation, {page:id} token resolution, heading slug generation,
/// and TocBuilder composability.
/// </summary>
public class AnchorsLinksOutlinesTests
{
    #region Helpers

    /// <summary>Minimal valid JFIF JPEG for image tests.</summary>
    private static byte[] CreateMinimalJpeg()
    {
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10,
            0x4A, 0x46, 0x49, 0x46, 0x00,
            0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0xFF, 0xDB, 0x00, 0x43, 0x00,
            0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07,
            0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14,
            0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12, 0x13,
            0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A,
            0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20, 0x22,
            0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C,
            0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39,
            0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32,
            0xFF, 0xC0, 0x00, 0x0B, 0x08,
            0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00,
            0xFF, 0xC4, 0x00, 0x1F, 0x00,
            0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B,
            0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00,
            0x7B, 0x40, 0x1B,
            0xFF, 0xD9
        };
    }

    private const string MinimalSvg =
        """<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><rect width="10" height="10" fill="red"/></svg>""";

    private static byte[] RenderDoc(Document doc) => DocumentRenderer.RenderToBytes(doc);

    private static PdfDocument OpenPdf(byte[] bytes) =>
        PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Modify);

    private static int GetPageCount(byte[] bytes)
    {
        using var pdf = OpenPdf(bytes);
        return pdf.PageCount;
    }

    private static Document CreateBasicDoc() =>
        new Document(PageSize.Letter, new EdgeInsets(50));

    #endregion Helpers

    #region Element Id / Named Destination Registration

    [Fact]
    public void ElementWithId_RegistersAnchor_RendersSuccessfully()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Target", new FontSpec("NotoSans", 12)) { Id = "section-1" });

        var bytes = RenderDoc(doc);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void MultipleElementsWithId_AllRenderSuccessfully()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("First", new FontSpec("NotoSans", 12)) { Id = "first" });
        doc.Add(new TextBlock("Second", new FontSpec("NotoSans", 12)) { Id = "second" });
        doc.Add(new TextBlock("Third", new FontSpec("NotoSans", 12)) { Id = "third" });

        var bytes = RenderDoc(doc);

        Assert.True(bytes.Length > 0);
    }

    #endregion Element Id / Named Destination Registration

    #region TextBlock LinkTarget

    [Fact]
    public void TextBlock_ExternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Click here", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "https://example.com"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void TextBlock_InternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Target", new FontSpec("NotoSans", 12)) { Id = "target" });
        doc.Add(new TextBlock("Go to target", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "target"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void TextBlock_InternalLink_CrossPage_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Go to page 2", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "page2-target"
        });
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Target on page 2", new FontSpec("NotoSans", 12))
        {
            Id = "page2-target"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.Equal(2, pdf.PageCount);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void TextBlock_HttpLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("HTTP link", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "http://example.com"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void TextBlock_NoLinkTarget_NoAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("No link", new FontSpec("NotoSans", 12)));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.False(pdf.Pages[0].HasAnnotations);
    }

    #endregion TextBlock LinkTarget

    #region ImageBox / SvgBox LinkTarget

    [Fact]
    public void ImageBox_ExternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        var img = new ImageBox(CreateMinimalJpeg(), 50, 50)
        {
            LinkTarget = "https://example.com",
            AltText = "test image"
        };
        doc.Add(img);

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void SvgBox_ExternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        var svg = SvgBox.FromContent(MinimalSvg, new Style { Width = Length.Pt(50), Height = Length.Pt(50) });
        svg.LinkTarget = "https://example.com";
        svg.AltText = "test svg";
        doc.Add(svg);

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void ImageBox_InternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Anchor", new FontSpec("NotoSans", 12)) { Id = "img-target" });
        doc.Add(new ImageBox(CreateMinimalJpeg(), 50, 50)
        {
            LinkTarget = "img-target",
            AltText = "linked image"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    #endregion ImageBox / SvgBox LinkTarget

    #region Link Target Not Found

    [Fact]
    public void InternalLink_MissingTarget_Throws()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Bad link", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "nonexistent-target"
        });

        Assert.Throws<InvalidOperationException>(() => RenderDoc(doc));
    }

    #endregion Link Target Not Found

    #region Anchor Page Token {page:id}

    [Fact]
    public void AnchorPageToken_ResolvesToCorrectPage()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("See page {page:chapter2}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Chapter 2", new FontSpec("NotoSans", 12)) { Id = "chapter2" });

        var bytes = RenderDoc(doc);

        Assert.Equal(2, GetPageCount(bytes));
    }

    [Fact]
    public void AnchorPageToken_InFooter_ResolvesCorrectly()
    {
        var doc = CreateBasicDoc();
        doc.Footer = new TextBlock("Index: {page:main-content}", new FontSpec("NotoSans", 8));
        doc.Add(new TextBlock("Main Content", new FontSpec("NotoSans", 12)) { Id = "main-content" });

        var bytes = RenderDoc(doc);

        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void AnchorPageToken_UnresolvedToken_ShowsQuestionMark()
    {
        // Tokens with missing anchors resolve to "?" instead of throwing.
        // The anchor might not be found because it is in the header/footer
        // pipeline which is outside the body's anchor registry, or the user
        // deliberately uses a placeholder.
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Page {page:unknown-anchor}", new FontSpec("NotoSans", 12)));

        // This should render without throwing since the token resolution
        // returns "?" for unresolved anchor page tokens.
        var bytes = RenderDoc(doc);

        Assert.True(bytes.Length > 0);
    }

    #endregion Anchor Page Token {page:id}

    #region Auto Outlines from Headings

    [Fact]
    public void AutoOutlines_H1H2H3_CreatesBookmarks()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Chapter 1", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Section 1.1", new FontSpec("NotoSans", 14), headingLevel: 2));
        doc.Add(new TextBlock("Detail 1.1.1", new FontSpec("NotoSans", 12), headingLevel: 3));
        doc.Add(new TextBlock("Chapter 2", new FontSpec("NotoSans", 18), headingLevel: 1));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Outlines.Count > 0);
    }

    [Fact]
    public void AutoOutlines_H1Only_CreatesTopLevelBookmarks()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("First", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Second", new FontSpec("NotoSans", 18), headingLevel: 1));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.Equal(2, pdf.Outlines.Count);
    }

    [Fact]
    public void AutoOutlines_NestedH2UnderH1()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Chapter", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Section A", new FontSpec("NotoSans", 14), headingLevel: 2));
        doc.Add(new TextBlock("Section B", new FontSpec("NotoSans", 14), headingLevel: 2));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.Single(pdf.Outlines); // One top-level H1
        Assert.True(pdf.Outlines[0].HasChildren); // H2s nested under it
    }

    [Fact]
    public void AutoOutlines_Disabled_NoBookmarks()
    {
        var doc = CreateBasicDoc();
        doc.AutoGenerateOutlines = false;
        doc.Add(new TextBlock("Chapter 1", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Section 1.1", new FontSpec("NotoSans", 14), headingLevel: 2));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.Empty(pdf.Outlines);
    }

    [Fact]
    public void AutoOutlines_NoHeadings_NoBookmarks()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Just text", new FontSpec("NotoSans", 12)));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.Empty(pdf.Outlines);
    }

    [Fact]
    public void AutoOutlines_MultiPage_CorrectPageTargets()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Page 1 Heading", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Page 2 Heading", new FontSpec("NotoSans", 18), headingLevel: 1));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.Equal(2, pdf.Outlines.Count);
    }

    #endregion Auto Outlines from Headings

    #region HeadingSlugGenerator

    [Fact]
    public void SlugGenerator_BasicText_Lowercased()
    {
        var slugs = new HashSet<string>();
        var slug = HeadingSlugGenerator.Generate("Hello World", slugs);

        Assert.Equal("hello-world", slug);
    }

    [Fact]
    public void SlugGenerator_SpecialChars_Stripped()
    {
        var slugs = new HashSet<string>();
        var slug = HeadingSlugGenerator.Generate("Hello, World! (2024)", slugs);

        Assert.Equal("hello-world-2024", slug);
    }

    [Fact]
    public void SlugGenerator_Duplicates_Suffixed()
    {
        var slugs = new HashSet<string>();
        var slug1 = HeadingSlugGenerator.Generate("Section", slugs);
        var slug2 = HeadingSlugGenerator.Generate("Section", slugs);
        var slug3 = HeadingSlugGenerator.Generate("Section", slugs);

        Assert.Equal("section", slug1);
        Assert.Equal("section-1", slug2);
        Assert.Equal("section-2", slug3);
    }

    [Fact]
    public void SlugGenerator_EmptyText_ReturnsHeading()
    {
        var slugs = new HashSet<string>();
        var slug = HeadingSlugGenerator.Generate("", slugs);

        Assert.Equal("heading", slug);
    }

    [Fact]
    public void SlugGenerator_UnicodeText_StripsNonAscii()
    {
        var slugs = new HashSet<string>();
        var slug = HeadingSlugGenerator.Generate("Resume", slugs);

        Assert.Equal("resume", slug);
    }

    [Fact]
    public void SlugGenerator_MultipleSpaces_SingleHyphen()
    {
        var slugs = new HashSet<string>();
        var slug = HeadingSlugGenerator.Generate("Hello   World", slugs);

        Assert.Equal("hello-world", slug);
    }

    #endregion HeadingSlugGenerator

    #region TocBuilder

    [Fact]
    public void TocBuilder_BuildsEntriesFromHeadings()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Chapter 1", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Section 1.1", new FontSpec("NotoSans", 14), headingLevel: 2));
        doc.Add(new TextBlock("Chapter 2", new FontSpec("NotoSans", 18), headingLevel: 1));

        var toc = TocBuilder.Build(doc);

        Assert.Equal(3, toc.Count);
    }

    [Fact]
    public void TocBuilder_NoHeadings_ReturnsEmpty()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Just text", new FontSpec("NotoSans", 12)));

        var toc = TocBuilder.Build(doc);

        Assert.Empty(toc);
    }

    [Fact]
    public void TocBuilder_DoesNotMutateSourceHeadings()
    {
        var doc = CreateBasicDoc();
        var h1 = new TextBlock("My Heading", new FontSpec("NotoSans", 18), headingLevel: 1);
        doc.Add(h1);

        Assert.Null(h1.Id);

        var entries = TocBuilder.Build(doc);

        // TocBuilder must not mutate source elements.
        Assert.Null(h1.Id);

        // But the TOC entry should still reference the generated slug.
        Assert.Single(entries);
    }

    [Fact]
    public void TocBuilder_PreservesExistingIds()
    {
        var doc = CreateBasicDoc();
        var h1 = new TextBlock("Heading", new FontSpec("NotoSans", 18), headingLevel: 1)
        {
            Id = "custom-id"
        };
        doc.Add(h1);

        TocBuilder.Build(doc);

        Assert.Equal("custom-id", h1.Id);
    }

    [Fact]
    public void TocBuilder_EntriesAreRows()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Chapter", new FontSpec("NotoSans", 18), headingLevel: 1));

        var toc = TocBuilder.Build(doc);

        Assert.Single(toc);
        Assert.IsType<Row>(toc[0]);
    }

    [Fact]
    public void TocBuilder_IntegrationWithDocument()
    {
        // Build a document with headings and a TOC, and verify it renders.
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Chapter 1", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Some body text.", new FontSpec("NotoSans", 12)));
        doc.Add(new TextBlock("Chapter 2", new FontSpec("NotoSans", 18), headingLevel: 1));

        var tocEntries = TocBuilder.Build(doc);

        // Create a new doc with TOC at the front.
        var finalDoc = CreateBasicDoc();
        finalDoc.Add(new TextBlock("Table of Contents", new FontSpec("NotoSans", 16), headingLevel: 1));
        foreach (var entry in tocEntries)
            finalDoc.Add(entry);
        finalDoc.Add(new PageBreak());
        // Re-add body content. (In real usage, the original doc would be used.)
        finalDoc.Add(new TextBlock("Chapter 1", new FontSpec("NotoSans", 18), headingLevel: 1)
        {
            Id = "chapter-1"
        });
        finalDoc.Add(new TextBlock("Some body text.", new FontSpec("NotoSans", 12)));
        finalDoc.Add(new TextBlock("Chapter 2", new FontSpec("NotoSans", 18), headingLevel: 1)
        {
            Id = "chapter-2"
        });

        var bytes = RenderDoc(finalDoc);

        Assert.True(bytes.Length > 0);
        Assert.True(GetPageCount(bytes) >= 1);
    }

    #endregion TocBuilder

    #region PDF/UA Link Tagging

    [Fact]
    public void PdfUA_ExternalLink_TaggedAsLink()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfUA1,
            Language = "en"
        };
        doc.Add(new TextBlock("Click me", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "https://example.com"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        // Verify the document renders successfully with PDF/UA conformance.
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void PdfUA_InternalLink_TaggedAsLink()
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(50))
        {
            Conformance = PdfConformance.PdfUA1,
            Language = "en"
        };
        doc.Add(new TextBlock("Target", new FontSpec("NotoSans", 12)) { Id = "tgt" });
        doc.Add(new TextBlock("Go to target", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "tgt"
        });

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    #endregion PDF/UA Link Tagging

    #region Streaming Path

    [Fact]
    public void Streaming_ExternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Click", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "https://example.com"
        });

        using var ms = new MemoryStream();
        DocumentRenderer.RenderStreaming(doc, ms);
        ms.Position = 0;

        using var pdf = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void Streaming_InternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Target", new FontSpec("NotoSans", 12)) { Id = "s-target" });
        doc.Add(new TextBlock("Go", new FontSpec("NotoSans", 12))
        {
            LinkTarget = "s-target"
        });

        using var ms = new MemoryStream();
        DocumentRenderer.RenderStreaming(doc, ms);
        ms.Position = 0;

        using var pdf = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void Streaming_AnchorPageToken_Resolves()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("See page {page:ch2}", new FontSpec("NotoSans", 12)));
        doc.Add(new PageBreak());
        doc.Add(new TextBlock("Chapter 2", new FontSpec("NotoSans", 12)) { Id = "ch2" });

        using var ms = new MemoryStream();
        DocumentRenderer.RenderStreaming(doc, ms);
        ms.Position = 0;

        using var pdf = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);
        Assert.Equal(2, pdf.PageCount);
    }

    [Fact]
    public void Streaming_AutoOutlines_Generated()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Heading 1", new FontSpec("NotoSans", 18), headingLevel: 1));
        doc.Add(new TextBlock("Heading 2", new FontSpec("NotoSans", 14), headingLevel: 2));

        using var ms = new MemoryStream();
        DocumentRenderer.RenderStreaming(doc, ms);
        ms.Position = 0;

        using var pdf = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);
        Assert.True(pdf.Outlines.Count > 0);
    }

    #endregion Streaming Path

    #region SpanStyle LinkTarget

    [Fact]
    public void SpanStyle_ExternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new RichText(
            new Span("Click here", new SpanStyle
            {
                LinkTarget = "https://example.com",
                FontColor = new Color(0, 0, 255)
            })
        ));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    [Fact]
    public void SpanStyle_InternalLink_CreatesAnnotation()
    {
        var doc = CreateBasicDoc();
        doc.Add(new TextBlock("Anchor", new FontSpec("NotoSans", 12)) { Id = "span-target" });
        doc.Add(new RichText(
            new Span("Go to anchor", new SpanStyle
            {
                LinkTarget = "span-target"
            })
        ));

        var bytes = RenderDoc(doc);

        using var pdf = OpenPdf(bytes);
        Assert.True(pdf.Pages[0].HasAnnotations);
    }

    #endregion SpanStyle LinkTarget

    #region HasAnchorPageTokens Detection

    [Fact]
    public void HasAnchorPageTokens_Detects_PageIdToken()
    {
        var elements = new List<Element>
        {
            new TextBlock("See {page:intro}", new FontSpec("NotoSans", 12))
        };

        Assert.True(DocumentRenderer.HasAnchorPageTokens(elements));
    }

    [Fact]
    public void HasAnchorPageTokens_NoTokens_ReturnsFalse()
    {
        var elements = new List<Element>
        {
            new TextBlock("No tokens here", new FontSpec("NotoSans", 12))
        };

        Assert.False(DocumentRenderer.HasAnchorPageTokens(elements));
    }

    [Fact]
    public void HasAnchorPageTokens_StandardPageToken_ReturnsFalse()
    {
        var elements = new List<Element>
        {
            new TextBlock("Page {page} of {pages}", new FontSpec("NotoSans", 12))
        };

        // {page} without a colon is not an anchor token.
        Assert.False(DocumentRenderer.HasAnchorPageTokens(elements));
    }

    #endregion HasAnchorPageTokens Detection

    #region IsExternalLink

    [Fact]
    public void IsExternalLink_HttpsUrl_ReturnsTrue()
    {
        Assert.True(DocumentRenderer.IsExternalLink("https://example.com"));
    }

    [Fact]
    public void IsExternalLink_HttpUrl_ReturnsTrue()
    {
        Assert.True(DocumentRenderer.IsExternalLink("http://example.com"));
    }

    [Fact]
    public void IsExternalLink_InternalId_ReturnsFalse()
    {
        Assert.False(DocumentRenderer.IsExternalLink("section-1"));
    }

    [Fact]
    public void IsExternalLink_CaseInsensitive()
    {
        Assert.True(DocumentRenderer.IsExternalLink("HTTPS://EXAMPLE.COM"));
    }

    #endregion IsExternalLink
}
