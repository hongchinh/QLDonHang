using System.IO.Compression;
using ClosedXML.Excel;
using OrderMgmt.Application.Identity.UserSettings.Models;
using OrderMgmt.Domain.Common;

namespace OrderMgmt.Application.Identity.UserSettings.Services;

public static class TemplateUploadValidator
{
    private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 };

    public static void Validate(UploadedFile file, TemplateUploadOptions options)
    {
        if (file.Length == 0)
            throw new DomainException("VALIDATION", "File rỗng.");
        if (file.Length > options.UploadMaxBytes)
            throw new DomainException("VALIDATION",
                $"File vượt quá {options.UploadMaxBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            throw new DomainException("VALIDATION", "Chỉ chấp nhận file .xlsx.");

        if (!options.AllowedMimeTypes.Contains(file.ContentType))
            throw new DomainException("VALIDATION", $"MIME không hợp lệ: {file.ContentType}.");

        // Buffer once so subsequent checks can re-read without depending on stream rewindability.
        byte[] bytes;
        using (var src = file.OpenReadStream())
        using (var ms = new MemoryStream())
        {
            src.CopyTo(ms);
            bytes = ms.ToArray();
        }

        if (bytes.Length < 4 || !bytes.AsSpan(0, 4).SequenceEqual(ZipMagic))
            throw new DomainException("VALIDATION", "File không phải định dạng .xlsx hợp lệ (magic bytes).");

        using (var zipStream = new MemoryStream(bytes, writable: false))
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            long totalUnzipped = zip.Entries.Sum(e => e.Length);
            if (totalUnzipped > options.UnzippedMaxBytes)
                throw new DomainException("VALIDATION",
                    $"File giải nén vượt {options.UnzippedMaxBytes / (1024 * 1024)} MB.");
        }

        try
        {
            using var xlsxStream = new MemoryStream(bytes, writable: false);
            using var wb = new XLWorkbook(xlsxStream);
            _ = wb.Worksheets.Count();
        }
        catch (DomainException) { throw; }
        catch (Exception ex)
        {
            throw new DomainException("VALIDATION", $"Không mở được file Excel: {ex.Message}");
        }
    }
}
