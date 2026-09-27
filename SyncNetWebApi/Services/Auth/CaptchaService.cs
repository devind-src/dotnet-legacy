using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SyncNetApi.Dtos.Auth;
using SyncNetApi.Options;

namespace SyncNetApi.Services.Auth
{
    /// <summary>Captcha gambar (SVG) buatan sendiri: teks acak dengan rotasi, warna, dan garis
    /// pengganggu. Tanpa layanan eksternal karena dashboard ini dipakai di jaringan internal.
    /// Sengaja tanpa huruf/angka yang mirip (0/O, 1/I/L).</summary>
    public class CaptchaService : ICaptchaService
    {
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int Width = 170;
        private const int Height = 56;
        private static readonly string[] Colors = { "#1b3a6b", "#7a1f1f", "#1f6b3a", "#5b2a86", "#8a4b00", "#0f5f6b" };

        private readonly IMemoryCache _cache;
        private readonly CaptchaOptions _options;

        public CaptchaService(IMemoryCache cache, IOptions<CaptchaOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        public bool Enabled => _options.Enabled;

        public CaptchaResponse Generate()
        {
            if (!_options.Enabled)
                return new CaptchaResponse(false, string.Empty, string.Empty);

            int length = Math.Clamp(_options.Length, 4, 8);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            string answer = new(chars);

            string id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            _cache.Set(CacheKey(id), answer, TimeSpan.FromMinutes(Math.Max(1, _options.ExpiryMinutes)));

            string svg = RenderSvg(answer);
            string dataUri = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            return new CaptchaResponse(true, id, dataUri);
        }

        public bool Validate(string? captchaId, string? answer)
        {
            if (!_options.Enabled) return true;
            if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(answer)) return false;

            string key = CacheKey(captchaId);
            if (!_cache.TryGetValue(key, out string? expected) || expected == null) return false;

            _cache.Remove(key); // one attempt per captcha

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(answer.Trim().ToUpperInvariant()));
        }

        private static string CacheKey(string id) => "captcha:" + id;

        private static string RenderSvg(string text)
        {
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{Height}\" viewBox=\"0 0 {Width} {Height}\">");
            sb.Append($"<rect width=\"{Width}\" height=\"{Height}\" fill=\"#f4f6fb\"/>");

            for (int i = 0; i < 7; i++)
            {
                sb.Append($"<line x1=\"{Rand(Width)}\" y1=\"{Rand(Height)}\" x2=\"{Rand(Width)}\" y2=\"{Rand(Height)}\" " +
                          $"stroke=\"{Colors[Rand(Colors.Length)]}\" stroke-opacity=\"0.35\" stroke-width=\"{1 + Rand(2)}\"/>");
            }

            double step = (double)(Width - 24) / text.Length;
            for (int i = 0; i < text.Length; i++)
            {
                double x = 14 + step * i + Rand(6);
                double y = 36 + Rand(8);
                int rotate = Rand(41) - 20;
                int size = 28 + Rand(8);
                sb.Append($"<text x=\"{x:0.#}\" y=\"{y:0.#}\" font-family=\"Verdana,Arial,sans-serif\" font-weight=\"700\" " +
                          $"font-size=\"{size}\" fill=\"{Colors[Rand(Colors.Length)]}\" transform=\"rotate({rotate} {x:0.#} {y:0.#})\">{text[i]}</text>");
            }

            for (int i = 0; i < 25; i++)
                sb.Append($"<circle cx=\"{Rand(Width)}\" cy=\"{Rand(Height)}\" r=\"1\" fill=\"{Colors[Rand(Colors.Length)]}\" fill-opacity=\"0.5\"/>");

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static int Rand(int max) => RandomNumberGenerator.GetInt32(max);
    }
}
