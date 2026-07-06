using System.Windows;
using DailyWorkJournal.ViewModels;

namespace DailyWorkJournal.Views;

/// <summary>
/// Window for viewing and copying the raw log file contents.
/// </summary>
public partial class LogFileViewerWindow : Window
{
    /// <summary>
    /// Initialises a new instance of the <see cref="LogFileViewerWindow"/> class.
    /// Wires the view-model's <see cref="LogFileViewerViewModel.RequestClose"/> callback
    /// to this window's <see cref="Window.Close"/> method so the Close command can dismiss
    /// the correct window without a ViewModel-to-View type dependency.
    /// </summary>
    public LogFileViewerWindow()
    {
        InitializeComponent();

        if (DataContext is LogFileViewerViewModel vm)
            vm.RequestClose = Close;
    }
}
