// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

//#pragma warning disable 0436

namespace Middleman.PdfFlex
{
    /// <summary>
    /// Version information for the Middleman.PdfFlex assembly.
    /// </summary>
    public static class PdfFlexProductVersionInformation
    {
        // Cannot use const string anymore because GitVersionInformation used static string.
        // The fields are reordered to take initialization chronology into account.

        /// <summary>
        /// The title of the product.
        /// </summary>
        public const string Title = "Middleman.PdfFlex";

        /// <summary>
        /// A characteristic description of the product.
        /// </summary>
        public const string Description = "Declarative PDF layout engine by Middleman Software.";

        /// <summary>
        /// The major version number of the product.
        /// </summary>
        public static readonly string VersionMajor = PdfFlexGitVersionInformation.Major;

        /// <summary>
        /// The minor version number of the product.
        /// </summary>
        public static readonly string VersionMinor = PdfFlexGitVersionInformation.Minor;

        /// <summary>
        /// The patch number of the product.
        /// </summary>
        public static readonly string VersionPatch = PdfFlexGitVersionInformation.Patch;

        /// <summary>
        /// The Version PreRelease string for NuGet.
        /// </summary>
        public static readonly string VersionPreRelease = PdfFlexGitVersionInformation.PreReleaseLabel;
        
        /// <summary>
        /// The PDF creator application information string.
        /// </summary>
        public static readonly string Creator = $"{Title} {PdfFlexGitVersionInformation.InformationalVersion}{Technology}";

        /// <summary>
        /// The PDF producer (created by) information string.
        /// </summary>
        public static readonly string Producer = $"{Title} {PdfFlexGitVersionInformation.InformationalVersion} ({Url})";

        /// <summary>
        /// The full version number.
        /// </summary>
        public static readonly string Version = PdfFlexGitVersionInformation.MajorMinorPatch;

        /// <summary>
        /// The full semantic version number created by GitVersion.
        /// </summary>
        public static readonly string SemanticVersion = PdfFlexGitVersionInformation.SemVer;

        /// <summary>
        /// The home page of this product.
        /// </summary>
        public const string Url = "github.com/middlemansoftware/Middleman.PdfFlex";

        /// <summary>
        /// Unused.
        /// </summary>
        public const string Configuration = "";

        /// <summary>
        /// The company that created/owned the product.
        /// </summary>
        public const string Company = "Middleman Software, Inc.";

        /// <summary>
        /// The name of the product.
        /// </summary>
        public const string Product = "Middleman.PdfFlex";

        /// <summary>
        /// The copyright information.
        /// </summary>
        public const string Copyright = "Copyright (c) Middleman Software, Inc.";

        /// <summary>
        /// The trademark of the product.
        /// </summary>
        public const string Trademark = "Middleman.PdfFlex";

        /// <summary>
        /// Unused.
        /// </summary>
        public const string Culture = "";

        /// <summary>
        /// The technology tag of the product. Always empty for the Core build.
        /// </summary>
        public const string Technology = "";
    }
}
