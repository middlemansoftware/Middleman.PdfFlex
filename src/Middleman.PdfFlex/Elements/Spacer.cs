// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Styling;

namespace Middleman.PdfFlex.Elements;

/// <summary>
/// Flex-grow filler element with zero intrinsic size. Expands to fill available space
/// based on its flex-grow factor, pushing adjacent siblings apart within a flex container.
/// </summary>
public class Spacer : Element
{
    #region Constructors

    /// <summary>Creates a spacer that grows to fill available space.</summary>
    /// <param name="flexGrow">The flex-grow factor. Defaults to 1.</param>
    public Spacer(float flexGrow = 1)
    {
        Style = new Style { FlexGrow = flexGrow };
    }

    #endregion Constructors
}
