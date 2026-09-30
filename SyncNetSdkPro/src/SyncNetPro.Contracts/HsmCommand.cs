namespace SyncNetPro.Contracts;

/// <summary>
/// Perintah HSM yang dibawa pesan. Diserialisasi sebagai angka (<c>security.hsm_cmd</c>).
/// Nilai 0–2 dipakai Core, 3–4 dapat diisi interface.
/// </summary>
public enum HsmCommand
{
    /// <summary>Generate key node (dipakai Core).</summary>
    GenerateKey = 0,

    /// <summary>Generate key terminal (dipakai Core).</summary>
    GenerateKeyTerminal = 1,

    /// <summary>Translate key (dipakai Core).</summary>
    TranslateKey = 2,

    /// <summary>Translate PIN block antar node.</summary>
    TranslatePinblock = 3,

    /// <summary>Translate PIN block dari terminal.</summary>
    TranslatePinblockTerminal = 4,
}
