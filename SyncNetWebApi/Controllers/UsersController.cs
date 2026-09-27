using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Users;
using SyncNetApi.Services.Auth;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/users")]
    public class UsersController : PermissionGatedControllerBase
    {
        private readonly IUserService _users;
        private readonly IRefreshTokenService _refreshTokens;

        public UsersController(IUserService users, IRefreshTokenService refreshTokens) : base(users)
        {
            _users = users;
            _refreshTokens = refreshTokens;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _users.GetRecordsAsync(filter));

        [HttpGet("{userName}")]
        public async Task<ActionResult<UserDto>> GetByUserName(string userName)
        {
            var user = await _users.GetByUserNameAsync(userName);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add users.");

            try
            {
                var created = await _users.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetByUserName), new { userName = created.UserName }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "User already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{userName}")]
        public async Task<ActionResult<UserDto>> Update(string userName, UpdateUserRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit users.");

            try
            {
                return Ok(await _users.UpdateAsync(userName, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "User not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{userName}")]
        public async Task<IActionResult> Delete(string userName)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete users.");

            try
            {
                await _users.DeleteAsync(userName, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "User not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        /// <summary>Admin-initiated reset — no old password needed. Always forces
        /// must_change_password so the temporary password admin just picked can't be used
        /// indefinitely, and revokes every existing session for the account (an admin
        /// resetting someone's password is very often a response to a compromise). Gated
        /// on the "edit" right rather than adding a fourth permission flag.</summary>
        [HttpPost("{userName}/reset-password")]
        public async Task<IActionResult> ResetPassword(string userName, ResetPasswordRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to reset other users' passwords.");

            try
            {
                await _users.ResetPasswordAsync(userName, request.NewPassword, CurrentUser, mustChangePassword: true);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "User not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }

            await _refreshTokens.RevokeAllForUserAsync(userName);
            return NoContent();
        }
    }
}
