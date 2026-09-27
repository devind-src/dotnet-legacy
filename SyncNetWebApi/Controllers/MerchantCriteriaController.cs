using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.MerchantCriteria;
using SyncNetApi.Services.MerchantCriteria;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/merchant-criteria")]
    public class MerchantCriteriaController : PermissionGatedControllerBase
    {
        private readonly IMerchantCriteriaService _criteria;

        public MerchantCriteriaController(IMerchantCriteriaService criteria, IUserService users) : base(users)
        {
            _criteria = criteria;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MerchantCriteriaDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _criteria.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<MerchantCriteriaDto>> GetById(long id)
        {
            var item = await _criteria.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<MerchantCriteriaDto>> Create(CreateMerchantCriteriaRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add merchant criteria.");

            try
            {
                var created = await _criteria.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Merchant criteria already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<MerchantCriteriaDto>> Update(long id, UpdateMerchantCriteriaRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit merchant criteria.");

            try
            {
                return Ok(await _criteria.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Merchant criteria not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Merchant criteria already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete merchant criteria.");

            try
            {
                await _criteria.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Merchant criteria not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
