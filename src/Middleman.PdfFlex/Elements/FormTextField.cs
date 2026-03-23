// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A single-line text input form field. Renders as a rectangular input area with
/// optional placeholder text, and creates a PDF text field AcroForm entry.
/// </summary>
public class FormTextField : FormField
{
    #region Public Properties

    /// <summary>Gets or sets the placeholder text displayed when the field has no value.</summary>
    public string? Placeholder { get; set; }

    /// <summary>Gets or sets the maximum number of characters allowed. Null means no limit.</summary>
    public int? MaxLength { get; set; }

    #endregion Public Properties
}
