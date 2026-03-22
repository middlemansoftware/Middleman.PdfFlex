// Copyright (c) Middleman Software, Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Middleman.PdfFlex.Pdf.Signatures
{
    /// <summary>
    /// Interface for classes that generate digital signatures.
    /// </summary>
    public interface IDigitalSigner
    {
        /// <summary>
        /// Gets a human-readable name of the used certificate.
        /// </summary>
        string CertificateName { get; }

        /// <summary>
        /// Gets the size of the signature in bytes.
        /// The size is used to reserve space in the PDF file that is later filled with the signature.
        /// </summary>
        Task<int> GetSignatureSizeAsync();

        /// <summary>
        /// Gets the signatures of the specified stream.
        /// </summary>
        /// <param name="stream"></param>
        Task<byte[]> GetSignatureAsync(Stream stream);
    }
}
