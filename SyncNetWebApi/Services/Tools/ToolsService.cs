using System.Collections.Generic;
using System.Linq;
using SyncNetApi.Common;
using SyncNetApi.Cryptography;
using SyncNetApi.Dtos.Tools;
using SyncNetApi.Services.Auth;

namespace SyncNetApi.Services.Tools
{
    /// <summary>Backs the 4 pages under menu "Tools" — all stateless calculators/decoders,
    /// nothing persisted to the database. Ported from legacy Pages/Tools/* + the Library/
    /// Cryptography helpers they call.</summary>
    public class ToolsService : IToolsService
    {
        private readonly LegacyResourcesLoader _resources;

        public ToolsService(LegacyResourcesLoader resources)
        {
            _resources = resources;
        }

        // --- DES Calculator (legacy Pages/Tools/DesCalculator) ---------------------------

        public string DesEncrypt(string valueHex, string keyHex)
        {
            ValidateDesInput(valueHex, keyHex);
            return HsmDesCipher.EncryptHex(valueHex, keyHex);
        }

        public string DesDecrypt(string valueHex, string keyHex)
        {
            ValidateDesInput(valueHex, keyHex);
            return HsmDesCipher.DecryptHex(valueHex, keyHex);
        }

        private static void ValidateDesInput(string value, string key)
        {
            if (string.IsNullOrEmpty(value))
                throw new ValidationException("Field 'value' is required.");
            if (string.IsNullOrEmpty(key))
                throw new ValidationException("Field 'key' is required.");
            if (value.Length != 16 && value.Length != 32 && value.Length != 48)
                throw new ValidationException("Length field 'value' must be 16/32/48.");
            if (key.Length != 16 && key.Length != 32 && key.Length != 48)
                throw new ValidationException("Length field 'key' must be 16/32/48.");
            if (!IsHex(value))
                throw new ValidationException("Field 'value' must be in hex format.");
            if (!IsHex(key))
                throw new ValidationException("Field 'key' must be in hex format.");
        }

        // --- Pinblock Calculator (legacy Pages/Tools/PinBlock + Library/NbCard) -----------

        public string CalculatePinBlock(string format, string pan, string pin, string keyHex)
        {
            if (string.IsNullOrEmpty(pan))
                throw new ValidationException("Field 'PAN' is required.");
            if (pan.Length != 16)
                throw new ValidationException("Length field 'PAN' must be 16.");
            if (string.IsNullOrEmpty(pin))
                throw new ValidationException("Field 'PIN' is required.");
            if (pin.Length != 6)
                throw new ValidationException("Length field 'PIN' must be 6.");
            if (string.IsNullOrEmpty(keyHex))
                throw new ValidationException("Field 'key' is required.");
            if (keyHex.Length != 16 && keyHex.Length != 32 && keyHex.Length != 48)
                throw new ValidationException("Length field 'key' must be 16/32/48.");
            if (!IsHex(keyHex))
                throw new ValidationException("Field 'key' must be in hex format.");
            if (!pan.All(char.IsDigit))
                throw new ValidationException("PAN must be numeric only.");
            if (!pin.All(char.IsDigit))
                throw new ValidationException("PIN must be numeric only.");

            string clearPinBlock;
            switch (format)
            {
                case "ISO ANSI":
                {
                    var pinData = ("0" + pin.Length + pin).PadRight(16, 'F');
                    var panData = "0000" + pan.Substring(3, 12);
                    clearPinBlock = HsmDesCipher.XorHex(pinData, panData);
                    break;
                }
                case "DOCUTEL":
                {
                    clearPinBlock = pin.Length + pin.PadRight(6, '0') + "987654321";
                    break;
                }
                case "IBM":
                    clearPinBlock = pin.PadRight(16, 'F');
                    break;
                case "PLUS":
                {
                    var pinData = ("0" + pin.Length + pin).PadRight(16, 'F');
                    var panData = "0000" + pan.Substring(0, 12);
                    clearPinBlock = HsmDesCipher.XorHex(pinData, panData);
                    break;
                }
                default:
                    throw new ValidationException("Invalid pinblock format.");
            }

            return HsmDesCipher.EncryptHex(clearPinBlock, keyHex);
        }

        // --- ICC Data Decode (legacy Pages/Tools/IccDataDecode + Library/NbTlvEmv) --------

        public IReadOnlyList<IccTlvEntryDto> DecodeIccData(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ValidationException("Field 'value' is required.");
            if (!IsHex(hex))
                throw new ValidationException("Field 'value' must be in hex format.");
            if (hex.Length % 2 != 0)
                throw new ValidationException("Length field 'value' must be even.");

            var bytes = Convert.FromHexString(hex);
            IReadOnlyList<EmvTlvParser.TlvEntry> entries;
            try
            {
                entries = EmvTlvParser.ParseTlv(bytes);
            }
            catch (Exception)
            {
                // TLV parsing walks a user-supplied byte array with raw index/Array.Copy
                // arithmetic and no bounds validation of its own — a truncated or malformed
                // structure can surface as IndexOutOfRangeException, ArgumentException (from
                // Array.Copy), etc. All of them mean the same thing here: bad input, not a
                // server fault, so they map to a 400 like the other validation failures above.
                throw new ValidationException("Value could not be parsed as valid TLV data.");
            }

            return entries.Select(e =>
            {
                var tagHex = Convert.ToHexString(e.Tag);
                var valueHex = Convert.ToHexString(e.Value);
                return new IccTlvEntryDto(tagHex, e.Length, valueHex, EmvTlvParser.GetTagName(tagHex));
            }).ToList();
        }

        // --- Encrypt Credential (legacy Pages/Tools/EncryptCreden) ------------------------

        public string EncryptCredential(string value, string configType)
        {
            if (string.Equals(configType, "XML", StringComparison.OrdinalIgnoreCase))
            {
                var desKeyHex = _resources.GetDesConfigKeyHex()
                    ?? throw new HsmUnavailableException("Resources.bin's DES.Config key is not loaded — cannot encrypt in XML mode.");

                return LegacyDesTextCipher.EncryptText(value, desKeyHex);
            }

            var aesKeyHex = _resources.GetAesConfigKeyHex();
            var aesIvHex = _resources.GetAesConfigIvHex();
            if (aesKeyHex == null || aesIvHex == null)
                throw new HsmUnavailableException("Resources.bin's AES.Config key/IV is not loaded — cannot encrypt in JSON mode.");

            return AesConfigCipher.Encrypt(value, aesKeyHex, aesIvHex);
        }

        public string DecryptCredential(string value, string configType)
        {
            if (string.Equals(configType, "XML", StringComparison.OrdinalIgnoreCase))
            {
                var desKeyHex = _resources.GetDesConfigKeyHex()
                    ?? throw new HsmUnavailableException("Resources.bin's DES.Config key is not loaded — cannot encrypt in XML mode.");

                return LegacyDesTextCipher.DecryptText(value, desKeyHex);
            }

            var aesKeyHex = _resources.GetAesConfigKeyHex();
            var aesIvHex = _resources.GetAesConfigIvHex();
            if (aesKeyHex == null || aesIvHex == null)
                throw new HsmUnavailableException("Resources.bin's AES.Config key/IV is not loaded — cannot encrypt in JSON mode.");

            return AesConfigCipher.Decrypt(value, aesKeyHex, aesIvHex);
        }

        private static bool IsHex(string input) => input.Length > 0 && input.All(Uri.IsHexDigit);
    }
}
