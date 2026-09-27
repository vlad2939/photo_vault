using PhotoVault.Core.Services;

namespace PhotoVault.Tests.Services;

public sealed class MetadataServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "PhotoVaultTests", Guid.NewGuid().ToString("N"));
    private readonly MetadataService _service = new();

    [Fact]
    public void ReadDetails_ReturnsPixelDimensionsForJpegAndPng()
    {
        var jpeg = Path.Combine(_dir, "a.jpg");
        var png = Path.Combine(_dir, "b.png");
        TestImages.WriteJpeg(jpeg, 1200, 800);
        TestImages.WritePng(png, 300, 500);

        var jpegDetails = _service.ReadDetails(jpeg);
        Assert.Equal((1200, 800), (jpegDetails.Width, jpegDetails.Height));
        Assert.Null(jpegDetails.Camera);

        var pngDetails = _service.ReadDetails(png);
        Assert.Equal((300, 500), (pngDetails.Width, pngDetails.Height));
    }

    [Fact]
    public void ReadDetails_UnreadableFile_ReturnsEmptyDetails()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "stricat.jpg");
        File.WriteAllText(path, "nu e imagine");

        var details = _service.ReadDetails(path);

        Assert.Null(details.Width);
        Assert.Null(details.DateTaken);
        Assert.Equal(1, _service.Read(path).Orientation);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }
}
