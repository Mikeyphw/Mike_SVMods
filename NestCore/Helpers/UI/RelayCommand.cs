// FILE: NestCore/Helpers/UI/RelayCommand.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mikew.NestCore.Helpers.UI;

/// <summary>Shared command base with optional change notifications for nested-property binding.</summary>
public abstract class CommandBase : ICommand, INotifyPropertyChanged
{
    public event EventHandler? CanExecuteChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public abstract bool CanExecute(object? parameter);
    public abstract void Execute(object? parameter);

    protected void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>Simple synchronous command.</summary>
public sealed class RelayCommand : CommandBase
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter)
        => _canExecute?.Invoke() ?? true;

    public override void Execute(object? parameter)
        => _execute();

    public void NotifyCanExecuteChanged()
        => RaiseCanExecuteChanged();
}

/// <summary>Simple synchronous command with a parameter.</summary>
public sealed class RelayCommand<T> : CommandBase
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter)
        => _canExecute?.Invoke((T?)parameter) ?? true;

    public override void Execute(object? parameter)
        => _execute((T?)parameter);

    public void NotifyCanExecuteChanged()
        => RaiseCanExecuteChanged();
}

/// <summary>Async command with IsExecuting gating and optional error handler.</summary>
public sealed class AsyncRelayCommand : CommandBase
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;

    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onError = onError;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value)
                return;

            _isExecuting = value;
            OnPropertyChanged();
            RaiseCanExecuteChanged();
        }
    }

    public override bool CanExecute(object? parameter)
        => !IsExecuting && (_canExecute?.Invoke() ?? true);

    public override void Execute(object? parameter)
        => _ = ExecuteCoreAsync();

    private async Task ExecuteCoreAsync()
    {
        if (!CanExecute(null))
            return;

        IsExecuting = true;
        try
        {
            await _executeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}

/// <summary>Async command with parameter, IsExecuting gating and optional error handler.</summary>
public sealed class AsyncRelayCommand<T> : CommandBase
{
    private readonly Func<T?, Task> _executeAsync;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<Exception>? _onError;

    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onError = onError;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value)
                return;

            _isExecuting = value;
            OnPropertyChanged();
            RaiseCanExecuteChanged();
        }
    }

    public override bool CanExecute(object? parameter)
        => !IsExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);

    public override void Execute(object? parameter)
        => _ = ExecuteCoreAsync((T?)parameter);

    private async Task ExecuteCoreAsync(T? parameter)
    {
        if (!CanExecute(parameter))
            return;

        IsExecuting = true;
        try
        {
            await _executeAsync(parameter).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
