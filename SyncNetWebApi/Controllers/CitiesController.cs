using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Cities;
using SyncNetApi.Services.Cities;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/cities")]
    public class CitiesController : PermissionGatedControllerBase
    {
        private readonly ICityService _cities;

        public CitiesController(ICityService cities, IUserService users) : base(users)
        {
            _cities = cities;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CityDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _cities.GetRecordsAsync(filter));

        [HttpGet("{id:long}")]
        public async Task<ActionResult<CityDto>> GetById(long id)
        {
            var item = await _cities.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CityDto>> Create(CreateCityRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add cities.");

            var created = await _cities.CreateAsync(request, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<CityDto>> Update(long id, UpdateCityRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit cities.");

            try
            {
                return Ok(await _cities.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "City not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete cities.");

            try
            {
                await _cities.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "City not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
