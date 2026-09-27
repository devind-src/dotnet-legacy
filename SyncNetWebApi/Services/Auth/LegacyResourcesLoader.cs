using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetApi.Options;

namespace SyncNetApi.Services.Auth
{
    /// <summary>
    /// Decrypts the legacy app's Resources.bin once at startup and holds the DES key it
    /// contains (the cipher for dashboard_user.password) in memory for the process
    /// lifetime — never logged, never written anywhere else. Ported from
    /// Common/Resources.cs in the Blazor source: an RSA-OAEP-SHA256-wrapped AES-256-GCM
    /// blob. File layout (little-endian, written by whatever tool encrypted it):
    ///   int32 encryptedAesKeyLength | encryptedAesKey | 12-byte GCM nonce | 16-byte GCM tag | ciphertext
    ///
    /// Loading is best-effort and MUST NOT block application startup the way the license
    /// check does — if the files aren't configured or present, legacy (pre-migration)
    /// accounts simply can't log in; every account created directly through this API is
    /// unaffected since it never uses this key.
    /// </summary>
    public class LegacyResourcesLoader
    {
        private readonly string? _desDatabaseKeyHex;
        private readonly string? _hsmLmk1Hex;
        private readonly string? _hsmLmk2Hex;
        private readonly string? _desConfigKeyHex;
        private readonly string? _aesConfigKeyHex;
        private readonly string? _aesConfigIvHex;

        public bool IsLoaded => _desDatabaseKeyHex != null;

        /// <summary>True when DES.HSM.LMK_1 (the key the HSM Emulator encrypts/decrypts
        /// components under) was present in Resources.bin. LMK_2 is loaded for completeness
        /// (mirrors ResourcesModel) but the legacy NbHSM Emulator path never actually uses it —
        /// only LMK_1 backs GetKeyEncrypted/GetClearKey/GenerateZMK.</summary>
        public bool IsHsmLmkLoaded => _hsmLmk1Hex != null;

        /// <summary>True when DES.Config (Tools &gt; Encrypt Credential's XML mode key) was
        /// present in Resources.bin.</summary>
        public bool IsDesConfigLoaded => _desConfigKeyHex != null;

        /// <summary>True when AES.Config.Key/IV (Tools &gt; Encrypt Credential's JSON mode key)
        /// were both present in Resources.bin.</summary>
        public bool IsAesConfigLoaded => _aesConfigKeyHex != null && _aesConfigIvHex != null;

        public LegacyResourcesLoader(IOptions<LegacyCredentialsOptions> options, ILogger<LegacyResourcesLoader> logger)
        {
            var opt = options.Value;

            if (string.IsNullOrWhiteSpace(opt.ResourcesPath) || string.IsNullOrWhiteSpace(opt.PrivateKeyPath))
            {
                logger.LogWarning(
                    "LegacyCredentials:ResourcesPath / PrivateKeyPath are not configured — accounts created by the legacy Blazor app cannot log in, and the HSM Emulator cannot compute real values, until this is set.");
                return;
            }

            try
            {
                var json = DecryptResourcesFile(opt.ResourcesPath, opt.PrivateKeyPath);
                using var doc = JsonDocument.Parse(json);
                if (!TryGetPropertyIgnoreCase(doc.RootElement, "DES", out var desElement))
                    throw new InvalidOperationException("Resources.bin decrypted but DES section is missing.");

                _desDatabaseKeyHex = TryGetPropertyIgnoreCase(desElement, "Database", out var dbEl) ? dbEl.GetString() : null;
                if (string.IsNullOrEmpty(_desDatabaseKeyHex))
                    throw new InvalidOperationException("Resources.bin decrypted but DES.Database key is empty.");

                if (TryGetPropertyIgnoreCase(desElement, "HSM", out var hsmElement))
                {
                    _hsmLmk1Hex = TryGetPropertyIgnoreCase(hsmElement, "LMK_1", out var lmk1) ? lmk1.GetString() : null;
                    _hsmLmk2Hex = TryGetPropertyIgnoreCase(hsmElement, "LMK_2", out var lmk2) ? lmk2.GetString() : null;
                }

                _desConfigKeyHex = TryGetPropertyIgnoreCase(desElement, "Config", out var desConfig) ? desConfig.GetString() : null;

                if (TryGetPropertyIgnoreCase(doc.RootElement, "AES", out var aesElement) && TryGetPropertyIgnoreCase(aesElement, "Config", out var aesConfig))
                {
                    _aesConfigKeyHex = TryGetPropertyIgnoreCase(aesConfig, "Key", out var aesKeyEl) ? aesKeyEl.GetString() : null;
                    _aesConfigIvHex = TryGetPropertyIgnoreCase(aesConfig, "IV", out var aesIvEl) ? aesIvEl.GetString() : null;
                }

                logger.LogInformation(
                    "Legacy credentials loaded — DES password verification for pre-migration accounts is available. HSM LMK loaded: {HsmLmkLoaded}. Tools > Encrypt Credential keys loaded: DES.Config={DesConfigLoaded}, AES.Config={AesConfigLoaded}.",
                    _hsmLmk1Hex != null, _desConfigKeyHex != null, IsAesConfigLoaded);
            }
            catch (Exception ex)
            {
                _desDatabaseKeyHex = null;
                _hsmLmk1Hex = null;
                _hsmLmk2Hex = null;
                _desConfigKeyHex = null;
                _aesConfigKeyHex = null;
                _aesConfigIvHex = null;
                logger.LogError(ex,
                    "Failed to load legacy credentials from {ResourcesPath} — accounts created by the legacy Blazor app cannot log in, and the HSM Emulator cannot compute real values, until this is fixed.",
                    opt.ResourcesPath);
            }
        }

        /// <summary>Hex-encoded DES/TripleDES key for dashboard_user.password, or null if
        /// Resources.bin couldn't be loaded.</summary>
        public string? GetDesDatabaseKeyHex() => _desDatabaseKeyHex;

        /// <summary>Hex-encoded LMK_1 — the key HSM Emulator math (component encrypt/decrypt,
        /// KCV, ZMK formation) runs under. Null if Resources.bin couldn't be loaded or didn't
        /// contain a DES.HSM.LMK_1 value.</summary>
        public string? GetHsmLmk1Hex() => _hsmLmk1Hex;

        /// <summary>Hex-encoded LMK_2 — loaded for completeness (mirrors ResourcesModel) but
        /// unused by the ported NbHSM Emulator logic, same as in the legacy source.</summary>
        public string? GetHsmLmk2Hex() => _hsmLmk2Hex;

        /// <summary>Hex-encoded DES key backing Tools &gt; Encrypt Credential's "XML" mode
        /// (legacy NbXml.EncryptValue / DesAlgorithm.EncryptText). Null if not loaded.</summary>
        public string? GetDesConfigKeyHex() => _desConfigKeyHex;

        /// <summary>Hex-encoded AES-256 key backing Tools &gt; Encrypt Credential's "JSON" mode
        /// (legacy CredenHelper.EncryptValue). Null if not loaded.</summary>
        public string? GetAesConfigKeyHex() => _aesConfigKeyHex;

        /// <summary>Hex-encoded AES IV backing Tools &gt; Encrypt Credential's "JSON" mode. Null
        /// if not loaded.</summary>
        public string? GetAesConfigIvHex() => _aesConfigIvHex;

        private static string DecryptResourcesFile(string resourcesPath, string privateKeyPath)
        {
            using var fs = new FileStream(resourcesPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            var encryptedKeyLength = reader.ReadInt32();
            var encryptedAesKey = reader.ReadBytes(encryptedKeyLength);
            var nonce = reader.ReadBytes(12);
            var tag = reader.ReadBytes(16);
            var cipherTextLength = (int)(fs.Length - fs.Position);
            var cipherText = reader.ReadBytes(cipherTextLength);

            using var rsa = RSA.Create();
            ImportPrivateKey(rsa, privateKeyPath);
            var aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

            var plainBytes = new byte[cipherTextLength];
            using (var aesGcm = new AesGcm(aesKey, 16))
            {
                aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>JsonElement.TryGetProperty is case-sensitive; Resources.bin's generator is
        /// inconsistent about casing (e.g. "AES.Config.KEY" vs the rest of the file's Pascal
        /// case), so every property lookup above goes through this instead.</summary>
        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static void ImportPrivateKey(RSA rsa, string path)
        {
            var content = File.ReadAllText(path);
            if (content.Contains("-----BEGIN"))
                rsa.ImportFromPem(content);
            else
                rsa.ImportRSAPrivateKey(File.ReadAllBytes(path), out _);
        }
    }
}
