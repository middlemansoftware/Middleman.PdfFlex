// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// A dropdown (combo box) form field. Renders as a rectangular selector with a
/// down-arrow indicator, and creates a PDF combo box AcroForm entry with the
/// specified options.
/// </summary>
public class FormDropdown : FormField
{
    #region Public Properties

    /// <summary>Gets or sets the list of selectable options.</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Gets or sets the currently selected option text.</summary>
    public string? SelectedOption { get; set; }

    /// <summary>
    /// Gets or sets whether the user can type a custom value in addition to
    /// selecting from the options list.
    /// </summary>
    public bool Editable { get; set; }

    #endregion Public Properties
}
