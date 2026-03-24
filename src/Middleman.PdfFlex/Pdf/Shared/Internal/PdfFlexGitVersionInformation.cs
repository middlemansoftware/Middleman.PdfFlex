// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Reflection;

namespace Middleman.PdfFlex.Internal
{
    /// <summary>
    /// Product version information for Middleman.PdfFlex, derived from the assembly
    /// metadata set by the csproj Version property. This is the single source of truth
    /// for version numbers — change Version in the csproj and everything follows.
    /// </summary>
    public static class PdfFlexGitVersionInformation
    {
        static PdfFlexGitVersionInformation()
        {
            var asm = typeof(PdfFlexGitVersionInformation).Assembly;

            // InformationalVersion comes from <Version> in the csproj. For "1.0.0" it
            // produces "1.0.0+commitsha". For "1.0.0-preview" it produces
            // "1.0.0-preview+commitsha". We strip the +sha suffix for display.
            var infoAttr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            string raw = infoAttr?.InformationalVersion ?? "0.0.0";

            // Strip the +commitsha suffix that the SDK appends by default.
            int plusIdx = raw.IndexOf('+');
            InformationalVersion = plusIdx >= 0 ? raw.Substring(0, plusIdx) : raw;

            // Parse pre-release label: "1.0.0-preview" → "preview", "1.0.0" → ""
            int dashIdx = InformationalVersion.IndexOf('-');
            if (dashIdx >= 0)
            {
                MajorMinorPatch = InformationalVersion.Substring(0, dashIdx);
                PreReleaseLabel = InformationalVersion.Substring(dashIdx + 1);
            }
            else
            {
                MajorMinorPatch = InformationalVersion;
                PreReleaseLabel = "";
            }

            SemVer = InformationalVersion;

            // Parse major.minor.patch components
            var parts = MajorMinorPatch.Split('.');
            Major = parts.Length > 0 ? parts[0] : "0";
            Minor = parts.Length > 1 ? parts[1] : "0";
            Patch = parts.Length > 2 ? parts[2] : "0";
        }

        public static readonly string Major;
        public static readonly string Minor;
        public static readonly string Patch;
        public static readonly string PreReleaseLabel;
        public static readonly string MajorMinorPatch;
        public static readonly string SemVer;
        public static readonly string InformationalVersion;
    }
}
