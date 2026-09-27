using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.KeyManagement
{
    /// <summary>Replaces the old KeyLength-only request now that this endpoint runs real
    /// NbHSM-equivalent math (2026-09-09): a session key is generated wrapped both under LMK_1
    /// and under a ZMK, so it needs the caller's already-generated Master Key value (encrypted
    /// under LMK_1, currently sitting in the form's "Master Key" field) as input — the session
    /// key's length is derived from it, matching legacy NbHSM.GenerateSessionKey(Key) exactly
    /// (int KeyLength = Key.Length).</summary>
    public class GenerateSessionKeyRequest
    {
        [Required]
        public string MasterKeyUnderLmk { get; set; } = string.Empty;
    }
}
