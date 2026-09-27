using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Brands;
using SyncNetApi.Services.Brands;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/brands")]
    public class BrandsController : PermissionGatedControllerBase
    {
        private readonly IBrandService _brands;

        public BrandsController(IBrandService brands, IUserService users) : base(users)
        {
            _brands = brands;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _brands.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BrandDto>> GetById(int id)
        {
            var item = await _brands.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<BrandDto>> Create(CreateBrandRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add brands.");

            var created = await _brands.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BrandDto>> Update(int id, UpdateBrandRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit brands.");

            try
            {
                return Ok(await _brands.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Brand not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete brands.");

            try
            {
                await _brands.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Brand not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
