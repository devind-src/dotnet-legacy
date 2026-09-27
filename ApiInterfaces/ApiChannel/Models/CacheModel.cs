using Microsoft.AspNetCore.Http;

namespace ApiChannel.Models
{
    public class CacheModel
    {
        public string json { get; set; }
        public HttpContext ctx { get; set; }
        // Stopwatch.GetTimestamp() saat request dikirim ke core, untuk latency health check
        public long sent_ts { get; set; }
    }
}
