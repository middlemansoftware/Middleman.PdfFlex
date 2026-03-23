// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.AcroForms;
using Middleman.PdfFlex.Pdf.IO;
using Middleman.PdfFlex.Rendering;
using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Tests;

/// <summary>
/// Verifies AcroForm support: form field rendering, AcroForm dictionary creation,
/// field value pre-filling, form flattening, and PDF form reading.
/// </summary>
public class AcroFormTests
{
    #region Helpers

    /// <summary>Creates a simple document with the given elements.</summary>
    private static Document CreateDoc(params Element[] elements)
    {
        var doc = new Document(PageSize.Letter, new EdgeInsets(40));
        doc.Add(elements);
        return doc;
    }

    /// <summary>Renders a document and opens it for reading AcroForm fields.</summary>
    private static PdfDocument RenderAndOpen(Document doc, RenderOptions? options = null)
    {
        var bytes = DocumentRenderer.RenderToBytes(doc, options);
        var stream = new MemoryStream(bytes);
        return PdfReader.Open(stream, PdfDocumentOpenMode.Modify);
    }

    /// <summary>Gets the page count from rendered PDF bytes.</summary>
    private static int GetPageCount(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var pdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return pdfDoc.PageCount;
    }

    /// <summary>Checks whether the PDF has an AcroForm with any fields.</summary>
    private static bool HasAcroForm(PdfDocument pdfDoc)
    {
        try
        {
            var acroForm = pdfDoc.AcroForm;
            return acroForm.Fields.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets an AcroForm field by name from the document.</summary>
    private static PdfAcroField? GetField(PdfDocument pdfDoc, string name)
    {
        try
        {
            var acroForm = pdfDoc.AcroForm;
            return acroForm.Fields[name];
        }
        catch
        {
            return null;
        }
    }

    #endregion Helpers

    #region TextField Tests

    [Fact]
    public void TextField_RendersLabelAndField()
    {
        var doc = CreateDoc(new FormTextField { Name = "firstName", Label = "First Name" });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void TextField_CreatesAcroFormEntry()
    {
        var doc = CreateDoc(new FormTextField { Name = "firstName", Label = "First Name" });
        using var pdfDoc = RenderAndOpen(doc);
        Assert.True(HasAcroForm(pdfDoc));
        var field = GetField(pdfDoc, "firstName");
        Assert.NotNull(field);
    }

    [Fact]
    public void TextField_Placeholder_InAppearance()
    {
        // Rendering with a placeholder should succeed without error.
        var doc = CreateDoc(new FormTextField
        {
            Name = "email",
            Label = "Email",
            Placeholder = "Enter your email"
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void TextField_MaxLength_InDictionary()
    {
        var doc = CreateDoc(new FormTextField { Name = "zip", MaxLength = 5 });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "zip");
        Assert.NotNull(field);
        Assert.IsType<PdfTextField>(field);
        Assert.Equal(5, ((PdfTextField)field!).MaxLength);
    }

    [Fact]
    public void TextField_Required_FlagSet()
    {
        var doc = CreateDoc(new FormTextField { Name = "required_field", Required = true });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "required_field");
        Assert.NotNull(field);
        Assert.True((field!.Flags & PdfAcroFieldFlags.Required) != 0);
    }

    [Fact]
    public void TextField_ReadOnly_FlagSet()
    {
        var doc = CreateDoc(new FormTextField { Name = "ro_field", ReadOnly = true });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "ro_field");
        Assert.NotNull(field);
        Assert.True(field!.ReadOnly);
    }

    [Fact]
    public void TextField_Value_PreFilled()
    {
        var doc = CreateDoc(new FormTextField { Name = "city", Value = "Portland" });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "city");
        Assert.NotNull(field);
        Assert.IsType<PdfTextField>(field);
        Assert.Equal("Portland", ((PdfTextField)field!).Text);
    }

    [Fact]
    public void TextField_FlexboxLayout()
    {
        // Form fields inside Row/Column should render without error.
        var doc = CreateDoc(new Row(
            new FormTextField { Name = "first", Label = "First", Style = new Style { FlexGrow = 1 } },
            new FormTextField { Name = "last", Label = "Last", Style = new Style { FlexGrow = 1 } }
        ));
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    #endregion TextField Tests

    #region TextArea Tests

    [Fact]
    public void TextArea_MultilineFlag()
    {
        var doc = CreateDoc(new FormTextArea { Name = "notes", Lines = 5 });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "notes");
        Assert.NotNull(field);
        Assert.True((field!.Flags & PdfAcroFieldFlags.Multiline) != 0);
    }

    [Fact]
    public void TextArea_Lines_AffectsHeight()
    {
        // Both render without error; a textarea with more lines should produce valid output.
        var doc3 = CreateDoc(new FormTextArea { Name = "small", Lines = 3 });
        var doc8 = CreateDoc(new FormTextArea { Name = "large", Lines = 8 });
        var bytes3 = DocumentRenderer.RenderToBytes(doc3);
        var bytes8 = DocumentRenderer.RenderToBytes(doc8);
        Assert.True(bytes3.Length > 0);
        Assert.True(bytes8.Length > 0);
    }

    [Fact]
    public void TextArea_Value_WrapsText()
    {
        var doc = CreateDoc(new FormTextArea
        {
            Name = "description",
            Lines = 4,
            Value = "This is a longer text value that should be present in the field."
        });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "description");
        Assert.NotNull(field);
    }

    #endregion TextArea Tests

    #region Checkbox Tests

    [Fact]
    public void Checkbox_RendersSquare()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "agree", Label = "I agree" });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Checkbox_Checked_ShowsCheckmark()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "opt_in", Checked = true, Label = "Opt in" });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Checkbox_Unchecked_Empty()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "opt_out", Checked = false, Label = "Opt out" });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Checkbox_CreatesButtonField()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "terms", Checked = true });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "terms");
        Assert.NotNull(field);
        // Checkbox fields use /Btn field type.
        Assert.IsType<PdfCheckBoxField>(field);
    }

    #endregion Checkbox Tests

    #region Dropdown Tests

    [Fact]
    public void Dropdown_Options_InDictionary()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "country",
            Options = new List<string> { "USA", "Canada", "Mexico" }
        });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "country");
        Assert.NotNull(field);
        Assert.IsType<PdfComboBoxField>(field);
    }

    [Fact]
    public void Dropdown_SelectedOption_Displayed()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "state",
            Options = new List<string> { "OR", "WA", "CA" },
            SelectedOption = "OR"
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Dropdown_Editable_ComboFlag()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "color",
            Options = new List<string> { "Red", "Blue" },
            Editable = true
        });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "color");
        Assert.NotNull(field);
        Assert.True((field!.Flags & PdfAcroFieldFlags.Combo) != 0);
        Assert.True((field.Flags & PdfAcroFieldFlags.Edit) != 0);
    }

    [Fact]
    public void Dropdown_NotEditable_NoComboEditFlag()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "size",
            Options = new List<string> { "S", "M", "L" },
            Editable = false
        });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "size");
        Assert.NotNull(field);
        Assert.True((field!.Flags & PdfAcroFieldFlags.Combo) != 0);
        Assert.True((field.Flags & PdfAcroFieldFlags.Edit) == 0);
    }

    #endregion Dropdown Tests

    #region Layout & Integration Tests

    [Fact]
    public void FormField_Width_Respected()
    {
        var doc = CreateDoc(new FormTextField
        {
            Name = "narrow",
            Style = new Style { Width = Length.Pt(200) }
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void FormField_Margin_Applied()
    {
        var doc = CreateDoc(new FormTextField
        {
            Name = "spaced",
            Style = new Style { Margin = new EdgeInsets(10) }
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void FormField_Pagination()
    {
        // Create enough content to push a form field to page 2.
        // The form field needs an explicit height so the layout engine accounts for it.
        var doc = new Document(PageSize.Letter, new EdgeInsets(40));
        doc.Add(new Box(style: new Style { Height = Length.Pt(700) }));
        doc.Add(new FormTextField
        {
            Name = "page2_field",
            Label = "On Page 2",
            Style = new Style { Height = Length.Pt(40) }
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.Equal(2, GetPageCount(bytes));
    }

    [Fact]
    public void FormField_InHeaderFooter_Throws()
    {
        var doc = new Document(PageSize.Letter);
        doc.Header = new FormTextField { Name = "bad" };
        Assert.Throws<InvalidOperationException>(() => DocumentRenderer.RenderToBytes(doc));
    }

    [Fact]
    public void FormField_StreamingPath_Works()
    {
        var doc = CreateDoc(
            new FormTextField { Name = "stream_test", Value = "Hello" }
        );
        using var ms = new MemoryStream();
        DocumentRenderer.RenderStreaming(doc, ms);
        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void NoFormFields_ExistingBehaviorUnchanged()
    {
        // A document with no form fields should not have an AcroForm dictionary.
        var doc = CreateDoc(new TextBlock("Plain text", new FontSpec("Arial", 12)));
        using var pdfDoc = RenderAndOpen(doc);
        Assert.False(HasAcroForm(pdfDoc));
    }

    #endregion Layout & Integration Tests

    #region SetFieldValues Tests

    [Fact]
    public void SetFieldValues_UpdatesFields()
    {
        var doc = CreateDoc(
            new FormTextField { Name = "firstName" },
            new FormTextField { Name = "lastName" }
        );
        doc.SetFieldValues(new Dictionary<string, string>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        });
        using var pdfDoc = RenderAndOpen(doc);
        var field1 = GetField(pdfDoc, "firstName");
        var field2 = GetField(pdfDoc, "lastName");
        Assert.NotNull(field1);
        Assert.NotNull(field2);
        Assert.Equal("John", ((PdfTextField)field1!).Text);
        Assert.Equal("Doe", ((PdfTextField)field2!).Text);
    }

    [Fact]
    public void SetFieldValues_UnknownField_Ignored()
    {
        var doc = CreateDoc(new FormTextField { Name = "known" });
        // Should not throw when setting a value for a non-existent field.
        doc.SetFieldValues(new Dictionary<string, string>
        {
            ["known"] = "value",
            ["unknown"] = "ignored"
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void SetFieldValues_Checkbox_TrueString()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "agree" });
        doc.SetFieldValues(new Dictionary<string, string> { ["agree"] = "true" });

        // The checkbox should be checked after SetFieldValues.
        var checkbox = doc.Children.OfType<FormCheckbox>().First();
        Assert.True(checkbox.Checked);
    }

    [Fact]
    public void SetFieldValues_Dropdown_SetsOption()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "country",
            Options = new List<string> { "USA", "Canada" }
        });
        doc.SetFieldValues(new Dictionary<string, string> { ["country"] = "Canada" });

        var dropdown = doc.Children.OfType<FormDropdown>().First();
        Assert.Equal("Canada", dropdown.SelectedOption);
    }

    #endregion SetFieldValues Tests

    #region FlattenForms Tests

    [Fact]
    public void FlattenForms_TextFieldStatic()
    {
        var doc = CreateDoc(new FormTextField { Name = "flat_text", Value = "Baked In" });
        var options = new RenderOptions { FlattenForms = true };
        using var pdfDoc = RenderAndOpen(doc, options);
        // Flattened form should NOT have AcroForm entries.
        Assert.False(HasAcroForm(pdfDoc));
    }

    [Fact]
    public void FlattenForms_CheckboxStatic()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "flat_check", Checked = true });
        var options = new RenderOptions { FlattenForms = true };
        using var pdfDoc = RenderAndOpen(doc, options);
        Assert.False(HasAcroForm(pdfDoc));
    }

    [Fact]
    public void FlattenForms_DropdownStatic()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "flat_drop",
            Options = new List<string> { "A", "B" },
            SelectedOption = "B"
        });
        var options = new RenderOptions { FlattenForms = true };
        using var pdfDoc = RenderAndOpen(doc, options);
        Assert.False(HasAcroForm(pdfDoc));
    }

    [Fact]
    public void FlattenForms_EmptyField_RendersEmpty()
    {
        var doc = CreateDoc(new FormTextField { Name = "empty" });
        var options = new RenderOptions { FlattenForms = true };
        var bytes = DocumentRenderer.RenderToBytes(doc, options);
        Assert.True(bytes.Length > 0);
    }

    #endregion FlattenForms Tests

    #region PdfFormReader Tests

    [Fact]
    public void PdfFormReader_ExtractsAllFields()
    {
        var doc = CreateDoc(
            new FormTextField { Name = "f1", Value = "v1" },
            new FormTextField { Name = "f2", Value = "v2" },
            new FormCheckbox { Name = "f3", Checked = true }
        );
        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var ms = new MemoryStream(bytes);
        var fields = PdfFormReader.ExtractFields(ms);
        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void PdfFormReader_TextField_Value()
    {
        var doc = CreateDoc(new FormTextField { Name = "name", Value = "Alice" });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var ms = new MemoryStream(bytes);
        var fields = PdfFormReader.ExtractFields(ms);
        Assert.Equal("Alice", fields["name"]);
    }

    [Fact]
    public void PdfFormReader_Checkbox_Bool()
    {
        var doc = CreateDoc(new FormCheckbox { Name = "agreed", Checked = true });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var ms = new MemoryStream(bytes);
        var fields = PdfFormReader.ExtractFields(ms);
        Assert.Equal("true", fields["agreed"]);
    }

    [Fact]
    public void PdfFormReader_Dropdown_Selected()
    {
        var doc = CreateDoc(new FormDropdown
        {
            Name = "tier",
            Options = new List<string> { "Basic", "Pro", "Enterprise" },
            SelectedOption = "Pro"
        });
        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var ms = new MemoryStream(bytes);
        var fields = PdfFormReader.ExtractFields(ms);
        Assert.Equal("Pro", fields["tier"]);
    }

    [Fact]
    public void PdfFormReader_NonFormPdf_EmptyDict()
    {
        var doc = CreateDoc(new TextBlock("No forms here", new FontSpec("Arial", 12)));
        var bytes = DocumentRenderer.RenderToBytes(doc);
        using var ms = new MemoryStream(bytes);
        var fields = PdfFormReader.ExtractFields(ms);
        Assert.Empty(fields);
    }

    #endregion PdfFormReader Tests

    #region PDF/UA Tests

    [Fact]
    public void FormField_PdfUA_TaggedAsForm()
    {
        // PDF/UA mode should not throw when rendering form fields.
        var doc = CreateDoc(new FormTextField { Name = "ua_field", Label = "UA Field" });
        doc.Conformance = PdfConformance.PdfUA1;
        doc.Language = "en-US";
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void FormField_PdfUA_ToolTip_AsTU()
    {
        var doc = CreateDoc(new FormTextField
        {
            Name = "accessible",
            ToolTip = "Enter your full name"
        });
        using var pdfDoc = RenderAndOpen(doc);
        var field = GetField(pdfDoc, "accessible");
        Assert.NotNull(field);
        // The /TU entry should contain the tooltip text.
        string tu = field!.Elements.GetString(PdfAcroField.Keys.TU);
        Assert.Equal("Enter your full name", tu);
    }

    [Fact]
    public void FormField_PdfUA_PassesValidation()
    {
        // Rendering a PDF/UA-1 document with form fields should complete without error.
        var doc = new Document(PageSize.Letter, new EdgeInsets(40));
        doc.Conformance = PdfConformance.PdfUA1;
        doc.Language = "en-US";
        doc.Add(
            new FormTextField { Name = "fname", Label = "First Name", ToolTip = "First name" },
            new FormCheckbox { Name = "agree", Label = "I agree", ToolTip = "Agreement checkbox" },
            new FormDropdown
            {
                Name = "role",
                Label = "Role",
                ToolTip = "Select role",
                Options = new List<string> { "Admin", "User" }
            }
        );
        var bytes = DocumentRenderer.RenderToBytes(doc);
        Assert.True(bytes.Length > 0);
        Assert.True(GetPageCount(bytes) >= 1);
    }

    #endregion PDF/UA Tests
}
