using System.Diagnostics;
using System.Windows;

namespace PhotoVault.App.Utils;

/// <summary>Repornirea aplicației (ex. după schimbarea limbii, §6.12).</summary>
public interface IAppLifetime
{
    void Restart();
}

public sealed class AppLifetime : IAppLifetime
{
    public void Restart()
    {
        // Executabilul curent (inclusiv varianta portabilă single-file), din același folder de lucru
        var exe = Environment.ProcessPath;
        if (exe is null) return;
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory });
        Application.Current.Shutdown();
    }
}
