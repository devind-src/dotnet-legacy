using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Auth;
using SyncNetApi.Entities;
using SyncNetApi.Options;
using SyncNetApi.Services.Auth;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _users;
        private readonly IPasswordService _passwords;
        private readonly IJwtTokenService _jwt;
        private readonly ITokenBlacklistService _blacklist;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IPasswordResetService _passwordReset;
        private readonly SyncNetDbContext _context;
        private readonly JwtOptions _jwtOptions;
        private readonly ICaptchaService _captcha;

        public AuthController(
            IUserService users,
            IPasswordService passwords,
            IJwtTokenService jwt,
            ITokenBlacklistService blacklist,
            IRefreshTokenService refreshTokens,
            IPasswordResetService passwordReset,
            SyncNetDbContext context,
            IOptions<JwtOptions> jwtOptions,
            ICaptchaService captcha)
        {
            _users = users;
            _passwords = passwords;
            _jwt = jwt;
            _blacklist = blacklist;
            _refreshTokens = refreshTokens;
            _passwordReset = passwordReset;
            _context = context;
            _jwtOptions = jwtOptions.Value;
            _captcha = captcha;
        }

        /// <summary>Captcha baru untuk form login (anonymous). Enabled=false bila dimatikan di server.</summary>
        [HttpGet("captcha")]
        [EnableRateLimiting("captcha")]
        [AllowAnonymous]
        public ActionResult<CaptchaResponse> GetCaptcha() => Ok(_captcha.Generate());

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Checked before anything account-related so a wrong captcha neither reveals whether
            // the user exists nor counts as a failed login attempt against the account.
            if (!_captcha.Validate(request.CaptchaId, request.CaptchaAnswer))
                return BadRequest(new ProblemDetails
                {
                    Title = "Captcha invalid",
                    Detail = "Captcha salah atau sudah kedaluwarsa. Silakan coba lagi.",
                    Status = StatusCodes.Status400BadRequest
                });

            var entity = await _users.GetEntityByUserNameAsync(request.UserName);
            if (entity == null)
                return Unauthorized(AuthProblem("Invalid username or password."));

            if (entity.retry >= (entity.max_retry ?? 3))
                return StatusCode(StatusCodes.Status423Locked, AuthProblem("Account is locked due to too many failed attempts. Ask an administrator to reset it."));

            if (entity.status != "1")
                return Unauthorized(AuthProblem("Account is not active."));

            var verify = _passwords.Verify(request.UserName, request.Password, entity.password ?? string.Empty);
            if (verify == PasswordVerifyResult.Failed)
            {
                await _users.RegisterFailedLoginAsync(request.UserName);
                return Unauthorized(AuthProblem("Invalid username or password."));
            }

            // Legacy DES password matched -> transparently upgrade to the modern hash now.
            if (verify == PasswordVerifyResult.SuccessNeedsRehash)
            {
                entity.password = _passwords.Hash(request.Password);
            }

            await _users.MarkLoginSuccessAsync(request.UserName);
            await LogActivity(request.UserName, "1");

            // Credentials are correct, but the account must set its own password before it
            // gets a real session — no access/refresh tokens are issued for this response.
            if (entity.must_change_password == true)
            {
                return Ok(new LoginChallengeResponse
                {
                    ChallengeRequired = LoginChallengeResponse.NewPasswordRequired,
                    UserName = entity.user_name
                });
            }

            var response = await IssueTokensAsync(entity.user_name, entity.role_name ?? string.Empty, entity.full_name ?? string.Empty);
            return Ok(response);
        }

        /// <summary>Completes the NEW_PASSWORD_REQUIRED challenge from /login. Anonymous
        /// (no session exists yet at this point) — re-verifies CurrentPassword itself rather
        /// than trusting that the client only calls this after a genuine challenge.</summary>
        [HttpPost("login/new-password")]
        [EnableRateLimiting("login")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteNewPassword(CompleteNewPasswordRequest request)
        {
            var entity = await _users.GetEntityByUserNameAsync(request.UserName);
            if (entity == null || entity.status != "1")
                return Unauthorized(AuthProblem("Invalid username or password."));

            var verify = _passwords.Verify(request.UserName, request.CurrentPassword, entity.password ?? string.Empty);
            if (verify == PasswordVerifyResult.Failed)
                return Unauthorized(AuthProblem("Invalid username or password."));

            await _users.ResetPasswordAsync(request.UserName, request.NewPassword, actingUser: request.UserName, mustChangePassword: false);
            await _refreshTokens.RevokeAllForUserAsync(request.UserName);

            await _users.MarkLoginSuccessAsync(request.UserName);
            await LogActivity(request.UserName, "1");

            var response = await IssueTokensAsync(entity.user_name, entity.role_name ?? string.Empty, entity.full_name ?? string.Empty);
            return Ok(response);
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _passwordReset.RequestResetAsync(request.Identifier, ip);

            // Always the same response — this endpoint must never reveal whether the
            // identifier matched an account.
            return Ok(new { message = "If that account exists, a password reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(CompletePasswordResetRequest request)
        {
            try
            {
                await _passwordReset.CompleteResetAsync(request.Token, request.NewPassword);
            }
            catch (Exception ex) when (ex is ValidationException or NotFoundException)
            {
                // Same generic message either way — a "user not found" here would only ever
                // happen for an already-issued token whose account got deleted, and
                // distinguishing that from "bad token" tells an attacker something real.
                return BadRequest(new ProblemDetails
                {
                    Title = "Password reset failed",
                    Detail = "Invalid or expired reset token.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return NoContent();
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request)
        {
            var hash = _jwt.HashRefreshToken(request.RefreshToken);
            var stored = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);

            if (stored == null)
                return Unauthorized(AuthProblem("Invalid refresh token."));

            if (!stored.IsActive)
            {
                // Reuse of an already-revoked/expired token is a strong signal of theft —
                // revoke every other active token for this user as a precaution.
                var others = await _context.RefreshTokens
                    .Where(x => x.UserName == stored.UserName && x.RevokedAt == null)
                    .ToListAsync();
                foreach (var t in others) t.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Unauthorized(AuthProblem("Refresh token has already been used or expired. Please log in again."));
            }

            var user = await _users.GetEntityByUserNameAsync(stored.UserName);
            if (user == null || user.status != "1")
                return Unauthorized(AuthProblem("Account is no longer active."));

            var response = await IssueTokensAsync(user.user_name, user.role_name ?? string.Empty, user.full_name ?? string.Empty, revokeAndReplace: stored);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (!string.IsNullOrEmpty(jti) && !string.IsNullOrEmpty(userName) && long.TryParse(expClaim, out var expUnix))
            {
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                await _blacklist.RevokeAsync(jti, userName, expiresAt);
            }

            if (request != null && !string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var hash = _jwt.HashRefreshToken(request.RefreshToken);
                var stored = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
                if (stored != null && stored.RevokedAt == null)
                {
                    stored.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrEmpty(userName))
                await LogActivity(userName, "0");

            return NoContent();
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userName)) return Unauthorized();

            try
            {
                await _users.ChangePasswordAsync(userName, request.OldPassword, request.NewPassword);
            }
            catch (InvalidCredentialsException ex)
            {
                return Unauthorized(AuthProblem(ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(AuthProblem(ex.Message));
            }

            // Deliberately chosen new password — kill every other session so a stolen
            // refresh token from before the change stops working immediately.
            await _refreshTokens.RevokeAllForUserAsync(userName);

            return NoContent();
        }

        private async Task<LoginResponse> IssueTokensAsync(string userName, string roleName, string fullName, RefreshToken? revokeAndReplace = null)
        {
            var access = _jwt.CreateAccessToken(userName, roleName);
            var rawRefresh = _jwt.CreateRefreshToken();
            var refreshHash = _jwt.HashRefreshToken(rawRefresh);
            var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

            if (revokeAndReplace != null)
            {
                revokeAndReplace.RevokedAt = DateTime.UtcNow;
                revokeAndReplace.ReplacedByTokenHash = refreshHash;
            }

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserName = userName,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt,
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            var menus = await _users.GetMenuAsync(roleName);
            var permissions = await _users.GetEffectivePermissionsAsync(userName);

            return new LoginResponse
            {
                AccessToken = access.Token,
                AccessTokenExpiresAt = access.ExpiresAt,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAt = refreshExpiresAt,
                UserName = userName,
                FullName = fullName,
                RoleName = roleName,
                Menus = menus,
                UserManagementPermissions = permissions
            };
        }

        private async Task LogActivity(string userName, string state)
        {
            var hostAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var hostName = Request.Host.Host;
            var userAgent = Request.Headers.UserAgent.ToString();

            await _users.LogActivityAsync(userName, hostAddress, hostName, userAgent, state);
        }

        private static ProblemDetails AuthProblem(string detail) => new()
        {
            Title = "Authentication failed",
            Detail = detail,
            Status = StatusCodes.Status401Unauthorized
        };
    }
}
