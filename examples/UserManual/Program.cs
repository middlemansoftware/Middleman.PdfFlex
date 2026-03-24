using Middleman.PdfFlex;
using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

// Acme Widget Pro -- User Manual
//
// Demonstrates: PDF/UA-1 conformance with automatic accessibility tagging,
// TocBuilder for automatic table of contents with linked page numbers,
// heading hierarchy driving PDF outlines and TOC entries,
// first-page header/footer suppression, and full-bleed footer.

var brandBlue = Color.FromHex("#0f4c75");
var accentBlue = Color.FromHex("#0f4c75");
var medGray = Color.FromHex("#95a5a6");
var darkText = Color.FromHex("#2c3e50");

// Logo SVG shared across examples in the assets folder.
var logoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "acme-logo.svg"));

var doc = new Document(PageSize.Letter, new EdgeInsets(60));
doc.Conformance = PdfConformance.PdfUA1;
doc.Language = "en-US";
doc.DefaultStyle = new Style { FontFamily = "NotoSans", FontSize = 10, FontColor = darkText };

// Footer on pages 2+ with company name and page numbers.
var footerRow = new Row(
    new Element[]
    {
        new TextBlock("Acme Corporation", style: new Style { FontSize = 8, FontColor = medGray }),
        new Spacer(),
        new TextBlock("Page {page} of {pages}", style: new Style { FontSize = 8, FontColor = medGray, TextAlign = TextAlign.Right })
    },
    align: Align.Center);
var pdfFlexNote = new TextBlock("Made with Middleman Software\u2019s PdfFlex library",
    style: new Style { FontSize = 7, FontColor = medGray, TextAlign = TextAlign.Center, Padding = new EdgeInsets(4, 0, 0, 0) });
var footer = new Column(new Divider(0.5, medGray), footerRow, pdfFlexNote);
footer.Style = new Style { Padding = new EdgeInsets(8, 60, 36, 60) };
doc.Footer = footer;

// Suppress header and footer on the title page.
doc.FirstPageHeader = new Column();
doc.FirstPageFooter = new Column();

// -- Lorem ipsum paragraphs rotated across chapters for variety --

const string lorem1 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";
const string lorem2 = "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo.";
const string lorem3 = "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.";
const string lorem4 = "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur. Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur.";
const string lorem5 = "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga.";

// -- Title page --

doc.Add(
    new Box(style: new Style { Height = Length.Pt(120) }),
    // Centered logo on the title page — 100pt tall, auto-width from viewBox aspect ratio.
    new Row(new Spacer(), new SvgBox(logoPath, style: new Style { Height = Length.Pt(100) }) { AltText = "Acme Corporation logo" }, new Spacer()),
    new Box(style: new Style { Height = Length.Pt(30) }),
    new TextBlock("Acme Widget Pro", new FontSpec("NotoSans", 36), headingLevel: 1,
        style: new Style { FontWeight = FontWeight.Bold, FontColor = brandBlue, TextAlign = TextAlign.Center }),
    new Box(style: new Style { Height = Length.Pt(12) }),
    new TextBlock("User Manual", new FontSpec("NotoSans", 18),
        style: new Style { FontColor = accentBlue, TextAlign = TextAlign.Center }),
    new Box(style: new Style { Height = Length.Pt(40) }),
    new Divider(2, accentBlue),
    new Box(style: new Style { Height = Length.Pt(40) }),
    new TextBlock("Version 2.0", new FontSpec("NotoSans", 14),
        style: new Style { TextAlign = TextAlign.Center, FontColor = medGray }),
    new TextBlock("March 2026", new FontSpec("NotoSans", 12),
        style: new Style { TextAlign = TextAlign.Center, FontColor = medGray }));

// -- Chapters --
// Content is added to the document first so TocBuilder can scan headings,
// then the document is reassembled with the TOC inserted between title and body.

var chapters = new List<Element>();

chapters.Add(new PageBreak()); // placeholder for TOC page

// Chapter 1
chapters.Add(H2("Introduction"));
chapters.Add(Gap(8));
chapters.Add(P(lorem1));
chapters.Add(Gap(6));
chapters.Add(P(lorem2));
chapters.Add(Gap(6));
chapters.Add(P(lorem3));

// Chapter 2
chapters.Add(H2("Getting Started"));
chapters.Add(Gap(8));
chapters.Add(H3("System Requirements"));
chapters.Add(Gap(4));
chapters.Add(P(lorem4));
chapters.Add(Gap(6));
chapters.Add(H3("Installation"));
chapters.Add(Gap(4));
chapters.Add(P(lorem5));
chapters.Add(Gap(6));
chapters.Add(P(lorem1));
chapters.Add(Gap(6));
chapters.Add(H3("Initial Setup"));
chapters.Add(Gap(4));
chapters.Add(P(lorem2));
chapters.Add(Gap(6));
chapters.Add(P(lorem3));

// Chapter 3
chapters.Add(H2("Configuration"));
chapters.Add(Gap(8));
chapters.Add(H3("General Settings"));
chapters.Add(Gap(4));
chapters.Add(P(lorem4));
chapters.Add(Gap(6));
chapters.Add(P(lorem5));
chapters.Add(Gap(6));
chapters.Add(H3("Advanced Options"));
chapters.Add(Gap(4));
chapters.Add(P(lorem1));
chapters.Add(Gap(6));
chapters.Add(P(lorem2));

// Chapter 4
chapters.Add(H2("Operation"));
chapters.Add(Gap(8));
chapters.Add(P(lorem3));
chapters.Add(Gap(6));
chapters.Add(P(lorem4));
chapters.Add(Gap(6));
chapters.Add(P(lorem5));
chapters.Add(Gap(6));
chapters.Add(P(lorem1));

// Chapter 5
chapters.Add(H2("Integrations"));
chapters.Add(Gap(8));
chapters.Add(H3("REST API"));
chapters.Add(Gap(4));
chapters.Add(P(lorem2));
chapters.Add(Gap(6));
chapters.Add(P(lorem3));
chapters.Add(Gap(6));
chapters.Add(H3("Webhooks"));
chapters.Add(Gap(4));
chapters.Add(P(lorem4));
chapters.Add(Gap(6));
chapters.Add(H3("Third-Party Plugins"));
chapters.Add(Gap(4));
chapters.Add(P(lorem5));
chapters.Add(Gap(6));
chapters.Add(P(lorem1));

// Chapter 6
chapters.Add(H2("Monitoring"));
chapters.Add(Gap(8));
chapters.Add(P(lorem2));
chapters.Add(Gap(6));
chapters.Add(P(lorem3));
chapters.Add(Gap(6));
chapters.Add(P(lorem4));

// Chapter 7
chapters.Add(H2("Troubleshooting"));
chapters.Add(Gap(8));
chapters.Add(P(lorem5));
chapters.Add(Gap(6));
chapters.Add(P(lorem1));
chapters.Add(Gap(6));
chapters.Add(P(lorem2));
chapters.Add(Gap(6));
chapters.Add(P(lorem3));

// Chapter 8
chapters.Add(H2("Appendix"));
chapters.Add(Gap(8));
chapters.Add(P(lorem4));
chapters.Add(Gap(6));
chapters.Add(P(lorem5));
chapters.Add(Gap(6));
chapters.Add(P(lorem1));
chapters.Add(Gap(6));
chapters.Add(P(lorem2));

// Add chapters so TocBuilder can discover headings.
foreach (var ch in chapters)
    doc.Add(ch);

// -- Table of Contents --

var tocEntries = TocBuilder.Build(doc, new TocStyle
{
    IndentPerLevel = 20,
    FontPerLevel = new Dictionary<int, FontSpec>
    {
        [2] = new FontSpec("NotoSans", 11),
        [3] = new FontSpec("NotoSans", 10)
    }
});

// Reassemble: title page, TOC, then chapter content.
doc.Children.Clear();

doc.Add(
    new Box(style: new Style { Height = Length.Pt(120) }),
    // Centered logo on the title page — 100pt tall, auto-width from viewBox aspect ratio.
    new Row(new Spacer(), new SvgBox(logoPath, style: new Style { Height = Length.Pt(100) }) { AltText = "Acme Corporation logo" }, new Spacer()),
    new Box(style: new Style { Height = Length.Pt(30) }),
    new TextBlock("Acme Widget Pro", new FontSpec("NotoSans", 36), headingLevel: 1,
        style: new Style { FontWeight = FontWeight.Bold, FontColor = brandBlue, TextAlign = TextAlign.Center }),
    new Box(style: new Style { Height = Length.Pt(12) }),
    new TextBlock("User Manual", new FontSpec("NotoSans", 18),
        style: new Style { FontColor = accentBlue, TextAlign = TextAlign.Center }),
    new Box(style: new Style { Height = Length.Pt(40) }),
    new Divider(2, accentBlue),
    new Box(style: new Style { Height = Length.Pt(40) }),
    new TextBlock("Version 2.0", new FontSpec("NotoSans", 14),
        style: new Style { TextAlign = TextAlign.Center, FontColor = medGray }),
    new TextBlock("March 2026", new FontSpec("NotoSans", 12),
        style: new Style { TextAlign = TextAlign.Center, FontColor = medGray }));

doc.Add(new PageBreak());
doc.Add(new TextBlock("Table of Contents", new FontSpec("NotoSans", 20), headingLevel: 2,
    style: new Style { FontWeight = FontWeight.Bold, FontColor = brandBlue }));
doc.Add(new Box(style: new Style { Height = Length.Pt(16) }));
foreach (var entry in tocEntries)
    doc.Add(entry);

doc.Add(new PageBreak());
// Skip the first element (placeholder PageBreak) when re-adding chapters.
for (int i = 1; i < chapters.Count; i++)
    doc.Add(chapters[i]);

// -- Render --

var outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Acme-UserManual.pdf");
outputPath = Path.GetFullPath(outputPath);

DocumentRenderer.Render(doc, outputPath);
Console.WriteLine($"User manual generated: {outputPath}");

// -- Local helpers --

TextBlock H2(string text) => new(text, new FontSpec("NotoSans", 18), headingLevel: 2,
    style: new Style { FontWeight = FontWeight.Bold, FontColor = brandBlue, Padding = new EdgeInsets(12, 0, 0, 0) });

TextBlock H3(string text) => new(text, new FontSpec("NotoSans", 14), headingLevel: 3,
    style: new Style { FontWeight = FontWeight.Bold, FontColor = brandBlue, Padding = new EdgeInsets(8, 0, 0, 0) });

TextBlock P(string text) => new(text, style: new Style { Padding = new EdgeInsets(3, 0, 3, 0) });

Box Gap(double height) => new(style: new Style { Height = Length.Pt(height) });
