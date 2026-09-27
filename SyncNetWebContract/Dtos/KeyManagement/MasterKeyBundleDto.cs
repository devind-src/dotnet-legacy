namespace SyncNetApi.Dtos.KeyManagement
{
    public record MasterKeyBundleDto(string MasterKey, string MasterKcv);

    public record SessionKeyBundleDto(string KeyUnderLmk, string KeyUnderZmk, string KeyCheckValue);
}
