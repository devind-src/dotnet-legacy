using SyncNet.Common;
using SyncNet.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SyncNet.Library
{
    public class NbXml
    {
        private readonly string _fileConfig;

        public NbXml(string fileName)
        {
            _fileConfig = Path.Combine(AppContext.BaseDirectory, fileName);
        }

        public NbXml(string pathDir, string fileName)
        {
            _fileConfig = Path.Combine(pathDir, fileName);
        }

        public void SetValue(string key, string value)
        {
            try
            {
                key = key.Replace(" ", "");

                string[] parts = key.Split(['/', '.'], StringSplitOptions.RemoveEmptyEntries);

                XDocument doc = File.Exists(_fileConfig)
                    ? XDocument.Load(_fileConfig)
                    : new XDocument(new XElement("Configuration"));

                if (parts.Length > 1)
                {
                    string[] parentParts = parts.Take(parts.Length - 1).ToArray();
                    string lastKey = parts.Last();

                    XElement parentElement = GetOrCreateElement(doc.Root, parentParts);

                    parentElement.Element(lastKey)?.Remove();
                    parentElement.Add(new XElement(lastKey, value));
                }
                else
                {
                    doc.Root?.Element(key)?.Remove();
                    doc.Root?.Add(new XElement(key, value));
                }

                doc.Save(_fileConfig);
            }
            catch { }
        }

        public void SetArrayValue(string key, IEnumerable<string> values)
        {
            try
            {
                key = key.Replace(" ", "");
                string[] parts = key.Split(['/', '.'], StringSplitOptions.RemoveEmptyEntries);

                XDocument doc = File.Exists(_fileConfig)
                    ? XDocument.Load(_fileConfig)
                    : new XDocument(new XElement("Configuration"));

                XElement parentElement = doc.Root;
                string lastKey = key;

                if (parts.Length > 1)
                {
                    string[] parentParts = parts.Take(parts.Length - 1).ToArray();
                    lastKey = parts.Last();
                    parentElement = GetOrCreateElement(doc.Root, parentParts);
                }

                // Hapus semua elemen dengan nama sama yang sudah ada sebelumnya (reset array)
                parentElement.Elements(lastKey).Remove();

                // Masukkan semua item baru ke dalam XML
                foreach (var val in values)
                {
                    parentElement.Add(new XElement(lastKey, val));
                }

                doc.Save(_fileConfig);
            }
            catch { }
        }

        public string GetValue(string key) => GetValue(key, "");

        public string GetValue(string key, string defaultvalue)
        {
            try
            {
                key = key.Replace(" ", "");
                string[] parts = key.Split(['/', '.'], StringSplitOptions.RemoveEmptyEntries);

                if (File.Exists(_fileConfig))
                {
                    var doc = XDocument.Load(_fileConfig);
                    XElement element = doc.Root;

                    foreach (var part in parts)
                    {
                        element = element?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(part, StringComparison.OrdinalIgnoreCase));
                    }

                    if (element != null) return element.Value;
                }
            }
            catch { }

            return defaultvalue;
        }

        public string[] GetArrayValue(string key)
        {
            try
            {
                key = key.Replace(" ", "");
                string[] parts = key.Split(['/', '.'], StringSplitOptions.RemoveEmptyEntries);

                if (File.Exists(_fileConfig))
                {
                    var doc = XDocument.Load(_fileConfig);
                    XContainer current = doc.Root;

                    // Telusuri parent-nya terlebih dahulu jika jalurnya bertingkat
                    if (parts.Length > 1)
                    {
                        for (int i = 0; i < parts.Length - 1; i++)
                        {
                            current = current?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(parts[i], StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    if (current != null)
                    {
                        string targetKey = parts.Last();

                        // Ambil semua elemen yang memiliki nama tag sama
                        return current.Elements()
                                      .Where(e => e.Name.LocalName.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                                      .Select(e => e.Value)
                                      .ToArray();
                    }
                }
            }
            catch { }

            return Array.Empty<string>(); // Kembalikan array kosong jika tidak ditemukan/error
        }

        public void DeleteKey(string key)
        {
            try
            {
                key = key.Replace(" ", "");
                string[] parts = key.Split(['/', '.'], StringSplitOptions.RemoveEmptyEntries);

                if (File.Exists(_fileConfig))
                {
                    var doc = XDocument.Load(_fileConfig);

                    if (parts.Length > 1)
                    {
                        XElement element = doc.Root;
                        foreach (var part in parts)
                        {
                            element = element?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(part, StringComparison.OrdinalIgnoreCase));
                        }
                        element?.Remove();
                    }
                    else
                    {
                        // Menghapus semua elemen yang cocok di root level (termasuk jika berupa array)
                        doc.Root?.Elements(key).Remove();
                    }

                    doc.Save(_fileConfig);
                }
            }
            catch { }
        }

        public void SetValueEncrypted(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) value = "".PadLeft(16, '0');
            string tmp = DesAlgorithm.EncryptText(value, Resources.Keys.DES.Config);
            SetValue(key, tmp);
        }

        public string GetValueDecrypted(string key) => GetValueDecrypted(key, "");

        public string GetValueDecrypted(string key, string defaultvalue)
        {
            string ret = defaultvalue;
            try
            {
                string tmp = GetValue(key);
                if (!string.IsNullOrEmpty(tmp)) ret = DesAlgorithm.DecryptText(tmp, Resources.Keys.DES.Config);
            }
            catch { }

            return string.IsNullOrEmpty(ret) ? defaultvalue : ret;
        }

        public static string EncryptValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            try
            {
                string ret = DesAlgorithm.EncryptText(value, Resources.Keys.DES.Config);

                return string.IsNullOrEmpty(ret) ? value : ret;
            }
            catch { return value; }
        }

        public static string DecryptValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            try
            {
                string ret = DesAlgorithm.DecryptText(value, Resources.Keys.DES.Config);

                return string.IsNullOrEmpty(ret) ? value : ret;
            }
            catch { return value; }
        }

        private XElement GetOrCreateElement(XContainer parent, string[] pathParts)
        {
            XContainer current = parent;
            XElement element = null;

            foreach (var part in pathParts)
            {
                element = current.Elements().FirstOrDefault(
                    e => e.Name.LocalName.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (element == null)
                {
                    element = new XElement(part);
                    current.Add(element);
                }

                current = element;
            }

            return element;
        }

    }
}
