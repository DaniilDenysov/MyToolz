using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MyToolz.IO
{
    /// <summary>
    /// Turns the serialized save text into the stored text and back. Any key a strategy uses ships
    /// inside the build, so none of these protect a save from a determined player; they range from
    /// "unreadable at a glance" (XOR) to "edits are detected" (AES, which authenticates every save).
    /// </summary>
    [Serializable]
    public abstract class EncryptionStrategy
    {
        public abstract string Encrypt(string raw);
        public abstract string Decrypt(string encrypted);

        /// <summary>A configuration problem worth warning about (e.g. a public default key), or null.</summary>
        public virtual string ConfigurationWarning => null;
    }

    [Serializable]
    [AddTypeMenu("None")]
    public sealed class NoEncryptionStrategy : EncryptionStrategy
    {
        public override string Encrypt(string raw) => raw;
        public override string Decrypt(string encrypted) => encrypted;
    }

    /// <summary>
    /// Obfuscation only: XOR with a repeating key hides the text from a casual look but is trivially
    /// reversible and does not detect edits. Use <see cref="AesEncryptionStrategy"/> when edits must be caught.
    /// </summary>
    [Serializable]
    [AddTypeMenu("XOR (obfuscation only)")]
    public sealed class XorEncryptionStrategy : EncryptionStrategy
    {
        [SerializeField, Tooltip("Obfuscation key. XOR is not encryption: anyone with the build can undo it.")]
        private string key = "NoSaints";

        public override string Encrypt(string raw)
        {
            byte[] data = Encoding.UTF8.GetBytes(raw);
            byte[] keyBytes = GetKeyBytes();

            for (int i = 0; i < data.Length; i++)
                data[i] ^= keyBytes[i % keyBytes.Length];

            return Convert.ToBase64String(data);
        }

        public override string Decrypt(string encrypted)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            byte[] keyBytes = GetKeyBytes();

            for (int i = 0; i < data.Length; i++)
                data[i] ^= keyBytes[i % keyBytes.Length];

            return Encoding.UTF8.GetString(data);
        }

        private byte[] GetKeyBytes()
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("XOR encryption requires a non-empty key.");
            }

            return Encoding.UTF8.GetBytes(key);
        }
    }

    /// <summary>
    /// AES-256-CBC with a fresh random IV for every save, authenticated with HMAC-SHA256
    /// (encrypt-then-MAC), so any edit to the stored bytes is rejected on load. The key is derived from
    /// <c>key</c>; change it per project, since the default is public. Saves written by IO 1.1.x
    /// (fixed IV, no authentication) are still read, and are upgraded by the next save.
    /// </summary>
    [Serializable]
    [AddTypeMenu("AES-256 + HMAC (authenticated)")]
    public sealed class AesEncryptionStrategy : EncryptionStrategy
    {
        public const string DefaultKey = "NoSaintsDefaultK";

        private const string FormatPrefix = "MTAES2:";
        private const int IvLength = 16;
        private const int MacLength = 32;

        [SerializeField, Tooltip("Secret the AES and HMAC keys are derived from. Set a project-specific value: the default is public, and whatever you choose ships inside the build.")]
        private string key = DefaultKey;

        [SerializeField, Tooltip("Legacy: only used to read saves written by IO 1.1.x, which used this fixed IV. New saves use a random IV per write.")]
        private string iv = "NoSaintsDefaultI";

        [NonSerialized] private string derivedFor;
        [NonSerialized] private byte[] encryptionKey;
        [NonSerialized] private byte[] macKey;

        public bool UsesDefaultKey => string.IsNullOrEmpty(key) || key == DefaultKey;

        public override string ConfigurationWarning => UsesDefaultKey
            ? "AesEncryptionStrategy is using the public default key. Set a project-specific key so saves from other games built on MyToolz cannot be forged with it."
            : null;

        public override string Encrypt(string raw)
        {
            DeriveKeys();

            byte[] ivBytes = new byte[IvLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(ivBytes);
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(raw ?? string.Empty);
            byte[] cipherBytes;

            using (Aes aes = CreateAes(encryptionKey, ivBytes))
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            byte[] envelope = new byte[IvLength + cipherBytes.Length + MacLength];
            Buffer.BlockCopy(ivBytes, 0, envelope, 0, IvLength);
            Buffer.BlockCopy(cipherBytes, 0, envelope, IvLength, cipherBytes.Length);

            byte[] mac = ComputeMac(envelope, IvLength + cipherBytes.Length);
            Buffer.BlockCopy(mac, 0, envelope, IvLength + cipherBytes.Length, MacLength);

            return FormatPrefix + Convert.ToBase64String(envelope);
        }

        public override string Decrypt(string encrypted)
        {
            if (encrypted == null || !encrypted.StartsWith(FormatPrefix, StringComparison.Ordinal))
            {
                return DecryptLegacy(encrypted);
            }

            DeriveKeys();

            byte[] envelope = Convert.FromBase64String(encrypted.Substring(FormatPrefix.Length));
            int cipherLength = envelope.Length - IvLength - MacLength;

            if (cipherLength <= 0 || cipherLength % IvLength != 0)
            {
                throw new InvalidDataException("AES save payload is truncated or malformed.");
            }

            byte[] expectedMac = ComputeMac(envelope, IvLength + cipherLength);
            if (!FixedTimeEquals(expectedMac, envelope, IvLength + cipherLength))
            {
                throw new CryptographicException("AES save payload failed authentication (wrong key or edited data).");
            }

            byte[] ivBytes = new byte[IvLength];
            Buffer.BlockCopy(envelope, 0, ivBytes, 0, IvLength);

            using Aes aes = CreateAes(encryptionKey, ivBytes);
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(envelope, IvLength, cipherLength);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private string DecryptLegacy(string encrypted)
        {
            using Aes aes = CreateAes(Truncate(Sha256(key ?? string.Empty), 32), Truncate(Sha256(iv ?? string.Empty), IvLength));
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(encrypted ?? string.Empty);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private void DeriveKeys()
        {
            string secret = key ?? string.Empty;
            if (derivedFor == secret && encryptionKey != null)
            {
                return;
            }

            byte[] master = Sha256(secret);
            using (var hmac = new HMACSHA256(master))
            {
                encryptionKey = hmac.ComputeHash(Encoding.UTF8.GetBytes("MyToolz.IO/aes-256-cbc"));
                macKey = hmac.ComputeHash(Encoding.UTF8.GetBytes("MyToolz.IO/hmac-sha256"));
            }

            derivedFor = secret;
        }

        private byte[] ComputeMac(byte[] data, int count)
        {
            using var hmac = new HMACSHA256(macKey);
            hmac.TransformBlock(Encoding.ASCII.GetBytes(FormatPrefix), 0, FormatPrefix.Length, null, 0);
            hmac.TransformFinalBlock(data, 0, count);
            return hmac.Hash;
        }

        private static Aes CreateAes(byte[] keyBytes, byte[] ivBytes)
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            return aes;
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] buffer, int offset)
        {
            if (buffer.Length - offset < expected.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                difference |= expected[i] ^ buffer[offset + i];
            }

            return difference == 0;
        }

        private static byte[] Sha256(string input)
        {
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        private static byte[] Truncate(byte[] source, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(source, result, length);
            return result;
        }
    }
}
