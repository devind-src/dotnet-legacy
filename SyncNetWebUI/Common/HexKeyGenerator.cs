using System.Security.Cryptography;

namespace SyncNetWasm.Common
{
    /// <summary>Client-side random hex generator for API client credentials (Credential /
    /// sw_terminal_client secret_id/secret_key/master_key/session_key/callback_secret_key) —
    /// mirrors the legacy "Generate" buttons backed by NbRandom.GenerateHex.</summary>
    public static class HexKeyGenerator
    {
        public static string Generate(int byteLength = 16)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
