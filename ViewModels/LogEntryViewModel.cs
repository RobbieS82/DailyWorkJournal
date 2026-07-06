using System;
using DailyWorkJournal.Models;

namespace DailyWorkJournal.ViewModels;

/// <summary>
/// View-model wrapper for a single <see cref="LogEntry"/>.
/// Exposes bindable properties for the entry's date and content,
/// and tracks whether the content has changed since the last save (dirty state).
/// </summary>
public sealed class LogEntryViewModel : ViewModelBase
{
    // ──────────────────────────────────────────────────────────────────────────
    // Backing fields
    // ──────────────────────────────────────────────────────────────────────────

    private string _content = string.Empty;
    private bool _isDirty;

    // ──────────────────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a new <see cref="LogEntryViewModel"/> wrapping the given
    /// <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The underlying <see cref="LogEntry"/> data model.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="model"/> is <c>null</c>.
    /// </exception>
    public LogEntryViewModel(LogEntry model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _content = model.Content;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Properties
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Gets the underlying <see cref="LogEntry"/> data model.</summary>
    public LogEntry Model { get; }

    /// <summary>Gets the date this entry represents.</summary>
    public DateTime Date => Model.Date;

    /// <summary>
    /// Gets the short day-of-week name for this entry (e.g. "Monday").
    /// </summary>
    public string DayName => Model.Date.DayOfWeek.ToString();

    /// <summary>
    /// Gets a formatted display label for this entry (e.g. "Monday, Jul 7").
    /// </summary>
    public string DayLabel => Model.Date.ToString("dddd, MMM d");

    /// <summary>
    /// Gets the full display label including year (e.g. "Monday, Jul 7, 2026").
    /// </summary>
    public string FullDayLabel => Model.Date.ToString("dddd, MMMM d, yyyy");

    /// <summary>
    /// Gets or sets the text content of this log entry.
    /// Setting this property marks the entry as dirty (<see cref="IsDirty"/> becomes <c>true</c>).
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value ?? string.Empty))
            {
                // Mark dirty only if content differs from what was last persisted
                IsDirty = _content != Model.Content;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the content has been modified since the last save.
    /// Resets to <c>false</c> after <see cref="MarkSaved"/> is called.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    /// <summary>
    /// Gets whether today's date matches this entry's date.
    /// Used by the UI to highlight today's panel.
    /// </summary>
    public bool IsToday => Model.Date.Date == DateTime.Today;

    // ──────────────────────────────────────────────────────────────────────────
    // Methods
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Flushes the current <see cref="Content"/> back to the underlying
    /// <see cref="LogEntry"/> model and clears the dirty flag.
    /// Call this after the model has been persisted to disk.
    /// </summary>
    public void MarkSaved()
    {
        Model.Content = _content;
        Model.LastModified = DateTime.Now;
        IsDirty = false;
    }

    /// <summary>
    /// Discards any unsaved changes, reverting <see cref="Content"/> to
    /// the value stored in the underlying model.
    /// </summary>
    public void DiscardChanges()
    {
        _content = Model.Content;
        OnPropertyChanged(nameof(Content));
        IsDirty = false;
    }
}
