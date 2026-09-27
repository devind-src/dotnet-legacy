using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Mccs
{
    /// <summary>mcc_code is varchar(5) PK, mcc_desc varchar(200), floor_limit varchar(5),
    /// currency varchar(4) — real column widths.</summary>
    public class CreateMccRequest
    {
        [Required, MaxLength(5)]
        public string MccCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? MccDesc { get; set; }

        [MaxLength(5)]
        public string? FloorLimit { get; set; }

        [MaxLength(4)]
        public string? Currency { get; set; }
    }
}
