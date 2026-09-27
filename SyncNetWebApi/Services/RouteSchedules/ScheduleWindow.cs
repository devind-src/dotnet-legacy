using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SyncNetApi.Dtos.RouteSchedules;

namespace SyncNetApi.Services.RouteSchedules
{
    /// <summary>Aturan jadwal dalam bentuk yang dipakai evaluator (lepas dari entity EF), supaya logika
    /// yang sama bisa disalin ke SDK routing. Days = hari ISO (WEEKLY) atau tanggal (MONTHLY).</summary>
    public sealed class ScheduleRule
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string RuleType { get; init; } = RouteScheduleCodes.Closed;
        public string? RoutingType { get; init; }
        public string? InstId { get; init; }
        public int? NodeId { get; init; }
        public short? Priority { get; init; }
        public string Recurrence { get; init; } = RouteScheduleCodes.Once;
        public DateTime? StartDt { get; init; }
        public DateTime? EndDt { get; init; }
        public TimeSpan? TimeStart { get; init; }
        public TimeSpan? TimeEnd { get; init; }
        public IReadOnlyList<int> Days { get; init; } = [];
        public DateOnly? ValidFrom { get; init; }
        public DateOnly? ValidUntil { get; init; }
        public bool IsActive { get; init; } = true;

        public bool IsOnce => Recurrence == RouteScheduleCodes.Once;
    }

    /// <summary>Perhitungan jendela waktu aturan jadwal (FASE-3 §3.3):
    /// jam selesai eksklusif; jam selesai lebih kecil dari jam mulai = lewat tengah malam (hari/tanggal
    /// dicocokkan dengan hari mulai); jam selesai sama dengan jam mulai = 24 jam; tanggal bulanan
    /// yang tidak ada di bulan itu dilewati; masa berlaku dicocokkan dengan hari mulai.</summary>
    public static class ScheduleWindow
    {
        private const int SearchDays = 400;

        /// <summary>Jendela yang memuat waktu t, atau null. Aturan nonaktif tidak pernah berlaku.</summary>
        public static (DateTime Start, DateTime End)? ActiveWindow(ScheduleRule r, DateTime t)
        {
            if (!r.IsActive) return null;

            if (r.IsOnce)
            {
                return r.StartDt.HasValue && r.EndDt.HasValue && r.StartDt.Value <= t && t < r.EndDt.Value
                    ? (r.StartDt.Value, r.EndDt.Value)
                    : null;
            }

            // jendela yang dimulai hari ini atau kemarin (lewat tengah malam) bisa memuat t
            for (int back = 0; back <= 1; back++)
            {
                var w = WindowOn(r, t.Date.AddDays(-back));
                if (w.HasValue && w.Value.Start <= t && t < w.Value.End)
                    return w;
            }

            return null;
        }

        public static bool IsActiveAt(ScheduleRule r, DateTime t) => ActiveWindow(r, t).HasValue;

        /// <summary>Jendela yang sedang berlaku pada <paramref name="from"/>, atau jendela berikutnya
        /// yang dimulai setelahnya (dicari sampai ±400 hari). Null bila tidak ada lagi.</summary>
        public static (DateTime Start, DateTime End)? NextWindow(ScheduleRule r, DateTime from)
        {
            var active = ActiveWindow(r, from);
            if (active.HasValue) return active;
            if (!r.IsActive) return null;

            if (r.IsOnce)
                return r.StartDt.HasValue && r.EndDt.HasValue && r.StartDt.Value >= from ? (r.StartDt.Value, r.EndDt.Value) : null;

            for (int i = 0; i <= SearchDays; i++)
            {
                var w = WindowOn(r, from.Date.AddDays(i));
                if (w.HasValue && w.Value.Start >= from)
                    return w;
            }

            return null;
        }

        // jendela aturan berulang yang dimulai pada hari `day`, atau null bila hari itu tidak cocok
        private static (DateTime Start, DateTime End)? WindowOn(ScheduleRule r, DateTime day)
        {
            if (!r.TimeStart.HasValue || !r.TimeEnd.HasValue) return null;

            var d = DateOnly.FromDateTime(day);
            if (r.ValidFrom.HasValue && d < r.ValidFrom.Value) return null;
            if (r.ValidUntil.HasValue && d > r.ValidUntil.Value) return null;

            bool match = r.Recurrence switch
            {
                RouteScheduleCodes.Daily => true,
                RouteScheduleCodes.Weekly => r.Days.Contains(IsoDay(day)),
                RouteScheduleCodes.Monthly => r.Days.Contains(day.Day),
                _ => false
            };
            if (!match) return null;

            var start = day.Date + r.TimeStart.Value;
            var end = r.TimeEnd.Value > r.TimeStart.Value
                ? day.Date + r.TimeEnd.Value
                : day.Date.AddDays(1) + r.TimeEnd.Value;
            return (start, end);
        }

        /// <summary>Senin = 1 .. Minggu = 7.</summary>
        public static int IsoDay(DateTime d) => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;

        /// <summary>Teks ringkas jendela, mis. "30-09-2026 01:00 – 30-09-2026 03:00", "Harian 23:45–00:15",
        /// "Sen, Sel, Rab 07:00–21:00", "Tgl 1, 15 00:00–01:00", ditambah masa berlaku bila ada.</summary>
        public static string Describe(string? recurrence, DateTime? startDt, DateTime? endDt, TimeSpan? timeStart,
            TimeSpan? timeEnd, string? daysOfWeek, string? daysOfMonth, DateOnly? validFrom, DateOnly? validUntil)
        {
            if (recurrence == RouteScheduleCodes.Once)
                return $"{Fmt(startDt)} – {Fmt(endDt)}";

            string hours = $"{Hm(timeStart)}–{Hm(timeEnd)}";
            if (timeStart.HasValue && timeStart == timeEnd) hours = "24 jam";

            string text = recurrence switch
            {
                RouteScheduleCodes.Daily => $"Harian {hours}",
                RouteScheduleCodes.Weekly => $"{string.Join(", ", RouteScheduleCodes.ParseDays(daysOfWeek).Select(RouteScheduleCodes.DayName))} {hours}",
                RouteScheduleCodes.Monthly => $"Tgl {string.Join(", ", RouteScheduleCodes.ParseDays(daysOfMonth))} {hours}",
                _ => hours
            };

            if (validFrom.HasValue || validUntil.HasValue)
                text += $" ({FmtDate(validFrom) ?? "…"} s.d. {FmtDate(validUntil) ?? "…"})";

            return text;
        }

        private static string Fmt(DateTime? d) => d?.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture) ?? "?";
        private static string? FmtDate(DateOnly? d) => d?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        private static string Hm(TimeSpan? t) => t.HasValue ? $"{(int)t.Value.TotalHours:00}:{t.Value.Minutes:00}" : "?";
    }
}
