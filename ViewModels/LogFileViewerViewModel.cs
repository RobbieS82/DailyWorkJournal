using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DailyWorkJournal.Services;

namespace DailyWorkJournal.ViewModels;

/// <summary>
/// View-model for the log file viewer window.
/// Loads the raw journal log from disk, exposes it for read-only display,
/// and provides commands for copying, refreshing, and closing the window.
/// </summary>
public sealed class LogFileViewerViewModel : ViewModelBase
{
    private string _logFileContent = string.Empty;
    private string _statusMessage = "Loading log file contents...";

    /// <summary>
    /// Initialises a new instance of the <see cref="LogFileViewerViewModel"/> class.
    /// Sets up the viewer commands and loads the current log file contents immediately.
    /// </summary>
    public LogFileViewerViewModel()
    {
        CopyAllCommand = new RelayCommand(CopyToClipboard, () => !string.IsNullOrEmpty(LogFileContent));
        RefreshCommand = new RelayCommand(RefreshContent);
        CloseCommand = new RelayCommand(CloseWindow);

        LoadLogFileContent();
    }

    /// <summary>
    /// Gets the raw UTF-8 text currently loaded from the journal log file.
    /// This value is bound to the read-only viewer text box.
    /// </summary>
    public string LogFileContent
    {
        get => _logFileContent;
        private set
        {
            if (SetProperty(ref _logFileContent, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Gets the status message shown in the viewer status bar.
    /// Displays entry counts, character counts, file path information, and transient feedback.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets the command that copies the entire loaded log file content to the clipboard.
    /// </summary>
    public ICommand CopyAllCommand { get; }

    /// <summary>
    /// Gets the command that reloads the log file content from disk.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command that closes the viewer window.
    /// The command parameter is expected to be the owning <see cref="Window"/>.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Loads the log file content from disk and updates the view state.
    /// When the file does not yet exist, a friendly placeholder message is shown instead.
    /// </summary>
    private void LoadLogFileContent()
    {
        try
        {
            if (!File.Exists(LogFileService.LogFilePath))
            {
                LogFileContent = "No log file found. Start creating entries to generate the log file.";
                StatusMessage = $"No log file at {LogFileService.LogFilePath}";
                return;
            }

            LogFileContent = File.ReadAllText(LogFileService.LogFilePath, Encoding.UTF8);

            int charCount = LogFileContent.Length;
            int entryCount = CountEntries(LogFileContent);

            StatusMessage = $"Loaded {entryCount} entries ({charCount:N0} characters) from {LogFileService.LogFilePath}";
        }
        catch (Exception ex)
        {
            LogFileContent = $"Error loading log file: {ex.Message}";
            StatusMessage = $"Error loading file: {LogFileService.LogFilePath}";
        }
    }

    /// <summary>
    /// Copies the current log file content to the clipboard and briefly shows confirmation feedback.
    /// </summary>
    private void CopyToClipboard()
    {
        try
        {
            if (string.IsNullOrEmpty(LogFileContent))
                return;

            Clipboard.SetText(LogFileContent);
            StatusMessage = "Copied all log content to the clipboard.";
            _ = RestoreLoadedStatusAfterDelayAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Reloads the log file content from disk so the viewer reflects the latest saved state.
    /// </summary>
    private void RefreshContent()
    {
        LoadLogFileContent();
    }

    /// <summary>
    /// Closes the viewer window when a valid <see cref="Window"/> parameter is supplied.
    /// </summary>
    /// <param name="parameter">The window instance to close.</param>
    private void CloseWindow(object? parameter)
    {
        if (parameter is Window window)
            window.Close();
    }

    /// <summary>
    /// Waits briefly after a successful copy action and then restores the normal loaded-file status message.
    /// </summary>
    /// <returns>A task that completes after the status message has been refreshed.</returns>
    private async Task RestoreLoadedStatusAfterDelayAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        Application.Current.Dispatcher.Invoke(LoadLogFileContent);
    }

    /// <summary>
    /// Counts the number of entry headers in the raw log file content.
    /// </summary>
    /// <param name="content">The raw log file text to inspect.</param>
    /// <returns>The number of entry blocks found.</returns>
    private static int CountEntries(string content)
    {
        return Regex.Matches(content, @"^===== ENTRY:", RegexOptions.Multiline).Count;
    }
}
