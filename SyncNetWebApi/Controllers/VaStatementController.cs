using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Dtos.VaTransRequests;
using SyncNetApi.Services.Users;
using SyncNetApi.Services.VaStatements;

namespace SyncNetApi.Controllers
{
    /// <summary>Virtual Account &gt; Statement — read-only report, no mutation endpoints.</summary>
    [Route("api/v1/va-statement")]
    public class VaStatementController : PermissionGatedControllerBase
    {
        private readonly IVaStatementService _statements;

        public VaStatementController(IVaStatementService statements, IUserService users) : base(users)
        {
            _statements = statements;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<VaStatementDto>>> GetAll(
            [FromQuery] DateTime dateStart, [FromQuery] DateTime dateEnd, [FromQuery] string? vaName)
            => Ok(await _statements.GetStatementAsync(dateStart, dateEnd, vaName));
    }
}
