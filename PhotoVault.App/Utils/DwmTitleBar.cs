using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PhotoVault.App.Utils;

/// <summary>
/// Sincronizează title bar-ul nativ Windows cu tema aplicației (§5.1).
/// Title bar-ul rămâne cel nativ (minimize/maximize/close standard); se schimbă
/// doar modul întunecat și, pe Windows 11, culoarea de fundal / text a acestuia.
/// </summary>
internal static class DwmTitleBar
{
    // Windows 10 20H1+ / Windows 11
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    // Windows 10 înainte de 20H1 (valoare nedocumentată, folosită ca fallback)
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
    // Doar Windows 11 (build 22000+)
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window, bool isDark, Color caption, Color text)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        try
        {
            var useDark = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDark, sizeof(int));

            // Culoarea title bar-ului = culoarea barei secundare → tranziție vizuală continuă.
            // Pe Windows 10 aceste atribute nu există; apelul eșuează silențios.
            var captionRef = ToColorRef(caption);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionRef, sizeof(int));
            var textRef = ToColorRef(text);
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textRef, sizeof(int));
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // DWM indisponibil — title bar-ul rămâne cu aspectul sistemului.
        }
    }

    /// <summary>COLORREF = 0x00BBGGRR.</summary>
    private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);
}
