// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies the <see cref="PdfConformance"/> class: static factories, the With() combinator,
/// capability queries, requirement flags, XMP identifiers, declaration URIs, and ToString().
/// </summary>
public class PdfConformanceTests
{
    #region Static Factories

    [Fact]
    public void None_IsNone_ReturnsTrue()
    {
        var c = PdfConformance.None;

        Assert.True(c.IsNone);
        Assert.Null(c.PdfA);
        Assert.Null(c.PdfUA);
    }

    [Fact]
    public void PdfA1b_HasCorrectPdfALevel()
    {
        var c = PdfConformance.PdfA1b;

        Assert.Equal(PdfALevel.A1b, c.PdfA);
        Assert.Null(c.PdfUA);
        Assert.False(c.IsNone);
    }

    [Fact]
    public void PdfA1a_HasCorrectPdfALevel()
    {
        var c = PdfConformance.PdfA1a;

        Assert.Equal(PdfALevel.A1a, c.PdfA);
        Assert.Null(c.PdfUA);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA2b), PdfALevel.A2b)]
    [InlineData(nameof(PdfConformance.PdfA2u), PdfALevel.A2u)]
    [InlineData(nameof(PdfConformance.PdfA2a), PdfALevel.A2a)]
    [InlineData(nameof(PdfConformance.PdfA3b), PdfALevel.A3b)]
    [InlineData(nameof(PdfConformance.PdfA3a), PdfALevel.A3a)]
    public void PdfA_Factories_HaveCorrectLevel(string factoryName, PdfALevel expectedLevel)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expectedLevel, c.PdfA);
        Assert.Null(c.PdfUA);
        Assert.False(c.IsNone);
    }

    [Fact]
    public void PdfUA1_HasCorrectPdfUALevel()
    {
        var c = PdfConformance.PdfUA1;

        Assert.Null(c.PdfA);
        Assert.Equal(PdfUALevel.UA1, c.PdfUA);
        Assert.False(c.IsNone);
    }

    #endregion Static Factories

    #region With() Combinator

    [Fact]
    public void With_PdfA2a_And_PdfUA1_HasBoth()
    {
        var combined = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);

        Assert.Equal(PdfALevel.A2a, combined.PdfA);
        Assert.Equal(PdfUALevel.UA1, combined.PdfUA);
    }

    [Fact]
    public void With_IsCommutative()
    {
        var ab = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);
        var ba = PdfConformance.PdfUA1.With(PdfConformance.PdfA2a);

        Assert.Equal(ab.PdfA, ba.PdfA);
        Assert.Equal(ab.PdfUA, ba.PdfUA);
    }

    [Fact]
    public void With_TwoPdfALevels_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PdfConformance.PdfA1b.With(PdfConformance.PdfA2b));
    }

    [Fact]
    public void With_TwoPdfUALevels_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PdfConformance.PdfUA1.With(PdfConformance.PdfUA1));
    }

    [Fact]
    public void With_NoneAndPdfA1b_ReturnsPdfA1b()
    {
        var result = PdfConformance.None.With(PdfConformance.PdfA1b);

        Assert.Equal(PdfALevel.A1b, result.PdfA);
        Assert.Null(result.PdfUA);
    }

    [Fact]
    public void With_PdfA1bAndNone_ReturnsPdfA1b()
    {
        var result = PdfConformance.PdfA1b.With(PdfConformance.None);

        Assert.Equal(PdfALevel.A1b, result.PdfA);
        Assert.Null(result.PdfUA);
    }

    #endregion With() Combinator

    #region AllowsTransparency

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), false)]
    [InlineData(nameof(PdfConformance.PdfA1a), false)]
    [InlineData(nameof(PdfConformance.PdfA2b), true)]
    [InlineData(nameof(PdfConformance.PdfA2u), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3b), true)]
    [InlineData(nameof(PdfConformance.PdfA3a), true)]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.None), true)]
    public void AllowsTransparency_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.AllowsTransparency);
    }

    [Fact]
    public void AllowsTransparency_PdfA1bWithPdfUA1_ReturnsFalse()
    {
        // AND logic: PDF/A-1 restriction wins.
        var c = PdfConformance.PdfA1b.With(PdfConformance.PdfUA1);

        Assert.False(c.AllowsTransparency);
    }

    [Fact]
    public void AllowsTransparency_PdfA2aWithPdfUA1_ReturnsTrue()
    {
        var c = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);

        Assert.True(c.AllowsTransparency);
    }

    #endregion AllowsTransparency

    #region AllowsImageInterpolation

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), false)]
    [InlineData(nameof(PdfConformance.PdfA1a), false)]
    [InlineData(nameof(PdfConformance.PdfA2b), true)]
    [InlineData(nameof(PdfConformance.PdfA2u), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3b), true)]
    [InlineData(nameof(PdfConformance.PdfA3a), true)]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.None), true)]
    public void AllowsImageInterpolation_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.AllowsImageInterpolation);
    }

    #endregion AllowsImageInterpolation

    #region AllowsAlphaMasks

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), false)]
    [InlineData(nameof(PdfConformance.PdfA1a), false)]
    [InlineData(nameof(PdfConformance.PdfA2b), true)]
    [InlineData(nameof(PdfConformance.PdfA2u), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3b), true)]
    [InlineData(nameof(PdfConformance.PdfA3a), true)]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.None), true)]
    public void AllowsAlphaMasks_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.AllowsAlphaMasks);
    }

    #endregion AllowsAlphaMasks

    #region RequiresTaggedStructure

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1a), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3a), true)]
    [InlineData(nameof(PdfConformance.PdfA1b), false)]
    [InlineData(nameof(PdfConformance.PdfA2b), false)]
    [InlineData(nameof(PdfConformance.PdfA2u), false)]
    [InlineData(nameof(PdfConformance.PdfA3b), false)]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.None), false)]
    public void RequiresTaggedStructure_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.RequiresTaggedStructure);
    }

    [Fact]
    public void RequiresTaggedStructure_PdfA2bWithPdfUA1_ReturnsTrue()
    {
        // OR logic: PDF/UA-1 requires it even though PDF/A-2b does not.
        var c = PdfConformance.PdfA2b.With(PdfConformance.PdfUA1);

        Assert.True(c.RequiresTaggedStructure);
    }

    #endregion RequiresTaggedStructure

    #region RequiresFontEmbedding

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), true)]
    [InlineData(nameof(PdfConformance.PdfA1a), true)]
    [InlineData(nameof(PdfConformance.PdfA2b), true)]
    [InlineData(nameof(PdfConformance.PdfA2u), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3b), true)]
    [InlineData(nameof(PdfConformance.PdfA3a), true)]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.None), false)]
    public void RequiresFontEmbedding_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.RequiresFontEmbedding);
    }

    #endregion RequiresFontEmbedding

    #region RequiresOutputIntent

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), true)]
    [InlineData(nameof(PdfConformance.PdfA1a), true)]
    [InlineData(nameof(PdfConformance.PdfA2b), true)]
    [InlineData(nameof(PdfConformance.PdfA2u), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3b), true)]
    [InlineData(nameof(PdfConformance.PdfA3a), true)]
    [InlineData(nameof(PdfConformance.PdfUA1), false)]
    [InlineData(nameof(PdfConformance.None), false)]
    public void RequiresOutputIntent_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.RequiresOutputIntent);
    }

    #endregion RequiresOutputIntent

    #region RequiresXmpMetadata

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), true)]
    [InlineData(nameof(PdfConformance.PdfA2a), true)]
    [InlineData(nameof(PdfConformance.PdfA3b), true)]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.None), false)]
    public void RequiresXmpMetadata_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.RequiresXmpMetadata);
    }

    #endregion RequiresXmpMetadata

    #region RequiresDocumentLanguage

    [Theory]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.PdfA1b), false)]
    [InlineData(nameof(PdfConformance.PdfA2a), false)]
    [InlineData(nameof(PdfConformance.None), false)]
    public void RequiresDocumentLanguage_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.RequiresDocumentLanguage);
    }

    [Fact]
    public void RequiresDocumentLanguage_PdfA2aWithPdfUA1_ReturnsTrue()
    {
        var c = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);

        Assert.True(c.RequiresDocumentLanguage);
    }

    #endregion RequiresDocumentLanguage

    #region RequiresDisplayDocTitle

    [Theory]
    [InlineData(nameof(PdfConformance.PdfUA1), true)]
    [InlineData(nameof(PdfConformance.PdfA1b), false)]
    [InlineData(nameof(PdfConformance.PdfA2a), false)]
    [InlineData(nameof(PdfConformance.None), false)]
    public void RequiresDisplayDocTitle_ReturnsExpected(string factoryName, bool expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.RequiresDisplayDocTitle);
    }

    [Fact]
    public void RequiresDisplayDocTitle_PdfA2bWithPdfUA1_ReturnsTrue()
    {
        var c = PdfConformance.PdfA2b.With(PdfConformance.PdfUA1);

        Assert.True(c.RequiresDisplayDocTitle);
    }

    #endregion RequiresDisplayDocTitle

    #region MinimumPdfVersion

    [Theory]
    [InlineData(nameof(PdfConformance.None), 0)]
    [InlineData(nameof(PdfConformance.PdfA1b), 14)]
    [InlineData(nameof(PdfConformance.PdfA1a), 14)]
    [InlineData(nameof(PdfConformance.PdfA2b), 17)]
    [InlineData(nameof(PdfConformance.PdfA2u), 17)]
    [InlineData(nameof(PdfConformance.PdfA2a), 17)]
    [InlineData(nameof(PdfConformance.PdfA3b), 17)]
    [InlineData(nameof(PdfConformance.PdfA3a), 17)]
    [InlineData(nameof(PdfConformance.PdfUA1), 17)]
    public void MinimumPdfVersion_ReturnsExpected(string factoryName, int expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.MinimumPdfVersion);
    }

    [Fact]
    public void MinimumPdfVersion_PdfA1bWithPdfUA1_ReturnsMax()
    {
        // PDF/A-1b = 14, PDF/UA-1 = 17. Max wins.
        var c = PdfConformance.PdfA1b.With(PdfConformance.PdfUA1);

        Assert.Equal(17, c.MinimumPdfVersion);
    }

    #endregion MinimumPdfVersion

    #region XMP Identifiers

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), 1)]
    [InlineData(nameof(PdfConformance.PdfA1a), 1)]
    [InlineData(nameof(PdfConformance.PdfA2b), 2)]
    [InlineData(nameof(PdfConformance.PdfA2u), 2)]
    [InlineData(nameof(PdfConformance.PdfA2a), 2)]
    [InlineData(nameof(PdfConformance.PdfA3b), 3)]
    [InlineData(nameof(PdfConformance.PdfA3a), 3)]
    public void PdfAIdPart_PdfALevels_ReturnsCorrectPart(string factoryName, int expectedPart)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expectedPart, c.PdfAIdPart);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.None))]
    [InlineData(nameof(PdfConformance.PdfUA1))]
    public void PdfAIdPart_NoPdfA_ReturnsNull(string factoryName)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Null(c.PdfAIdPart);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), "B")]
    [InlineData(nameof(PdfConformance.PdfA2b), "B")]
    [InlineData(nameof(PdfConformance.PdfA3b), "B")]
    [InlineData(nameof(PdfConformance.PdfA2u), "U")]
    [InlineData(nameof(PdfConformance.PdfA1a), "A")]
    [InlineData(nameof(PdfConformance.PdfA2a), "A")]
    [InlineData(nameof(PdfConformance.PdfA3a), "A")]
    public void PdfAIdConformance_PdfALevels_ReturnsCorrectLetter(string factoryName, string expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.PdfAIdConformance);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.None))]
    [InlineData(nameof(PdfConformance.PdfUA1))]
    public void PdfAIdConformance_NoPdfA_ReturnsNull(string factoryName)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Null(c.PdfAIdConformance);
    }

    [Fact]
    public void PdfUAIdPart_PdfUA1_Returns1()
    {
        Assert.Equal(1, PdfConformance.PdfUA1.PdfUAIdPart);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.None))]
    [InlineData(nameof(PdfConformance.PdfA1b))]
    [InlineData(nameof(PdfConformance.PdfA2a))]
    public void PdfUAIdPart_NoPdfUA_ReturnsNull(string factoryName)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Null(c.PdfUAIdPart);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), "/GTS_PDFA1")]
    [InlineData(nameof(PdfConformance.PdfA2a), "/GTS_PDFA1")]
    [InlineData(nameof(PdfConformance.PdfA3b), "/GTS_PDFA1")]
    public void OutputIntentSubtype_PdfA_ReturnsGtsPdfA1(string factoryName, string expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.OutputIntentSubtype);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.None))]
    [InlineData(nameof(PdfConformance.PdfUA1))]
    public void OutputIntentSubtype_NoPdfA_ReturnsNull(string factoryName)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Null(c.OutputIntentSubtype);
    }

    #endregion XMP Identifiers

    #region GetDeclarationUris

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b))]
    [InlineData(nameof(PdfConformance.PdfA1a))]
    public void GetDeclarationUris_PdfA1_ReturnsEmpty(string factoryName)
    {
        // PDF/A-1 must not emit pdfd:declarations (postdates ISO 19005-1:2005).
        var c = GetConformanceByName(factoryName);

        Assert.Empty(c.GetDeclarationUris());
    }

    [Fact]
    public void GetDeclarationUris_PdfA2b_ContainsCorrectUri()
    {
        var uris = PdfConformance.PdfA2b.GetDeclarationUris();

        Assert.Single(uris);
        Assert.Contains("http://pdfa.org/declarations/#pdfa-2b", uris);
    }

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA2u), "pdfa-2u")]
    [InlineData(nameof(PdfConformance.PdfA2a), "pdfa-2a")]
    [InlineData(nameof(PdfConformance.PdfA3b), "pdfa-3b")]
    [InlineData(nameof(PdfConformance.PdfA3a), "pdfa-3a")]
    public void GetDeclarationUris_PdfA2Plus_ContainsExpectedFragment(string factoryName, string expectedFragment)
    {
        var c = GetConformanceByName(factoryName);
        var uris = c.GetDeclarationUris();

        Assert.Single(uris);
        Assert.Contains($"http://pdfa.org/declarations/#{expectedFragment}", uris);
    }

    [Fact]
    public void GetDeclarationUris_PdfUA1_ContainsPdfUAUri()
    {
        var uris = PdfConformance.PdfUA1.GetDeclarationUris();

        Assert.Single(uris);
        Assert.Contains("http://pdfa.org/declarations/#pdfua-1", uris);
    }

    [Fact]
    public void GetDeclarationUris_PdfA2aWithPdfUA1_ContainsBothUris()
    {
        var c = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);
        var uris = c.GetDeclarationUris();

        Assert.Equal(2, uris.Length);
        Assert.Contains("http://pdfa.org/declarations/#pdfa-2a", uris);
        Assert.Contains("http://pdfa.org/declarations/#pdfua-1", uris);
    }

    [Fact]
    public void GetDeclarationUris_None_ReturnsEmpty()
    {
        Assert.Empty(PdfConformance.None.GetDeclarationUris());
    }

    #endregion GetDeclarationUris

    #region ToString

    [Fact]
    public void ToString_None_ReturnsNone()
    {
        Assert.Equal("None", PdfConformance.None.ToString());
    }

    [Theory]
    [InlineData(nameof(PdfConformance.PdfA1b), "PDF/A-1b")]
    [InlineData(nameof(PdfConformance.PdfA1a), "PDF/A-1a")]
    [InlineData(nameof(PdfConformance.PdfA2b), "PDF/A-2b")]
    [InlineData(nameof(PdfConformance.PdfA2u), "PDF/A-2u")]
    [InlineData(nameof(PdfConformance.PdfA2a), "PDF/A-2a")]
    [InlineData(nameof(PdfConformance.PdfA3b), "PDF/A-3b")]
    [InlineData(nameof(PdfConformance.PdfA3a), "PDF/A-3a")]
    [InlineData(nameof(PdfConformance.PdfUA1), "PDF/UA-1")]
    public void ToString_SingleProfile_ReturnsFormattedName(string factoryName, string expected)
    {
        var c = GetConformanceByName(factoryName);

        Assert.Equal(expected, c.ToString());
    }

    [Fact]
    public void ToString_CombinedProfile_ReturnsBothJoined()
    {
        var c = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);

        Assert.Equal("PDF/A-2a + PDF/UA-1", c.ToString());
    }

    #endregion ToString

    #region Helpers

    /// <summary>
    /// Resolves a <see cref="PdfConformance"/> static factory by name.
    /// </summary>
    private static PdfConformance GetConformanceByName(string name)
    {
        return name switch
        {
            nameof(PdfConformance.None) => PdfConformance.None,
            nameof(PdfConformance.PdfA1b) => PdfConformance.PdfA1b,
            nameof(PdfConformance.PdfA1a) => PdfConformance.PdfA1a,
            nameof(PdfConformance.PdfA2b) => PdfConformance.PdfA2b,
            nameof(PdfConformance.PdfA2u) => PdfConformance.PdfA2u,
            nameof(PdfConformance.PdfA2a) => PdfConformance.PdfA2a,
            nameof(PdfConformance.PdfA3b) => PdfConformance.PdfA3b,
            nameof(PdfConformance.PdfA3a) => PdfConformance.PdfA3a,
            nameof(PdfConformance.PdfUA1) => PdfConformance.PdfUA1,
            _ => throw new ArgumentException($"Unknown conformance factory: {name}")
        };
    }

    #endregion Helpers
}
