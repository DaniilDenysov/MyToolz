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
        public void Aes_ProducesBase64()
        {
            string cipher = new AesEncryptionStrategy().Encrypt(Plain);
            Assert.DoesNotThrow(() => Convert.FromBase64String(cipher));
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
