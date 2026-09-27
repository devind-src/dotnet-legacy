using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Provinces;
using SyncNetApi.Services.Provinces;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/provinces")]
    public class ProvincesController : PermissionGatedControllerBase
    {
        private readonly IProvinceService _provinces;

        public ProvincesController(IProvinceService provinces, IUserService users) : base(users)
        {
            _provinces = provinces;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProvinceDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _provinces.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ProvinceDto>> GetById(long id)
        {
            var item = await _provinces.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProvinceDto>> Create(CreateProvinceRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add provinces.");

            var created = await _provinces.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<ProvinceDto>> Update(long id, UpdateProvinceRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit provinces.");

            try
            {
                return Ok(await _provinces.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Province not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete provinces.");

            try
            {
                await _provinces.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Province not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
