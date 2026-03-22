// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Text;
using System.Xml.Linq;
using Middleman.PdfFlex.Pdf;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies XMP metadata generation for different conformance profiles. Tests exercise the
/// full save pipeline via <see cref="PdfDocument"/> to ensure the XMP block in the output
/// PDF contains the correct pdfaid, pdfuaid, and pdfd:declarations elements.
/// </summary>
public class XmpMetadataTests
{
    #region PDF/A-1b XMP

    [Fact]
    public void PdfA1b_XmpContainsPdfAIdPart1()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfA1b);

        Assert.Contains("<pdfaid:part>1</pdfaid:part>", xmp);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", xmp);
    }

    [Fact]
    public void PdfA1b_XmpDoesNotContainDeclarations()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfA1b);

        Assert.DoesNotContain("pdfd:declarations", xmp);
    }

    #endregion PDF/A-1b XMP

    #region PDF/A-2a XMP

    [Fact]
    public void PdfA2a_XmpContainsPdfAIdPart2()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfA2a);

        Assert.Contains("<pdfaid:part>2</pdfaid:part>", xmp);
        Assert.Contains("<pdfaid:conformance>A</pdfaid:conformance>", xmp);
    }

    [Fact]
    public void PdfA2a_XmpContainsDeclarationUri()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfA2a);

        Assert.Contains("pdfd:declarations", xmp);
        Assert.Contains("http://pdfa.org/declarations/#pdfa-2a", xmp);
    }

    #endregion PDF/A-2a XMP

    #region PDF/UA-1 XMP

    [Fact]
    public void PdfUA1_XmpContainsPdfUAIdPart1()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfUA1);

        Assert.Contains("<pdfuaid:part>1</pdfuaid:part>", xmp);
    }

    [Fact]
    public void PdfUA1_XmpDoesNotContainPdfAId()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfUA1);

        Assert.DoesNotContain("pdfaid:part", xmp);
        Assert.DoesNotContain("pdfaid:conformance", xmp);
    }

    [Fact]
    public void PdfUA1_XmpContainsDeclarationUri()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfUA1);

        Assert.Contains("pdfd:declarations", xmp);
        Assert.Contains("http://pdfa.org/declarations/#pdfua-1", xmp);
    }

    #endregion PDF/UA-1 XMP

    #region Combined PDF/A-2a + PDF/UA-1 XMP

    [Fact]
    public void PdfA2aWithPdfUA1_XmpContainsBothIds()
    {
        var conformance = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);
        var xmp = SaveAndExtractXmp(conformance);

        Assert.Contains("<pdfaid:part>2</pdfaid:part>", xmp);
        Assert.Contains("<pdfaid:conformance>A</pdfaid:conformance>", xmp);
        Assert.Contains("<pdfuaid:part>1</pdfuaid:part>", xmp);
    }

    [Fact]
    public void PdfA2aWithPdfUA1_XmpContainsBothDeclarationUris()
    {
        var conformance = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);
        var xmp = SaveAndExtractXmp(conformance);

        Assert.Contains("http://pdfa.org/declarations/#pdfa-2a", xmp);
        Assert.Contains("http://pdfa.org/declarations/#pdfua-1", xmp);
    }

    #endregion Combined PDF/A-2a + PDF/UA-1 XMP

    #region None Conformance XMP

    [Fact]
    public void None_XmpDoesNotContainPdfAIdOrPdfUAIdOrDeclarations()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.None);

        Assert.DoesNotContain("pdfaid:part", xmp);
        Assert.DoesNotContain("pdfuaid:part", xmp);
        Assert.DoesNotContain("pdfd:declarations", xmp);
    }

    #endregion None Conformance XMP

    #region XMP Validity

    [Fact]
    public void XmpOutput_IsValidXml()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfA1b);

        // XDocument.Parse will throw if the XML is malformed.
        var doc = XDocument.Parse(xmp);
        Assert.NotNull(doc.Root);
    }

    #endregion XMP Validity

    #region Document Info Propagation

    [Fact]
    public void DocumentInfo_TitleAndAuthor_PropagateToXmp()
    {
        var xmp = SaveAndExtractXmp(PdfConformance.PdfA1b, title: "Test Report", author: "Jane Doe");

        Assert.Contains("Test Report", xmp);
        Assert.Contains("Jane Doe", xmp);
    }

    #endregion Document Info Propagation

    #region Helpers

    /// <summary>
    /// Creates a PdfDocument directly with the given conformance, saves it to a MemoryStream,
    /// and extracts the XMP metadata string from the raw PDF bytes.
    /// </summary>
    private static string SaveAndExtractXmp(PdfConformance conformance, string? title = null, string? author = null)
    {
        using var ms = new MemoryStream();
        using (var pdfDoc = new PdfDocument())
        {
            pdfDoc.Conformance = conformance;
            if (title != null)
                pdfDoc.Info.Title = title;
            if (author != null)
                pdfDoc.Info.Author = author;
            pdfDoc.AddPage();
            pdfDoc.Save(ms, closeStream: false);
        }

        return ExtractXmpFromPdfBytes(ms.ToArray());
    }

    /// <summary>
    /// Extracts the XMP metadata packet from raw PDF bytes by finding the xpacket begin/end markers.
    /// </summary>
    private static string ExtractXmpFromPdfBytes(byte[] pdfBytes)
    {
        // Use Latin1 to avoid mangling binary data while still finding ASCII markers.
        var pdfText = Encoding.Latin1.GetString(pdfBytes);

        const string beginMarker = "<?xpacket begin=";
        const string endMarker = "<?xpacket end=";

        int beginIndex = pdfText.IndexOf(beginMarker, StringComparison.Ordinal);
        Assert.True(beginIndex >= 0, "XMP xpacket begin marker not found in PDF.");

        int endIndex = pdfText.IndexOf(endMarker, beginIndex, StringComparison.Ordinal);
        Assert.True(endIndex >= 0, "XMP xpacket end marker not found in PDF.");

        int endOfEndMarker = pdfText.IndexOf("?>", endIndex, StringComparison.Ordinal);
        Assert.True(endOfEndMarker >= 0, "XMP xpacket end closing not found in PDF.");

        // Extract and re-decode as UTF-8 from the original bytes for correct XML parsing.
        // The xpacket markers are ASCII so the Latin1 indices match the byte offsets.
        return Encoding.UTF8.GetString(pdfBytes, beginIndex, endOfEndMarker + 2 - beginIndex);
    }

    #endregion Helpers
}
