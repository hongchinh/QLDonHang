namespace OrderMgmt.Application.Branding.Interfaces;

public interface IPwaIconRenderer
{
    Task<byte[]> RenderAsync(byte[]? sourceImage, string? sourceContentType, int size, CancellationToken ct = default);
}
