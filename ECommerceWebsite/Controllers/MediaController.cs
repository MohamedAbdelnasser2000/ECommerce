using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ECommerceWebsite.Controllers;

public class MediaController : Controller
{
    private readonly IWebHostEnvironment _env;

    public MediaController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("media/{size}/{*relative}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, NoStore = false)]
    [EnableRateLimiting("global")]
    public async Task<IActionResult> Get(string size, string relative, string? mode = "cover", string? format = null)
    {
        if (string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(relative)) return BadRequest();

        // Parse size WxH
        var parts = size.ToLowerInvariant().Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h) || w <= 0 || h <= 0)
            return BadRequest("Invalid size.");

        // Sanitize relative path
        relative = relative.Replace("\\", "/").TrimStart('/');
        if (relative.Contains("..")) return BadRequest();

        // Only allow inside images/ by default
        var allowedRoot = Path.Combine(_env.WebRootPath, "images");
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
        if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            // fallback to images folder
            full = Path.Combine(_env.WebRootPath, "images", relative);
            full = Path.GetFullPath(full);
            if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)) return NotFound();
        }

        if (!System.IO.File.Exists(full))
        {
            Response.StatusCode = 404;
            return Content($"File not found at: {full}", "text/plain", System.Text.Encoding.UTF8);
        }

        // Disk cache path
        var cacheDir = Path.Combine(_env.WebRootPath, "cache", "media");
        Directory.CreateDirectory(cacheDir);
        var ext = format?.ToLowerInvariant();
        if (ext != "webp" && ext != "jpg" && ext != "jpeg" && ext != "png") ext = null; // keep source
        var cacheKey = $"{w}x{h}_{mode}_{ext ?? "src"}_" + relative.Replace('/', '_');
        var cachePath = Path.Combine(cacheDir, cacheKey);

        if (System.IO.File.Exists(cachePath))
        {
            var cachedBytes = await System.IO.File.ReadAllBytesAsync(cachePath);
            return File(cachedBytes, GetContentTypeByExt(Path.GetExtension(cachePath)));
        }

        using var srcImage = Image.FromFile(full);
        using var canvas = new Bitmap(w, h);
        using (var g = Graphics.FromImage(canvas))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var destRect = new Rectangle(0, 0, w, h);
            var (sx, sy, sw, sh) = CalculateSourceRect(srcImage.Width, srcImage.Height, w, h, mode);
            g.DrawImage(srcImage, destRect, new Rectangle(sx, sy, sw, sh), GraphicsUnit.Pixel);
        }

        // Encode
        byte[] bytes;
        string outContentType;
        if (ext == "png")
        {
            using var ms = new MemoryStream();
            canvas.Save(ms, ImageFormat.Png);
            bytes = ms.ToArray();
            outContentType = "image/png";
        }
        else // jpg default; webp if supported (fallback to jpg)
        {
            using var ms = new MemoryStream();
            canvas.Save(ms, ImageFormat.Jpeg);
            bytes = ms.ToArray();
            outContentType = "image/jpeg";
        }

        await System.IO.File.WriteAllBytesAsync(cachePath, bytes);
        return File(bytes, outContentType);
    }

    private static (int sx, int sy, int sw, int sh) CalculateSourceRect(int srcW, int srcH, int destW, int destH, string? mode)
    {
        var cover = string.Equals(mode, "cover", StringComparison.OrdinalIgnoreCase);
        var scale = cover
            ? Math.Max((double)destW / srcW, (double)destH / srcH)
            : Math.Min((double)destW / srcW, (double)destH / srcH);
        var sw = (int)Math.Round(destW / scale);
        var sh = (int)Math.Round(destH / scale);
        var sx = (srcW - sw) / 2;
        var sy = (srcH - sh) / 2;
        // Clamp
        if (sx < 0) sx = 0; if (sy < 0) sy = 0;
        if (sw > srcW) sw = srcW; if (sh > srcH) sh = srcH;
        return (sx, sy, sw, sh);
    }

    private static string GetContentTypeByExt(string ext)
    {
        return ext.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}


