using SyncNet.Models;
using System;

namespace SyncNet.Routing.Schedule
{
    // jendela waktu aturan jadwal. salinan logika SyncNetApi Services/RouteSchedules/ScheduleWindow.cs
    // (dashboard Cek Jadwal) supaya hasilnya sama (FASE-3-PRIORITY-TIME.md §3.3):
    //   jam selesai eksklusif; jam selesai < jam mulai = lewat tengah malam (hari/tanggal dicocokkan
    //   dengan hari mulai); jam selesai = jam mulai = 24 jam; tanggal bulanan yang tidak ada dilewati;
    //   masa berlaku dicocokkan dengan hari mulai.
    public static class ScheduleWindow
    {
        public static bool IsActiveAt(RoutingScheduleModel r, DateTime t)
        {
            if (r.IsOnce == true)
                return r.StartDt.HasValue && r.EndDt.HasValue && r.StartDt.Value <= t && t < r.EndDt.Value;

            //jendela yang dimulai hari ini atau kemarin (lewat tengah malam) bisa memuat t
            for (int back = 0; back <= 1; back++)
            {
                if (TryWindowOn(r, t.Date.AddDays(-back), out var start, out var end) == true && start <= t && t < end)
                    return true;
            }

            return false;
        }

        private static bool TryWindowOn(RoutingScheduleModel r, DateTime day, out DateTime start, out DateTime end)
        {
            start = end = default;
            if (r.TimeStart.HasValue == false || r.TimeEnd.HasValue == false) return false;
            if (r.ValidFrom.HasValue == true && day < r.ValidFrom.Value.Date) return false;
            if (r.ValidUntil.HasValue == true && day > r.ValidUntil.Value.Date) return false;

            bool match = r.Recurrence switch
            {
                RoutingScheduleModel.DAILY => true,
                RoutingScheduleModel.WEEKLY => r.Days.Contains(IsoDay(day)),
                RoutingScheduleModel.MONTHLY => r.Days.Contains(day.Day),
                _ => false
            };
            if (match == false) return false;

            start = day.Date + r.TimeStart.Value;
            end = r.TimeEnd.Value > r.TimeStart.Value
                ? day.Date + r.TimeEnd.Value
                : day.Date.AddDays(1) + r.TimeEnd.Value;
            return true;
        }

        // Senin = 1 .. Minggu = 7
        public static int IsoDay(DateTime d) => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;
    }
}
