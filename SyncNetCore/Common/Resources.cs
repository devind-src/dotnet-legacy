using Newtonsoft.Json;
using SyncNet.Models.Common;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SyncNet.Common
{
    internal static class Resources
    {
        public static ResourcesModel Keys;

        public static void Initialize()
        {
            try
            {
                string resourcesFile = Path.Combine(AppConfig.AppDir, "Core", "Bin", "Resources.bin");

                string result = DecryptFileHybrid(resourcesFile);
                if (string.IsNullOrEmpty(result)) throw new Exception("Unable to read file resources.");

                Keys = JsonConvert.DeserializeObject<ResourcesModel>(result);
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static string DecryptFileHybrid(string inputFilePath)
        {
            string privateKeyPath = Path.Combine(AppConfig.AppDir, "Keys", "PrivateKey.pem");

            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("File terenkripsi tidak ditemukan.", inputFilePath);
            if (!File.Exists(privateKeyPath))
                throw new FileNotFoundException("File Private Key tidak ditemukan.", privateKeyPath);

            try
            {
                using var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fs);

                // 1. Baca RSA Encrypted Key dari header file .enc
                int encryptedKeyLength = reader.ReadInt32();
                byte[] encryptedAesKey = reader.ReadBytes(encryptedKeyLength);

                // 2. Baca komponen AES-GCM
                byte[] nonce = reader.ReadBytes(12);
                byte[] tag = reader.ReadBytes(16);

                int cipherTextLength = (int)(fs.Length - fs.Position);
                byte[] cipherText = reader.ReadBytes(cipherTextLength);

                // 3. Baca RSA Private Key dari file path dan dekripsi kunci AES
                using RSA rsa = RSA.Create();
                LoadPrivateKeyFromFile(rsa, privateKeyPath);

                byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

                // 4. Dekripsi isi file menggunakan AES-256-GCM
                byte[] decryptedBytes = new byte[cipherTextLength];
                using (AesGcm aesGcm = new AesGcm(aesKey, 16))
                {
                    aesGcm.Decrypt(nonce, cipherText, tag, decryptedBytes);
                }

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private static void LoadPrivateKeyFromFile(RSA rsa, string path)
        {
            string keyContent = File.ReadAllText(path);

            if (keyContent.Contains("-----BEGIN"))
            {
                // Format PEM (Text)
                rsa.ImportFromPem(keyContent);
            }
            else
            {
                // Format Binary (DER / Raw PKCS#1)
                byte[] keyBytes = File.ReadAllBytes(path);
                rsa.ImportRSAPrivateKey(keyBytes, out _);
            }
        }
    }
}
