using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.PublicHolidays;
using SyncNetApi.Services.PublicHolidays;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/public-holidays")]
    public class PublicHolidaysController : PermissionGatedControllerBase
    {
        private readonly IPublicHolidayService _holidays;

        public PublicHolidaysController(IPublicHolidayService holidays, IUserService users) : base(users)
        {
            _holidays = holidays;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PublicHolidayDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _holidays.GetRecordsAsync(filter));

        [HttpGet("{holidayDate}")]
        public async Task<ActionResult<PublicHolidayDto>> GetById(string holidayDate)
        {
            var item = await _holidays.GetByIdAsync(holidayDate);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<PublicHolidayDto>> Create(CreatePublicHolidayRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add public holidays.");

            try
            {
                var created = await _holidays.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { holidayDate = created.HolidayDate }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Public holiday already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{holidayDate}")]
        public async Task<ActionResult<PublicHolidayDto>> Update(string holidayDate, UpdatePublicHolidayRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit public holidays.");

            try
            {
                return Ok(await _holidays.UpdateAsync(holidayDate, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Public holiday not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{holidayDate}")]
        public async Task<IActionResult> Delete(string holidayDate)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete public holidays.");

            try
            {
                await _holidays.DeleteAsync(holidayDate, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Public holiday not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
