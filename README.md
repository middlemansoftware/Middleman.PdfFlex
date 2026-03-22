# Middleman.PdfFlex

Declarative PDF layout engine for .NET. Flexbox layout, PDF/A and PDF/UA conformance, streaming output, pure managed C#.

## Features

- Flexbox layout - rows, columns, flex-grow/shrink, justify, align, gap, padding, margin
- CSS-like styling - cascading properties, typed units (pt, mm, in, cm, %, fr)
- Automatic pagination with watermarks and page breaks
- Tables with repeated headers and automatic page splitting
- Rich text with inline spans
- Images (PNG, JPEG, BMP, GIF, TGA, PSD) and SVG (native vectors via Middleman.Svg)
- PDF/A (1a, 1b, 2a, 2b, 2u, 3a, 3b) with XMP metadata, ICC output intents, save-time validation
- Automatic PDF/UA-1 tagging - the layout engine knows what every element is semantically, so setting `Conformance = PdfConformance.PdfUA1` produces a fully tagged, accessible PDF with zero manual tagging code
- Digital signatures (PKCS#7/CMS), encryption v1–v5
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

## Comparison

| | PdfFlex | QuestPDF | PDFsharp | IronPDF | iText 7 | Aspose.PDF |
|---|---|---|---|---|---|---|
| **License** | MIT | Hybrid* | MIT | Commercial ($749+) | AGPL / commercial ($10K+/yr) | Commercial ($1,199+) |
| **Layout** | Flexbox (native) | Fluent (native) | Manual coordinates | HTML/CSS (Chromium) | Programmatic | Programmatic |
| **PDF/A** | 1a/1b – 3a/3b | 2a/2b – 3a/3u | Hardcoded 1a only | 1–3 | All | All |
| **PDF/UA** | UA-1 (automatic) | UA-1 (manual) | UA-1 (manual) | UA-1 | UA-1/2 | UA-1 |
| **Conformance selection** | Per-level, combinable | Per-level | Boolean flag | Per-level | Per-level | Per-level |
| **Save-time validation** | Yes | No | No | — | Yes | — |
| **Digital signatures** | PKCS#7/CMS | No | Limited | Yes | Yes | Yes |
| **SVG** | Native vectors | Native | No | Via Chromium | Via add-on | Separate product |
| **Large docs** | Streaming (50K+ pages) | Lazy elements | In-memory | Chromium-limited | Streaming | In-memory |
| **Deploy size** | ~5 MB | ~36 MB | ~4.4 MB | ~250 MB | ~5 MB | ~196 MB |
| **Image formats** | PNG, JPEG, BMP, GIF, SVG | PNG, JPEG (via Skia) | PNG, JPEG, BMP (Core) | Via Chromium | PNG, JPEG, BMP | PNG, JPEG, BMP |
| **Native deps** | None | Skia | None (Core) | Chromium | None | System.Drawing |
| **Cross-platform** | Win/Linux/macOS | Win/Linux/macOS | Win/Linux/macOS (Core) | Win/Linux/macOS | Win/Linux/macOS | Needs libgdiplus on Linux |

*QuestPDF was MIT through 2022.12.x. Current versions require a paid license above $1M annual revenue.

PDFsharp's PDF/A: a single `SetPdfA()` method, no level parameter, XMP hardcoded to PDF/A-1a. Described in their docs as "very early state."

PDF/UA automatic vs manual: PDF/UA requires a structure tree mapping every piece of content to its semantic role (paragraph, heading, table cell, figure, etc.). Most libraries require the developer to manually tag each element. QuestPDF recently added manual semantic methods (`SemanticHeader1()`, `SemanticImage()`, etc.). PDFsharp requires manual `BeginElement`/`End` calls around every draw operation. PdfFlex's layout engine already knows the semantic role of every element in the tree, so the tagging happens automatically during rendering with no additional API calls.

## License

MIT. See [LICENSE](LICENSE).

## Attribution

The PDF internals are originally based on [PDFsharp](https://github.com/empira/PDFsharp) 6.2.x by empira Software GmbH (MIT). Key improvements since:

- `PdfConformance` system with per-level PDF/A and PDF/UA profiles, capability queries, and combinable conformance via `With()`
- Save-time conformance validation
- ISO 19005-1:2005 clause 6.1.8 and 6.7.9 compliance fixes
- Conformance-aware transparency handling
- Replaced BigGustave PNG decoder with [StbImageSharp](https://github.com/StbSharp/StbImageSharp) (public domain) for broader format support (PNG, BMP, GIF, TGA, PSD, HDR) and simplified alpha handling
- Removed platform-specific dependencies (GDI+, WPF, System.Drawing)

See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) and [licenses/PdfSharp.txt](licenses/PdfSharp.txt).
