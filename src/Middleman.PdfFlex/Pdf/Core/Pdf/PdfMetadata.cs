// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Text;
using Middleman.PdfFlex.Pdf.Internal;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Represents an XML Metadata stream.
    /// </summary>
    public sealed class PdfMetadata : PdfDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfMetadata"/> class.
        /// </summary>
        public PdfMetadata()
        {
            Elements.SetName(Keys.Type, "/Metadata");
            Elements.SetName(Keys.Subtype, "/XML");
            SetupStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfMetadata"/> class.
        /// </summary>
        /// <param name="document">The document that owns this object.</param>
        public PdfMetadata(PdfDocument document)
            : base(document)
        {
            document.Internals.AddObject(this);
            Elements.SetName(Keys.Type, "/Metadata");
            Elements.SetName(Keys.Subtype, "/XML");
            SetupStream();
        }

        void SetupStream()
        {
            const string begin = @"begin=""";

            var stream = GenerateXmp();

            // Preserve "ï»¿" if text is UTF8 encoded.
            var i = stream.IndexOf(begin, StringComparison.Ordinal);
            var pos = i + begin.Length;
            stream = stream[..pos] + "xxx" + stream[(pos + 3)..];

            byte[] bytes = Encoding.UTF8.GetBytes(stream);
            bytes[pos++] = (byte)'ï';
            bytes[pos++] = (byte)'»';
            bytes[pos] = (byte)'¿';

            CreateStream(bytes);
        }

        string GenerateXmp()
        {
            var instanceId = Guid.NewGuid().ToString();
            var documentId = Guid.NewGuid().ToString();

            static DateTime SpecifyLocalDateTimeKindIfUnspecified(DateTime value)
                => value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value;

            var creationDate = SpecifyLocalDateTimeKindIfUnspecified(_document.Info.CreationDate).ToString("yyyy-MM-ddTHH:mm:ssK");
            var modificationDate = creationDate;

            var author = _document.Info.Author;
            var creator = _document.Info.Creator;
            var producer = _document.Info.Producer;
            var title = _document.Info.Title;
            var subject = _document.Info.Subject;
            var keywords = _document.Info.Keywords;
            var conformance = _document.Conformance;

            // Build conformance-specific XMP blocks.
            var conformanceBlocks = new StringBuilder();

            // pdfaid block: emit if PDF/A is declared.
            if (conformance.PdfAIdPart.HasValue)
            {
                conformanceBlocks.AppendLine($"""
                      <rdf:Description rdf:about="" xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
                        <pdfaid:part>{conformance.PdfAIdPart.Value}</pdfaid:part>
                        <pdfaid:conformance>{conformance.PdfAIdConformance}</pdfaid:conformance>
                      </rdf:Description>
                """);
            }

            // pdfuaid block: emit if PDF/UA is declared.
            if (conformance.PdfUAIdPart.HasValue)
            {
                conformanceBlocks.AppendLine($"""
                      <rdf:Description rdf:about="" xmlns:pdfuaid="http://www.aiim.org/pdfua/ns/id/">
                        <pdfuaid:part>{conformance.PdfUAIdPart.Value}</pdfuaid:part>
                      </rdf:Description>
                """);
            }

            // pdfd:declarations block: emit if any conformance is declared.
            var declarationUris = conformance.GetDeclarationUris();
            if (declarationUris.Length > 0)
            {
                conformanceBlocks.AppendLine("""
                      <rdf:Description rdf:about="" xmlns:pdfd="http://pdfa.org/declarations/">
                        <pdfd:declarations>
                          <rdf:Bag>
                """);
                foreach (var uri in declarationUris)
                {
                    conformanceBlocks.AppendLine($"""
                            <rdf:li rdf:parseType="Resource">
                              <pdfd:conformsTo>{uri}</pdfd:conformsTo>
                            </rdf:li>
                    """);
                }
                conformanceBlocks.AppendLine("""
                          </rdf:Bag>
                        </pdfd:declarations>
                      </rdf:Description>
                """);
            }

            var str = $"""
                <?xpacket begin="ï»¿" id="W5M0MpCehiHzreSzNTczkc9d"?>
                  <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="3.1-701">
                    <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                      <rdf:Description rdf:about="" xmlns:pdf="http://ns.adobe.com/pdf/1.3/">
                        <pdf:Producer>{producer}</pdf:Producer><pdf:Keywords>{keywords}</pdf:Keywords>
                      </rdf:Description>
                      <rdf:Description rdf:about="" xmlns:dc="http://purl.org/dc/elements/1.1/">
                        <dc:title><rdf:Alt><rdf:li xml:lang="x-default">{title}</rdf:li></rdf:Alt></dc:title>
                        <dc:creator><rdf:Seq><rdf:li>{author}</rdf:li></rdf:Seq></dc:creator>
                        <dc:description><rdf:Alt><rdf:li xml:lang="x-default">{subject}</rdf:li></rdf:Alt></dc:description>
                      </rdf:Description>
                      <rdf:Description rdf:about="" xmlns:xmp="http://ns.adobe.com/xap/1.0/">
                        <xmp:CreatorTool>{creator}</xmp:CreatorTool>
                        <xmp:CreateDate>{creationDate}</xmp:CreateDate>
                        <xmp:ModifyDate>{modificationDate}</xmp:ModifyDate>
                      </rdf:Description>
                      <rdf:Description rdf:about="" xmlns:xmpMM="http://ns.adobe.com/xap/1.0/mm/">
                        <xmpMM:DocumentID>uuid:{documentId}</xmpMM:DocumentID>
                        <xmpMM:InstanceID>uuid:{instanceId}</xmpMM:InstanceID>
                      </rdf:Description>
                {conformanceBlocks}
                    </rdf:RDF>
                  </x:xmpmeta>
                <?xpacket end="w"?>
                """;

            return str;
        }

        /// <summary>
        /// Predefined keys of this dictionary.
        /// </summary>
        internal class Keys : KeysBase
        {
            /// <summary>
            /// (Required) The type of PDF object that this dictionary describes; must be Metadata for a metadata stream.
            /// </summary>
            [KeyInfo(KeyType.Name | KeyType.Optional, FixedValue = "Metadata")]
            public const string Type = "/Type";

            /// <summary>
            /// (Required) The type of metadata stream that this dictionary describes; must be XML.
            /// </summary>
            [KeyInfo(KeyType.Name | KeyType.Optional, FixedValue = "XML")]
            public const string Subtype = "/Subtype";
        }
    }
}
