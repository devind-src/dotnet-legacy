using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.VaTransRequests;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaTransRequests;

namespace SyncNetApi.Controllers
{
    /// <summary>Virtual Account &gt; Approval — lists every Topup/Adjustment request regardless
    /// of type and lets an admin/spv approve or reject it. Approve mutates va_account.balance
    /// and inserts a row into sw_trans_pg (the core switch ledger) — see
    /// VaTransRequestService.ApproveAsync.</summary>
    [Route("api/v1/va-approval")]
    public class VaApprovalController : PermissionGatedControllerBase
    {
        private readonly IVaTransRequestService _requests;

        public VaApprovalController(IVaTransRequestService requests, IUserService users) : base(users)
        {
            _requests = requests;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<VaTransRequestDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _requests.GetApprovalRecordsAsync(filter));

        [HttpGet("{tranNr:int}")]
        public async Task<ActionResult<VaTransRequestDto>> GetById(int tranNr)
        {
            var item = await _requests.GetByIdAsync(tranNr);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("{tranNr:int}/approve")]
        public async Task<ActionResult<VaTransRequestDto>> Approve(int tranNr)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to approve VA transaction requests.");

            try
            {
                return Ok(await _requests.ApproveAsync(tranNr, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Request not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Cannot approve request", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPost("{tranNr:int}/reject")]
        public async Task<ActionResult<VaTransRequestDto>> Reject(int tranNr)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to reject VA transaction requests.");

            try
            {
                return Ok(await _requests.RejectAsync(tranNr, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Request not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Cannot reject request", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }
    }
}
