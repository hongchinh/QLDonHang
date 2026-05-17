namespace OrderMgmt.Application.Branding.Models;

public sealed class LogoUpload
{
    private readonly Func<Stream> _openReadStream;

    public LogoUpload(string fileName, string contentType, long length, Func<Stream> openReadStream)
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
