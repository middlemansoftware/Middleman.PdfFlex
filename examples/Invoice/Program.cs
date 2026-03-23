using Middleman.PdfFlex;
using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

// Acme Corporation Invoice -- PdfFlex example
//
// Demonstrates: PDF/A-1b conformance, table pagination with repeated headers,
// watermarks, first-page header suppression, SVG logos, and full-bleed footer
// with inset padding.

var acmeBlue = Color.FromHex("#1a3c6e");
var accentGold = Color.FromHex("#d4a843");
var lightBg = Color.FromHex("#f7f8fa");
var borderGray = Color.FromHex("#d0d5dd");
var subtleGray = Color.FromHex("#6b7280");
var darkText = Color.FromHex("#333333");

var invoiceNumber = "INV-2026-00847";

// Logo SVG shared across examples in the assets folder.
var logoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "acme-logo.svg"));

// PDF/A-1b for archival conformance on financial documents.
var doc = new Document(PageSize.Letter, new EdgeInsets(60, 50, 60, 50))
{
    Conformance = PdfConformance.PdfA1b,
    Language = "en-US",
    DefaultStyle = new Style
    {
        FontFamily = "NotoSans",
        FontSize = 9,
        FontColor = darkText,
        LineHeight = 1.3
    }
};

doc.Watermark = new Watermark("SAMPLE", opacity: 0.08, color: borderGray);

// Footer on every page. Full-bleed with inset padding to match body margins.
var footerRow = new Row(
    new Element[]
    {
        new TextBlock("Acme Corporation", style: new Style { FontSize = 7, FontColor = subtleGray }),
        new Spacer(),
        new TextBlock("Page {page} of {pages}", style: new Style { FontSize = 7, FontColor = subtleGray, TextAlign = TextAlign.Right })
    },
    align: Align.Center);
var pdfFlexNote = new TextBlock("Made with Middleman Software\u2019s PdfFlex library",
    style: new Style { FontSize = 7, FontColor = subtleGray, TextAlign = TextAlign.Center, Padding = new EdgeInsets(4, 0, 0, 0) });
var footer = new Column(new Divider(0.5, borderGray), footerRow, pdfFlexNote);
footer.Style = new Style { Padding = new EdgeInsets(8, 50, 10, 50) };
doc.Footer = footer;

// Compact header for pages 2+ with a small logo and invoice number.
var headerRow = new Row(
    new Element[]
    {
        new SvgBox(logoPath, style: new Style { Height = Length.Pt(24) }),
        new Spacer(),
        new TextBlock(invoiceNumber, style: new Style { FontSize = 9, FontColor = subtleGray, TextAlign = TextAlign.Right })
    },
    align: Align.Center);
var header = new Column(headerRow, new Divider(0.5, acmeBlue));
header.Style = new Style { Padding = new EdgeInsets(12, 50, 8, 50) };
doc.Header = header;

// Page 1 has its own full header in the body, so suppress the compact one.
doc.FirstPageHeader = new Column();

// -- Page 1: Company header with invoice title --

// The SVG logo's viewBox is 201.5 x 115, so at 50pt height the width is ~88pt.
// The logo already contains "ACME CORPORATION" text, so no separate company name needed.
doc.Add(new Row(
    new Element[]
    {
        new SvgBox(logoPath, style: new Style { Height = Length.Pt(50) }),
        new Spacer(),
        new TextBlock("INVOICE", new FontSpec("NotoSans", 22, FontWeight.Bold), new Style { FontColor = accentGold, TextAlign = TextAlign.Right })
    },
    align: Align.Center));

doc.Add(new TextBlock("Premium Widgets & Industrial Solutions", style: new Style { FontSize = 8, FontColor = subtleGray }));
doc.Add(new Box(style: new Style { Height = Length.Pt(12) }));

// Invoice metadata and company address side by side.
doc.Add(new Row(
    new Element[]
    {
        new Column(
            new Element[]
            {
                MetadataRow("Invoice #:", invoiceNumber),
                MetadataRow("Date:", "March 15, 2026"),
                MetadataRow("Due Date:", "April 14, 2026"),
                MetadataRow("PO Number:", "PO-9182-A")
            },
            gap: 3,
            style: new Style { Width = Length.Pct(50) }),
        new Column(
            new Element[]
            {
                new TextBlock("1234 Innovation Drive, Suite 500", style: new Style { FontSize = 8, FontColor = subtleGray, TextAlign = TextAlign.Right }),
                new TextBlock("Springfield, IL 62704", style: new Style { FontSize = 8, FontColor = subtleGray, TextAlign = TextAlign.Right }),
                new TextBlock("(555) 123-4567 | billing@acmecorp.com", style: new Style { FontSize = 8, FontColor = subtleGray, TextAlign = TextAlign.Right }),
                new TextBlock("www.acmecorp.com", style: new Style { FontSize = 8, FontColor = acmeBlue, TextAlign = TextAlign.Right })
            },
            gap: 2,
            style: new Style { FlexGrow = 1 })
    }));

doc.Add(new Divider(1, acmeBlue, style: new Style { Margin = new EdgeInsets(14, 0, 14, 0) }));

// Bill To / Ship To addresses.
doc.Add(new Row(
    new Element[]
    {
        new Column(
            new Element[]
            {
                new TextBlock("BILL TO", new FontSpec("NotoSans", 8, FontWeight.Bold), new Style { FontColor = acmeBlue }),
                new Box(style: new Style { Height = Length.Pt(4) }),
                new TextBlock("Widgets International, Inc.", style: new Style { FontWeight = FontWeight.SemiBold }),
                new TextBlock("Attn: Sarah Chen, Procurement"),
                new TextBlock("789 Commerce Boulevard"),
                new TextBlock("Chicago, IL 60601"),
                new TextBlock("sarah.chen@widgetsintl.com", style: new Style { FontColor = acmeBlue })
            },
            gap: 2,
            style: new Style { Width = Length.Pct(48) }),
        new Column(
            new Element[]
            {
                new TextBlock("SHIP TO", new FontSpec("NotoSans", 8, FontWeight.Bold), new Style { FontColor = acmeBlue }),
                new Box(style: new Style { Height = Length.Pt(4) }),
                new TextBlock("Widgets International -- Warehouse", style: new Style { FontWeight = FontWeight.SemiBold }),
                new TextBlock("Attn: Receiving Dock B"),
                new TextBlock("2100 Industrial Parkway"),
                new TextBlock("Joliet, IL 60435"),
                new TextBlock("receiving@widgetsintl.com", style: new Style { FontColor = acmeBlue })
            },
            gap: 2,
            style: new Style { FlexGrow = 1 })
    }));

doc.Add(new Box(style: new Style { Height = Length.Pt(16) }));

// -- Line items table --
// 18 items forces the table onto 2 pages, demonstrating header repetition.

var columns = new[]
{
    new TableColumn("#", Length.Pt(28), TextAlign.Center),
    new TableColumn("Item", Length.Fr(3)),
    new TableColumn("SKU", Length.Fr(1)),
    new TableColumn("Qty", Length.Pt(40), TextAlign.Right),
    new TableColumn("Unit Price", Length.Pt(72), TextAlign.Right),
    new TableColumn("Amount", Length.Pt(72), TextAlign.Right)
};

var lineItems = new (string Item, string Sku, int Qty, decimal UnitPrice)[]
{
    ("Standard Widget A-100", "WDG-A100", 250, 12.50m),
    ("Precision Widget B-200", "WDG-B200", 100, 24.75m),
    ("Heavy-Duty Widget C-350", "WDG-C350", 75, 38.00m),
    ("Micro Widget D-50", "WDG-D050", 500, 6.25m),
    ("Widget Mounting Bracket", "BRK-M100", 250, 3.50m),
    ("Widget Lubricant (1 gal)", "LUB-G001", 12, 45.00m),
    ("Calibration Service", "SVC-CAL1", 4, 175.00m),
    ("Widget Seal Kit", "SEL-K200", 100, 8.75m),
    ("Titanium Widget T-900", "WDG-T900", 25, 89.50m),
    ("Widget Assembly Jig", "TOL-AJ50", 10, 125.00m),
    ("Replacement Spring Pack", "SPR-R100", 200, 4.25m),
    ("Anti-Corrosion Coating (qt)", "COT-AC01", 30, 22.50m),
    ("Widget Alignment Tool", "TOL-AT25", 15, 67.00m),
    ("Bulk Fastener Set (1000pc)", "FST-B001", 8, 55.00m),
    ("Express Shipping Surcharge", "SHP-EXP1", 1, 250.00m),
    ("Extended Warranty -- 2 Year", "WRN-2YR1", 25, 35.00m),
    ("On-Site Installation", "SVC-INS1", 2, 450.00m),
    ("Recycled Widget R-Eco", "WDG-RECO", 150, 9.95m)
};

var rows = new List<object[]>();
decimal subtotal = 0m;

for (int i = 0; i < lineItems.Length; i++)
{
    var item = lineItems[i];
    var amount = item.Qty * item.UnitPrice;
    subtotal += amount;

    rows.Add(new object[]
    {
        (i + 1).ToString(),
        item.Item,
        item.Sku,
        item.Qty.ToString(),
        item.UnitPrice.ToString("C2"),
        amount.ToString("C2")
    });
}

var taxRate = 0.0825m;
var tax = Math.Round(subtotal * taxRate, 2);
var total = subtotal + tax;

var table = new Table(
    columns,
    rows,
    border: Border.All(0.5, borderGray),
    headerStyle: new Style
    {
        Background = new Background(acmeBlue),
        FontColor = Colors.White,
        FontWeight = FontWeight.Bold,
        FontSize = 8,
        Padding = new EdgeInsets(6, 8, 6, 8)
    },
    cellStyle: new Style
    {
        Padding = new EdgeInsets(5, 8, 5, 8),
        FontSize = 8
    },
    rowStyle: new Style
    {
        Background = new Background(Colors.White)
    },
    alternateRowStyle: new Style
    {
        Background = new Background(lightBg)
    },
    continuationText: "(continued)",
    minRowsBeforeBreak: 3);

doc.Add(table);

// -- Totals --
doc.Add(new Box(style: new Style { Height = Length.Pt(12) }));

var totalsSection = new Column(
    new Element[]
    {
        TotalRow("Subtotal:", subtotal.ToString("C2"), subtleGray),
        TotalRow($"Tax ({taxRate:P2}):", tax.ToString("C2"), subtleGray),
        new Divider(0.5, borderGray, style: new Style { Margin = new EdgeInsets(4, 0, 4, 0) }),
        TotalRow("Total Due:", total.ToString("C2"), acmeBlue, isBold: true, fontSize: 12)
    },
    gap: 3,
    style: new Style { Width = Length.Pt(220) });

doc.Add(new Row(new Spacer(), totalsSection));
doc.Add(new Box(style: new Style { Height = Length.Pt(20) }));

// -- Payment terms --
doc.Add(new Column(
    new Element[]
    {
        new TextBlock("Payment Terms", new FontSpec("NotoSans", 10, FontWeight.Bold), new Style
        {
            FontColor = acmeBlue,
            Padding = new EdgeInsets(0, 0, 6, 0)
        }),
        new TextBlock("Payment is due within 30 days of the invoice date. Please reference the invoice number on all payments.",
            style: new Style { FontSize = 8.5 }),
        new Box(style: new Style { Height = Length.Pt(6) }),
        new RichText(
            new Span("Wire Transfer: ", new SpanStyle { FontWeight = FontWeight.SemiBold, FontSize = 8.5 }),
            new Span("First National Bank | Routing: 071000013 | Account: 29384756", new SpanStyle { FontSize = 8.5 })),
        new RichText(
            new Span("Check: ", new SpanStyle { FontWeight = FontWeight.SemiBold, FontSize = 8.5 }),
            new Span("Make payable to Acme Corporation, 1234 Innovation Drive, Suite 500, Springfield, IL 62704", new SpanStyle { FontSize = 8.5 })),
        new Box(style: new Style { Height = Length.Pt(6) }),
        new TextBlock("A 1.5% monthly finance charge will be applied to past-due balances.", style: new Style
        {
            FontSize = 7.5,
            FontColor = subtleGray,
            FontStyle = Middleman.PdfFlex.Styling.FontStyle.Italic
        })
    },
    gap: 2,
    style: new Style
    {
        Background = new Background(lightBg),
        Border = Border.All(0.5, borderGray, cornerRadius: 4),
        Padding = new EdgeInsets(14, 16, 14, 16)
    }));

doc.Add(new Box(style: new Style { Height = Length.Pt(16) }));

doc.Add(new TextBlock("Thank you for your business!",
    new FontSpec("NotoSans", 10, FontWeight.SemiBold),
    new Style { FontColor = acmeBlue, TextAlign = TextAlign.Center }));

// -- Render --

var outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Acme-Invoice.pdf");
outputPath = Path.GetFullPath(outputPath);

DocumentRenderer.Render(doc, outputPath);
Console.WriteLine($"Invoice generated: {outputPath}");

// -- Helpers --

static Element MetadataRow(string label, string value) =>
    new Row(
        new TextBlock(label, new FontSpec("NotoSans", 9, FontWeight.SemiBold), new Style
        {
            FontColor = Color.FromHex("#333333"),
            Width = Length.Pt(80)
        }),
        new TextBlock(value, style: new Style { FontColor = Color.FromHex("#333333") }));

static Element TotalRow(string label, string value, Color color, bool isBold = false, double fontSize = 9) =>
    new Row(
        new Element[]
        {
            new TextBlock(label, style: new Style
            {
                FontSize = fontSize,
                FontColor = color,
                FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
                TextAlign = TextAlign.Right,
                FlexGrow = 1
            }),
            new TextBlock(value, style: new Style
            {
                FontSize = fontSize,
                FontColor = color,
                FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
                TextAlign = TextAlign.Right,
                Width = Length.Pt(90)
            })
        });
