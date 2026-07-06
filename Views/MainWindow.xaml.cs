using System.Windows;
using System.Windows.Controls;
using DailyWorkJournal.ViewModels;

namespace DailyWorkJournal.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// Handles UI events that require code-behind and delegates all logic to
/// <see cref="MainViewModel"/> via data binding.
/// </summary>
public partial class MainWindow : Window
{
    // ──────────────────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the main application window and sets up the keyboard shortcut
    /// for manual save (Ctrl+S).
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Register Ctrl+S as a global save shortcut for the window
        var saveGesture = new System.Windows.Input.KeyBinding(
            new System.Windows.Input.RoutedCommand(),
            new System.Windows.Input.KeyGesture(
                System.Windows.Input.Key.S,
                System.Windows.Input.ModifierKeys.Control));
        InputBindings.Add(saveGesture);

        // Wire up the Ctrl+S gesture to the view-model's SaveCommand
        // (done after DataContext is set by XAML)
        Loaded += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
            {
                InputBindings.Clear();
                InputBindings.Add(new System.Windows.Input.KeyBinding(
                    vm.SaveCommand,
                    new System.Windows.Input.KeyGesture(
                        System.Windows.Input.Key.S,
                        System.Windows.Input.ModifierKeys.Control)));
            }
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles the window <c>Closing</c> event.
    /// Delegates the unsaved-changes check to the view-model; cancels the close
    /// if the user elects to stay.
    /// </summary>
    /// <param name="sender">The window raising the event.</param>
    /// <param name="e">Event args that allow the close to be cancelled.</param>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            bool shouldClose = vm.PromptSaveOnClose();
            if (!shouldClose)
            {
                e.Cancel = true;
                return;
            }

            // Dispose the view-model to stop the auto-save timer cleanly
            vm.Dispose();
        }
    }

    /// <summary>
    /// Handles the calendar <c>SelectedDatesChanged</c> event.
    /// Forwards the selected date to the view-model's navigate command so the
    /// displayed week updates when the user picks a date in the calendar control.
    /// </summary>
    /// <param name="sender">The <see cref="Calendar"/> control.</param>
    /// <param name="e">Event args containing the new selection.</param>
    private void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Calendar calendar
            && calendar.SelectedDate.HasValue
            && DataContext is MainViewModel vm)
        {
            vm.NavigateToDateCommand.Execute(calendar.SelectedDate.Value);
        }
    }
}
