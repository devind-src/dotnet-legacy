namespace SyncNetApi.Services.Hsm
{
    public record HsmComponentResult(string Component, string Kcv);
    public record HsmZmkResult(string EncryptedKey, string KeyCheckValue);
    public record HsmMasterKeyResult(string EncryptedKey, string Kcv);
    public record HsmSessionKeyResult(string KeyUnderLmk, string KeyUnderZmk, string Kcv);
}
