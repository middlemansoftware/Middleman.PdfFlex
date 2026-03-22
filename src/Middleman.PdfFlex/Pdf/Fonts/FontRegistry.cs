// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using Middleman.PdfFlex.Fonts;

namespace Middleman.PdfFlex.Pdf.Fonts;

/// <summary>
/// Manages font registration and provides the active font resolver to PdfFlex.
/// </summary>
/// <remarks>
/// <para>Call <see cref="EnsureInitialized"/> before any PdfFlex font operation. This is done
/// automatically by the document creation pipeline, so most callers never need to invoke it
/// directly.</para>
/// <para>Custom fonts can be registered via <see cref="RegisterFont(string, byte[])"/> or
/// <see cref="RegisterFont(string, string)"/> at any time. Fonts registered before initialization
/// are preserved; fonts registered after are added to the active resolver immediately.</para>
/// </remarks>
public static class FontRegistry
{
    /// <summary>
    /// The singleton resolver instance shared across the application.
    /// </summary>
    private static readonly DefaultFontResolver Resolver = new();

    /// <summary>
    /// Tracks whether <see cref="EnsureInitialized"/> has completed successfully.
    /// </summary>
    private static volatile bool _initialized;

    /// <summary>
    /// Guards one-time PdfFlex registration.
    /// </summary>
    private static readonly object InitLock = new();

    /// <summary>
    /// Initializes the default font resolver and registers it with PdfFlex.
    /// Safe to call multiple times; only the first call has any effect.
    /// </summary>
    /// <remarks>
    /// This method is called automatically on first document creation. Call it explicitly only
    /// if you need font resolution before creating a document (e.g., for font metrics queries).
    /// </remarks>
    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (InitLock)
        {
            if (_initialized)
                return;

            GlobalFontSettings.FontResolver = Resolver;
            _initialized = true;
        }
    }

    /// <summary>
    /// Registers a custom font from raw byte data.
    /// </summary>
    /// <param name="familyName">The font family name to register (e.g., "MyCustomFont").</param>
    /// <param name="fontData">The raw TrueType or OpenType font bytes.</param>
    /// <param name="bold">Whether this font data represents a bold variant.</param>
    /// <param name="italic">Whether this font data represents an italic variant.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="familyName"/> or <paramref name="fontData"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="familyName"/> is empty or whitespace, or when
    /// <paramref name="fontData"/> is empty.
    /// </exception>
    public static void RegisterFont(string familyName, byte[] fontData, bool bold = false, bool italic = false)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        ArgumentNullException.ThrowIfNull(fontData);

        if (string.IsNullOrWhiteSpace(familyName))
            throw new ArgumentException("Family name cannot be empty or whitespace.", nameof(familyName));
        if (fontData.Length == 0)
            throw new ArgumentException("Font data cannot be empty.", nameof(fontData));

        Resolver.RegisterFont(familyName, fontData, bold, italic);
    }

    /// <summary>
    /// Registers a custom font from a file path.
    /// </summary>
    /// <param name="familyName">The font family name to register (e.g., "MyCustomFont").</param>
    /// <param name="filePath">The absolute path to the TrueType or OpenType font file.</param>
    /// <param name="bold">Whether this font file represents a bold variant.</param>
    /// <param name="italic">Whether this font file represents an italic variant.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="familyName"/> or <paramref name="filePath"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="familyName"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the file at <paramref name="filePath"/> does not exist.
    /// </exception>
    public static void RegisterFont(string familyName, string filePath, bool bold = false, bool italic = false)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        ArgumentNullException.ThrowIfNull(filePath);

        if (string.IsNullOrWhiteSpace(familyName))
            throw new ArgumentException("Family name cannot be empty or whitespace.", nameof(familyName));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Font file not found.", filePath);

        byte[] fontData = File.ReadAllBytes(filePath);
        Resolver.RegisterFont(familyName, fontData, bold, italic);
    }
}
