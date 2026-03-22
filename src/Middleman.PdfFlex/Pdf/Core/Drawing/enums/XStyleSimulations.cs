// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Middleman.PdfFlex.Drawing
{
    /// <summary>
    /// Describes the simulation style of a font.
    /// </summary>
    [Flags]
    public enum XStyleSimulations  // Identical to WpfStyleSimulations.
    {
        /// <summary>
        /// No font style simulation.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bold style simulation.
        /// </summary>
        BoldSimulation = 1,

        /// <summary>
        /// Italic style simulation.
        /// </summary>
        ItalicSimulation = 2,

        /// <summary>
        /// Bold and Italic style simulation.
        /// </summary>
        BoldItalicSimulation = ItalicSimulation | BoldSimulation,
    }
}
