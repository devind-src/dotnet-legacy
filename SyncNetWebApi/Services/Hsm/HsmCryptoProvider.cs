using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SyncNetApi.Common;
using SyncNetApi.Cryptography;
using SyncNetApi.Services.Auth;

namespace SyncNetApi.Services.Hsm
{
    public class HsmCryptoProvider : IHsmCryptoProvider
    {
        private static readonly int[] ValidLengths = { 16, 32, 48 };

        private readonly LegacyResourcesLoader _resources;

        public HsmCryptoProvider(LegacyResourcesLoader resources)
        {
            _resources = resources;
        }

        public bool IsAvailable => _resources.IsHsmLmkLoaded;

        public HsmComponentResult GenerateComponent(int lengthHexChars, string parity)
        {
            EnsureAvailable();
            ValidateLength(lengthHexChars, nameof(lengthHexChars));

            var component = parity is "odd" or "even"
                ? GenerateParityConstrainedHex(lengthHexChars, parity)
                : RandomNumberGenerator.GetHexString(lengthHexChars, lowercase: false);

            return new HsmComponentResult(component, GetKeyCheckValue(component));
        }

        public string GetKeyCheckValue(string clearHex)
        {
            EnsureAvailable();
            ValidateLength(clearHex.Length, nameof(clearHex));

            var encrypted = SafeEncrypt("0000000000000000", clearHex);
            return encrypted[..6];
        }

        public string GetKeyCheckValueUnderLmk(string encryptedHex)
        {
            EnsureAvailable();
            ValidateLength(encryptedHex.Length, nameof(encryptedHex));

            return GetKeyCheckValue(GetClearKey(encryptedHex));
        }

        public string GetKeyEncrypted(string clearHex)
        {
            EnsureAvailable();
            ValidateLength(clearHex.Length, nameof(clearHex));

            return SafeEncrypt(clearHex, Lmk1);
        }

        public string GetClearKey(string encryptedHex)
        {
            EnsureAvailable();
            ValidateLength(encryptedHex.Length, nameof(encryptedHex));

            return SafeDecrypt(encryptedHex, Lmk1);
        }

        public HsmZmkResult GenerateZmk(IReadOnlyList<string> components, string inputMode)
        {
            EnsureAvailable();

            if (components.Count == 0)
                throw new ValidationException("At least one component is required to form a key.");

            foreach (var c in components)
                ValidateLength(c.Length, nameof(components));

            var clearComponents = inputMode == "encrypted"
                ? components.Select(GetClearKey).ToList()
                : components.ToList();

            var xor = clearComponents.Aggregate((a, b) => HsmDesCipher.XorHex(a, b));

            return new HsmZmkResult(GetKeyEncrypted(xor), GetKeyCheckValue(xor));
        }

        public HsmMasterKeyResult GenerateMasterKey(string keyLength)
        {
            EnsureAvailable();

            var lengthHexChars = KeyLengthToHexChars(keyLength);
            var clear = RandomNumberGenerator.GetHexString(lengthHexChars, lowercase: false);

            return new HsmMasterKeyResult(GetKeyEncrypted(clear), GetKeyCheckValue(clear));
        }

        public HsmSessionKeyResult GenerateSessionKey(string masterKeyUnderLmk)
        {
            EnsureAvailable();
            ValidateLength(masterKeyUnderLmk.Length, nameof(masterKeyUnderLmk));

            var clearSession = RandomNumberGenerator.GetHexString(masterKeyUnderLmk.Length, lowercase: false);
            var clearZmk = GetClearKey(masterKeyUnderLmk);

            var keyUnderLmk = GetKeyEncrypted(clearSession);
            var keyUnderZmk = SafeEncrypt(clearSession, clearZmk);
            var kcv = GetKeyCheckValue(clearSession);

            return new HsmSessionKeyResult(keyUnderLmk, keyUnderZmk, kcv);
        }

        private string Lmk1 => _resources.GetHsmLmk1Hex()
            ?? throw new HsmUnavailableException("HSM LMK belum dimuat (Resources.bin/PrivateKey.pem belum dikonfigurasi atau gagal didekripsi).");

        private void EnsureAvailable()
        {
            if (!IsAvailable)
                throw new HsmUnavailableException("HSM LMK belum dimuat (Resources.bin/PrivateKey.pem belum dikonfigurasi atau gagal didekripsi).");
        }

        private static void ValidateLength(int hexLength, string paramName)
        {
            if (!ValidLengths.Contains(hexLength))
                throw new ValidationException($"'{paramName}' must be 16, 32 or 48 hex characters long (got {hexLength}).");
        }

        /// <summary>.NET's DES/TripleDES refuse a handful of degenerate key values (all-zero,
        /// and a few other known-weak patterns) by throwing CryptographicException from the
        /// Key setter. This is a real, reachable case here — not just a theoretical one — e.g.
        /// XOR-ing two IDENTICAL components in "Form a Key" legitimately produces an all-zero
        /// key. Translated to ValidationException (400) so it surfaces as a normal "try
        /// different components" error instead of an unhandled 500.</summary>
        private static string SafeEncrypt(string dataHex, string keyHex)
        {
            try { return HsmDesCipher.EncryptHex(dataHex, keyHex); }
            catch (CryptographicException) { throw new ValidationException("Nilai key/component yang dihasilkan adalah weak key (mis. hasil XOR bernilai nol) — coba component lain."); }
        }

        private static string SafeDecrypt(string dataHex, string keyHex)
        {
            try { return HsmDesCipher.DecryptHex(dataHex, keyHex); }
            catch (CryptographicException) { throw new ValidationException("Nilai key/component yang dihasilkan adalah weak key (mis. hasil XOR bernilai nol) — coba component lain."); }
        }

        private static int KeyLengthToHexChars(string keyLength) => keyLength switch
        {
            "1" => 16,
            "3" => 48,
            _ => 32
        };

        /// <summary>Legacy's "parity" is a check on the generated byte's numeric value
        /// (value % 2), not true DES key-parity bit counting — replicated exactly, using a
        /// CSPRNG (RandomNumberGenerator) instead of legacy's System.Random for the underlying
        /// randomness, same substitution already used throughout this project's key generation
        /// (see KeyManagementController).</summary>
        private static string GenerateParityConstrainedHex(int lengthHexChars, string parity)
        {
            var result = new System.Text.StringBuilder(lengthHexChars);

            while (result.Length < lengthHexChars)
            {
                int value;
                do
                {
                    value = RandomNumberGenerator.GetInt32(0, 256);
                }
                while ((parity == "odd" && value % 2 == 0) || (parity == "even" && value % 2 != 0));

                result.Append(value.ToString("X2"));
            }

            return result.ToString(0, lengthHexChars);
        }
    }
}
