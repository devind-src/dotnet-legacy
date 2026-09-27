using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.HsmConsole;
using SyncNetApi.Services.Hsm;

namespace SyncNetApi.Controllers
{
    /// <summary>Port of legacy Pages/HSM/HsmConsole/Index.razor's 4-tab key-ceremony tool
    /// (Component / Encrypt Component / Form a Key / Key Check Value) — all math now lives
    /// server-side in IHsmCryptoProvider (real DES/3DES under LMK_1, not a fabricated
    /// placeholder) instead of client-side NbHSM calls. Plain [Authorize] — nothing is
    /// persisted, so any logged-in user with access to the HSM &gt; Console menu item can use
    /// it, same reasoning as KeyManagementController/LookupsController.</summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/hsm-console")]
    public class HsmConsoleController : ControllerBase
    {
        private readonly IHsmCryptoProvider _crypto;

        public HsmConsoleController(IHsmCryptoProvider crypto)
        {
            _crypto = crypto;
        }

        [HttpPost("generate-component")]
        public ActionResult<IReadOnlyList<ComponentResultDto>> GenerateComponent(GenerateComponentRequest request)
        {
            try
            {
                var results = Enumerable.Range(0, request.Count)
                    .Select(_ => _crypto.GenerateComponent(request.Length, request.Parity))
                    .Select(r => new ComponentResultDto(r.Component, r.Kcv))
                    .ToList();

                return Ok(results);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid component request", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }

        [HttpPost("encrypt-component")]
        public ActionResult<EncryptComponentResponseDto> EncryptComponent(EncryptComponentRequest request)
        {
            try
            {
                var encrypted = _crypto.GetKeyEncrypted(request.ClearComponent);
                var kcv = _crypto.GetKeyCheckValue(request.ClearComponent);
                return Ok(new EncryptComponentResponseDto(encrypted, kcv));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid component", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }

        [HttpPost("key-check-value")]
        public ActionResult<KeyCheckValueResponseDto> GetKeyCheckValue(KeyCheckValueRequest request)
        {
            try
            {
                return Ok(new KeyCheckValueResponseDto(_crypto.GetKeyCheckValue(request.Component)));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid component", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }

        [HttpPost("zmk/component-kcv")]
        public ActionResult<KeyCheckValueResponseDto> GetComponentKcv(ComponentKcvRequest request)
        {
            try
            {
                var kcv = request.InputMode == "encrypted"
                    ? _crypto.GetKeyCheckValueUnderLmk(request.Component)
                    : _crypto.GetKeyCheckValue(request.Component);

                return Ok(new KeyCheckValueResponseDto(kcv));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid component", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }

        [HttpPost("zmk/generate")]
        public ActionResult<ZmkResultDto> GenerateZmk(FormZmkRequest request)
        {
            try
            {
                var result = _crypto.GenerateZmk(request.Components, request.InputMode);
                return Ok(new ZmkResultDto(result.EncryptedKey, result.KeyCheckValue));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid components", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }
    }
}
