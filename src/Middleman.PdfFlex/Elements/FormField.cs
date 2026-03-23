// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Abstract base class for all interactive form field elements. Provides common
/// properties shared by all AcroForm field types including name, label, tooltip,
/// and validation flags.
/// </summary>
public abstract class FormField : Element
{
    #region Public Properties

    /// <summary>Gets or sets the AcroForm field name. Required and must be unique within the document.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the label text rendered above the field. Null suppresses the label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets whether the field must have a value when submitted.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets whether the field is read-only and does not accept user input.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the tooltip text for screen readers. When null, falls back to
    /// <see cref="Label"/> for the PDF /TU entry.
    /// </summary>
    public string? ToolTip { get; set; }

    /// <summary>Gets or sets the default or pre-filled value of the field.</summary>
    public string? Value { get; set; }

    #endregion Public Properties
}
