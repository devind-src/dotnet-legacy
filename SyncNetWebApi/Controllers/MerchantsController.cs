using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Merchants;
using SyncNetApi.Services.Merchants;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/merchants")]
    public class MerchantsController : PermissionGatedControllerBase
    {
        private readonly IMerchantService _merchants;

        public MerchantsController(IMerchantService merchants, IUserService users) : base(users)
        {
            _merchants = merchants;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MerchantDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _merchants.GetRecordsAsync(filter));

        [HttpGet("{merchantId}")]
        public async Task<ActionResult<MerchantDto>> GetById(string merchantId)
        {
            var merchant = await _merchants.GetByIdAsync(merchantId);
            return merchant == null ? NotFound() : Ok(merchant);
        }

        [HttpPost]
        public async Task<ActionResult<MerchantDto>> Create(CreateMerchantRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add merchants.");

            try
            {
                var created = await _merchants.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { merchantId = created.MerchantId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Merchant already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{merchantId}")]
        public async Task<ActionResult<MerchantDto>> Update(string merchantId, UpdateMerchantRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit merchants.");

            try
            {
                return Ok(await _merchants.UpdateAsync(merchantId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Merchant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{merchantId}")]
        public async Task<IActionResult> Delete(string merchantId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete merchants.");

            try
            {
                await _merchants.DeleteAsync(merchantId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Merchant not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Merchant in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
