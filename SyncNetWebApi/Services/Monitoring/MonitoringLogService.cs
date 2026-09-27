using System.IO.Compression;
using Microsoft.Extensions.Options;
using SyncNetApi.Common;
using SyncNetApi.Dtos.Monitoring;
using SyncNetApi.Options;

namespace SyncNetApi.Services.Monitoring
{
    public class MonitoringLogService : IMonitoringLogService
    {
        private readonly string _logDir;
        private readonly string _traceDir;
        private readonly int _defaultPageSize;

        public MonitoringLogService(IOptions<MonitoringOptions> options)
        {
            var opts = options.Value;
            _logDir = Path.GetFullPath(opts.LogDir);
            _traceDir = Path.GetFullPath(opts.TraceDir);
            _defaultPageSize = opts.PageSize;
        }

        // ---------------------------------------------------------------------------
        // Log Viewer — files sit flat in _logDir, named "{AppName}_{date}.log" (legacy
        // NbLogger convention). The "app name" isn't a real folder, just the substring before
        // the first underscore — mirrors legacy GetLogName.
        // ---------------------------------------------------------------------------

        public IReadOnlyList<string> GetLogAppNames()
        {
            if (!Directory.Exists(_logDir)) return Array.Empty<string>();

            return Directory.GetFiles(_logDir)
                .Select(f => Path.GetFileName(f))
                .Select(name => name.Split('_')[0])
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<MonitoringFileDto> GetLogFiles(string appName)
        {
            RequireSafeSegment(appName, nameof(appName));
            if (!Directory.Exists(_logDir)) return Array.Empty<MonitoringFileDto>();

            return Directory.GetFiles(_logDir, $"{appName}*.log", SearchOption.TopDirectoryOnly)
                .Select(ToFileDto)
                .OrderByDescending(f => f.LastModified)
                .ToList();
        }

        public MonitoringFileContentDto GetLogContent(string fileName, int offset, int pageSize)
            => ReadContent(_logDir, fileName, offset, pageSize, search: null);

        // ---------------------------------------------------------------------------
        // Trace Viewer — one real subfolder per app under _traceDir, files inside it.
        // ---------------------------------------------------------------------------

        public IReadOnlyList<string> GetTraceAppNames()
        {
            if (!Directory.Exists(_traceDir)) return Array.Empty<string>();

            return Directory.GetDirectories(_traceDir)
                .Select(d => Path.GetFileName(d))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<MonitoringFileDto> GetTraceFiles(string appName)
        {
            var appDir = ResolveTraceAppDir(appName);
            if (!Directory.Exists(appDir)) return Array.Empty<MonitoringFileDto>();

            return Directory.GetFiles(appDir, "*.log", SearchOption.TopDirectoryOnly)
                .Select(ToFileDto)
                .OrderByDescending(f => f.LastModified)
                .ToList();
        }

        public MonitoringFileContentDto GetTraceContent(string appName, string fileName, int offset, int pageSize, string? search)
        {
            var appDir = ResolveTraceAppDir(appName);
            return ReadContent(appDir, fileName, offset, pageSize, search);
        }

        public (byte[] Content, string DownloadFileName) GetTraceFileZip(string appName, string fileName)
        {
            var appDir = ResolveTraceAppDir(appName);
            var path = ResolveSafePath(appDir, fileName, requireLogExtension: true);
            if (!File.Exists(path))
                throw new NotFoundException($"Trace file '{fileName}' not found.");

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(Path.GetFileName(path), CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(path);
                fileStream.CopyTo(entryStream);
            }

            return (memoryStream.ToArray(), Path.GetFileNameWithoutExtension(path) + ".zip");
        }

        // ---------------------------------------------------------------------------
        // Shared helpers
        // ---------------------------------------------------------------------------

        private MonitoringFileContentDto ReadContent(string baseDir, string fileName, int offset, int pageSize, string? search)
        {
            var path = ResolveSafePath(baseDir, fileName, requireLogExtension: true);
            if (!File.Exists(path))
                throw new NotFoundException($"File '{fileName}' not found.");

            if (offset < 0) offset = 0;
            if (pageSize <= 0 || pageSize > _defaultPageSize) pageSize = _defaultPageSize;

            var lines = File.ReadLines(path);
            if (!string.IsNullOrEmpty(search))
                lines = lines.Where(l => l.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Take one extra line to cheaply know whether another page exists without
            // scanning/counting the rest of a potentially huge file.
            var page = lines.Skip(offset).Take(pageSize + 1).ToList();
            var hasMore = page.Count > pageSize;
            if (hasMore) page.RemoveAt(page.Count - 1);

            var size = new FileInfo(path).Length;
            return new MonitoringFileContentDto(Path.GetFileName(path), page, offset, pageSize, hasMore, size);
        }

        private string ResolveTraceAppDir(string appName)
        {
            RequireSafeSegment(appName, nameof(appName));
            return ResolveSafePath(_traceDir, appName, requireLogExtension: false);
        }

        /// <summary>Rejects path separators/".." up front, then combines and re-verifies the
        /// resolved absolute path is still inside <paramref name="baseDir"/> — defense in depth
        /// against path traversal from a client-supplied file/app name.</summary>
        private static string ResolveSafePath(string baseDir, string segment, bool requireLogExtension)
        {
            RequireSafeSegment(segment, nameof(segment));
            if (requireLogExtension && !segment.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Only .log files can be read.");

            var combined = Path.GetFullPath(Path.Combine(baseDir, segment));
            var normalizedBase = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Invalid path.");

            return combined;
        }

        private static void RequireSafeSegment(string? value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value)
                || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || value.Contains(".."))
                throw new ValidationException($"Invalid {paramName}.");
        }

        private static MonitoringFileDto ToFileDto(string path)
        {
            var info = new FileInfo(path);
            return new MonitoringFileDto(info.Name, info.LastWriteTime, info.Length);
        }
    }
}
