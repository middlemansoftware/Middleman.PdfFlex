// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Fonts;
using Middleman.PdfFlex.Internal;
using Middleman.PdfFlex.Logging;

namespace Middleman.PdfFlex.Diagnostics
{
    /// <summary>
    /// A helper class for central configuration.
    /// </summary>
    public static class PdfFlexCore
    {
        /// <summary>
        /// Resets PdfFlex to a state equivalent to the state after
        /// the assemblies are loaded.
        /// </summary>
        public static void ResetAll()
        {
            Capabilities.ResetAll();
            GlobalFontSettings.ResetAll();
            PdfFlexLogHost.ResetLogging();
            Globals.Global.RecreateGlobals();

            if (FontFactory.HasFontSources)
                throw new InvalidOperationException("Internal error.");
        }

        /// <summary>
        /// Resets the font management equivalent to the state after
        /// the assemblies are loaded.
        /// </summary>
        public static void ResetFontManagement()
        {
            GlobalFontSettings.ResetAll(true);
            Globals.Global.IncrementVersion();
        }
    }
}
