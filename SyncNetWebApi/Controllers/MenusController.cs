using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Menus;
using SyncNetApi.Services.Menus;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/menus")]
    public class MenusController : PermissionGatedControllerBase
    {
        private readonly IMenuService _menus;

        public MenusController(IMenuService menus, IUserService users) : base(users)
        {
            _menus = menus;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MenuDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _menus.GetRecordsAsync(filter));

        [HttpGet("{menuId:int}")]
        public async Task<ActionResult<MenuDto>> GetById(int menuId)
        {
            var menu = await _menus.GetByIdAsync(menuId);
            return menu == null ? NotFound() : Ok(menu);
        }

        [HttpPost]
        public async Task<ActionResult<MenuDto>> Create(CreateMenuRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add menus.");

            try
            {
                var created = await _menus.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { menuId = created.MenuId }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Menu already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{menuId:int}")]
        public async Task<ActionResult<MenuDto>> Update(int menuId, UpdateMenuRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit menus.");

            try
            {
                return Ok(await _menus.UpdateAsync(menuId, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Menu not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{menuId:int}")]
        public async Task<IActionResult> Delete(int menuId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete menus.");

            try
            {
                await _menus.DeleteAsync(menuId, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Menu not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
