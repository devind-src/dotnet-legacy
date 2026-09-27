using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.VaTransRequests;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaTransRequests;

namespace SyncNetApi.Controllers
{
    /// <summary>Virtual Account &gt; Adjustment — create-request only, no Update/Delete (mirrors
    /// legacy: once created a request is read-only until acted on via VaApprovalController).</summary>
    [Route("api/v1/va-adjustment")]
    public class VaAdjustmentController : PermissionGatedControllerBase
    {
        private readonly IVaTransRequestService _requests;

        public VaAdjustmentController(IVaTransRequestService requests, IUserService users) : base(users)
        {
            _requests = requests;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<VaTransRequestDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _requests.GetAdjustmentRecordsAsync(filter));

        [HttpGet("{tranNr:int}")]
        public async Task<ActionResult<VaTransRequestDto>> GetById(int tranNr)
        {
            var item = await _requests.GetByIdAsync(tranNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<VaTransRequestDto>> Create(CreateVaAdjustmentRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to request VA adjustments.");

            try
            {
                var created = await _requests.CreateAdjustmentAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { tranNr = created.TranNr }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid adjustment request", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }
    }
}
