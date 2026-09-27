using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteMargins
{
    /// <summary>Full desired set of routed products for one category — backs Routing &gt;
    /// Margin's checklist form. The server diffs this against what's currently routed for
    /// that category: codes not yet routed are added, previously routed codes missing from
    /// this list are removed. ProductCodes not actually in Category are ignored.</summary>
    public class SyncRouteMarginRequest
    {
        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        public List<string> ProductCodes { get; set; } = new();
    }
}
