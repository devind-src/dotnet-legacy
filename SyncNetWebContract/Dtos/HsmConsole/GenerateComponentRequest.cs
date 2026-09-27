using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmConsole
{
    public class GenerateComponentRequest
    {
        [Range(16, 48)]
        public int Length { get; set; } = 48;

        /// <summary>"none" (default), "odd" or "even".</summary>
        public string Parity { get; set; } = "none";

        [Range(1, 5)]
        public int Count { get; set; } = 1;
    }
}
