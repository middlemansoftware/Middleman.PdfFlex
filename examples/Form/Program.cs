using Middleman.PdfFlex;
using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

// Acme Corporation Expense Report -- PdfFlex form example
//
// Demonstrates: PDF/UA-1 conformance, interactive form fields (text, dropdown,
// checkbox, textarea), SetFieldValues for pre-filling, and FlattenForms for
// producing a static archival copy.
//
// Two PDFs from one document definition:
//   1. Interactive form with editable fields for end users
//   2. Pre-filled and flattened to a static archival PDF

var interactivePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Acme-ExpenseReport.pdf");
var filledPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Acme-ExpenseReport-Filled.pdf");

// Render 1: Interactive form with editable fields.
var doc = BuildExpenseReport();
DocumentRenderer.Render(doc, interactivePath);
Console.WriteLine($"Interactive form: {Path.GetFullPath(interactivePath)}");

// Render 2: Pre-filled and flattened to a static PDF.
// Rebuild from scratch so field state is clean — SetFieldValues mutates the tree.
doc = BuildExpenseReport();

doc.SetFieldValues(new Dictionary<string, string>
{
    ["employee_name"] = "Jane Nguyen",
    ["employee_id"] = "EMP-4821",
    ["department"] = "Engineering",
    ["report_date"] = "2026-03-20",

    ["desc_1"] = "Flight to NYC (client meeting)",
    ["amount_1"] = "487.50",
    ["category_1"] = "Travel",

    ["desc_2"] = "Hotel - 2 nights (Midtown)",
    ["amount_2"] = "396.00",
    ["category_2"] = "Lodging",

    ["desc_3"] = "Client dinner (4 attendees)",
    ["amount_3"] = "215.80",
    ["category_3"] = "Meals",

    ["desc_4"] = "Taxi to/from airport",
    ["amount_4"] = "68.00",
    ["category_4"] = "Transportation",

    ["desc_5"] = "Presentation supplies",
    ["amount_5"] = "42.15",
    ["category_5"] = "Supplies",

    ["desc_6"] = "Conference Wi-Fi pass",
    ["amount_6"] = "24.99",
    ["category_6"] = "Other",

    ["total"] = "1,234.44",
    ["notes"] = "NYC trip for Q1 planning meeting with Acme Broadcasting.\nReceipts attached for all items over $25.",

    ["certify"] = "true",
    ["mgr_approve"] = "true",
    ["mgr_name"] = "David Park"
});

DocumentRenderer.Render(doc, filledPath, new RenderOptions { FlattenForms = true });
Console.WriteLine($"Filled (flattened): {Path.GetFullPath(filledPath)}");

// =========================================================================
// Document Builder
// =========================================================================

static Document BuildExpenseReport()
{
    var navy = Color.FromHex("#0f4c75");
    var accent = Color.FromHex("#0f4c75");
    var medGray = Color.FromHex("#95a5a6");
    var lightGray = Color.FromHex("#ecf0f1");
    var darkText = Color.FromHex("#2c3e50");

    var categories = new List<string> { "Travel", "Meals", "Lodging", "Transportation", "Supplies", "Other" };
    var departments = new List<string> { "Engineering", "Sales", "Marketing", "Operations", "HR", "Finance" };

    var logoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "acme-logo.svg"));

    // 50pt margins all around. Form fields need room to breathe.
    var doc = new Document(PageSize.Letter, new EdgeInsets(50));
    doc.Conformance = PdfConformance.PdfUA1;
    doc.Language = "en-US";
    doc.DefaultStyle = new Style { FontFamily = "NotoSans", FontSize = 10, FontColor = darkText };

    // -- Footer: company name left, pagination right --
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
    footer.Style = new Style { Padding = new EdgeInsets(8, 50, 36, 50) };
    doc.Footer = footer;

    // -- Title bar: logo on left, "Expense Report" on right --
    doc.Add(new Row(
        new Element[]
        {
            new SvgBox(logoPath, style: new Style { Height = Length.Pt(50) }) { AltText = "Acme Corporation logo" },
            new Spacer(),
            new TextBlock("Expense Report", new FontSpec("NotoSans", 22), headingLevel: 1,
                style: new Style { FontWeight = FontWeight.Bold, FontColor = navy, TextAlign = TextAlign.Right })
        },
        align: Align.Center));
    doc.Add(new Divider(1.5, navy));
    doc.Add(Gap(12));

    // -- Employee Information --
    // FormField.Label renders above the field widget inside the element's total Height.
    // A 40pt height gives ~12pt for the label + 2pt gap + 26pt for the input — comfortable.
    doc.Add(SectionHeading("Employee Information", accent));
    doc.Add(Gap(4));

    // Row 1: Name (grows to fill) + Employee ID (fixed width)
    doc.Add(new Row(
        new Element[]
        {
            new FormTextField
            {
                Name = "employee_name",
                Label = "Employee Name",
                Placeholder = "Full name",
                Required = true,
                Style = new Style { FlexGrow = 1, Height = Length.Pt(40) }
            },
            new FormTextField
            {
                Name = "employee_id",
                Label = "Employee ID",
                Placeholder = "EMP-0000",
                MaxLength = 10,
                Style = new Style { Width = Length.Pt(140), Height = Length.Pt(40) }
            }
        },
        gap: 16));
    doc.Add(Gap(4));

    // Row 2: Department + Date (both fixed, left-aligned via Spacer absorbing remainder)
    doc.Add(new Row(
        new Element[]
        {
            new FormDropdown
            {
                Name = "department",
                Label = "Department",
                Options = departments,
                Required = true,
                Style = new Style { Width = Length.Pt(180), Height = Length.Pt(40) }
            },
            new FormTextField
            {
                Name = "report_date",
                Label = "Date",
                Placeholder = "YYYY-MM-DD",
                MaxLength = 10,
                Style = new Style { Width = Length.Pt(140), Height = Length.Pt(40) }
            },
            new Spacer()
        },
        gap: 16));
    doc.Add(Gap(8));
    doc.Add(new Divider(0.5, medGray));
    doc.Add(Gap(8));

    // -- Expense Line Items --
    // Each line: row number label, description (flex), amount (fixed), category dropdown (fixed).
    // No Label on these fields — the column headers above serve as labels.
    doc.Add(SectionHeading("Expense Items", accent));
    doc.Add(Gap(4));

    // Column headers — widths match the fields below.
    doc.Add(new Row(
        new Element[]
        {
            new TextBlock("#", style: new Style { FontWeight = FontWeight.Bold, FontSize = 9, Width = Length.Pt(20), TextAlign = TextAlign.Center }),
            new TextBlock("Description", style: new Style { FontWeight = FontWeight.Bold, FontSize = 9, FlexGrow = 1 }),
            new TextBlock("Amount ($)", style: new Style { FontWeight = FontWeight.Bold, FontSize = 9, Width = Length.Pt(90), TextAlign = TextAlign.Right }),
            new TextBlock("Category", style: new Style { FontWeight = FontWeight.Bold, FontSize = 9, Width = Length.Pt(130) })
        },
        gap: 8));
    doc.Add(Gap(4));

    for (int i = 1; i <= 6; i++)
    {
        // Alternating background for visual grouping.
        var rowBg = i % 2 == 0 ? new Background(lightGray) : new Background(Colors.White);

        doc.Add(new Row(
            new Element[]
            {
                // Row number — plain text, vertically centered in the row.
                new TextBlock($"{i}", style: new Style { FontSize = 9, FontColor = medGray, Width = Length.Pt(20), TextAlign = TextAlign.Center }),
                // Description — no Label, the column header above identifies it.
                new FormTextField
                {
                    Name = $"desc_{i}",
                    Placeholder = "Expense description",
                    Style = new Style { FlexGrow = 1, Height = Length.Pt(24) }
                },
                new FormTextField
                {
                    Name = $"amount_{i}",
                    Placeholder = "0.00",
                    MaxLength = 12,
                    Style = new Style { Width = Length.Pt(90), Height = Length.Pt(24) }
                },
                new FormDropdown
                {
                    Name = $"category_{i}",
                    Options = categories,
                    Style = new Style { Width = Length.Pt(130), Height = Length.Pt(24) }
                }
            },
            gap: 8,
            align: Align.Center,
            style: new Style { Background = rowBg, Padding = new EdgeInsets(3, 4, 3, 4) }));
    }

    // Total row — right-aligned label + read-only field.
    doc.Add(new Row(
        new Element[]
        {
            new Spacer(),
            new TextBlock("TOTAL:", style: new Style
            {
                FontWeight = FontWeight.Bold,
                FontSize = 12,
                FontColor = navy
            }),
            new FormTextField
            {
                Name = "total",
                Placeholder = "0.00",
                ReadOnly = true,
                Style = new Style { Width = Length.Pt(120), Height = Length.Pt(26) }
            }
        },
        gap: 10,
        align: Align.Center,
        style: new Style
        {
            Padding = new EdgeInsets(8, 4, 4, 4),
            Border = Border.TopOnly(1.5, navy)
        }));

    doc.Add(Gap(8));
    doc.Add(new Divider(0.5, medGray));
    doc.Add(Gap(8));

    // -- Notes --
    doc.Add(SectionHeading("Notes / Comments", accent));
    doc.Add(Gap(4));

    doc.Add(new FormTextArea
    {
        Name = "notes",
        Placeholder = "Additional details, justifications, or receipt references...",
        Lines = 3,
        Style = new Style { Height = Length.Pt(60) }
    });

    doc.Add(Gap(8));
    doc.Add(new Divider(0.5, medGray));
    doc.Add(Gap(8));

    // -- Certification & Approval --
    doc.Add(SectionHeading("Certification & Approval", accent));
    doc.Add(Gap(6));

    doc.Add(new FormCheckbox
    {
        Name = "certify",
        Label = "I certify that these expenses are accurate and were incurred for business purposes",
        Required = true
    });
    doc.Add(Gap(10));

    // Manager row: checkbox on the left, name field fills the rest.
    // The checkbox has no built-in label height issue — it renders inline.
    doc.Add(new TextBlock("Manager Name", style: new Style { FontWeight = FontWeight.Bold, FontSize = 9 }));
    doc.Add(Gap(2));
    doc.Add(new Row(
        new Element[]
        {
            new FormCheckbox
            {
                Name = "mgr_approve",
                Label = "Approved"
            },
            new FormTextField
            {
                Name = "mgr_name",
                Placeholder = "Approving manager",
                Style = new Style { FlexGrow = 1, Height = Length.Pt(24) }
            }
        },
        gap: 12,
        align: Align.Center));

    return doc;
}

// =========================================================================
// Helpers
// =========================================================================

static TextBlock SectionHeading(string text, Color color) =>
    new(text, new FontSpec("NotoSans", 12), headingLevel: 2,
        style: new Style
        {
            FontWeight = FontWeight.Bold,
            FontColor = color
        });

static Box Gap(double height) => new(style: new Style { Height = Length.Pt(height) });
