using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.VaTransRequests;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaTransRequests;

namespace SyncNetApi.Controllers
{
    /// <summary>Virtual Account &gt; Topup — create-request only, no Update/Delete (mirrors
    /// legacy: once created a request is read-only until acted on via VaApprovalController).</summary>
    [Route("api/v1/va-topup")]
    public class VaTopupController : PermissionGatedControllerBase
    {
        private readonly IVaTransRequestService _requests;

        public VaTopupController(IVaTransRequestService requests, IUserService users) : base(users)
        {
            _requests = requests;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<VaTransRequestDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _requests.GetTopupRecordsAsync(filter));

        [HttpGet("{tranNr:int}")]
        public async Task<ActionResult<VaTransRequestDto>> GetById(int tranNr)
        {
            var item = await _requests.GetByIdAsync(tranNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<VaTransRequestDto>> Create(CreateVaTopupRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to request VA topups.");

            try
            {
                var created = await _requests.CreateTopupAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { tranNr = created.TranNr }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid topup request", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }
    }
}
