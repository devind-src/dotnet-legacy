using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardBranches;
using SyncNetApi.Services.CardBranches;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-branches")]
    public class CardBranchesController : PermissionGatedControllerBase
    {
        private readonly ICardBranchService _branches;

        public CardBranchesController(ICardBranchService branches, IUserService users) : base(users)
        {
            _branches = branches;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardBranchDto>>> GetByIssuer([FromQuery] string issuer)
            => Ok(await _branches.GetByIssuerAsync(issuer));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CardBranchDto>> GetById(int id)
        {
            var item = await _branches.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardBranchDto>> Create(CreateCardBranchRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add branches.");

            try
            {
                var created = await _branches.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid branch", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CardBranchDto>> Update(int id, UpdateCardBranchRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit branches.");

            try
            {
                return Ok(await _branches.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Branch not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete branches.");

            try
            {
                await _branches.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Branch not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
