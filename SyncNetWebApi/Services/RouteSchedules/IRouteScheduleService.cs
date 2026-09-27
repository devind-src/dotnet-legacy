using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteSchedules;

namespace SyncNetApi.Services.RouteSchedules
{
    public interface IRouteScheduleService
    {
        /// <summary>states kosong = semua status. nodeId/ruleType opsional.</summary>
        Task<IReadOnlyList<RouteScheduleDto>> GetRecordsAsync(string? filter = null, IReadOnlyCollection<string>? states = null,
            int? nodeId = null, string? ruleType = null);
        Task<RouteScheduleDto?> GetByIdAsync(int id);
        Task<IReadOnlyList<RouteScheduleHistDto>> GetHistoryAsync(int id);
        Task<RouteScheduleDto> CreateAsync(SaveRouteScheduleRequest request, string actingUser);
        Task<RouteScheduleDto> UpdateAsync(int id, SaveRouteScheduleRequest request, string actingUser);
        Task<RouteScheduleDto> CancelAsync(int id, string reason, string actingUser);
        Task DeleteAsync(int id, string actingUser);

        /// <summary>Cek Jadwal: biller buka/tutup dan biller terpilih per produk pada waktu `at`.</summary>
        Task<ScheduleCheckResultDto> CheckAsync(DateTime at, string? productId = null, int? nodeId = null, int? denom = null);

        /// <summary>Dampak aturan yang sedang diisi (belum disimpan) pada jendela berikutnya.
        /// editingId = aturan yang sedang diubah (dikecualikan dari aturan tersimpan).</summary>
        Task<ScheduleCheckResultDto> PreviewAsync(SaveRouteScheduleRequest request, int? editingId = null);
    }
}
