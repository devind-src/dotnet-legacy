namespace SyncNetPro.Sdk.Tracing;

/// <summary>Masking data sensitif untuk trace (setara <c>DataHelper.GetMasking</c>: seluruh karakter menjadi <c>*</c>).</summary>
public static class SensitiveData
{
    /// <summary>Mengganti seluruh karakter dengan <c>*</c> (panjang dipertahankan).</summary>
    public static string? Mask(string? value) => string.IsNullOrEmpty(value) ? value : new string('*', value.Length);
}
