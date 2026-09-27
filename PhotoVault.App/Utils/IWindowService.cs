using PhotoVault.App.ViewModels;

namespace PhotoVault.App.Utils;

/// <summary>Deschiderea ferestrelor secundare din ViewModel-uri, fără referințe la View-uri.</summary>
public interface IWindowService
{
    /// <summary>Lightbox modal; întoarce poza afișată la închidere (pentru re-selectare în grid).</summary>
    PhotoItemViewModel? ShowLightbox(IReadOnlyList<PhotoItemViewModel> photos, int startIndex, string title);

    /// <summary>Logo-ul la dimensiune mare, peste fereastra blurată (§5.4).</summary>
    void ShowLogo();

    /// <summary>Modalul Info: versiune, autor, instrucțiuni, scurtături (§5.3).</summary>
    void ShowInfo();

    /// <summary>Fereastra de Setări / Opțiuni.</summary>
    void ShowSettings(MainViewModel viewModel);

    /// <summary>Utilitarul de redenumire batch (§6.8); true dacă au fost actualizate poze din bibliotecă.</summary>
    bool ShowBatchRename();

    /// <summary>Slideshow pe tot ecranul (§6.10); întoarce poza la care s-a ajuns.</summary>
    PhotoItemViewModel? ShowSlideshow(IReadOnlyList<PhotoItemViewModel> photos, int startIndex, string title);
}
