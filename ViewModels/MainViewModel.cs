using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DailyWorkJournal.Models;
using DailyWorkJournal.Services;

namespace DailyWorkJournal.ViewModels;

/// <summary>
/// Primary view-model for the <c>MainWindow</c>.
/// Manages the current work week, selected day, dirty-state tracking,
/// manual save commands, and auto-save integration.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    // ──────────────────────────────────────────────────────────────────────────
    // Fields
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>All entries loaded from the log file, keyed by <c>yyyy-MM-dd</c>.</summary>
    private Dictionary<string, LogEntry> _allEntries;

    /// <summary>The auto-save service instance managed by this view-model.</summary>
    private readonly AutoSaveService _autoSaveService;

    // Backing fields for bindable properties
    private WorkWeek _currentWeek;
    private LogEntryViewModel? _selectedEntry;
    private DateTime _selectedDate;
    private DateTime? _lastSaveTime;
    private string _statusMessage = string.Empty;
    private bool _hasUnsavedChanges;
    private bool _disposed;

    // ──────────────────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the main view-model by loading all persisted entries,
    /// building the current work week, and starting the auto-save timer.
    /// </summary>
    public MainViewModel()
    {
        // Load all previously saved entries
        _allEntries = LogFileService.LoadAllEntries();

        // Initialise week and date state
        _currentWeek = new WorkWeek(DateTime.Today);
        _selectedDate = DateTime.Today;

        // Build the observable list of day view-models for the current week
        WeekEntries = new ObservableCollection<LogEntryViewModel>();
        RebuildWeekEntries();

        // Pre-select today's entry if it falls in the work week; otherwise Monday
        _selectedEntry = WeekEntries.FirstOrDefault(e => e.IsToday)
                      ?? WeekEntries.FirstOrDefault();

        // Set up commands
        PreviousWeekCommand = new RelayCommand(GoToPreviousWeek);
        NextWeekCommand = new RelayCommand(GoToNextWeek);
        GoToTodayCommand = new RelayCommand(GoToToday);
        SaveCommand = new RelayCommand(SaveAll);
        NavigateToDateCommand = new RelayCommand(NavigateToDate);

        // Start auto-save
        _autoSaveService = new AutoSaveService();
        _autoSaveService.AutoSaveTriggered += OnAutoSaveTriggered;
        _autoSaveService.Start();

        UpdateStatusMessage();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Bindable Properties
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the collection of <see cref="LogEntryViewModel"/> objects for the
    /// five days (Mon–Fri) of <see cref="CurrentWeek"/>.
    /// </summary>
    public ObservableCollection<LogEntryViewModel> WeekEntries { get; }

    /// <summary>Gets the current work week being displayed.</summary>
    public WorkWeek CurrentWeek
    {
        get => _currentWeek;
        private set
        {
            if (SetProperty(ref _currentWeek, value))
                OnPropertyChanged(nameof(WeekLabel));
        }
    }

    /// <summary>Gets the display label for the current week (e.g. "Week of Jul 7 – Jul 11, 2026").</summary>
    public string WeekLabel => _currentWeek.WeekLabel;

    /// <summary>
    /// Gets or sets the currently selected date.
    /// Changing this navigates to the week containing the new date and selects that day's entry.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
                NavigateToDate(value);
        }
    }

    /// <summary>Gets or sets the currently selected <see cref="LogEntryViewModel"/>.</summary>
    public LogEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    /// <summary>Gets whether any entry in the current week has unsaved changes.</summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
                UpdateStatusMessage();
        }
    }

    /// <summary>Gets the UTC timestamp of the most recent save operation, or <c>null</c> if never saved.</summary>
    public DateTime? LastSaveTime
    {
        get => _lastSaveTime;
        private set
        {
            if (SetProperty(ref _lastSaveTime, value))
                UpdateStatusMessage();
        }
    }

    /// <summary>Gets the human-readable status message shown in the status bar.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Commands
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Navigates to the previous work week.</summary>
    public ICommand PreviousWeekCommand { get; }

    /// <summary>Navigates to the next work week.</summary>
    public ICommand NextWeekCommand { get; }

    /// <summary>Navigates to the work week containing today's date.</summary>
    public ICommand GoToTodayCommand { get; }

    /// <summary>Saves all dirty entries in the current week.</summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Navigates to the week that contains a date passed as the command parameter.
    /// Accepts a <see cref="DateTime"/> or <see cref="DateTime?"/> parameter.
    /// </summary>
    public ICommand NavigateToDateCommand { get; }

    // ──────────────────────────────────────────────────────────────────────────
    // Public Methods
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves all dirty entries immediately (e.g. on application close or manual save).
    /// Resets the auto-save countdown after a successful save.
    /// </summary>
    public void SaveAll()
    {
        var dirtyEntries = WeekEntries.Where(vm => vm.IsDirty).ToList();
        if (!dirtyEntries.Any())
        {
            UpdateStatusMessage();
            return;
        }

        try
        {
            var modelsToSave = dirtyEntries.Select(vm =>
            {
                vm.MarkSaved(); // flush VM content into model, clear dirty flag
                return vm.Model;
            });

            LogFileService.SaveEntries(modelsToSave);

            LastSaveTime = DateTime.Now;
            HasUnsavedChanges = false;

            // Reset the auto-save countdown so the next auto-save is a full 5 minutes away
            _autoSaveService.Reset();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Checks whether there are any unsaved changes and asks the user whether to save.
    /// Returns <c>true</c> if the application should proceed with closing,
    /// <c>false</c> if the user cancelled the close.
    /// </summary>
    /// <returns>
    /// <c>true</c> to allow the window to close; <c>false</c> to cancel.
    /// </returns>
    public bool PromptSaveOnClose()
    {
        RefreshDirtyState();
        if (!HasUnsavedChanges)
            return true;

        MessageBoxResult result = MessageBox.Show(
            "You have unsaved changes. Would you like to save before closing?",
            "Daily Work Journal — Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        switch (result)
        {
            case MessageBoxResult.Yes:
                SaveAll();
                return true;
            case MessageBoxResult.No:
                return true;
            default: // Cancel
                return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private Helpers – Navigation
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves back one work week and rebuilds the entry list.
    /// </summary>
    private void GoToPreviousWeek()
    {
        CurrentWeek = new WorkWeek(CurrentWeek.WeekStart.AddDays(-7));
        _selectedDate = CurrentWeek.WeekStart;
        OnPropertyChanged(nameof(SelectedDate));
        RebuildWeekEntries();
        SelectedEntry = WeekEntries.FirstOrDefault();
    }

    /// <summary>
    /// Moves forward one work week and rebuilds the entry list.
    /// </summary>
    private void GoToNextWeek()
    {
        CurrentWeek = new WorkWeek(CurrentWeek.WeekStart.AddDays(7));
        _selectedDate = CurrentWeek.WeekStart;
        OnPropertyChanged(nameof(SelectedDate));
        RebuildWeekEntries();
        SelectedEntry = WeekEntries.FirstOrDefault();
    }

    /// <summary>
    /// Navigates to the work week containing today's date and selects today's entry.
    /// </summary>
    private void GoToToday()
    {
        CurrentWeek = new WorkWeek(DateTime.Today);
        _selectedDate = DateTime.Today;
        OnPropertyChanged(nameof(SelectedDate));
        RebuildWeekEntries();
        SelectedEntry = WeekEntries.FirstOrDefault(e => e.IsToday) ?? WeekEntries.FirstOrDefault();
    }

    /// <summary>
    /// Navigates to the work week containing the date specified by <paramref name="parameter"/>.
    /// </summary>
    /// <param name="parameter">
    /// A <see cref="DateTime"/> or <see cref="DateTime?"/> value representing the target date.
    /// </param>
    private void NavigateToDate(object? parameter)
    {
        DateTime? date = null;

        if (parameter is DateTime dt)
            date = dt;

        if (date == null || date.Value == default)
            return;

        NavigateToDate(date.Value);
    }

    /// <summary>
    /// Navigates to the work week containing <paramref name="date"/> and selects that day's entry.
    /// </summary>
    /// <param name="date">The target date.</param>
    private void NavigateToDate(DateTime date)
    {
        // If the date is a weekend, snap to Monday of that week
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            date = WorkWeek.GetMondayOfWeek(date);

        if (!CurrentWeek.ContainsDate(date))
        {
            CurrentWeek = new WorkWeek(date);
            RebuildWeekEntries();
        }

        SelectedEntry = WeekEntries.FirstOrDefault(e => e.Date.Date == date.Date)
                     ?? WeekEntries.FirstOrDefault();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private Helpers – Entry Management
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds <see cref="WeekEntries"/> for the current week, merging
    /// persisted content from <see cref="_allEntries"/> where available.
    /// </summary>
    private void RebuildWeekEntries()
    {
        WeekEntries.Clear();

        foreach (LogEntry model in CurrentWeek.Entries)
        {
            string key = model.Date.ToString("yyyy-MM-dd");

            // Use persisted content if it exists
            if (_allEntries.TryGetValue(key, out LogEntry? persisted))
            {
                model.Content = persisted.Content;
                model.LastModified = persisted.LastModified;
            }

            var vm = new LogEntryViewModel(model);
            vm.PropertyChanged += OnEntryPropertyChanged;
            WeekEntries.Add(vm);
        }

        RefreshDirtyState();
    }

    /// <summary>
    /// Called when a property on any <see cref="LogEntryViewModel"/> in the week changes.
    /// Used to track the aggregate dirty state.
    /// </summary>
    private void OnEntryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogEntryViewModel.IsDirty))
            RefreshDirtyState();
    }

    /// <summary>
    /// Recalculates <see cref="HasUnsavedChanges"/> based on the dirty flags of
    /// all entries in <see cref="WeekEntries"/>.
    /// </summary>
    private void RefreshDirtyState()
    {
        HasUnsavedChanges = WeekEntries.Any(vm => vm.IsDirty);
    }

    /// <summary>
    /// Updates the human-readable <see cref="StatusMessage"/> shown in the status bar.
    /// </summary>
    private void UpdateStatusMessage()
    {
        if (HasUnsavedChanges)
        {
            StatusMessage = "● Unsaved changes";
        }
        else if (LastSaveTime.HasValue)
        {
            StatusMessage = $"✓ Saved at {LastSaveTime.Value:HH:mm:ss}";
        }
        else
        {
            StatusMessage = "No changes";
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auto-save
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles the <see cref="AutoSaveService.AutoSaveTriggered"/> event.
    /// Saves any dirty entries silently.
    /// </summary>
    private void OnAutoSaveTriggered(object? sender, EventArgs e)
    {
        // Reload the on-disk entries to pick up changes from other sessions (optional)
        _allEntries = LogFileService.LoadAllEntries();
        SaveAll();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IDisposable
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stops the auto-save timer and unsubscribes from its event.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        foreach (LogEntryViewModel vm in WeekEntries)
            vm.PropertyChanged -= OnEntryPropertyChanged;

        _autoSaveService.AutoSaveTriggered -= OnAutoSaveTriggered;
        _autoSaveService.Dispose();
        _disposed = true;
    }
}
