using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.BusinessDates;
using SyncNetApi.Services.BusinessDates;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/business-dates")]
    public class BusinessDatesController : PermissionGatedControllerBase
    {
        private readonly IBusinessDateService _businessDates;

        public BusinessDatesController(IBusinessDateService businessDates, IUserService users) : base(users)
        {
            _businessDates = businessDates;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<BusinessDateDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _businessDates.GetRecordsAsync(filter));

        [HttpGet("{businessCalendar}")]
        public async Task<ActionResult<BusinessDateDto>> GetById(string businessCalendar)
        {
            var item = await _businessDates.GetByIdAsync(businessCalendar);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<BusinessDateDto>> Create(CreateBusinessDateRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add business calendars.");

            try
            {
                var created = await _businessDates.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { businessCalendar = created.BusinessCalendar }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Business calendar already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{businessCalendar}")]
        public async Task<ActionResult<BusinessDateDto>> Update(string businessCalendar, UpdateBusinessDateRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit business calendars.");

            try
            {
                return Ok(await _businessDates.UpdateAsync(businessCalendar, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Business calendar not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{businessCalendar}")]
        public async Task<IActionResult> Delete(string businessCalendar)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete business calendars.");

            try
            {
                await _businessDates.DeleteAsync(businessCalendar, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Business calendar not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
