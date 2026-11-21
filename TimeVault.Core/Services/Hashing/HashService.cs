using System;
using System.IO;
using System.Security.Cryptography;

namespace TimeVault.Core.Services.Hashing
{
    public class HashService
    {
        public string ComputeHash(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);

                    return BytesToHex(hashBytes);
                }
            }
        }

        public bool AreEqual(string hash1, string hash2)
        {
            if (hash1 == null || hash2 == null)
            {
                return false;
            }

            return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyFile(string filePath, string expectedHash)
        {
            string currentHash = ComputeHash(filePath);

            return AreEqual(currentHash, expectedHash);
        }

        private string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
