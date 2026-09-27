using System;
using System.Collections.Generic;

namespace SyncNetApi.Dtos.RouteSchedules
{
    /// <summary>Hasil Cek Jadwal (dry-run) pada satu waktu: per produk, biller mana yang buka/tutup dan
    /// biller yang dipilih untuk siklus baru menurut jadwal + Routing Mode. Status health saat ini hanya
    /// informasi; hasil dihitung seolah semua biller sehat.</summary>
    public record ScheduleCheckResultDto(
        DateTime At,
        IReadOnlyList<ScheduleCheckProductDto> Products,
        IReadOnlyList<string> Warnings);

    /// <summary>Result: ROUTE (ke SelectedNode), REJECT (ditolak X15, tidak ada biller buka), CORE
    /// (routing statis oleh core, jadwal tidak dievaluasi: tombol darurat OFF).</summary>
    public record ScheduleCheckProductDto(
        string ProductId,
        string? ProductName,
        string RoutingType,
        string RoutingMode,
        int? Denom,
        string Result,
        string? SelectedNode,
        string? Note,
        IReadOnlyList<ScheduleCheckBillerDto> Billers);

    /// <summary>Rank = urutan efektif (1 = dicoba pertama) di antara biller yang buka; null bila tutup.</summary>
    public record ScheduleCheckBillerDto(
        int NodeId,
        string NodeName,
        int BasePriority,
        bool Open,
        string? ClosedBy,
        short? SchedulePriority,
        string? PriorityBy,
        int? Rank,
        string? HealthStatus);

    public static class ScheduleCheckResults
    {
        public const string Route = "ROUTE";
        public const string Reject = "REJECT";
        public const string Core = "CORE";
    }
}
