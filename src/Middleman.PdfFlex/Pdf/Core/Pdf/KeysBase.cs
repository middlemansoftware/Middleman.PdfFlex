// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Diagnostics.CodeAnalysis;

namespace Middleman.PdfFlex.Pdf
{
    /// <summary>
    /// Base class for all dictionary Keys classes.
    /// </summary>
    public class KeysBase
    {
        internal static DictionaryMeta CreateMeta(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            Type type)
                => new(type);

        /// <summary>
        /// Creates the DictionaryMeta with the specified default type to return in DictionaryElements.GetValue
        /// if the key is not defined.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultContentKeyType">Default type of the content key.</param>
        /// <param name="defaultContentType">Default type of the content.</param>
        internal static DictionaryMeta CreateMeta(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            Type type,
            KeyType defaultContentKeyType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type defaultContentType)
                => new(type, defaultContentKeyType, defaultContentType);
    }
}
