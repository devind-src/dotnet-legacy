namespace SyncNetApi.Dtos.KeyManagement
{
    /// <summary>KeyLength: "1"=Single (8 bytes), "2"=Double (16 bytes), "3"=Triple (24 bytes)
    /// — same convention as the legacy Key Length dropdown.</summary>
    public class GenerateKeyBundleRequest
    {
        public string KeyLength { get; set; } = "2";
    }
}
