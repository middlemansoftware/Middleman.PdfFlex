// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Defines a column in a <see cref="Table"/> element, including its header text,
/// width, and text alignment.
/// </summary>
public class TableColumn
{
    #region Public Properties

    /// <summary>Gets the header text for this column.</summary>
    public string Header { get; }

    /// <summary>Gets the width specification for this column.</summary>
    public Length Width { get; }

    /// <summary>Gets the text alignment for cells in this column.</summary>
    public TextAlign Align { get; }

    #endregion Public Properties

    #region Constructors

    /// <summary>Creates a table column definition.</summary>
    /// <param name="header">The header text.</param>
    /// <param name="width">The column width specification.</param>
    /// <param name="align">The text alignment for cells in this column. Defaults to <see cref="TextAlign.Left"/>.</param>
    public TableColumn(string header, Length width, TextAlign align = TextAlign.Left)
    {
        ArgumentNullException.ThrowIfNull(header);
        Header = header;
        Width = width;
        Align = align;
    }

    #endregion Constructors
}
