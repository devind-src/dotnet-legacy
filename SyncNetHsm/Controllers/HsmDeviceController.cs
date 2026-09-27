using Microsoft.AspNetCore.Mvc;
using SyncNet.Helpers;
using SyncNet.Services;
using System.Threading.Tasks;

namespace SyncNet.Controllers
{
    [ApiController]
    //[Route("/hsm/[action]")]
    public class HsmDeviceController : ControllerBase
    {
        private readonly HsmService _hsmService;

        public HsmDeviceController(HsmService hsmService)
        {
            _hsmService = hsmService;
        }

        [HttpPost]
        [Route("/hsm/generate-key")]
        public async Task<IActionResult> GenerateKey()
        {
            string JsonMessage = await Request.Body.ReadAsStringAsync();
            return Ok(await _hsmService.GenerateKey(JsonMessage));
        }

        [HttpPost]
        [Route("/hsm/generate-key-terminal")]
        public async Task<IActionResult> GenerateKeyTerminal()
        {
            string JsonMessage = await Request.Body.ReadAsStringAsync();
            return Ok(await _hsmService.GenerateKeyTerminal(JsonMessage));
        }

        [HttpPost]
        [Route("/hsm/translate-key")]
        public async Task<IActionResult> TranslateKeyToLmk()
        {
            string JsonMessage = await Request.Body.ReadAsStringAsync();
            return Ok(await _hsmService.TranslateKeyToLmk(JsonMessage));
        }

        [HttpPost]
        [Route("/hsm/translate-pinblock")]
        public async Task<IActionResult> TranslatePinblock()
        {
            string JsonMessage = await Request.Body.ReadAsStringAsync();
            return Ok(await _hsmService.TranslatePinblock(JsonMessage));
        }

        [HttpPost]
        [Route("/hsm/translate-pinblock-terminal")]
        public async Task<IActionResult> TranslatePinblockTerminal()
        {
            string JsonMessage = await Request.Body.ReadAsStringAsync();
            return Ok(await _hsmService.TranslatePinblockTerminal(JsonMessage));
        }
    }
}
