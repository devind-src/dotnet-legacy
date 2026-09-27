using SyncNetApi.Dtos.Auth;

namespace SyncNetApi.Services.Auth
{
    public interface ICaptchaService
    {
        bool Enabled { get; }

        /// <summary>Buat captcha baru. Jawaban disimpan di server saja.</summary>
        CaptchaResponse Generate();

        /// <summary>Verifikasi jawaban. Captcha dihapus setelah dicoba, benar atau salah, sehingga
        /// satu captcha hanya berlaku untuk satu percobaan login.</summary>
        bool Validate(string? captchaId, string? answer);
    }
}
