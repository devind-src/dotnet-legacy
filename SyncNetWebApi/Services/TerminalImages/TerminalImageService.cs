using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.TerminalImages;
using SyncNetApi.Entities;
using SyncNetApi.Options;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.TerminalImages
{
    public class TerminalImageService : ITerminalImageService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB, matches legacy MaxFileSize

        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;
        private readonly TerminalImageStorageOptions _storage;
        private readonly string _uploadDir;

        public TerminalImageService(SyncNetDbContext context, IAuditService audit, IOptions<TerminalImageStorageOptions> storage)
        {
            _context = context;
            _audit = audit;
            _storage = storage.Value;
            _uploadDir = Path.IsPathRooted(_storage.UploadDir)
                ? _storage.UploadDir
                : Path.Combine(AppContext.BaseDirectory, _storage.UploadDir);

            Directory.CreateDirectory(_uploadDir);
        }

        public async Task<IReadOnlyList<TerminalImageDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.TerminalImages.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(i => i.name.ToLower().Contains(f));
            }

            var list = await query.OrderBy(i => i.name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<TerminalImageDto?> GetByNameAsync(string name)
        {
            var entity = await _context.TerminalImages.AsNoTracking().FirstOrDefaultAsync(i => i.name == name);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<TerminalImageDto> CreateAsync(CreateTerminalImageRequest request, string actingUser)
        {
            var exists = await _context.TerminalImages.AsNoTracking().AnyAsync(i => i.name == request.Name);
            if (exists)
                throw new ConflictException($"Terminal image '{request.Name}' already exists.");

            var entity = new SwTerminalImage { name = request.Name, url_base = request.UrlBase };

            _context.TerminalImages.Add(entity);
            _audit.LogInsert(entity, "sw_terminal_image", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<TerminalImageDto> UpdateAsync(string name, UpdateTerminalImageRequest request, string actingUser)
        {
            var entity = await _context.TerminalImages.FirstOrDefaultAsync(i => i.name == name)
                ?? throw new NotFoundException($"Terminal image '{name}' not found.");

            var before = new { entity.url_base };
            entity.url_base = request.UrlBase;
            _audit.LogUpdate(before, new { entity.url_base }, "sw_terminal_image", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string name, string actingUser)
        {
            var entity = await _context.TerminalImages.FirstOrDefaultAsync(i => i.name == name)
                ?? throw new NotFoundException($"Terminal image '{name}' not found.");

            foreach (var fileName in new[] { entity.logo, entity.banner_1, entity.banner_2, entity.banner_3, entity.banner_4, entity.banner_5 })
                DeleteFileIfExists(fileName);

            _context.TerminalImages.Remove(entity);
            _audit.LogDelete(new { entity.name }, "sw_terminal_image", actingUser);

            await _context.SaveChangesAsync();
        }

        public async Task<TerminalImageDto> UploadBannerAsync(string name, int slot, IFormFile file, string actingUser)
        {
            if (slot < 1 || slot > 5)
                throw new ValidationException("Banner slot must be between 1 and 5.");

            var entity = await _context.TerminalImages.FirstOrDefaultAsync(i => i.name == name)
                ?? throw new NotFoundException($"Terminal image '{name}' not found.");

            if (file.Length == 0)
                throw new ValidationException("Uploaded file is empty.");
            if (file.Length > MaxFileSizeBytes)
                throw new ValidationException("Uploaded file exceeds the 5 MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new ValidationException("Only .jpg, .jpeg, .png and .bmp files are allowed.");

            // Matches legacy SetFileName: cap at 50 chars (banner_N column width) by keeping
            // the tail of the name, then make it unique per image row + slot.
            var safeBase = $"{name}_banner{slot}{ext}";
            var fileName = safeBase.Length > 50 ? safeBase[^50..] : safeBase;

            var oldFileName = GetBannerFileName(entity, slot);
            DeleteFileIfExists(oldFileName);

            var filePath = Path.Combine(_uploadDir, fileName);
            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            SetBannerFileName(entity, slot, fileName);
            _audit.LogUpdate(new { OldFile = oldFileName }, new { NewFile = fileName }, "sw_terminal_image", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        private static string? GetBannerFileName(SwTerminalImage e, int slot) => slot switch
        {
            1 => e.banner_1,
            2 => e.banner_2,
            3 => e.banner_3,
            4 => e.banner_4,
            5 => e.banner_5,
            _ => null
        };

        private static void SetBannerFileName(SwTerminalImage e, int slot, string fileName)
        {
            switch (slot)
            {
                case 1: e.banner_1 = fileName; break;
                case 2: e.banner_2 = fileName; break;
                case 3: e.banner_3 = fileName; break;
                case 4: e.banner_4 = fileName; break;
                case 5: e.banner_5 = fileName; break;
            }
        }

        private void DeleteFileIfExists(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_uploadDir, fileName);
            if (File.Exists(path)) File.Delete(path);
        }

        private string? ToPublicUrl(string? fileName) =>
            string.IsNullOrEmpty(fileName) ? null : $"{_storage.UrlPrefix}/{fileName}";

        private TerminalImageDto ToDto(SwTerminalImage e) => new(
            e.name, e.url_base,
            ToPublicUrl(e.logo), ToPublicUrl(e.banner_1), ToPublicUrl(e.banner_2),
            ToPublicUrl(e.banner_3), ToPublicUrl(e.banner_4), ToPublicUrl(e.banner_5));
    }
}
