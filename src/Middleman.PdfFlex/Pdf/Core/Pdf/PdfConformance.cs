// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Specifies the PDF/A conformance level.
    /// </summary>
    public enum PdfALevel
    {
        /// <summary>PDF/A-1b (ISO 19005-1, Level B).</summary>
        A1b,

        /// <summary>PDF/A-1a (ISO 19005-1, Level A). Requires tagged structure.</summary>
        A1a,

        /// <summary>PDF/A-2b (ISO 19005-2, Level B).</summary>
        A2b,

        /// <summary>PDF/A-2u (ISO 19005-2, Level U).</summary>
        A2u,

        /// <summary>PDF/A-2a (ISO 19005-2, Level A). Requires tagged structure.</summary>
        A2a,

        /// <summary>PDF/A-3b (ISO 19005-3, Level B).</summary>
        A3b,

        /// <summary>PDF/A-3a (ISO 19005-3, Level A). Requires tagged structure.</summary>
        A3a
    }

    /// <summary>
    /// Specifies the PDF/UA conformance level.
    /// </summary>
    public enum PdfUALevel
    {
        /// <summary>PDF/UA-1 (ISO 14289-1).</summary>
        UA1
    }

    /// <summary>
    /// Declares the conformance profile for a PDF document. Replaces the legacy boolean
    /// <c>IsPdfA</c> approach with a proper conformance system supporting PDF/A levels,
    /// PDF/UA, and their valid combinations.
    /// <para>
    /// Use the static factory members (e.g. <see cref="PdfA1b"/>, <see cref="PdfUA1"/>)
    /// to create instances. Combine PDF/A and PDF/UA using the <see cref="With"/> method.
    /// </para>
    /// </summary>
    public sealed class PdfConformance
    {
        #region Static Factories

        /// <summary>No conformance declared. Standard PDF.</summary>
        public static PdfConformance None { get; } = new(null, null);

        /// <summary>PDF/A-1b (ISO 19005-1, Level B). Visual appearance preservation.</summary>
        public static PdfConformance PdfA1b { get; } = new(PdfALevel.A1b, null);

        /// <summary>PDF/A-1a (ISO 19005-1, Level A). Requires tagged structure for accessibility.</summary>
        public static PdfConformance PdfA1a { get; } = new(PdfALevel.A1a, null);

        /// <summary>PDF/A-2b (ISO 19005-2, Level B). Allows transparency; visual preservation.</summary>
        public static PdfConformance PdfA2b { get; } = new(PdfALevel.A2b, null);

        /// <summary>PDF/A-2u (ISO 19005-2, Level U). Requires Unicode mapping.</summary>
        public static PdfConformance PdfA2u { get; } = new(PdfALevel.A2u, null);

        /// <summary>PDF/A-2a (ISO 19005-2, Level A). Requires tagged structure for accessibility.</summary>
        public static PdfConformance PdfA2a { get; } = new(PdfALevel.A2a, null);

        /// <summary>PDF/A-3b (ISO 19005-3, Level B). Allows embedded files; visual preservation.</summary>
        public static PdfConformance PdfA3b { get; } = new(PdfALevel.A3b, null);

        /// <summary>PDF/A-3a (ISO 19005-3, Level A). Requires tagged structure for accessibility.</summary>
        public static PdfConformance PdfA3a { get; } = new(PdfALevel.A3a, null);

        /// <summary>PDF/UA-1 (ISO 14289-1). Universal accessibility.</summary>
        public static PdfConformance PdfUA1 { get; } = new(null, PdfUALevel.UA1);

        #endregion Static Factories

        #region Constructors

        /// <summary>
        /// Private constructor. Use static factories or <see cref="With"/> to create instances.
        /// </summary>
        PdfConformance(PdfALevel? pdfA, PdfUALevel? pdfUA)
        {
            PdfA = pdfA;
            PdfUA = pdfUA;
        }

        #endregion Constructors

        #region Combinator

        /// <summary>
        /// Combines this conformance with another to produce a joint profile
        /// (e.g. PDF/A-2a + PDF/UA-1).
        /// </summary>
        /// <param name="other">The conformance to combine with.</param>
        /// <returns>A new <see cref="PdfConformance"/> representing the combined profile.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="other"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Both conformances declare the same dimension (two PDF/A levels or two PDF/UA levels).
        /// </exception>
        public PdfConformance With(PdfConformance other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (IsNone)
                return other;
            if (other.IsNone)
                return this;

            // Resolve the PDF/A level.
            PdfALevel? combinedA = PdfA;
            if (other.PdfA.HasValue)
            {
                if (combinedA.HasValue)
                {
                    throw new ArgumentException(
                        "PdfFlex: Cannot combine two PDF/A conformance levels. " +
                        $"This instance declares {FormatPdfALevel(combinedA.Value)} " +
                        $"and the other declares {FormatPdfALevel(other.PdfA.Value)}.");
                }
                combinedA = other.PdfA;
            }

            // Resolve the PDF/UA level.
            PdfUALevel? combinedUA = PdfUA;
            if (other.PdfUA.HasValue)
            {
                if (combinedUA.HasValue)
                {
                    throw new ArgumentException(
                        "PdfFlex: Cannot combine two PDF/UA conformance levels. " +
                        $"This instance declares PDF/UA-{(int)combinedUA.Value + 1} " +
                        $"and the other declares PDF/UA-{(int)other.PdfUA.Value + 1}.");
                }
                combinedUA = other.PdfUA;
            }

            return new PdfConformance(combinedA, combinedUA);
        }

        #endregion Combinator

        #region Declared Conformance

        /// <summary>Gets the declared PDF/A level, or <c>null</c> if none.</summary>
        public PdfALevel? PdfA { get; }

        /// <summary>Gets the declared PDF/UA level, or <c>null</c> if none.</summary>
        public PdfUALevel? PdfUA { get; }

        /// <summary>Gets whether no conformance is declared.</summary>
        public bool IsNone => !PdfA.HasValue && !PdfUA.HasValue;

        #endregion Declared Conformance

        #region Capability Queries

        /// <summary>
        /// Gets whether the conformance profile allows transparency (alpha, blend modes).
        /// PDF/A-1 prohibits transparency; PDF/A-2+ and PDF/UA-1 allow it.
        /// When combined, the most restrictive rule wins (AND).
        /// </summary>
        public bool AllowsTransparency
        {
            get
            {
                if (IsNone)
                    return true;
                bool allowed = true;
                if (PdfA.HasValue)
                    allowed &= GetPdfAAllowsTransparency(PdfA.Value);
                if (PdfUA.HasValue)
                    allowed &= true; // PDF/UA-1 allows transparency.
                return allowed;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile allows image interpolation.
        /// PDF/A-1 prohibits it; PDF/A-2+ and PDF/UA-1 allow it.
        /// When combined, the most restrictive rule wins (AND).
        /// </summary>
        public bool AllowsImageInterpolation
        {
            get
            {
                if (IsNone)
                    return true;
                bool allowed = true;
                if (PdfA.HasValue)
                    allowed &= GetPdfAAllowsImageInterpolation(PdfA.Value);
                if (PdfUA.HasValue)
                    allowed &= true; // PDF/UA-1 allows interpolation.
                return allowed;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile allows alpha masks on images.
        /// PDF/A-1 prohibits them; PDF/A-2+ and PDF/UA-1 allow them.
        /// When combined, the most restrictive rule wins (AND).
        /// </summary>
        public bool AllowsAlphaMasks
        {
            get
            {
                if (IsNone)
                    return true;
                bool allowed = true;
                if (PdfA.HasValue)
                    allowed &= GetPdfAAllowsAlphaMasks(PdfA.Value);
                if (PdfUA.HasValue)
                    allowed &= true; // PDF/UA-1 allows alpha masks.
                return allowed;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile requires a tagged structure tree.
        /// Required by PDF/A "a" levels (1a, 2a, 3a) and PDF/UA-1.
        /// When combined, the union applies (OR).
        /// </summary>
        public bool RequiresTaggedStructure
        {
            get
            {
                if (PdfA.HasValue && GetPdfARequiresTaggedStructure(PdfA.Value))
                    return true;
                if (PdfUA.HasValue)
                    return true; // PDF/UA-1 always requires tagged structure.
                return false;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile requires all fonts to be embedded.
        /// Required by all PDF/A levels and PDF/UA-1.
        /// When combined, the union applies (OR).
        /// </summary>
        public bool RequiresFontEmbedding
        {
            get
            {
                if (PdfA.HasValue)
                    return true; // All PDF/A levels require font embedding.
                if (PdfUA.HasValue)
                    return true; // PDF/UA-1 requires font embedding (ISO 14289-1, 7.21.3).
                return false;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile requires an output intent (ICC color profile).
        /// Required by all PDF/A levels. Not required by PDF/UA-1 alone.
        /// When combined, the union applies (OR).
        /// </summary>
        public bool RequiresOutputIntent
        {
            get
            {
                if (PdfA.HasValue)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile requires XMP metadata.
        /// Required by all PDF/A levels and PDF/UA-1.
        /// When combined, the union applies (OR).
        /// </summary>
        public bool RequiresXmpMetadata
        {
            get
            {
                if (PdfA.HasValue || PdfUA.HasValue)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile requires a document language
        /// (<c>/Lang</c> entry in the catalog). Required by PDF/UA-1.
        /// When combined, the union applies (OR).
        /// </summary>
        public bool RequiresDocumentLanguage
        {
            get
            {
                if (PdfUA.HasValue)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Gets whether the conformance profile requires <c>DisplayDocTitle</c> to be
        /// <c>true</c> in the viewer preferences. Required by PDF/UA-1.
        /// When combined, the union applies (OR).
        /// </summary>
        public bool RequiresDisplayDocTitle
        {
            get
            {
                if (PdfUA.HasValue)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Gets the minimum PDF version number required by this conformance profile.
        /// Returns 0 when <see cref="IsNone"/> is <c>true</c>. PDF/A-1 requires version 14
        /// (PDF 1.4); PDF/A-2+, PDF/A-3, and PDF/UA-1 require version 17 (PDF 1.7).
        /// When combined, the highest version wins.
        /// </summary>
        public int MinimumPdfVersion
        {
            get
            {
                int version = 0;
                if (PdfA.HasValue)
                    version = Math.Max(version, GetPdfAMinimumVersion(PdfA.Value));
                if (PdfUA.HasValue)
                    version = Math.Max(version, 17);
                return version;
            }
        }

        #endregion Capability Queries

        #region XMP Identifiers

        /// <summary>
        /// Gets the <c>pdfaid:part</c> value for XMP metadata, or <c>null</c> if no PDF/A level is declared.
        /// </summary>
        public int? PdfAIdPart
        {
            get
            {
                if (!PdfA.HasValue)
                    return null;
                return PdfA.Value switch
                {
                    PdfALevel.A1b or PdfALevel.A1a => 1,
                    PdfALevel.A2b or PdfALevel.A2u or PdfALevel.A2a => 2,
                    PdfALevel.A3b or PdfALevel.A3a => 3,
                    _ => null
                };
            }
        }

        /// <summary>
        /// Gets the <c>pdfaid:conformance</c> value for XMP metadata (e.g. "A", "B", or "U"),
        /// or <c>null</c> if no PDF/A level is declared.
        /// </summary>
        public string? PdfAIdConformance
        {
            get
            {
                if (!PdfA.HasValue)
                    return null;
                return PdfA.Value switch
                {
                    PdfALevel.A1b or PdfALevel.A2b or PdfALevel.A3b => "B",
                    PdfALevel.A1a or PdfALevel.A2a or PdfALevel.A3a => "A",
                    PdfALevel.A2u => "U",
                    _ => null
                };
            }
        }

        /// <summary>
        /// Gets the <c>pdfuaid:part</c> value for XMP metadata, or <c>null</c> if no PDF/UA level is declared.
        /// </summary>
        public int? PdfUAIdPart
        {
            get
            {
                if (!PdfUA.HasValue)
                    return null;
                return PdfUA.Value switch
                {
                    PdfUALevel.UA1 => 1,
                    _ => null
                };
            }
        }

        /// <summary>
        /// Gets the output intent subtype name (e.g. "/GTS_PDFA1") for the conformance profile,
        /// or <c>null</c> if no output intent is required.
        /// </summary>
        public string? OutputIntentSubtype
        {
            get
            {
                if (!PdfA.HasValue)
                    return null;
                // All PDF/A levels use the same output intent subtype.
                return "/GTS_PDFA1";
            }
        }

        #endregion XMP Identifiers

        #region Declaration URIs

        /// <summary>
        /// Gets the PDF Declarations URIs for this conformance profile.
        /// Used in the <c>pdfd:declarations</c> XMP block.
        /// Returns an empty array when <see cref="IsNone"/> is <c>true</c>.
        /// <para>
        /// PDF/A-1 levels (A1b, A1a) are excluded. The <c>pdfd:declarations</c> namespace
        /// (defined 2019) postdates ISO 19005-1:2005 and PDF/A-1 validators flag it as an
        /// unrecognized XMP schema (clause 6.7.9). PDF/A-1 conformance is declared via
        /// <c>pdfaid:part</c> and <c>pdfaid:conformance</c> alone.
        /// </para>
        /// </summary>
        internal string[] GetDeclarationUris()
        {
            if (IsNone)
                return Array.Empty<string>();

            var uris = new List<string>(2);

            // Only emit PDF/A declaration URIs for PDF/A-2 and above.
            if (PdfA.HasValue && PdfA.Value is not (PdfALevel.A1b or PdfALevel.A1a))
            {
                string level = PdfA.Value switch
                {
                    PdfALevel.A2b => "pdfa-2b",
                    PdfALevel.A2u => "pdfa-2u",
                    PdfALevel.A2a => "pdfa-2a",
                    PdfALevel.A3b => "pdfa-3b",
                    PdfALevel.A3a => "pdfa-3a",
                    _ => throw new InvalidOperationException($"PdfFlex: Unknown PDF/A level: {PdfA.Value}.")
                };
                uris.Add($"http://pdfa.org/declarations/#{level}");
            }

            if (PdfUA.HasValue)
            {
                string level = PdfUA.Value switch
                {
                    PdfUALevel.UA1 => "pdfua-1",
                    _ => throw new InvalidOperationException($"PdfFlex: Unknown PDF/UA level: {PdfUA.Value}.")
                };
                uris.Add($"http://pdfa.org/declarations/#{level}");
            }

            return uris.ToArray();
        }

        #endregion Declaration URIs

        #region Overrides

        /// <summary>Returns a human-readable description of the conformance profile.</summary>
        public override string ToString()
        {
            if (IsNone)
                return "None";

            var parts = new List<string>(2);
            if (PdfA.HasValue)
                parts.Add(FormatPdfALevel(PdfA.Value));
            if (PdfUA.HasValue)
                parts.Add($"PDF/UA-{(int)PdfUA.Value + 1}");

            return string.Join(" + ", parts);
        }

        #endregion Overrides

        #region Private Helpers

        static string FormatPdfALevel(PdfALevel level) => level switch
        {
            PdfALevel.A1b => "PDF/A-1b",
            PdfALevel.A1a => "PDF/A-1a",
            PdfALevel.A2b => "PDF/A-2b",
            PdfALevel.A2u => "PDF/A-2u",
            PdfALevel.A2a => "PDF/A-2a",
            PdfALevel.A3b => "PDF/A-3b",
            PdfALevel.A3a => "PDF/A-3a",
            _ => level.ToString()
        };

        static bool GetPdfAAllowsTransparency(PdfALevel level) =>
            level is not (PdfALevel.A1b or PdfALevel.A1a);

        static bool GetPdfAAllowsImageInterpolation(PdfALevel level) =>
            level is not (PdfALevel.A1b or PdfALevel.A1a);

        static bool GetPdfAAllowsAlphaMasks(PdfALevel level) =>
            level is not (PdfALevel.A1b or PdfALevel.A1a);

        static bool GetPdfARequiresTaggedStructure(PdfALevel level) =>
            level is PdfALevel.A1a or PdfALevel.A2a or PdfALevel.A3a;

        static int GetPdfAMinimumVersion(PdfALevel level) => level switch
        {
            PdfALevel.A1b or PdfALevel.A1a => 14,
            _ => 17
        };

        #endregion Private Helpers
    }
}
