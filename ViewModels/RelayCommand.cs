using System;
using System.Windows.Input;

namespace DailyWorkJournal.ViewModels;

/// <summary>
/// A lightweight implementation of <see cref="ICommand"/> that delegates
/// <see cref="Execute"/> and <see cref="CanExecute"/> to caller-supplied lambdas.
/// Commonly referred to as the "Relay Command" or "Delegate Command" pattern.
/// </summary>
public sealed class RelayCommand : ICommand
{
    // ──────────────────────────────────────────────────────────────────────────
    // Fields
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>The action to run when the command is executed.</summary>
    private readonly Action<object?> _execute;

    /// <summary>Optional predicate that controls whether the command can be executed.</summary>
    private readonly Func<object?, bool>? _canExecute;

    // ──────────────────────────────────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a new <see cref="RelayCommand"/> with the given execute delegate.
    /// The command is always enabled when no <paramref name="canExecute"/> predicate is supplied.
    /// </summary>
    /// <param name="execute">The action to invoke on <see cref="Execute"/>.</param>
    /// <param name="canExecute">
    /// Optional predicate to determine whether the command is currently executable.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="execute"/> is <c>null</c>.
    /// </exception>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Convenience constructor that accepts parameterless delegates.
    /// </summary>
    /// <param name="execute">The parameterless action to invoke on <see cref="Execute"/>.</param>
    /// <param name="canExecute">Optional parameterless predicate.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ICommand
    // ──────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter)
        => _canExecute == null || _canExecute(parameter);

    /// <inheritdoc/>
    public void Execute(object? parameter)
        => _execute(parameter);

    /// <summary>
    /// Forces WPF to re-evaluate <see cref="CanExecute"/> for all commands
    /// by raising <see cref="CommandManager.RequerySuggested"/>.
    /// </summary>
    public void RaiseCanExecuteChanged()
        => CommandManager.InvalidateRequerySuggested();
}
