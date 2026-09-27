using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SyncNetApi.Services.Auth
{
    /// <summary>
    /// Verifies passwords against the legacy Blazor app's DES scheme, ported from
    /// Library/NbCreden.cs + Cryptography/DesAlgorithm.cs. Crucially, this mirrors
    /// AccountController.Login in the legacy source exactly: it RE-ENCRYPTS the submitted
    /// "{userName}|{password}" and compares ciphertext against dashboard_user.password —
    /// it never decrypts the stored value. Uses TripleDES-ECB/NoPadding when the loaded key
    /// is 16 or 24 bytes, single DES when it's 8 bytes (both were supported by the legacy
    /// DesAlgorithm.EncryptHex based on key length; this deployment's key is 24 bytes).
    /// </summary>
    public class LegacyPasswordVerifier : ILegacyPasswordVerifier
    {
        private readonly LegacyResourcesLoader _resources;
        private readonly ILogger<LegacyPasswordVerifier> _logger;

        public LegacyPasswordVerifier(LegacyResourcesLoader resources, ILogger<LegacyPasswordVerifier> logger)
        {
            _resources = resources;
            _logger = logger;
        }

        public bool Matches(string userName, string plainPassword, string storedValue)
        {
            var keyHex = _resources.GetDesDatabaseKeyHex();
            if (keyHex == null)
            {
                _logger.LogWarning("Legacy DES key is not loaded — cannot verify '{UserName}' against the legacy scheme.", userName);
                return false;
            }

            try
            {
                var computed = Encrypt($"{userName}|{plainPassword}", keyHex);
                return string.Equals(computed, storedValue, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Legacy DES verification threw for '{UserName}'.", userName);
                return false;
            }
        }

        /// <summary>Port of NbCreden.Encrypt + DesAlgorithm.EncryptHex, collapsed from the
        /// original's hex-string/nibble bookkeeping to the equivalent byte-level operations
        /// (the legacy padding is always a whole number of bytes since every intermediate
        /// hex string it builds comes from BytesToHex, i.e. always an even length).</summary>
        private static string Encrypt(string plainText, string keyHex)
        {
            // Legacy NbConvert.StringToBytes converts one char at a time (Convert.ToByte on
            // each char), i.e. Latin-1 semantics — identical to Encoding.Latin1 for any
            // password made of standard printable characters (the only realistic case here).
            var message = Encoding.Latin1.GetBytes(plainText);
            var key = Convert.FromHexString(keyHex);

            var remainder = message.Length % 8;
            var padCount = remainder == 0 ? 0 : 8 - remainder;

            var padded = new byte[message.Length + padCount];
            Buffer.BlockCopy(message, 0, padded, 0, message.Length);
            for (var i = message.Length; i < padded.Length; i++) padded[i] = 0xFF;

            using SymmetricAlgorithm des = key.Length == 8 ? DES.Create() : TripleDES.Create();
            des.Key = key;
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.None;

            using var encryptor = des.CreateEncryptor();
            var cipherBlocks = encryptor.TransformFinalBlock(padded, 0, padded.Length);

            // 1-byte header holding the pad count, exactly like the legacy format.
            var result = new byte[1 + cipherBlocks.Length];
            result[0] = (byte)padCount;
            Buffer.BlockCopy(cipherBlocks, 0, result, 1, cipherBlocks.Length);

            return Convert.ToBase64String(result);
        }
    }
}
