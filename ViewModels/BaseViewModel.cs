using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EuroklicMapMobile.ViewModels;

/// <summary>Základní ViewModel – INotifyPropertyChanged + pomocné metody.</summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool   _isBusy;
    private string _statusText = string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>Vytvoří Command, který se při vykonávání automaticky zamkne (IsBusy).</summary>
    protected Command CreateBusyCommand(Func<Task> execute)
        => new(async () =>
        {
            if (IsBusy) return;
            IsBusy = true;
            try   { await execute(); }
            finally { IsBusy = false; }
        });
}
