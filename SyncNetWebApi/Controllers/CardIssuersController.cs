using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardIssuers;
using SyncNetApi.Services.CardIssuers;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-issuers")]
    public class CardIssuersController : PermissionGatedControllerBase
    {
        private readonly ICardIssuerService _issuers;

        public CardIssuersController(ICardIssuerService issuers, IUserService users) : base(users)
        {
            _issuers = issuers;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardIssuerDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _issuers.GetRecordsAsync(filter));

        [HttpGet("{issuer}")]
        public async Task<ActionResult<CardIssuerDto>> GetById(string issuer)
        {
            var item = await _issuers.GetByIdAsync(issuer);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardIssuerDto>> Create(CreateCardIssuerRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add issuers.");

            try
            {
                var created = await _issuers.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { issuer = created.Issuer }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Issuer already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{issuer}")]
        public async Task<ActionResult<CardIssuerDto>> Update(string issuer, UpdateCardIssuerRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit issuers.");

            try
            {
                return Ok(await _issuers.UpdateAsync(issuer, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Issuer not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{issuer}")]
        public async Task<IActionResult> Delete(string issuer)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete issuers.");

            try
            {
                await _issuers.DeleteAsync(issuer, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Issuer not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Issuer is in use", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }
    }
}
