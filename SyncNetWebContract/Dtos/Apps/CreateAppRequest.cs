using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Apps
{
    /// <summary>app_name is varchar(20), host is varchar(30), command_port is varchar(5).
    /// AppType: "0" Transaction Processing / "1" Interface / "2" Terminal / "3" PCBase /
    /// "4" Mobile / "5" ATM / "6" Web / "7" Crypto / "8" Other.</summary>
    public class CreateAppRequest
    {
        [Required, MaxLength(20)]
        public string AppName { get; set; } = string.Empty;

        [Required, RegularExpression("^[0-8]$")]
        public string AppType { get; set; } = "1";

        [Required, MaxLength(30)]
        public string Host { get; set; } = "localhost";

        [Required, MaxLength(5)]
        public string CommandPort { get; set; } = string.Empty;
    }
}
