using System;
using System.Threading.Tasks;
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

/// <summary>
/// Async <see cref="ICommand"/> with built-in "is executing" gating.
/// </summary>
/// <remarks>
/// <see cref="Execute(object?)"/> is fire-and-forget (required by <see cref="ICommand"/>); exceptions are routed
/// to <paramref name="onError"/> if provided and otherwise swallowed.
/// </remarks>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    /// <summary>True while the command is running.</summary>
    public bool IsExecuting => _isExecuting;

    public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onError = onError;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public void Execute(object? parameter) => _ = ExecuteAsync();

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
            return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            await _executeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Async parameterized <see cref="ICommand"/> with built-in "is executing" gating.
/// </summary>
public sealed class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _executeAsync;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    /// <summary>True while the command is running.</summary>
    public bool IsExecuting => _isExecuting;

    public AsyncRelayCommand(
        Func<T?, Task> executeAsync,
        Func<T?, bool>? canExecute = null,
        Action<Exception>? onError = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onError = onError;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
            return false;

        if (!TryGetParameter(parameter, out var typed))
            return false;

        return _canExecute?.Invoke(typed) ?? true;
    }

    public void Execute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var typed))
            return;

        _ = ExecuteAsync(typed);
    }

    public async Task ExecuteAsync(T? parameter)
    {
        if (_isExecuting)
            return;

        if (!(_canExecute?.Invoke(parameter) ?? true))
            return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            await _executeAsync(parameter).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
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
