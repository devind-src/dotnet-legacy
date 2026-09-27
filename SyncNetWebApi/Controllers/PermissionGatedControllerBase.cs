using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Users;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Shared "who is the caller, and what are their effective Add/Edit/Delete
    /// rights" plumbing — originally lived only in UsersController, extracted so every module
    /// controller can reuse the same global CanAdd/CanEdit/CanDelete gate instead of
    /// re-deriving it per module. See PROJECT_TECHNICAL_SUMMARY.md §4 for why these flags are
    /// treated as global, not per-module.
    /// <br/><br/>
    /// [Authorize] (no Roles=) rather than [Authorize(Roles = "admin")] — corrected 2026-09-09
    /// after user confirmed the intended rule: ANY logged-in user can read (GET) every
    /// endpoint; only Add/Edit/Delete are gated, purely by the caller's effective
    /// CanAdd/CanEdit/CanDelete (dashboard_user.allow_* overriding dashboard_role.flag_*), not
    /// by role name. Previously this class required role="admin" outright, which silently
    /// locked out every non-admin account (spv/ops/mon) regardless of their allow_* flags —
    /// dashboard_user already had spv/mon/ops accounts with allow_add/edit/delete all true that
    /// could never reach any of the 63 controllers inheriting this class. Every mutating action
    /// across those controllers already calls CanAsync(...) before making a change, so removing
    /// the role restriction here doesn't remove any enforcement — it just lets that check run
    /// instead of being preempted by the framework's role check.</summary>
    [ApiController]
    [Authorize]
    public abstract class PermissionGatedControllerBase : ControllerBase
    {
        private readonly IUserService _users;

        protected PermissionGatedControllerBase(IUserService users)
        {
            _users = users;
        }

        // JWTs issued by JwtTokenService only carry a "sub" claim. JwtBearerHandler's default
        // inbound claim mapping (JwtBearerOptions.MapInboundClaims defaults to true) renames
        // "sub" to ClaimTypes.NameIdentifier on the way in — NOT ClaimTypes.Name — so that's
        // where the username actually ends up on User.Claims at request time.
        protected string CurrentUser => User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "system";

        /// <summary>Resolves the CALLER's own effective permissions (not the target row's)
        /// and applies <paramref name="selector"/> to them. A caller who somehow doesn't
        /// resolve to a real dashboard_user row (shouldn't happen once [Authorize] has
        /// already passed, but defensive) is treated as having no rights rather than
        /// throwing.</summary>
        protected async Task<bool> CanAsync(Func<UserPermissions, bool> selector)
        {
            try
            {
                var permissions = await _users.GetEffectivePermissionsAsync(CurrentUser);
                return selector(permissions);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        protected static ObjectResult Forbidden(string detail) => new(new ProblemDetails
        {
            Title = "Forbidden",
            Detail = detail,
            Status = StatusCodes.Status403Forbidden
        })
        { StatusCode = StatusCodes.Status403Forbidden };
    }
}
