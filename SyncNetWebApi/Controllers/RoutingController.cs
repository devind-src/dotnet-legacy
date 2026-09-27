using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Services.Routing;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/routing")]
    public class RoutingController : PermissionGatedControllerBase
    {
        private readonly IRoutingApplyService _apply;

        public RoutingController(IRoutingApplyService apply, IUserService users) : base(users)
        {
            _apply = apply;
        }

        /// <summary>Terapkan Perubahan: RESYNC ke aplikasi yang memakai routing. Tidak dibatasi role
        /// (K22): semua user yang bisa membuka menu terkait boleh mengirim.</summary>
        [HttpPost("apply")]
        public async Task<ActionResult<RoutingApplyResultDto>> Apply()
            => Ok(await _apply.ApplyAsync());
    }
}
