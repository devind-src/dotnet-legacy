using System.Reflection;
using System.Text;
using Microsoft.JSInterop;

namespace SyncNetWasm.Common
{
    /// <summary>Mirrors legacy's NbExport — reflection-based CSV (";"-delimited, matching the
    /// legacy export format) plus a JS-interop-triggered browser download. No zip/large-file
    /// branch (legacy compressed above a size threshold) — VA Statement exports are small
    /// enough that a plain .csv download is sufficient.</summary>
    public static class CsvExporter
    {
        public static async Task DownloadAsync<T>(IJSRuntime js, string namePrefix, IEnumerable<T> data)
        {
            var csv = ToCsv(data);
            if (string.IsNullOrEmpty(csv)) return;

            var filename = $"{namePrefix}-{DateTime.Now:yyyyMMddHHmmss}.csv";
            await js.InvokeVoidAsync("downloadTextFile", filename, csv);
        }

        private static string ToCsv<T>(IEnumerable<T> data)
        {
            var list = data.ToList();
            if (list.Count == 0) return string.Empty;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var csv = new StringBuilder();

            csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

            foreach (var item in list)
            {
                var values = properties.Select(p => p.GetValue(item)?.ToString()?.Replace(";", "\\;") ?? "");
                csv.AppendLine(string.Join(";", values));
            }

            return csv.ToString();
        }
    }
}
