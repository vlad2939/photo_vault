using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoVault.App.Utils;
using PhotoVault.Core.Services;

namespace PhotoVault.App.ViewModels;

/// <summary>Un rând etichetă–valoare din panoul de detalii.</summary>
public sealed record DetailRow(string Label, string Value);

/// <summary>
/// Panoul lateral de detalii (§5.6) pentru poza selectată: nume, cale, dimensiune, extensie
/// (din index) + câteva informații EXIF citite asincron. Tag-urile se adaugă în Faza 3.
/// </summary>
public partial class PhotoDetailsViewModel(IMetadataService metadata) : ObservableObject
{
    private int _version;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial PhotoItemViewModel? Photo { get; set; }

    public bool HasPhoto => Photo is not null;

    public ObservableCollection<DetailRow> Rows { get; } = [];

    public async Task ShowAsync(PhotoItemViewModel? photo)
    {
        Photo = photo;
        Rows.Clear();
        if (photo is null) return;
        var version = ++_version;

        Rows.Add(new DetailRow(Loc.Get("Str.Details.Path"), photo.FullPath));
        if (photo.FileSizeBytes is { } size) Rows.Add(new DetailRow(Loc.Get("Str.Details.Size"), FormatSize(size)));
        Rows.Add(new DetailRow(Loc.Get("Str.Details.Extension"), "." + photo.Extension.ToUpperInvariant()));

        var details = await Task.Run(() => metadata.ReadDetails(photo.FullPath));
        if (version != _version) return;   // între timp s-a selectat altă poză

        if (details.Width is { } w && details.Height is { } h)
            Rows.Add(new DetailRow(Loc.Get("Str.Details.Dimensions"), $"{w} × {h}"));
        if (details.DateTaken is { } taken)
            Rows.Add(new DetailRow(Loc.Get("Str.Details.DateTaken"), taken.ToString("dd.MM.yyyy HH:mm:ss", Loc.Culture)));
        AddIfPresent("Str.Details.Camera", details.Camera);
        AddIfPresent("Str.Details.Lens", details.Lens);
        AddIfPresent("Str.Details.Iso", details.Iso);
        AddIfPresent("Str.Details.Exposure", details.ExposureTime);
        AddIfPresent("Str.Details.Aperture", details.Aperture);
        AddIfPresent("Str.Details.FocalLength", details.FocalLength);
    }

    private void AddIfPresent(string labelKey, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Rows.Add(new DetailRow(Loc.Get(labelKey), value));
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => (bytes / 1024d / 1024d).ToString("0.0", Loc.Culture) + " MB",
        >= 1024 => (bytes / 1024d).ToString("0", Loc.Culture) + " KB",
        _ => bytes.ToString(Loc.Culture) + " B",
    };
}
