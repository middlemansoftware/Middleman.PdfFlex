// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A multi-line text input form field. Renders as a taller rectangular input area
/// whose height is determined by the <see cref="Lines"/> property, and creates a
/// PDF text field AcroForm entry with the Multiline flag set.
/// </summary>
public class FormTextArea : FormField
{
    #region Public Properties

    /// <summary>Gets or sets the number of visible lines. Defaults to 3.</summary>
    public int Lines { get; set; } = 3;

    /// <summary>Gets or sets the placeholder text displayed when the field has no value.</summary>
    public string? Placeholder { get; set; }

    #endregion Public Properties
}
