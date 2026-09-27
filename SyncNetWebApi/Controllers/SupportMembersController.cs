using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.SupportMembers;
using SyncNetApi.Services.SupportMembers;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/support-members")]
    public class SupportMembersController : PermissionGatedControllerBase
    {
        private readonly ISupportMemberService _members;

        public SupportMembersController(ISupportMemberService members, IUserService users) : base(users)
        {
            _members = members;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SupportMemberDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _members.GetRecordsAsync(filter));

        [HttpGet("{memberId:int}")]
        public async Task<ActionResult<SupportMemberDto>> GetById(int memberId)
        {
            var item = await _members.GetByIdAsync(memberId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<SupportMemberDto>> Create(CreateSupportMemberRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add support members.");

            try
            {
                var created = await _members.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { memberId = created.MemberId }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{memberId:int}")]
        public async Task<ActionResult<SupportMemberDto>> Update(int memberId, UpdateSupportMemberRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit support members.");

            try
            {
                return Ok(await _members.UpdateAsync(memberId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Support member not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{memberId:int}")]
        public async Task<IActionResult> Delete(int memberId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete support members.");

            try
            {
                await _members.DeleteAsync(memberId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Support member not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
