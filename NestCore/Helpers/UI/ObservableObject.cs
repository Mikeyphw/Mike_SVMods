using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mikew.NestCore.Helpers.UI;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base for UI view models (e.g. StardewUI binding contexts).
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises <see cref="PropertyChanged"/> when changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/>.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for all properties.
    /// </summary>
    protected void OnAllPropertiesChanged()
        => OnPropertyChanged(string.Empty);
}
