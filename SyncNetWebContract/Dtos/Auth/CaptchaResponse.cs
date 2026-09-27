namespace SyncNetApi.Dtos.Auth
{
    /// <summary>Captcha untuk form login. Enabled = false berarti server tidak mewajibkan captcha
    /// (CaptchaId/ImageDataUri kosong). Jawaban tidak pernah dikirim ke client; server
    /// menyimpannya dan memverifikasi saat login (sekali pakai, kedaluwarsa singkat).</summary>
    public record CaptchaResponse(bool Enabled, string CaptchaId, string ImageDataUri);
}
