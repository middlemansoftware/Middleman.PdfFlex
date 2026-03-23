// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Globalization;
using Middleman.PdfFlex.Drawing;
using Middleman.PdfFlex.Elements;
using Middleman.PdfFlex.Layout;
using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.AcroForms;
using Middleman.PdfFlex.Styling;
using Middleman.PdfFlex.Pdf.Advanced;
using Middleman.PdfFlex.Pdf.Annotations;
using Middleman.PdfFlex.UniversalAccessibility;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Renders form field elements both visually (label, field rectangle, value/placeholder)
/// and structurally (AcroForm dictionary entries). When <see cref="RenderOptions.FlattenForms"/>
/// is true, only the visual representation is rendered with no interactive form entries.
/// </summary>
internal static class FormFieldRenderer
{
    #region Constants

    /// <summary>Default font size for field content in points.</summary>
    private const double FieldFontSize = 10.0;

    /// <summary>Line height multiplier for calculating field heights.</summary>
    private const double LineHeightMultiplier = 1.4;

    /// <summary>Padding inside field rectangles in points.</summary>
    private const double FieldPadding = 3.0;

    /// <summary>Default field height for single-line text fields in points.</summary>
    private const double DefaultFieldHeight = FieldFontSize * LineHeightMultiplier + FieldPadding * 2;

    /// <summary>Checkbox square side length in points.</summary>
    private const double CheckboxSize = 12.0;

    /// <summary>Gap between label and field in points.</summary>
    private const double LabelGap = 2.0;

    /// <summary>Size of the dropdown arrow indicator in points.</summary>
    private const double ArrowSize = 6.0;

    #endregion Constants

    #region Public Methods

    /// <summary>
    /// Renders a form field element at the position specified by the layout node.
    /// Dispatches to the appropriate field-type-specific renderer.
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="node">The layout node containing position and size information.</param>
    /// <param name="field">The form field element to render.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="FormField.Name"/> is null, empty, or whitespace.
    /// </exception>
    public static void Render(RenderContext ctx, LayoutNode node, FormField field)
    {
        if (string.IsNullOrWhiteSpace(field.Name))
        {
            throw new InvalidOperationException(
                "FormField.Name is required and cannot be empty.");
        }

        var sb = ctx.StructureBuilder;

        // For PDF/UA: wrap the visual representation in a /Div structure element.
        // The /Form structure element (containing only an OBJR child pointing at the
        // widget annotation) is created separately by AssociateWidgetWithStructure
        // and is a sibling of this /Div, both parented under the current structure
        // context. For flattened forms, only the /Div with visual content is emitted.
        if (sb != null)
            sb.BeginBlockLevelElement("Div");

        switch (field)
        {
            case FormTextField textField:
                RenderTextField(ctx, node, textField);
                break;

            case FormTextArea textArea:
                RenderTextArea(ctx, node, textArea);
                break;

            case FormCheckbox checkbox:
                RenderCheckbox(ctx, node, checkbox);
                break;

            case FormDropdown dropdown:
                RenderDropdown(ctx, node, dropdown);
                break;
        }

        if (sb != null)
            sb.End(); // Div
    }

    #endregion Public Methods

    #region Private Methods - TextField

    /// <summary>Renders a single-line text field with label, rectangle, and AcroForm entry.</summary>
    private static void RenderTextField(RenderContext ctx, LayoutNode node, FormTextField field)
    {
        double labelHeight = RenderLabel(ctx, node, field);
        double fieldY = node.Y + labelHeight + (labelHeight > 0 ? LabelGap : 0);
        double fieldHeight = DefaultFieldHeight;
        double fieldWidth = node.Width;

        if (ctx.Options.FlattenForms)
        {
            // Flattened: rectangle and value/placeholder are real visible content.
            RenderFieldRectangle(ctx, node.X, fieldY, fieldWidth, fieldHeight);
            string displayText = field.Value ?? field.Placeholder ?? string.Empty;
            bool isPlaceholder = field.Value == null && field.Placeholder != null;
            RenderFieldText(ctx, node.X, fieldY, fieldWidth, fieldHeight, displayText, isPlaceholder, field);
        }
        else
        {
            // Interactive: the field rectangle and placeholder text are decorative
            // page content behind the widget. Mark as artifact for PDF/UA (clause 7.1).
            RenderDecorativeContent(ctx, () =>
            {
                RenderFieldRectangle(ctx, node.X, fieldY, fieldWidth, fieldHeight);
                if (field.Value == null && field.Placeholder != null)
                    RenderFieldText(ctx, node.X, fieldY, fieldWidth, fieldHeight, field.Placeholder, isPlaceholder: true, field);
            });

            RegisterTextField(ctx, node.X, fieldY, fieldWidth, fieldHeight, field, multiline: false);
        }
    }

    #endregion Private Methods - TextField

    #region Private Methods - TextArea

    /// <summary>Renders a multi-line text area with label, rectangle, and AcroForm entry.</summary>
    private static void RenderTextArea(RenderContext ctx, LayoutNode node, FormTextArea field)
    {
        double labelHeight = RenderLabel(ctx, node, field);
        double fieldY = node.Y + labelHeight + (labelHeight > 0 ? LabelGap : 0);
        double lineHeight = FieldFontSize * LineHeightMultiplier;
        double fieldHeight = lineHeight * field.Lines + FieldPadding * 2;
        double fieldWidth = node.Width;

        if (ctx.Options.FlattenForms)
        {
            // Flattened: rectangle and value/placeholder are real visible content.
            RenderFieldRectangle(ctx, node.X, fieldY, fieldWidth, fieldHeight);
            string displayText = field.Value ?? field.Placeholder ?? string.Empty;
            bool isPlaceholder = field.Value == null && field.Placeholder != null;
            RenderFieldText(ctx, node.X, fieldY, fieldWidth, fieldHeight, displayText, isPlaceholder, field);
        }
        else
        {
            // Interactive: the field rectangle and placeholder text are decorative
            // page content behind the widget. Mark as artifact for PDF/UA (clause 7.1).
            RenderDecorativeContent(ctx, () =>
            {
                RenderFieldRectangle(ctx, node.X, fieldY, fieldWidth, fieldHeight);
                if (field.Value == null && field.Placeholder != null)
                    RenderFieldText(ctx, node.X, fieldY, fieldWidth, fieldHeight, field.Placeholder, isPlaceholder: true, field);
            });

            RegisterTextField(ctx, node.X, fieldY, fieldWidth, fieldHeight, field, multiline: true);
        }
    }

    #endregion Private Methods - TextArea

    #region Private Methods - Checkbox

    /// <summary>Renders a checkbox with optional checkmark, label to the right, and AcroForm entry.</summary>
    private static void RenderCheckbox(RenderContext ctx, LayoutNode node, FormCheckbox field)
    {
        var gfx = ctx.Graphics;
        double x = node.X;
        double y = node.Y;

        if (ctx.Options.FlattenForms)
        {
            // Flattened: the checkbox square, checkmark, and label are real visible content.
            var borderPen = new XPen(XColors.DarkGray, 0.75);
            var bgBrush = XBrushes.White;
            gfx.DrawRectangle(borderPen, bgBrush, x, y, CheckboxSize, CheckboxSize);

            bool isChecked = field.Checked || string.Equals(field.Value, "true", StringComparison.OrdinalIgnoreCase);
            if (isChecked)
                DrawVectorCheckmark(gfx, x, y, CheckboxSize);

            string labelText = field.Label ?? field.Name;
            if (!string.IsNullOrEmpty(labelText))
            {
                var font = ResolveFieldFont(ctx, field);
                var brush = XBrushes.Black;
                double textX = x + CheckboxSize + 4;
                double textY = y + (CheckboxSize - FieldFontSize) / 2;
                gfx.DrawString(labelText, font, brush, textX, textY + FieldFontSize, XStringFormats.Default);
            }
        }
        else
        {
            // Interactive: the checkbox square and checkmark are decorative page content
            // behind the widget. Mark as artifact for PDF/UA (clause 7.1).
            bool isChecked = field.Checked || string.Equals(field.Value, "true", StringComparison.OrdinalIgnoreCase);

            RenderDecorativeContent(ctx, () =>
            {
                var borderPen = new XPen(XColors.DarkGray, 0.75);
                var bgBrush = XBrushes.White;
                gfx.DrawRectangle(borderPen, bgBrush, x, y, CheckboxSize, CheckboxSize);

                if (isChecked)
                    DrawVectorCheckmark(gfx, x, y, CheckboxSize);
            });

            // The label text IS real accessible content -- render it tagged.
            string labelText = field.Label ?? field.Name;
            if (!string.IsNullOrEmpty(labelText))
            {
                var font = ResolveFieldFont(ctx, field);
                var brush = XBrushes.Black;
                double textX = x + CheckboxSize + 4;
                double textY = y + (CheckboxSize - FieldFontSize) / 2;
                gfx.DrawString(labelText, font, brush, textX, textY + FieldFontSize, XStringFormats.Default);
            }

            RegisterCheckboxField(ctx, x, y, field, isChecked);
        }
    }

    #endregion Private Methods - Checkbox

    #region Private Methods - Dropdown

    /// <summary>Renders a dropdown field with label, rectangle, arrow indicator, and AcroForm entry.</summary>
    private static void RenderDropdown(RenderContext ctx, LayoutNode node, FormDropdown field)
    {
        double labelHeight = RenderLabel(ctx, node, field);
        double fieldY = node.Y + labelHeight + (labelHeight > 0 ? LabelGap : 0);
        double fieldHeight = DefaultFieldHeight;
        double fieldWidth = node.Width;

        if (ctx.Options.FlattenForms)
        {
            // Flattened: rectangle, arrow, and value are real visible content.
            RenderFieldRectangle(ctx, node.X, fieldY, fieldWidth, fieldHeight);
            DrawDownArrow(ctx.Graphics, node.X + fieldWidth - ArrowSize - FieldPadding,
                fieldY + (fieldHeight - ArrowSize) / 2, ArrowSize);
            string displayText = field.SelectedOption ?? field.Value ?? string.Empty;
            RenderFieldText(ctx, node.X, fieldY, fieldWidth - ArrowSize - FieldPadding,
                fieldHeight, displayText, isPlaceholder: false, field);
        }
        else
        {
            // Interactive: the field rectangle and arrow are decorative page content
            // behind the widget. Mark as artifact for PDF/UA (clause 7.1).
            RenderDecorativeContent(ctx, () =>
            {
                RenderFieldRectangle(ctx, node.X, fieldY, fieldWidth, fieldHeight);
                DrawDownArrow(ctx.Graphics, node.X + fieldWidth - ArrowSize - FieldPadding,
                    fieldY + (fieldHeight - ArrowSize) / 2, ArrowSize);
            });

            RegisterDropdownField(ctx, node.X, fieldY, fieldWidth, fieldHeight, field);
        }
    }

    #endregion Private Methods - Dropdown

    #region Private Methods - Visual Helpers

    /// <summary>
    /// Renders decorative page content (field rectangles, placeholder text) inside
    /// artifact markers when a structure builder is active. In PDF/UA, interactive
    /// form field visuals are decorative because the widget annotation is the real
    /// accessible element. Without artifact markers, veraPDF reports these drawing
    /// operations as untagged content (clause 7.1, test 3).
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="drawAction">The drawing operations to perform inside the artifact scope.</param>
    private static void RenderDecorativeContent(RenderContext ctx, Action drawAction)
    {
        var sb = ctx.StructureBuilder;
        if (sb != null)
            sb.BeginArtifact();

        drawAction();

        if (sb != null)
            sb.End();
    }

    /// <summary>
    /// Renders the label text above a field. Returns the height consumed by the label,
    /// or 0 if no label is present. Checkbox uses its own label rendering, so this is
    /// skipped for checkboxes.
    /// </summary>
    private static double RenderLabel(RenderContext ctx, LayoutNode node, FormField field)
    {
        // Checkboxes render labels inline to the right of the box.
        if (field is FormCheckbox)
            return 0;

        if (string.IsNullOrEmpty(field.Label))
            return 0;

        var font = ResolveFieldFont(ctx, field, bold: true);
        var brush = XBrushes.Black;
        double labelHeight = FieldFontSize * LineHeightMultiplier;
        ctx.Graphics.DrawString(field.Label, font, brush,
            node.X, node.Y + FieldFontSize, XStringFormats.Default);

        return labelHeight;
    }

    /// <summary>Draws the light gray background and border for a field input area.</summary>
    private static void RenderFieldRectangle(RenderContext ctx, double x, double y, double width, double height)
    {
        var bgBrush = new XSolidBrush(XColor.FromArgb(245, 245, 245));
        var borderPen = new XPen(XColor.FromArgb(180, 180, 180), 0.75);
        ctx.Graphics.DrawRectangle(borderPen, bgBrush, x, y, width, height);
    }

    /// <summary>Draws text inside a field rectangle with padding.</summary>
    /// <remarks>
    /// Multi-line text (containing newlines) is split and drawn line by line.
    /// Newline characters cannot be passed directly to DrawString — they would
    /// be encoded as literal characters in the PDF text operator, referencing
    /// glyph slots that don't exist in the font subset (causing .notdef errors).
    /// </remarks>
    private static void RenderFieldText(RenderContext ctx, double x, double y,
        double width, double height, string text, bool isPlaceholder, FormField field)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var font = ResolveFieldFont(ctx, field);
        var brush = isPlaceholder
            ? new XSolidBrush(XColor.FromArgb(160, 160, 160))
            : (XBrush)XBrushes.Black;

        double textX = x + FieldPadding;
        double textY = y + FieldPadding + FieldFontSize;
        double lineHeight = FieldFontSize * LineHeightMultiplier;

        // Split on newlines and draw each line separately.
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            if (!string.IsNullOrEmpty(line))
                ctx.Graphics.DrawString(line, font, brush, textX, textY + i * lineHeight, XStringFormats.Default);
        }
    }

    /// <summary>
    /// Resolves an <see cref="XFont"/> for rendering form field text by walking the
    /// element's style cascade and falling back to the document's default font family.
    /// Ensures the flattened text is drawn with the same font subset as the rest of
    /// the document (PDF/UA-1 clause 7.21).
    /// </summary>
    /// <param name="ctx">The render context providing the document-level default font family.</param>
    /// <param name="field">The form field element to resolve the font from.</param>
    /// <param name="bold">When true, forces bold weight regardless of the style cascade.</param>
    /// <returns>A configured <see cref="XFont"/> instance.</returns>
    private static XFont ResolveFieldFont(RenderContext ctx, FormField field, bool bold = false)
    {
        string family = ResolveFieldFontFamily(ctx, field);
        var xStyle = bold ? XFontStyleEx.Bold : XFontStyleEx.Regular;
        return new XFont(family, FieldFontSize, xStyle);
    }

    /// <summary>
    /// Resolves the font family name by walking the form field's parent chain.
    /// Falls back to <see cref="RenderContext.DefaultFontFamily"/> (from the document's
    /// default style), then to <see cref="FontSpec.Default"/> if no family is found
    /// anywhere in the cascade.
    /// </summary>
    private static string ResolveFieldFontFamily(RenderContext ctx, FormField field)
    {
        Element? current = field;
        while (current != null)
        {
            if (current.Style?.FontFamily is { } family)
                return family;
            current = current.Parent;
        }
        return ctx.DefaultFontFamily ?? FontSpec.Default.Family;
    }

    /// <summary>Draws a small downward-pointing triangle arrow for dropdown fields.</summary>
    private static void DrawDownArrow(XGraphics gfx, double x, double y, double size)
    {
        var brush = new XSolidBrush(XColor.FromArgb(100, 100, 100));
        double halfSize = size / 2;
        // Simple triangle: top-left, top-right, bottom-center.
        var path = new XGraphicsPath();
        path.AddLine(x, y, x + size, y);
        path.AddLine(x + size, y, x + halfSize, y + size * 0.6);
        path.CloseFigure();
        gfx.DrawPath(brush, path);
    }

    /// <summary>
    /// Draws a vector checkmark inside a checkbox area using two line segments.
    /// Avoids font dependencies by using pure path operators.
    /// </summary>
    private static void DrawVectorCheckmark(XGraphics gfx, double x, double y, double size)
    {
        var checkPen = new XPen(XColors.Black, 1.5);
        // Two-stroke checkmark: short leg down-right, long leg up-right.
        double cx = x + size * 0.2;
        double cy = y + size * 0.55;
        double mx = x + size * 0.4;
        double my = y + size * 0.8;
        double ex = x + size * 0.85;
        double ey = y + size * 0.2;
        gfx.DrawLine(checkPen, cx, cy, mx, my);
        gfx.DrawLine(checkPen, mx, my, ex, ey);
    }

    #endregion Private Methods - Visual Helpers

    #region Private Methods - AcroForm Registration

    /// <summary>
    /// Creates and registers a PdfTextField AcroForm entry for a text field or text area.
    /// After registration, associates the widget annotation with the current /Form
    /// structure element for PDF/UA compliance (clause 7.18.4).
    /// </summary>
    private static void RegisterTextField(RenderContext ctx, double x, double y,
        double width, double height, FormField field, bool multiline)
    {
        var pdfDoc = ctx.PdfDocument;
        var page = ctx.Page;
        if (pdfDoc == null || page == null)
            return;

        // Ensure the AcroForm dictionary exists on the catalog.
        EnsureAcroForm(pdfDoc);

        var pdfField = new PdfTextField(pdfDoc);
        pdfDoc.Internals.AddObject(pdfField);

        // Set field type.
        pdfField.Elements.SetName(PdfAcroField.Keys.FT, "/Tx");

        // Set field name.
        pdfField.Elements.SetString(PdfAcroField.Keys.T, field.Name);

        // Set tooltip (/TU) -- always required for PDF/UA (clause 7.18.1).
        // Fallback chain: ToolTip -> Label -> Name.
        string tooltip = field.ToolTip ?? field.Label ?? field.Name;
        pdfField.Elements.SetString(PdfAcroField.Keys.TU, tooltip);

        // Set value and default value.
        if (!string.IsNullOrEmpty(field.Value))
        {
            pdfField.Elements.SetString(PdfAcroField.Keys.V, field.Value);
            pdfField.Elements.SetString(PdfAcroField.Keys.DV, field.Value);
        }

        // Set flags.
        var flags = PdfAcroFieldFlags.None;
        if (field.ReadOnly) flags |= PdfAcroFieldFlags.ReadOnly;
        if (field.Required) flags |= PdfAcroFieldFlags.Required;
        if (multiline) flags |= PdfAcroFieldFlags.Multiline;
        if (flags != PdfAcroFieldFlags.None)
            pdfField.Elements.SetInteger(PdfAcroField.Keys.Ff, (int)flags);

        // Set max length for FormTextField.
        if (field is FormTextField tf && tf.MaxLength.HasValue)
            pdfField.Elements.SetInteger(PdfTextField.Keys.MaxLen, tf.MaxLength.Value);

        // Set default appearance string. Used by the viewer when regenerating
        // appearances during user interaction (e.g. typing in the field).
        pdfField.Elements.SetString(PdfAcroField.Keys.DA, $"/Helv {FieldFontSize} Tf 0 g");

        // Set widget annotation subtype and rectangle in PDF coordinates.
        pdfField.Elements.SetName(PdfAnnotation.Keys.Subtype, "/Widget");
        SetFieldRect(pdfField, ctx, x, y, width, height);

        // PDF/A requires all annotations to have the Print flag set (/F bit 3 = value 4).
        pdfField.Elements.SetInteger(PdfAnnotation.Keys.F, (int)PdfAnnotationFlags.Print);

        // Set /MK (appearance characteristics) with an opaque white background.
        // When the viewer regenerates the appearance during user interaction, the
        // opaque background covers any placeholder text drawn as page content.
        SetOpaqueBackground(pdfDoc, pdfField);

        // Build an explicit /AP /N appearance stream. For empty fields this is
        // transparent (no fill), allowing placeholder text on the page to show
        // through. For pre-filled fields, the value is rendered on an opaque
        // white background. We do NOT set NeedAppearances so the viewer preserves
        // these explicit appearances until the user interacts with the field.
        BuildTextFieldAppearance(pdfDoc, pdfField, width, height, field.Value);

        // Add the field to the AcroForm Fields array.
        pdfDoc.AcroForm.Fields.Elements.Add(pdfField.Reference);

        // Add widget annotation reference to the page's /Annots array.
        // PdfAcroField is not a PdfAnnotation subclass, so we add the reference directly.
        page.Annotations.Elements.Add(pdfField.Reference);

        // PDF/UA: associate the widget annotation with the /Form structure element.
        AssociateWidgetWithStructure(ctx, pdfField, page);
    }

    /// <summary>
    /// Creates and registers a PdfCheckBoxField AcroForm entry. Uses vector path
    /// appearance streams instead of ZapfDingbats to avoid font embedding issues
    /// (PDF/UA-1 clauses 7.21.4.1, 7.21.7).
    /// </summary>
    private static void RegisterCheckboxField(RenderContext ctx, double x, double y,
        FormCheckbox field, bool isChecked)
    {
        var pdfDoc = ctx.PdfDocument;
        var page = ctx.Page;
        if (pdfDoc == null || page == null)
            return;

        EnsureAcroForm(pdfDoc);

        var pdfField = new PdfCheckBoxField(pdfDoc);
        pdfDoc.Internals.AddObject(pdfField);

        // Set field type.
        pdfField.Elements.SetName(PdfAcroField.Keys.FT, "/Btn");

        // Set field name.
        pdfField.Elements.SetString(PdfAcroField.Keys.T, field.Name);

        // Set tooltip -- always required for PDF/UA (clause 7.18.1).
        string tooltip = field.ToolTip ?? field.Label ?? field.Name;
        pdfField.Elements.SetString(PdfAcroField.Keys.TU, tooltip);

        // Set value -- /Yes for checked, /Off for unchecked.
        string valueName = isChecked ? "/Yes" : "/Off";
        pdfField.Elements.SetName(PdfAcroField.Keys.V, valueName);
        pdfField.Elements.SetName(PdfAcroField.Keys.DV, valueName);
        pdfField.Elements.SetName(PdfAnnotation.Keys.AS, valueName);

        // Set flags.
        var flags = PdfAcroFieldFlags.None;
        if (field.ReadOnly) flags |= PdfAcroFieldFlags.ReadOnly;
        if (field.Required) flags |= PdfAcroFieldFlags.Required;
        if (flags != PdfAcroFieldFlags.None)
            pdfField.Elements.SetInteger(PdfAcroField.Keys.Ff, (int)flags);

        // Set default appearance string (not used for checkboxes with AP, but required).
        pdfField.Elements.SetString(PdfAcroField.Keys.DA, "0 g");

        // Set widget annotation.
        pdfField.Elements.SetName(PdfAnnotation.Keys.Subtype, "/Widget");
        SetFieldRect(pdfField, ctx, x, y, CheckboxSize, CheckboxSize);

        // PDF/A requires all annotations to have the Print flag set (/F bit 3 = value 4).
        pdfField.Elements.SetInteger(PdfAnnotation.Keys.F, (int)PdfAnnotationFlags.Print);

        // Build vector-path appearance dictionaries (no font dependencies).
        BuildCheckboxAppearance(pdfDoc, pdfField);

        pdfDoc.AcroForm.Fields.Elements.Add(pdfField.Reference);
        page.Annotations.Elements.Add(pdfField.Reference);

        // PDF/UA: associate the widget annotation with the /Form structure element.
        AssociateWidgetWithStructure(ctx, pdfField, page);
    }

    /// <summary>
    /// Creates and registers a PdfComboBoxField AcroForm entry for a dropdown.
    /// </summary>
    private static void RegisterDropdownField(RenderContext ctx, double x, double y,
        double width, double height, FormDropdown field)
    {
        var pdfDoc = ctx.PdfDocument;
        var page = ctx.Page;
        if (pdfDoc == null || page == null)
            return;

        EnsureAcroForm(pdfDoc);

        var pdfField = new PdfComboBoxField(pdfDoc);
        pdfDoc.Internals.AddObject(pdfField);

        // Set field type.
        pdfField.Elements.SetName(PdfAcroField.Keys.FT, "/Ch");

        // Set field name.
        pdfField.Elements.SetString(PdfAcroField.Keys.T, field.Name);

        // Set tooltip -- always required for PDF/UA (clause 7.18.1).
        string tooltip = field.ToolTip ?? field.Label ?? field.Name;
        pdfField.Elements.SetString(PdfAcroField.Keys.TU, tooltip);

        // Set options array.
        var optArray = new PdfArray(pdfDoc);
        foreach (string option in field.Options)
            optArray.Elements.Add(new PdfString(option));
        pdfField.Elements.SetObject(PdfChoiceField.Keys.Opt, optArray);

        // Set value.
        string selectedValue = field.SelectedOption ?? field.Value ?? string.Empty;
        if (!string.IsNullOrEmpty(selectedValue))
        {
            pdfField.Elements.SetString(PdfAcroField.Keys.V, selectedValue);
            pdfField.Elements.SetString(PdfAcroField.Keys.DV, selectedValue);
        }

        // Set flags -- Combo bit must be set for combo boxes.
        var flags = PdfAcroFieldFlags.Combo;
        if (field.Editable) flags |= PdfAcroFieldFlags.Edit;
        if (field.ReadOnly) flags |= PdfAcroFieldFlags.ReadOnly;
        if (field.Required) flags |= PdfAcroFieldFlags.Required;
        pdfField.Elements.SetInteger(PdfAcroField.Keys.Ff, (int)flags);

        // Set default appearance string.
        pdfField.Elements.SetString(PdfAcroField.Keys.DA, $"/Helv {FieldFontSize} Tf 0 g");

        // Set widget annotation.
        pdfField.Elements.SetName(PdfAnnotation.Keys.Subtype, "/Widget");
        SetFieldRect(pdfField, ctx, x, y, width, height);

        // PDF/A requires all annotations to have the Print flag set (/F bit 3 = value 4).
        pdfField.Elements.SetInteger(PdfAnnotation.Keys.F, (int)PdfAnnotationFlags.Print);

        // Set /MK with opaque background so viewer-generated appearances cover placeholders.
        SetOpaqueBackground(pdfDoc, pdfField);

        // Build explicit appearance stream for the selected value.
        BuildTextFieldAppearance(pdfDoc, pdfField, width, height, selectedValue);

        pdfDoc.AcroForm.Fields.Elements.Add(pdfField.Reference);
        page.Annotations.Elements.Add(pdfField.Reference);

        // PDF/UA: associate the widget annotation with the /Form structure element.
        AssociateWidgetWithStructure(ctx, pdfField, page);
    }

    /// <summary>
    /// Associates a widget annotation with the current /Form structure element in the
    /// structure tree. Creates an OBJR child, sets /Tabs /S on the page, and registers
    /// the widget in the parent tree (PDF/UA-1 clauses 7.18.3, 7.18.4).
    /// No-op when PDF/UA tagging is not active.
    /// </summary>
    private static void AssociateWidgetWithStructure(RenderContext ctx, PdfAcroField widget, PdfPage page)
    {
        ctx.StructureBuilder?.AssociateFormFieldWidget(widget, page);
    }

    /// <summary>
    /// Ensures the AcroForm dictionary exists on the document catalog. Creates one
    /// if it does not exist.
    /// </summary>
    private static void EnsureAcroForm(PdfDocument pdfDoc)
    {
        var catalog = pdfDoc.Internals.Catalog;
        if (catalog.Elements.GetObject(PdfCatalog.Keys.AcroForm) == null)
            catalog.Elements.Add(PdfCatalog.Keys.AcroForm, new PdfAcroForm(pdfDoc));

        // Ensure the /Fields array exists on the AcroForm.
        if (catalog.AcroForm.Elements.GetValue(PdfAcroForm.Keys.Fields) == null)
        {
            catalog.AcroForm.Elements.SetValue(PdfAcroForm.Keys.Fields,
                new PdfAcroField.PdfAcroFieldCollection(new PdfArray()));
        }
    }

    /// <summary>
    /// Sets the annotation rectangle on a field dictionary, converting from XGraphics
    /// coordinates (top-left origin) to PDF coordinates (bottom-left origin).
    /// </summary>
    private static void SetFieldRect(PdfAcroField field, RenderContext ctx,
        double x, double y, double width, double height)
    {
        double pageHeight = ctx.PageHeight;
        if (pageHeight <= 0 && ctx.Page != null)
            pageHeight = ctx.Page.Height.Point;

        // Convert from top-left origin to bottom-left origin.
        double pdfY1 = pageHeight - y - height;
        double pdfY2 = pageHeight - y;

        var rect = new PdfRectangle(x, pdfY1, x + width, pdfY2);
        field.Elements.SetRectangle(PdfAnnotation.Keys.Rect, rect);
    }

    /// <summary>
    /// Sets /MK (appearance characteristics) with a white background color on a widget
    /// annotation. When the PDF viewer regenerates the field's appearance during user
    /// interaction, the /BG array in /MK produces an opaque white fill that covers any
    /// placeholder text drawn as page content behind the widget.
    /// </summary>
    private static void SetOpaqueBackground(PdfDocument pdfDoc, PdfAcroField field)
    {
        var mk = new PdfDictionary(pdfDoc);
        // /BG array: [1 1 1] = white in DeviceRGB.
        mk.Elements["/BG"] = new PdfLiteral("[1 1 1]");
        field.Elements["/MK"] = mk;
    }

    /// <summary>
    /// Builds an explicit /AP /N (normal appearance) form XObject for a text field or
    /// dropdown. When <paramref name="value"/> is null or empty, the appearance is a
    /// transparent empty form XObject, allowing placeholder text drawn on the page to
    /// show through. When <paramref name="value"/> has content, the appearance renders
    /// the text on an opaque white background using the standard /Helv base font.
    /// </summary>
    /// <remarks>
    /// We build explicit appearances instead of relying on /NeedAppearances so the viewer
    /// preserves our transparent empty appearances for placeholder fields. Without this,
    /// /NeedAppearances causes the viewer to regenerate all appearances on open (using
    /// /MK /BG white), which would hide placeholder text before the user interacts.
    /// </remarks>
    private static void BuildTextFieldAppearance(PdfDocument pdfDoc, PdfAcroField field,
        double width, double height, string? value)
    {
        string Fmt(double v) => v.ToString("F2", CultureInfo.InvariantCulture);

        string wStr = Fmt(width);
        string hStr = Fmt(height);

        var form = new PdfDictionary(pdfDoc);
        pdfDoc.Internals.AddObject(form);
        form.Elements.SetName("/Type", "/XObject");
        form.Elements.SetName("/Subtype", "/Form");
        form.Elements["/BBox"] = new PdfLiteral($"[0 0 {wStr} {hStr}]");

        if (!string.IsNullOrEmpty(value))
        {
            // Opaque white background + value text using Helvetica base font.
            // The text baseline is positioned with padding from the bottom.
            double textX = FieldPadding;
            double textY = height - FieldPadding - FieldFontSize;
            string fontSize = Fmt(FieldFontSize);

            // Escape parentheses and backslashes in the value for PDF string literal.
            string escaped = value
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            string stream =
                "q\n" +
                // White background fill.
                $"1 1 1 rg 0 0 {wStr} {hStr} re f\n" +
                // Black text.
                "BT\n" +
                $"/Helv {fontSize} Tf\n" +
                "0 g\n" +
                $"{Fmt(textX)} {Fmt(textY)} Td\n" +
                $"({escaped}) Tj\n" +
                "ET\n" +
                "Q\n";

            byte[] streamBytes = System.Text.Encoding.ASCII.GetBytes(stream);
            form.CreateStream(streamBytes);

            // Add /Helv font reference in the form XObject's resource dictionary.
            var resources = new PdfDictionary(pdfDoc);
            var fontDict = new PdfDictionary(pdfDoc);
            var helvetica = new PdfDictionary(pdfDoc);
            helvetica.Elements.SetName("/Type", "/Font");
            helvetica.Elements.SetName("/Subtype", "/Type1");
            helvetica.Elements.SetName("/BaseFont", "/Helvetica");
            fontDict.Elements["/Helv"] = helvetica;
            resources.Elements["/Font"] = fontDict;
            form.Elements["/Resources"] = resources;
        }
        else
        {
            // Empty / transparent appearance -- no fill, no text.
            // Placeholder text on the page shows through.
            form.CreateStream(Array.Empty<byte>());
        }

        // Set /AP /N to the form XObject.
        var ap = new PdfDictionary(pdfDoc);
        ap.Elements["/N"] = form.Reference;
        field.Elements[PdfAnnotation.Keys.AP] = ap;
    }

    /// <summary>
    /// Builds /AP appearance dictionaries for a checkbox using vector path operators
    /// instead of ZapfDingbats text. The /Yes appearance draws a checkmark as two
    /// line segments; the /Off appearance is empty. This avoids font embedding issues
    /// (PDF/UA-1 clauses 7.21.4.1, 7.21.7).
    /// </summary>
    private static void BuildCheckboxAppearance(PdfDocument pdfDoc, PdfCheckBoxField field)
    {
        // Create normal appearance dictionary with /Yes and /Off streams.
        var ap = new PdfDictionary(pdfDoc);
        var normalDict = new PdfDictionary(pdfDoc);

        string sizeStr = CheckboxSize.ToString(CultureInfo.InvariantCulture);

        // /Off appearance -- empty.
        var offForm = new PdfDictionary(pdfDoc);
        pdfDoc.Internals.AddObject(offForm);
        offForm.Elements.SetName("/Type", "/XObject");
        offForm.Elements.SetName("/Subtype", "/Form");
        offForm.Elements["/BBox"] = new PdfLiteral($"[0 0 {sizeStr} {sizeStr}]");
        offForm.CreateStream(Array.Empty<byte>());
        normalDict.Elements["/Off"] = offForm.Reference;

        // /Yes appearance -- vector checkmark (no fonts required).
        var yesForm = new PdfDictionary(pdfDoc);
        pdfDoc.Internals.AddObject(yesForm);
        yesForm.Elements.SetName("/Type", "/XObject");
        yesForm.Elements.SetName("/Subtype", "/Form");
        yesForm.Elements["/BBox"] = new PdfLiteral($"[0 0 {sizeStr} {sizeStr}]");

        // Checkmark path: two line segments forming a checkmark.
        // Coordinates are in form space (0,0 = bottom-left, size,size = top-right).
        double cx = CheckboxSize * 0.2;
        double cy = CheckboxSize * 0.45;  // bottom-left origin: 1 - 0.55
        double mx = CheckboxSize * 0.4;
        double my = CheckboxSize * 0.2;   // bottom-left origin: 1 - 0.8
        double ex = CheckboxSize * 0.85;
        double ey = CheckboxSize * 0.8;   // bottom-left origin: 1 - 0.2

        string Fmt(double v) => v.ToString("F2", CultureInfo.InvariantCulture);

        string checkStream =
            "q\n" +
            "1.5 w\n" +
            "0 0 0 RG\n" +
            $"{Fmt(cx)} {Fmt(cy)} m\n" +
            $"{Fmt(mx)} {Fmt(my)} l\n" +
            $"{Fmt(ex)} {Fmt(ey)} l\n" +
            "S\n" +
            "Q\n";
        byte[] streamBytes = System.Text.Encoding.ASCII.GetBytes(checkStream);
        yesForm.CreateStream(streamBytes);

        normalDict.Elements["/Yes"] = yesForm.Reference;
        ap.Elements["/N"] = normalDict;
        field.Elements[PdfAnnotation.Keys.AP] = ap;
    }

    #endregion Private Methods - AcroForm Registration

    #region Internal Helpers

    /// <summary>
    /// No-op flags value used when no flags are set.
    /// PdfAcroFieldFlags does not define a None value, so we use 0 cast.
    /// </summary>
    private static class PdfAcroFieldFlags
    {
        public const Pdf.AcroForms.PdfAcroFieldFlags None = 0;
        public const Pdf.AcroForms.PdfAcroFieldFlags ReadOnly = Pdf.AcroForms.PdfAcroFieldFlags.ReadOnly;
        public const Pdf.AcroForms.PdfAcroFieldFlags Required = Pdf.AcroForms.PdfAcroFieldFlags.Required;
        public const Pdf.AcroForms.PdfAcroFieldFlags Multiline = Pdf.AcroForms.PdfAcroFieldFlags.Multiline;
        public const Pdf.AcroForms.PdfAcroFieldFlags Combo = Pdf.AcroForms.PdfAcroFieldFlags.Combo;
        public const Pdf.AcroForms.PdfAcroFieldFlags Edit = Pdf.AcroForms.PdfAcroFieldFlags.Edit;
    }

    #endregion Internal Helpers
}
