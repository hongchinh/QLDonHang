namespace OrderMgmt.Application.Identity.UserSettings.Models;

public sealed class UploadedFile
{
    private readonly Func<Stream> _openReadStream;

    public UploadedFile(string fileName, string contentType, long length, Func<Stream> openReadStream)
    {
        FileName = fileName;
        ContentType = contentType;
        Length = length;
        _openReadStream = openReadStream;
    }

    public string FileName { get; }
    public string ContentType { get; }
    public long Length { get; }

    public Stream OpenReadStream() => _openReadStream();
}

public sealed class TemplateUploadOptions
{
    public long UploadMaxBytes { get; set; } = 5 * 1024 * 1024;
    public long UnzippedMaxBytes { get; set; } = 50 * 1024 * 1024;
    public string[] AllowedMimeTypes { get; set; } =
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
    public string UserTemplatesPath { get; set; } = "templates/users";
}
