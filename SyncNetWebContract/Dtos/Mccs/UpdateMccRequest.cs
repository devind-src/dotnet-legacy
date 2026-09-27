using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Mccs
{
    /// <summary>MccCode is immutable after create — only the other fields change.</summary>
    public class UpdateMccRequest
    {
        [MaxLength(200)]
        public string? MccDesc { get; set; }

        [MaxLength(5)]
        public string? FloorLimit { get; set; }

        [MaxLength(4)]
        public string? Currency { get; set; }

        public bool Active { get; set; } = true;
    }
}
