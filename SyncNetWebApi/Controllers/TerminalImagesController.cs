using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.TerminalImages;
using SyncNetApi.Services.TerminalImages;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/terminal-images")]
    public class TerminalImagesController : PermissionGatedControllerBase
    {
        private readonly ITerminalImageService _images;

        public TerminalImagesController(ITerminalImageService images, IUserService users) : base(users)
        {
            _images = images;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TerminalImageDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _images.GetRecordsAsync(filter));

        [HttpGet("{name}")]
        public async Task<ActionResult<TerminalImageDto>> GetByName(string name)
        {
            var image = await _images.GetByNameAsync(name);
            return image == null ? NotFound() : Ok(image);
        }

        [HttpPost]
        public async Task<ActionResult<TerminalImageDto>> Create(CreateTerminalImageRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add terminal images.");

            try
            {
                var created = await _images.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetByName), new { name = created.Name }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Terminal image already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{name}")]
        public async Task<ActionResult<TerminalImageDto>> Update(string name, UpdateTerminalImageRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit terminal images.");

            try
            {
                return Ok(await _images.UpdateAsync(name, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Terminal image not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete terminal images.");

            try
            {
                await _images.DeleteAsync(name, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Terminal image not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpPost("{name}/banners/{slot:int}")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<TerminalImageDto>> UploadBanner(string name, int slot, IFormFile file)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit terminal images.");

            try
            {
                return Ok(await _images.UploadBannerAsync(name, slot, file, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Terminal image not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid upload", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }
    }
}
