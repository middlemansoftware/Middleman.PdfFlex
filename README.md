# Middleman.PdfFlex

Declarative PDF layout engine for .NET. Flexbox layout, PDF/A and PDF/UA conformance, streaming output, pure managed C#.

## Features

- Flexbox layout - rows, columns, flex-grow/shrink, justify, align, gap, padding, margin
- CSS-like styling - cascading properties, typed units (pt, mm, in, cm, %, fr)
- Headers & footers - full-bleed by default (full page width), first-page override (`FirstPageHeader`/`FirstPageFooter`), `{page}`/`{pages}` tokens, automatic PDF/UA artifact tagging
- Links and navigation - internal anchors (`Element.Id`), external URI links, `LinkTarget` on text/images/SVG/spans, auto-generated PDF outlines (bookmarks) from headings, `{page:anchor}` tokens for cross-references
- Table of contents - `TocBuilder` walks the heading tree and generates linked TOC entries with page numbers that resolve automatically during rendering
- Automatic pagination with page breaks
- Tables with repeated headers, continuation text, orphan prevention, and automatic page splitting
- Watermarks - pre-blended when transparency is not allowed (PDF/A-1), native alpha otherwise
- Rich text with inline spans
- Images (PNG, JPEG, BMP, GIF, TGA, PSD) and SVG (native vectors via Middleman.Svg)
- PDF/A (1a, 1b, 2a, 2b, 2u, 3a, 3b) with XMP metadata, ICC output intents, save-time validation
- Automatic PDF/UA-1 tagging - the layout engine knows what every element is semantically, so setting `Conformance = PdfConformance.PdfUA1` produces a fully tagged, accessible PDF with zero manual tagging code
- Digital signatures (PKCS#7/CMS), encryption v1 - v5
- Streaming output - renders pages one at a time and releases content stream memory, keeping usage bounded regardless of page count (50,000+ pages tested)
- .NET 8.0, no native dependencies, no GDI+, no WPF, no System.Drawing

## Quick Start

```csharp
using Middleman.PdfFlex;
using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Pdf;

var doc = new Document(PageSize.Letter, new EdgeInsets(50))
{
    Conformance = PdfConformance.PdfA1b
};

doc.Add(new TextBlock("Hello, PdfFlex!", new FontSpec("Arial", 24)));
doc.Add(new ImageBox("sample.png") { AltText = "Sample image" });

DocumentRenderer.Render(doc, "output.pdf");
```

### Accessible PDF (PDF/UA-1)

Same document construction, fully tagged output:

```csharp
var doc = new Document(PageSize.Letter, new EdgeInsets(50))
{
    Conformance = PdfConformance.PdfUA1,
    Language = "en-US"
};

doc.Add(new TextBlock("Quarterly Report", new FontSpec("Arial", 24), headingLevel: 1));
doc.Add(new TextBlock("Revenue exceeded projections by 12%."));
doc.Add(new ImageBox("sample.png") { AltText = "Sample PNG image" });

DocumentRenderer.Render(doc, "accessible.pdf");
```

The output PDF has a complete structure tree (/H1, /P, /Figure with alt text), document language metadata, and marked content sequences. No `Semantic*()` calls, no manual tagging.

### Combined conformance

```csharp
doc.Conformance = PdfConformance.PdfA2a.With(PdfConformance.PdfUA1);
```

## Examples

| Example | Source | PDF |
|:--------|:-------|:----|
| Invoice | [Program.cs](examples/Invoice/Program.cs) | [Acme-Invoice.pdf](examples/Invoice/Acme-Invoice.pdf) |
| User Manual | [Program.cs](examples/UserManual/Program.cs) | [Acme-UserManual.pdf](examples/UserManual/Acme-UserManual.pdf) |
| Expense Report (interactive form) | [Program.cs](examples/Form/Program.cs) | [Acme-ExpenseReport.pdf](examples/Form/Acme-ExpenseReport.pdf) |
| Expense Report (filled & flattened) | | [Acme-ExpenseReport-Filled.pdf](examples/Form/Acme-ExpenseReport-Filled.pdf) |

## PdfFlex vs. Other .NET PDF Libraries:

| | PdfFlex | QuestPDF | PDFsharp | IronPDF | Aspose.PDF |
|:--|:--:|:--:|:--:|:--:|:--:|
| MIT license | ✅ | ✅* | ✅ | ❌ | ❌ |
| Declarative layout | ✅ | ✅ | ❌ | ❌ | ❌ |
| PDF/A (all levels) | ✅ | ✅ | ❌ | ✅ | ✅ |
| Automatic PDF/UA | ✅ | ❌ | ❌ | ❌ | ❌ |
| Save-time validation | ✅ | ❌ | ❌ | ❌ | ❌ |
| Streaming (50K+ pages) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Full-bleed headers | ✅ | ❌ | ❌ | ❌ | ❌ |
| SVG (native vectors) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Digital signatures | ✅ | ❌ | ✅ | ✅ | ✅ |
| Zero native deps | ✅ | ❌ | ✅ | ❌ | ❌ |
| Deploy size | ~5 MB | ~36 MB | ~4.4 MB | ~250 MB | ~196 MB |

*QuestPDF requires a paid license above $1M annual revenue.

## License

MIT. See [LICENSE](LICENSE).

See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for component attributions.
