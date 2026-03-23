// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Rendering;

/// <summary>
/// Options that control document rendering behavior. Passed to
/// <see cref="DocumentRenderer.Render(Elements.Document, string, RenderOptions?)"/>
/// overloads to customize output.
/// </summary>
public sealed class RenderOptions
{
    /// <summary>
    /// Gets or sets whether form fields should be flattened into static content.
    /// When true, form field values are rendered as plain text with no interactive
    /// AcroForm dictionary entries in the output PDF. Defaults to false.
    /// </summary>
    public bool FlattenForms { get; set; }
}
