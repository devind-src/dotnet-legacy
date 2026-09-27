using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Tools;
using SyncNetApi.Services.Tools;

namespace SyncNetApi.Controllers
{
    /// <summary>Backs menu "Tools" (DES Calculator / Pinblock Calculator / ICC Data Decode /
    /// Encrypt Credential) — 4 stateless calculators/decoders, nothing persisted. Plain
    /// [Authorize] (no Roles=) — same reasoning as HsmConsoleController/KeyManagementController:
    /// no data is written here, so any logged-in user with access to the Tools menu can use it.</summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/tools")]
    public class ToolsController : ControllerBase
    {
        private readonly IToolsService _tools;

        public ToolsController(IToolsService tools)
        {
            _tools = tools;
        }

        [HttpPost("des/encrypt")]
        public ActionResult<ToolResultDto> DesEncrypt(DesCalculatorRequest request)
        {
            try
            {
                return Ok(new ToolResultDto(_tools.DesEncrypt(request.Value, request.Key)));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid input", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPost("des/decrypt")]
        public ActionResult<ToolResultDto> DesDecrypt(DesCalculatorRequest request)
        {
            try
            {
                return Ok(new ToolResultDto(_tools.DesDecrypt(request.Value, request.Key)));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid input", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPost("pinblock")]
        public ActionResult<ToolResultDto> Pinblock(PinblockCalculatorRequest request)
        {
            try
            {
                return Ok(new ToolResultDto(_tools.CalculatePinBlock(request.Format, request.Pan, request.Pin, request.Key)));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid input", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPost("icc-data-decode")]
        public ActionResult<IReadOnlyList<IccTlvEntryDto>> IccDataDecode(IccDataDecodeRequest request)
        {
            try
            {
                return Ok(_tools.DecodeIccData(request.Value));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid input", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPost("encrypt-credential")]
        public ActionResult<ToolResultDto> EncryptCredential(EncryptCredentialRequest request)
        {
            try
            {
                return Ok(new ToolResultDto(_tools.EncryptCredential(request.Value, request.ConfigType)));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid input", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
            catch (HsmUnavailableException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Legacy credentials unavailable");
            }
        }
    }
}
