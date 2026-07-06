using System;
using System.Windows.Threading;

namespace DailyWorkJournal.Services;

/// <summary>
/// Manages the five-minute auto-save timer for the Daily Work Journal application.
/// Wraps a <see cref="DispatcherTimer"/> so callbacks execute on the UI thread,
/// making it safe to interact with WPF view-model state directly from handlers.
/// </summary>
public sealed class AutoSaveService : IDisposable
{
    // ──────────────────────────────────────────────────────────────────────────
    // Fields
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>The underlying WPF dispatcher timer.</summary>
    private readonly DispatcherTimer _timer;

    /// <summary>Tracks whether <see cref="Dispose"/> has been called.</summary>
    private bool _disposed;

    // ──────────────────────────────────────────────────────────────────────────
    // Events
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised on the UI thread every <see cref="Interval"/> when the timer is running.
    /// Subscribers should perform the actual save operation.
    /// </summary>
    public event EventHandler? AutoSaveTriggered;

    // ──────────────────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the auto-save service with a default interval of five minutes.
    /// The timer is <em>not</em> started automatically; call <see cref="Start"/> when ready.
    /// </summary>
    public AutoSaveService()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _timer.Tick += OnTimerTick;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Properties
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the auto-save interval.
    /// Changes take effect on the next timer reset.
    /// Defaults to five minutes.
    /// </summary>
    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    /// <summary>Gets a value indicating whether the auto-save timer is currently running.</summary>
    public bool IsRunning => _timer.IsEnabled;

    // ──────────────────────────────────────────────────────────────────────────
    // Public Methods
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the auto-save timer.
    /// If the timer is already running this method is a no-op.
    /// </summary>
    public void Start()
    {
        if (!_timer.IsEnabled)
            _timer.Start();
    }

    /// <summary>
    /// Stops the auto-save timer.
    /// If the timer is already stopped this method is a no-op.
    /// </summary>
    public void Stop()
    {
        if (_timer.IsEnabled)
            _timer.Stop();
    }

    /// <summary>
    /// Resets the countdown to the full <see cref="Interval"/> without raising
    /// <see cref="AutoSaveTriggered"/>. Useful after a manual save so the next
    /// auto-save is deferred by the full interval.
    /// </summary>
    public void Reset()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
            _timer.Start();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IDisposable
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stops the timer and releases managed resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _disposed = true;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private Helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles the dispatcher timer tick by raising <see cref="AutoSaveTriggered"/>.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        AutoSaveTriggered?.Invoke(this, EventArgs.Empty);
    }
}
