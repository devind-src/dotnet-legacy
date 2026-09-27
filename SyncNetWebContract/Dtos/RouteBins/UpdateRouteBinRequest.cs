using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteBins
{
    /// <summary>GroupId is immutable after create — legacy renders the select disabled on
    /// edit.</summary>
    public class UpdateRouteBinRequest
    {
        [Required]
        public int NodeId { get; set; }
    }
}
