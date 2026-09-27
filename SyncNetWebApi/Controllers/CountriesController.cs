using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Countries;
using SyncNetApi.Services.Countries;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/countries")]
    public class CountriesController : PermissionGatedControllerBase
    {
        private readonly ICountryService _countries;

        public CountriesController(ICountryService countries, IUserService users) : base(users)
        {
            _countries = countries;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CountryDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _countries.GetRecordsAsync(filter));

        [HttpGet("{name}")]
        public async Task<ActionResult<CountryDto>> GetById(string name)
        {
            var item = await _countries.GetByIdAsync(name);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CountryDto>> Create(CreateCountryRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add countries.");

            try
            {
                var created = await _countries.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { name = created.Name }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Country already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{name}")]
        public async Task<ActionResult<CountryDto>> Update(string name, UpdateCountryRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit countries.");

            try
            {
                return Ok(await _countries.UpdateAsync(name, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Country not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete countries.");

            try
            {
                await _countries.DeleteAsync(name, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Country not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
