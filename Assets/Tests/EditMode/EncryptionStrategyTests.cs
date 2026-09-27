using System;
using MyToolz.IO;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class EncryptionStrategyTests
    {
        private const string Plain = "The quick brown fox — 1234567890 {}[]";

        [Test]
        public void NoEncryption_IsIdentity()
        {
            var strategy = new NoEncryptionStrategy();
            Assert.AreEqual(Plain, strategy.Encrypt(Plain));
            Assert.AreEqual(Plain, strategy.Decrypt(Plain));
        }

        [Test]
        public void Xor_RoundTrips()
        {
            var strategy = new XorEncryptionStrategy();
            string cipher = strategy.Encrypt(Plain);

            Assert.AreNotEqual(Plain, cipher, "cipher text should differ from plain text");
            Assert.AreEqual(Plain, strategy.Decrypt(cipher));
        }

        [Test]
        public void Xor_ProducesBase64()
        {
            string cipher = new XorEncryptionStrategy().Encrypt(Plain);
            Assert.DoesNotThrow(() => Convert.FromBase64String(cipher), "XOR output is base64-encoded");
        }

        [Test]
        public void Aes_RoundTrips()
        {
            var strategy = new AesEncryptionStrategy();
            string cipher = strategy.Encrypt(Plain);

            Assert.AreNotEqual(Plain, cipher);
            Assert.AreEqual(Plain, strategy.Decrypt(cipher));
        }

        [Test]
        public void Aes_ProducesPrefixedBase64()
        {
            string cipher = new AesEncryptionStrategy().Encrypt(Plain);
            StringAssert.StartsWith("MTAES2:", cipher);
            Assert.DoesNotThrow(() => Convert.FromBase64String(cipher.Substring("MTAES2:".Length)));
        }

        [Test]
        public void Aes_UsesFreshIvPerEncryption()
        {
            var strategy = new AesEncryptionStrategy();
            Assert.AreNotEqual(strategy.Encrypt(Plain), strategy.Encrypt(Plain),
                "a random IV per save means equal plain texts never produce equal cipher texts");
        }

        [Test]
        public void Aes_RejectsEditedPayload()
        {
            var strategy = new AesEncryptionStrategy();
            string cipher = strategy.Encrypt(Plain);
            byte[] bytes = Convert.FromBase64String(cipher.Substring("MTAES2:".Length));
            bytes[20] ^= 0x01;
            string edited = "MTAES2:" + Convert.ToBase64String(bytes);

            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => strategy.Decrypt(edited));
        }

        [Test]
        public void Aes_ReadsLegacyFixedIvPayload()
        {
            // IO 1.1.x wrote AES-CBC with key/IV = SHA-256 of the configured strings and no prefix.
            string legacy;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                byte[] key = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(AesEncryptionStrategy.DefaultKey));
                byte[] iv = new byte[16];
                Array.Copy(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("NoSaintsDefaultI")), iv, 16);
                aes.Key = key;
                aes.IV = iv;
                using var encryptor = aes.CreateEncryptor();
                byte[] plain = System.Text.Encoding.UTF8.GetBytes(Plain);
                legacy = Convert.ToBase64String(encryptor.TransformFinalBlock(plain, 0, plain.Length));
            }

            Assert.AreEqual(Plain, new AesEncryptionStrategy().Decrypt(legacy));
        }

        [Test]
        public void Aes_WarnsAboutDefaultKey()
        {
            Assert.IsNotNull(new AesEncryptionStrategy().ConfigurationWarning);
        }

        [Test]
        public void Xor_RoundTrips_EmptyString()
        {
            var strategy = new XorEncryptionStrategy();
            Assert.AreEqual(string.Empty, strategy.Decrypt(strategy.Encrypt(string.Empty)));
        }

        [Test]
        public void Aes_RoundTrips_EmptyString()
        {
            var strategy = new AesEncryptionStrategy();
            Assert.AreEqual(string.Empty, strategy.Decrypt(strategy.Encrypt(string.Empty)));
        }
    }
}
