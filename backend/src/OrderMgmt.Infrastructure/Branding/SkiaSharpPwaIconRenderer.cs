using SkiaSharp;
using OrderMgmt.Application.Branding.Interfaces;

namespace OrderMgmt.Infrastructure.Branding;

public class SkiaSharpPwaIconRenderer : IPwaIconRenderer
{
    private static readonly SKColor PlaceholderColor = new(0x1e, 0x40, 0xaf);

    public Task<byte[]> RenderAsync(byte[]? sourceImage, string? sourceContentType, int size, CancellationToken ct = default)
    {
        using var bitmap = BuildBitmap(sourceImage, sourceContentType, size);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return Task.FromResult(data.ToArray());
    }

    private static SKBitmap BuildBitmap(byte[]? sourceImage, string? contentType, int size)
    {
        if (sourceImage is not null && sourceImage.Length > 0
            && contentType?.Contains("svg", StringComparison.OrdinalIgnoreCase) != true)
        {
            try
            {
                using var source = SKBitmap.Decode(sourceImage);
                if (source is not null)
                    return ResizeWithPad(source, size);
            }
            catch { /* fall through to placeholder */ }
        }
        return CreatePlaceholder(size);
    }

    private static SKBitmap ResizeWithPad(SKBitmap source, int size)
    {
        var dest = new SKBitmap(size, size);
        using var canvas = new SKCanvas(dest);
        canvas.Clear(SKColors.White);

        float scale = Math.Min((float)size / source.Width, (float)size / source.Height);
        int w = (int)(source.Width * scale);
        int h = (int)(source.Height * scale);
        int x = (size - w) / 2;
        int y = (size - h) / 2;

        canvas.DrawBitmap(source, new SKRect(x, y, x + w, y + h));
        return dest;
    }

    private static SKBitmap CreatePlaceholder(int size)
    {
        var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(PlaceholderColor);
        return bitmap;
    }
}
