// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Pdf;
using Middleman.PdfFlex.Pdf.AcroForms;
using Middleman.PdfFlex.Pdf.IO;

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Reads AcroForm field values from an existing PDF document. Returns a dictionary
/// mapping field names to their string values.
/// </summary>
public static class PdfFormReader
{
    #region Public Methods

    /// <summary>
    /// Opens an existing PDF file and extracts all AcroForm field name/value pairs.
    /// </summary>
    /// <param name="filePath">The path to the PDF file to read.</param>
    /// <returns>
    /// A dictionary of field names to values. Checkbox fields return "true" or "false".
    /// Returns an empty dictionary if the PDF has no AcroForm.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    public static Dictionary<string, string> ExtractFields(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        using var stream = File.OpenRead(filePath);
        return ExtractFields(stream);
    }

    /// <summary>
    /// Opens an existing PDF from a stream and extracts all AcroForm field name/value pairs.
    /// </summary>
    /// <param name="stream">The stream containing the PDF data.</param>
    /// <returns>
    /// A dictionary of field names to values. Checkbox fields return "true" or "false".
    /// Returns an empty dictionary if the PDF has no AcroForm.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public static Dictionary<string, string> ExtractFields(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        using var pdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        PdfAcroForm acroForm;
        try
        {
            acroForm = pdfDoc.AcroForm;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // No AcroForm dictionary in the document.
            return result;
        }

        if (acroForm.Fields == null)
            return result;

        for (int i = 0; i < acroForm.Fields.Count; i++)
        {
            var field = acroForm.Fields[i];
            string name = field.Name;
            if (string.IsNullOrEmpty(name))
                continue;

            string value = ExtractFieldValue(field);
            result[name] = value;
        }

        return result;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>Extracts the string value from a single AcroForm field.</summary>
    private static string ExtractFieldValue(PdfAcroField field)
    {
        switch (field)
        {
            case PdfCheckBoxField:
            {
                // Checkbox: check the /V value. /Yes means checked, /Off means unchecked.
                var v = field.Value;
                if (v is PdfName name)
                    return name.Value != "/Off" ? "true" : "false";
                string str = v?.ToString() ?? string.Empty;
                return str != "/Off" && !string.IsNullOrEmpty(str) ? "true" : "false";
            }

            case PdfTextField:
            {
                var v = field.Value;
                if (v is PdfString pdfStr)
                    return pdfStr.Value;
                return v?.ToString() ?? string.Empty;
            }

            case PdfComboBoxField:
            {
                var v = field.Value;
                if (v is PdfString pdfStr)
                    return pdfStr.Value;
                return v?.ToString() ?? string.Empty;
            }

            default:
            {
                var v = field.Value;
                if (v is PdfString pdfStr)
                    return pdfStr.Value;
                if (v is PdfName pdfName)
                    return pdfName.Value;
                return v?.ToString() ?? string.Empty;
            }
        }
    }

    #endregion Private Methods
}
