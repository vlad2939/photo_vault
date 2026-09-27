using System.Windows;
using System.Windows.Threading;
using PhotoVault.App.Utils;
using PhotoVault.App.ViewModels;
using PhotoVault.App.Views;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;
using PhotoVault.Data;
using PhotoVault.Data.Repositories;

namespace PhotoVault.App;

/// <summary>
/// Punctul de intrare și „composition root"-ul aplicației: inițializează baza
/// de date, setările, limba, tema, apoi afișează fereastra principală.
/// </summary>
public partial class App : Application
{
    private readonly IDialogService _dialogs = new DialogService();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var paths = new AppPaths();
        ErrorLog.Initialize(paths.LogsDirectory);
        RegisterGlobalExceptionHandlers();

        ISettingsService settings;
        DatabaseContext database;
        try
        {
            paths.EnsureCreated();
            database = new DatabaseContext(paths.DatabasePath);
            database.Initialize();

            settings = new SettingsService(new AppSettingsRepository(database));
            settings.Load();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "Inițializare bază de date");
            ThemeManager.Instance.Initialize(AppTheme.Dark, AppSettings.DefaultAccentColor);
            _dialogs.Show(Loc.Get("Str.Dialog.ErrorTitle"), Loc.Get("Str.Dialog.StartupError"), DialogKind.Error);
            Shutdown(1);
            return;
        }

        Loc.Apply(settings.Current.Language);
        ThemeManager.Instance.Initialize(settings.Current.Theme, settings.Current.AccentColor);

        var metadata = new MetadataService();
        var thumbnails = new ThumbnailService(paths.ThumbnailsDirectory, metadata);
        var photoRepository = new PhotoRepository(database);
        var index = new PhotoIndexService(new SourceFolderRepository(database), photoRepository, thumbnails);
        var photoService = new PhotoService(photoRepository);
        var folderPicker = new FolderPicker();
        var windows = new WindowService(metadata, photoService, new BatchRenameService(photoRepository), _dialogs, folderPicker);

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(settings, ThemeManager.Instance, _dialogs, windows,
                index, thumbnails, metadata,
                new AlbumService(new AlbumRepository(database)), new TagService(new TagRepository(database)),
                photoService, folderPicker)
        };
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) ErrorLog.Write(ex, "AppDomain.UnhandledException");
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLog.Write(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLog.Write(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;   // aplicația rămâne deschisă; utilizatorul e informat printr-un dialog stilizat
        _dialogs.Show(Loc.Get("Str.Dialog.ErrorTitle"), Loc.Get("Str.Dialog.UnexpectedError"), DialogKind.Error);
    }
}
