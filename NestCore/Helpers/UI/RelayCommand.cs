using System;
using System.Windows.Input;

namespace Mikew.NestCore.Helpers.UI;

/// <summary>
/// Minimal <see cref="ICommand"/> implementation for UI binding contexts (e.g. StardewUI viewmodels).
/// </summary>
/// <remarks>
/// StardewUI event bindings typically call methods directly (e.g. <c>click=|OnClick()|</c>), but a command
/// wrapper is still useful for organizing intent and reusing enable/disable logic.
/// </remarks>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        _execute();
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Minimal <see cref="ICommand"/> implementation for parameterized commands.
/// </summary>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var typed))
            return false;

        return _canExecute?.Invoke(typed) ?? true;
    }

    public void Execute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var typed))
            return;

        if (!(_canExecute?.Invoke(typed) ?? true))
            return;

        _execute(typed);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static bool TryGetParameter(object? parameter, out T? typed)
    {
        if (parameter is null)
        {
            typed = default;
            return true;
        }

        if (parameter is T value)
        {
            typed = value;
            return true;
        }

        typed = default;
        return false;
    }
}
