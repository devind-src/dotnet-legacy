using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.VaTransRequests;

namespace SyncNetApi.Services.VaStatements
{
    public interface IVaStatementService
    {
        Task<IReadOnlyList<VaStatementDto>> GetStatementAsync(DateTime dateStart, DateTime dateEnd, string? vaName);
    }
}
