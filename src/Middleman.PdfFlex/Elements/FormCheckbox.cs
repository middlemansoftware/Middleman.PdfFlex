// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A checkbox form field. Renders as a small square with an optional checkmark,
/// and creates a PDF button field AcroForm entry.
/// </summary>
public class FormCheckbox : FormField
{
    #region Public Properties

    /// <summary>Gets or sets whether the checkbox is checked by default.</summary>
    public bool Checked { get; set; }

    #endregion Public Properties
}
