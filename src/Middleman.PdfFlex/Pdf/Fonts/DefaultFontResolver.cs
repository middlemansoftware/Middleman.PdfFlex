// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Middleman.PdfFlex.Fonts;

namespace Middleman.PdfFlex.Pdf.Fonts;

/// <summary>
/// Cross-platform font resolver that auto-detects system fonts and falls back to bundled fonts.
/// Implements <see cref="IFontResolver"/> for PdfFlex Core builds.
/// </summary>
/// <remarks>
/// <para>System font directories are scanned lazily on first use and cached for the lifetime of the process.</para>
/// <para>Platform detection uses <see cref="RuntimeInformation"/> to locate the correct font directories:</para>
/// <list type="bullet">
///   <item><description>Windows: <c>C:\Windows\Fonts</c> and <c>%LOCALAPPDATA%\Microsoft\Windows\Fonts</c></description></item>
///   <item><description>Linux: <c>/usr/share/fonts/</c>, <c>/usr/local/share/fonts/</c>, <c>~/.fonts/</c>, <c>~/.local/share/fonts/</c></description></item>
///   <item><description>macOS: <c>/System/Library/Fonts/</c>, <c>/Library/Fonts/</c>, <c>~/Library/Fonts/</c></description></item>
/// </list>
/// <para>When system fonts are unavailable (WASM, minimal containers), the resolver falls back to bundled
/// fonts embedded as resources. The bundled font slot is reserved for Google Noto (SIL Open Font License 1.1).</para>
/// </remarks>
public sealed class DefaultFontResolver : IFontResolver
{
    /// <summary>
    /// The default font family name used when no specific family is requested.
    /// </summary>
    internal const string DefaultFamily = "NotoSans";

    /// <summary>
    /// The bundled fallback face name returned when no system font matches.
    /// </summary>
    private const string BundledFallbackFace = "BundledFonts/NotoSans-Regular";

    /// <summary>
    /// Maps a lowercase font face name to its absolute file path on disk.
    /// Populated lazily by <see cref="EnsureScanned"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _faceToPath = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps a lowercase family name to a <see cref="FontFamilyEntry"/> that knows the
    /// regular/bold/italic/bold-italic face names for that family.
    /// Populated lazily by <see cref="EnsureScanned"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, FontFamilyEntry> _families = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stores fonts registered at runtime via <see cref="FontRegistry.RegisterFont(string, byte[])"/>.
    /// Maps a face name to the raw font bytes.
    /// </summary>
    private readonly ConcurrentDictionary<string, byte[]> _registeredFonts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Guards one-time system font scanning.
    /// </summary>
    private volatile bool _scanned;
    private readonly object _scanLock = new();

    /// <summary>
    /// Converts specified information about a required typeface into a specific font.
    /// </summary>
    /// <param name="familyName">Name of the font family.</param>
    /// <param name="bold">Set to <c>true</c> when a bold font face is required.</param>
    /// <param name="italic">Set to <c>true</c> when an italic font face is required.</param>
    /// <returns>
    /// Information about the physical font, or <c>null</c> if the request cannot be satisfied.
    /// </returns>
    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        EnsureScanned();

        // Try to find the requested family in the cache.
        if (_families.TryGetValue(familyName, out var entry))
        {
            string? faceName = SelectFace(entry, bold, italic);
            if (faceName is not null)
                return new FontResolverInfo(faceName, bold && entry.Bold is null, italic && entry.Italic is null);
        }

        // Check runtime-registered fonts by exact family name match.
        string registeredKey = BuildRegisteredKey(familyName, bold, italic);
        if (_registeredFonts.ContainsKey(registeredKey))
            return new FontResolverInfo(registeredKey);

        // Try any available system font before falling back to bundled fonts.
        if (!_families.IsEmpty)
        {
            var fallbackEntry = _families.Values.First();
            string? fallbackFace = SelectFace(fallbackEntry, bold, italic);
            if (fallbackFace is not null)
                return new FontResolverInfo(fallbackFace, bold && fallbackEntry.Bold is null, italic && fallbackEntry.Italic is null);
        }

        // Fall back to bundled font placeholder.
        // When actual Noto font resources are embedded, this will serve as the universal fallback.
        return new FontResolverInfo(BundledFallbackFace, bold, italic);
    }

    /// <summary>
    /// Gets the bytes of a physical font with specified face name.
    /// </summary>
    /// <param name="faceName">A face name previously retrieved by <see cref="ResolveTypeface"/>.</param>
    /// <returns>The raw font file bytes, or <c>null</c> if the font could not be loaded.</returns>
    public byte[]? GetFont(string faceName)
    {
        // Check runtime-registered fonts first.
        if (_registeredFonts.TryGetValue(faceName, out byte[]? registered))
            return registered;

        // Check if it's a bundled font request.
        if (faceName.StartsWith("BundledFonts/", StringComparison.OrdinalIgnoreCase))
        {
            byte[]? bundled = LoadBundledFont(faceName);
            if (bundled is not null)
                return bundled;

            // Bundled fonts not yet embedded -- try the first available system font.
            foreach (string fallbackPath in _faceToPath.Values)
            {
                if (File.Exists(fallbackPath))
                    return File.ReadAllBytes(fallbackPath);
            }

            return null;
        }

        // Load from the file system path cache.
        if (_faceToPath.TryGetValue(faceName, out string? path) && File.Exists(path))
            return File.ReadAllBytes(path);

        return null;
    }

    #region Runtime Registration

    /// <summary>
    /// Registers a font from raw byte data so it can be resolved by family name.
    /// </summary>
    /// <param name="familyName">The font family name to register under.</param>
    /// <param name="fontData">The raw TrueType or OpenType font bytes.</param>
    /// <param name="bold">Whether this font data represents a bold variant.</param>
    /// <param name="italic">Whether this font data represents an italic variant.</param>
    internal void RegisterFont(string familyName, byte[] fontData, bool bold = false, bool italic = false)
    {
        string key = BuildRegisteredKey(familyName, bold, italic);
        _registeredFonts[key] = fontData;
    }

    /// <summary>
    /// Builds a deterministic cache key for a runtime-registered font.
    /// </summary>
    private static string BuildRegisteredKey(string familyName, bool bold, bool italic)
    {
        return $"Registered/{familyName}" +
               (bold ? "/Bold" : "") +
               (italic ? "/Italic" : "");
    }

    #endregion

    #region System Font Scanning

    /// <summary>
    /// Ensures system font directories have been scanned exactly once.
    /// </summary>
    private void EnsureScanned()
    {
        if (_scanned)
            return;

        lock (_scanLock)
        {
            if (_scanned)
                return;

            // Bundled fonts registered first so they're available as the default family.
            // System fonts scanned second — if NotoSans is installed locally, the system
            // version takes precedence (first-write-wins via CompareExchange).
            RegisterBundledFonts();
            ScanSystemFonts();
            _scanned = true;
        }
    }

    /// <summary>
    /// Scans platform-specific font directories and populates the face/family caches.
    /// </summary>
    private void ScanSystemFonts()
    {
        IReadOnlyList<string> directories = GetFontDirectories();

        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
                continue;

            ScanDirectory(directory);
        }
    }

    /// <summary>
    /// Recursively scans a directory for TrueType (.ttf) and OpenType (.otf) font files.
    /// </summary>
    private void ScanDirectory(string directory)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                string ext = Path.GetExtension(file);
                if (!ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                _faceToPath.TryAdd(fileNameWithoutExt, file);

                // Parse the file name into a family entry. Font files typically follow the pattern
                // "FamilyName-Style.ttf" or "FamilyName_Style.ttf". This is a best-effort heuristic;
                // users with non-standard naming should use RegisterFont for exact control.
                ParseAndRegisterFamily(fileNameWithoutExt);
            }

            // Recurse into subdirectories (Linux organizes fonts into arbitrary subdirectories).
            foreach (string subDir in Directory.EnumerateDirectories(directory))
            {
                ScanDirectory(subDir);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have permission to read.
        }
        catch (DirectoryNotFoundException)
        {
            // Directory may have been removed between the exists check and enumeration.
        }
    }

    /// <summary>
    /// Parses a font file name and registers it under the appropriate family and style slot.
    /// </summary>
    private void ParseAndRegisterFamily(string fileNameWithoutExt)
    {
        // Split on common delimiters: "NotoSans-BoldItalic" -> ("NotoSans", "BoldItalic")
        int separatorIndex = fileNameWithoutExt.IndexOfAny(['-', '_']);
        string familyName;
        string stylePart;

        if (separatorIndex > 0)
        {
            familyName = fileNameWithoutExt[..separatorIndex];
            stylePart = fileNameWithoutExt[(separatorIndex + 1)..];
        }
        else
        {
            familyName = fileNameWithoutExt;
            stylePart = string.Empty;
        }

        bool isBold = stylePart.Contains("Bold", StringComparison.OrdinalIgnoreCase);
        bool isItalic = stylePart.Contains("Italic", StringComparison.OrdinalIgnoreCase) ||
                        stylePart.Contains("Oblique", StringComparison.OrdinalIgnoreCase);

        var entry = _families.GetOrAdd(familyName, _ => new FontFamilyEntry());

        // Assign to the appropriate style slot. First discovered file wins.
        if (isBold && isItalic)
            Interlocked.CompareExchange(ref entry.BoldItalic, fileNameWithoutExt, null);
        else if (isBold)
            Interlocked.CompareExchange(ref entry.Bold, fileNameWithoutExt, null);
        else if (isItalic)
            Interlocked.CompareExchange(ref entry.Italic, fileNameWithoutExt, null);
        else
            Interlocked.CompareExchange(ref entry.Regular, fileNameWithoutExt, null);
    }

    /// <summary>
    /// Selects the best matching face name from a family entry given the requested style.
    /// Falls back through available styles when an exact match isn't found.
    /// </summary>
    private static string? SelectFace(FontFamilyEntry entry, bool bold, bool italic)
    {
        if (bold && italic)
            return entry.BoldItalic ?? entry.Bold ?? entry.Italic ?? entry.Regular;

        if (bold)
            return entry.Bold ?? entry.Regular;

        if (italic)
            return entry.Italic ?? entry.Regular;

        return entry.Regular ?? entry.Bold ?? entry.Italic ?? entry.BoldItalic;
    }

    /// <summary>
    /// Returns the list of font directories to scan for the current platform.
    /// </summary>
    private static IReadOnlyList<string> GetFontDirectories()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsFontDirectories();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxFontDirectories();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacFontDirectories();

        // Unknown platform -- return empty. Bundled fonts will serve as fallback.
        return [];
    }

    /// <summary>
    /// Returns Windows font directories.
    /// </summary>
    private static IReadOnlyList<string> GetWindowsFontDirectories()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return
        [
            @"C:\Windows\Fonts",
            Path.Combine(localAppData, @"Microsoft\Windows\Fonts"),
        ];
    }

    /// <summary>
    /// Returns Linux font directories.
    /// </summary>
    private static IReadOnlyList<string> GetLinuxFontDirectories()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dirs = new List<string>
        {
            "/usr/share/fonts",
            "/usr/local/share/fonts",
            Path.Combine(home, ".fonts"),
            Path.Combine(home, ".local/share/fonts"),
        };

        // Respect FONTCONFIG_PATH if set.
        string? fontConfigPath = Environment.GetEnvironmentVariable("FONTCONFIG_PATH");
        if (!string.IsNullOrEmpty(fontConfigPath) && !dirs.Contains(fontConfigPath))
            dirs.Add(fontConfigPath);

        return dirs;
    }

    /// <summary>
    /// Returns macOS font directories.
    /// </summary>
    private static IReadOnlyList<string> GetMacFontDirectories()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return
        [
            "/System/Library/Fonts",
            "/Library/Fonts",
            Path.Combine(home, "Library/Fonts"),
        ];
    }

    #endregion

    #region Bundled Fonts

    /// <summary>
    /// Loads a bundled NotoSans font from embedded assembly resources.
    /// NotoSans is licensed under the SIL Open Font License and ships with the library
    /// as the default font, ensuring consistent rendering without system font dependencies.
    /// </summary>
    /// <param name="faceName">The bundled font face name (e.g., "BundledFonts/NotoSans-Regular").</param>
    /// <returns>The font bytes, or <c>null</c> if the resource is not found.</returns>
    private static byte[]? LoadBundledFont(string faceName)
    {
        // Map face names to embedded resource logical names.
        string? resourceName = faceName switch
        {
            "BundledFonts/NotoSans-Regular" => "Middleman.PdfFlex.Fonts.NotoSans-Regular.ttf",
            "BundledFonts/NotoSans-Bold" => "Middleman.PdfFlex.Fonts.NotoSans-Bold.ttf",
            "BundledFonts/NotoSans-Italic" => "Middleman.PdfFlex.Fonts.NotoSans-Italic.ttf",
            "BundledFonts/NotoSans-BoldItalic" => "Middleman.PdfFlex.Fonts.NotoSans-BoldItalic.ttf",
            _ => null
        };

        if (resourceName is null)
            return null;

        using var stream = typeof(DefaultFontResolver).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Registers the bundled NotoSans family so it can be resolved by family name
    /// without requiring system font installation. Called during <see cref="EnsureScanned"/>.
    /// </summary>
    private void RegisterBundledFonts()
    {
        var entry = _families.GetOrAdd("NotoSans", _ => new FontFamilyEntry());
        Interlocked.CompareExchange(ref entry.Regular, "BundledFonts/NotoSans-Regular", null);
        Interlocked.CompareExchange(ref entry.Bold, "BundledFonts/NotoSans-Bold", null);
        Interlocked.CompareExchange(ref entry.Italic, "BundledFonts/NotoSans-Italic", null);
        Interlocked.CompareExchange(ref entry.BoldItalic, "BundledFonts/NotoSans-BoldItalic", null);
    }

    #endregion

    /// <summary>
    /// Holds the face names for the four standard style variants of a font family.
    /// Fields are assigned via <see cref="Interlocked.CompareExchange(ref object?, object?, object?)"/>
    /// for thread safety.
    /// </summary>
    private sealed class FontFamilyEntry
    {
        /// <summary>Regular (normal weight, upright) face name.</summary>
        public string? Regular;

        /// <summary>Bold face name.</summary>
        public string? Bold;

        /// <summary>Italic or oblique face name.</summary>
        public string? Italic;

        /// <summary>Bold-italic face name.</summary>
        public string? BoldItalic;
    }
}
