using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DailyWorkJournal.ViewModels;

/// <summary>
/// Base class for all view-model types in the Daily Work Journal application.
/// Implements <see cref="INotifyPropertyChanged"/> and exposes a helper
/// <see cref="SetProperty{T}"/> method to reduce boilerplate in derived classes.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Raised when a property value changes.
    /// WPF data bindings subscribe to this event automatically.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for the specified property name.
    /// </summary>
    /// <param name="propertyName">
    /// The name of the property that changed.
    /// Automatically inferred by the compiler when called from a property setter.
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
    /// <see cref="PropertyChanged"/> if the value actually changed.
    /// </summary>
    /// <typeparam name="T">The type of the backing field.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">
    /// The name of the calling property (compiler-filled).
    /// </param>
    /// <returns>
    /// <c>true</c> if the value changed and <see cref="PropertyChanged"/> was raised;
    /// <c>false</c> if the new value equals the current value.
    /// </returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
