using PhotoVault.Core.Utils;

namespace PhotoVault.Tests.Utils;

public class RawPreviewExtractorTests
{
    [Fact]
    public void ExtractLargestJpeg_ReturnsBiggestEmbeddedPreview()
    {
        var dir = Path.Combine(Path.GetTempPath(), "PhotoVaultTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "IMG_0001.CR2");
        var large = TestImages.JpegBytes(640, 480);
        var small = TestImages.JpegBytes(160, 120);
        TestImages.WriteSyntheticRaw(path, large, small);

        try
        {
            var preview = RawPreviewExtractor.ExtractLargestJpeg(path);
            Assert.NotNull(preview);
            Assert.Equal(large, preview);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void IsDisplayableJpeg_AcceptsBaselineRejectsLossless()
    {
        Assert.True(RawPreviewExtractor.IsDisplayableJpeg(TestImages.JpegBytes(64, 48)));
        Assert.False(RawPreviewExtractor.IsDisplayableJpeg(TestImages.LosslessJpegStub(4096)));
        Assert.False(RawPreviewExtractor.IsDisplayableJpeg([0x3C, 0x44, 0x75, 0x6D]));   // „<Dummy"
    }

    [Fact]
    public void ExtractLargestJpeg_NonTiffFile_ReturnsNull()
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, [1, 2, 3, 4, 5, 6, 7, 8]);
        try
        {
            Assert.Null(RawPreviewExtractor.ExtractLargestJpeg(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ThumbnailRelativePath_IsStableCaseInsensitiveAndSharded()
    {
        var a = FileHashHelper.ThumbnailRelativePath("/poze/Vacanta/IMG_1.JPG");
        var b = FileHashHelper.ThumbnailRelativePath("/POZE/vacanta/img_1.jpg");
        Assert.Equal(a, b);
        Assert.Matches("^[0-9a-f]{2}/[0-9a-f]{40}\\.jpg$", a);
        Assert.Equal(a[..2], a[3..5]);
    }
}
