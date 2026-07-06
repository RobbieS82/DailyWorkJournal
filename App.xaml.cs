using System.Windows;
using DailyWorkJournal.Services;

namespace DailyWorkJournal;

/// <summary>
/// Interaction logic for App.xaml.
/// Entry point for the Daily Work Journal application.
/// Handles application startup and shutdown lifecycle events.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Handles the application startup event.
    /// Ensures the log file directory exists before the main window is shown.
    /// </summary>
    /// <param name="e">Startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure the log file directory exists on first run
        LogFileService.EnsureLogDirectoryExists();
    }
}

