using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.KeyManagement;
using SyncNetApi.Services.Hsm;

namespace SyncNetApi.Controllers
{
    /// <summary>Backs the "Generate" buttons on Interface &gt; Nodes and PosBase &gt; Merchant/
    /// Terminal's Key Management tab. Used to be a placeholder returning random hex (no HSM
    /// integration existed) — as of 2026-09-09 it runs the same real NbHSM-equivalent DES/3DES
    /// math (under LMK_1) as HSM &gt; Console, via IHsmCryptoProvider. Still not a real
    /// hardware/network HSM: TCP/IP protocol integration remains out of scope, and these
    /// endpoints don't persist anything themselves — the actual Terminal/Merchant/Node save
    /// still goes through its own PUT endpoint, which enforces CanEdit as usual.
    /// <br/><br/>
    /// [Authorize] (no Roles=) — nothing is persisted here, same reasoning as
    /// LookupsController/HsmConsoleController.</summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/key-management")]
    public class KeyManagementController : ControllerBase
    {
        private readonly IHsmCryptoProvider _crypto;

        public KeyManagementController(IHsmCryptoProvider crypto)
        {
            _crypto = crypto;
        }

        [HttpPost("generate-master-key")]
        public ActionResult<MasterKeyBundleDto> GenerateMasterKey(GenerateKeyBundleRequest request)
        {
            try
            {
                var result = _crypto.GenerateMasterKey(request.KeyLength);
                return Ok(new MasterKeyBundleDto(result.EncryptedKey, result.Kcv));
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }

        [HttpPost("generate-session-key")]
        public ActionResult<SessionKeyBundleDto> GenerateSessionKey(GenerateSessionKeyRequest request)
        {
            try
            {
                var result = _crypto.GenerateSessionKey(request.MasterKeyUnderLmk);
                return Ok(new SessionKeyBundleDto(result.KeyUnderLmk, result.KeyUnderZmk, result.Kcv));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid Master Key", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "HSM unavailable");
            }
        }
    }
}
