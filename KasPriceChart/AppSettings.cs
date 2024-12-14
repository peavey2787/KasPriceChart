using Newtonsoft.Json;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;

namespace KasPriceChart
{
    public static class AppSettings
    {
        private static readonly string SettingsFilePath = "AppSettings.json";
        private static readonly object FileLock = new object();

        public static void Save<T>(string key, T value)
        {
            lock (FileLock)
            {
                var settings = LoadSettings();
                settings[key] = JsonConvert.SerializeObject(value);
                SaveSettings(settings);
            }
        }

        public static T Load<T>(string key)
        {
            lock (FileLock)
            {
                var settings = LoadSettings();
                if (settings.ContainsKey(key))
                {
                    string serializedValue = settings[key];
                    return JsonConvert.DeserializeObject<T>(serializedValue);
                }
                else
                {
                    return default(T);
                }
            }
        }

        public static void SaveSerialized<T>(string key, T value)
        {
            lock (FileLock)
            {
                IFormatter formatter = new BinaryFormatter();
                using (Stream stream = new FileStream(key + ".dat", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    formatter.Serialize(stream, value);
                }
            }
        }

        public static T LoadSerialized<T>(string key)
        {
            lock (FileLock)
            {
                IFormatter formatter = new BinaryFormatter();
                try
                {
                    using (Stream stream = new FileStream(key + ".dat", FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (stream.Length > 0)
                        {
                            return (T)formatter.Deserialize(stream);
                        }
                        else
                        {
                            return default(T); // Assuming T is a reference type, this will return null.
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // Handle the case where the file is not found (key doesn't exist)
                    return default(T);
                }
            }
        }

        private static void SaveSettings(Dictionary<string, string> settings)
        {
            lock (FileLock)
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
        }


        private static Dictionary<string, string> LoadSettings()
        {
            lock (FileLock)
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                else
                {
                    return new Dictionary<string, string>();
                }
            }
        }
    }

    public class EncryptionHelper
    {
        private readonly string EncryptionKey = Generate256BitKey();

        public EncryptionHelper() { EncryptionKey = Generate256BitKey(); }
        public EncryptionHelper(string encryptionKey) { EncryptionKey = encryptionKey; }

        private static string Generate256BitKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256; // Explicitly set key size
                aes.BlockSize = 128; // Explicitly set block size
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        public string GetEncryptionKey()
        {
            return EncryptionKey;
        }

        public string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256; // Explicitly set key size
                aesAlg.BlockSize = 128; // Explicitly set block size
                aesAlg.Key = Convert.FromBase64String(EncryptionKey);
                aesAlg.IV = new byte[aesAlg.BlockSize / 8];

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256; // Explicitly set key size
                aesAlg.BlockSize = 128; // Explicitly set block size
                aesAlg.Key = Convert.FromBase64String(EncryptionKey);
                aesAlg.IV = new byte[aesAlg.BlockSize / 8];

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public void Show(string message)
        {
            string originalText = message;
            Console.WriteLine($"Original Text: {originalText}");

            string encryptedText = Encrypt(originalText);
            Console.WriteLine($"Encrypted Text: {encryptedText}");

            string decryptedText = Decrypt(encryptedText);
            Console.WriteLine($"Decrypted Text: {decryptedText}");
        }
    }
}
