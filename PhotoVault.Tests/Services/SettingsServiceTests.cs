using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Data.Repositories;

namespace PhotoVault.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void Load_EmptyDatabase_ReturnsDefaults()
    {
        using var db = new TestDatabase();
        var service = new SettingsService(new AppSettingsRepository(db.Context));

        var settings = service.Load();

        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal(AppSettings.DefaultAccentColor, settings.AccentColor);
        Assert.Equal("ro", settings.Language);
        Assert.Empty(settings.SlideshowPlaylistPaths);
    }

    [Fact]
    public void SetTheme_PersistsAcrossReload()
    {
        using var db = new TestDatabase();
        var repository = new AppSettingsRepository(db.Context);
        new SettingsService(repository).SetTheme(AppTheme.Light);

        Assert.Equal("light", repository.Get(AppSettingKeys.Theme));
        Assert.Equal(AppTheme.Light, new SettingsService(repository).Load().Theme);
    }

    [Fact]
    public void Save_RoundTripsAllValues()
    {
        using var db = new TestDatabase();
        var repository = new AppSettingsRepository(db.Context);
        var service = new SettingsService(repository);
        service.Load();
        service.Current.AccentColor = "#2D7FF9";
        service.Current.Language = "en";
        service.Current.SlideshowDurationSec = 9.5;
        service.Current.SlideshowZoomIntensity = 1.2;
        service.Current.SlideshowPlaylistPaths = [@"C:\Muzica\a.mp3", @"D:\b.mp3"];
        service.Current.SlideshowVolume = 35;
        service.Save();

        var reloaded = new SettingsService(repository).Load();

        Assert.Equal("#2D7FF9", reloaded.AccentColor);
        Assert.Equal("en", reloaded.Language);
        Assert.Equal(9.5, reloaded.SlideshowDurationSec);
        Assert.Equal(1.2, reloaded.SlideshowZoomIntensity);
        Assert.Equal([@"C:\Muzica\a.mp3", @"D:\b.mp3"], reloaded.SlideshowPlaylistPaths);
        Assert.Equal(35, reloaded.SlideshowVolume);
    }

    [Fact]
    public void Load_InvalidValues_FallBackOrClamp()
    {
        using var db = new TestDatabase();
        var repository = new AppSettingsRepository(db.Context);
        repository.Set(AppSettingKeys.AccentColor, "portocaliu");
        repository.Set(AppSettingKeys.SlideshowDurationSec, "99");
        repository.Set(AppSettingKeys.SlideshowPlaylistPaths, "{not json");

        var settings = new SettingsService(repository).Load();

        Assert.Equal(AppSettings.DefaultAccentColor, settings.AccentColor);
        Assert.Equal(15, settings.SlideshowDurationSec);
        Assert.Empty(settings.SlideshowPlaylistPaths);
    }
}
