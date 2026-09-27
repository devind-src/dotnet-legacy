using System;

namespace SyncNetApi.Common
{
    /// <summary>Waktu lokal server untuk kolom waktu tabel failover routing. SDK (API Channel)
    /// menulis waktu lokal (`DateTime.Now`) dan membandingkan `blocked_until` dengan waktu
    /// lokal, jadi dashboard harus memakai zona yang sama supaya kolom-kolomnya seragam.
    /// Kind dibuat Unspecified karena kolomnya `timestamp without time zone` — Npgsql menolak
    /// Kind=Local/Utc untuk kolom bertipe itu bila EF Core memetakannya sebagai timestamptz.</summary>
    public static class LocalClock
    {
        public static DateTime Now => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    }
}
