using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Stores;
using SyncNetApi.Services.Stores;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/stores")]
    public class StoresController : PermissionGatedControllerBase
    {
        private readonly IStoreService _stores;

        public StoresController(IStoreService stores, IUserService users) : base(users)
        {
            _stores = stores;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<StoreDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _stores.GetRecordsAsync(filter));

        [HttpGet("{storeId}")]
        public async Task<ActionResult<StoreDto>> GetById(string storeId)
        {
            var store = await _stores.GetByIdAsync(storeId);
            return store == null ? NotFound() : Ok(store);
        }

        [HttpPost]
        public async Task<ActionResult<StoreDto>> Create(CreateStoreRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add stores.");

            try
            {
                var created = await _stores.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { storeId = created.StoreId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Store already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{storeId}")]
        public async Task<ActionResult<StoreDto>> Update(string storeId, UpdateStoreRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit stores.");

            try
            {
                return Ok(await _stores.UpdateAsync(storeId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Store not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{storeId}")]
        public async Task<IActionResult> Delete(string storeId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete stores.");

            try
            {
                await _stores.DeleteAsync(storeId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Store not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Store in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
