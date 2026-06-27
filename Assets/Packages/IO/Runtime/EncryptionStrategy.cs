using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MyToolz.IO
{
    [Serializable]
    public abstract class EncryptionStrategy
    {
        public abstract string Encrypt(string raw);
        public abstract string Decrypt(string encrypted);
    }

    [Serializable]
    public sealed class NoEncryptionStrategy : EncryptionStrategy
    {
        public override string Encrypt(string raw) => raw;
        public override string Decrypt(string encrypted) => encrypted;
    }

    [Serializable]
    public sealed class XorEncryptionStrategy : EncryptionStrategy
    {
        [SerializeField] private string key = "NoSaints";

        public override string Encrypt(string raw)
        {
            byte[] data = Encoding.UTF8.GetBytes(raw);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            for (int i = 0; i < data.Length; i++)
                data[i] ^= keyBytes[i % keyBytes.Length];

            return Convert.ToBase64String(data);
        }

        public override string Decrypt(string encrypted)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            for (int i = 0; i < data.Length; i++)
                data[i] ^= keyBytes[i % keyBytes.Length];

            return Encoding.UTF8.GetString(data);
        }
    }

    [Serializable]
    public sealed class AesEncryptionStrategy : EncryptionStrategy
    {
        [SerializeField] private string key = "NoSaintsDefaultK";
        [SerializeField] private string iv = "NoSaintsDefaultI";

        public override string Encrypt(string raw)
        {
            using Aes aes = Aes.Create();
            aes.Key = DeriveKey(key, aes.KeySize / 8);
            aes.IV = DeriveKey(iv, aes.BlockSize / 8);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(raw);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(cipherBytes);
        }

        public override string Decrypt(string encrypted)
        {
            using Aes aes = Aes.Create();
            aes.Key = DeriveKey(key, aes.KeySize / 8);
            aes.IV = DeriveKey(iv, aes.BlockSize / 8);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(encrypted);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static byte[] DeriveKey(string input, int length)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            byte[] result = new byte[length];
            Array.Copy(hash, result, length);
            return result;
        }
    }
}
