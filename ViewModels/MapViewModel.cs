using System.Text.Json;
using EuroklicMapMobile.Models;
using EuroklicMapMobile.Services;

namespace EuroklicMapMobile.ViewModels;

public class MapViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _db;
    private readonly ISyncService          _sync;

    private List<EuroklicPoint> _points = [];
    private bool                _initialized;
    private string              _pointCountText = string.Empty;

    public List<EuroklicPoint> Points
    {
        get => _points;
        private set => SetField(ref _points, value);
    }

    public string PointCountText
    {
        get => _pointCountText;
        private set => SetField(ref _pointCountText, value);
    }

    /// <summary>Vyvolano kdyz se stahla nova data - mapa se ma prekreslit.</summary>
    public event EventHandler? DataRefreshed;

    public Command RefreshCommand { get; }

    public MapViewModel(ILocalDatabaseService db, ISyncService sync)
    {
        _db   = db;
        _sync = sync;

        RefreshCommand = CreateBusyCommand(ForceSyncAsync);
    }

    /// <summary>Zavolat z MapPage.OnAppearing.</summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        await _db.InitAsync();

        // 1. Nacti lokalni data okamzite (offline)
        Points = await _db.GetAllPointsAsync();
        UpdateCountText();

        // 2. Zkus synchronizaci na pozadi
        _ = BackgroundSyncAsync();
    }

    /// <summary>Vraci body jako JSON pro Leaflet mapu.</summary>
    public string GetPointsJson()
    {
        return JsonSerializer.Serialize(_points, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private async Task BackgroundSyncAsync()
    {
        StatusText = "Kontroluji aktualizace...";
        try
        {
            var result = await _sync.SyncIfNeededAsync();
            if (result.DataUpdated)
            {
                Points = await _db.GetAllPointsAsync();
                UpdateCountText();
                StatusText = $"Aktualizovano: {result.PointCount} bodu";
                DataRefreshed?.Invoke(this, EventArgs.Empty);
            }
            else if (result.Success)
            {
                StatusText = $"Data jsou aktualni ({result.PointCount} bodu)";
            }
            else
            {
                StatusText = $"Offline - {_points.Count} bodu ze zalohy";
            }
        }
        catch
        {
            StatusText = $"Offline - {_points.Count} bodu ze zalohy";
        }
    }

    private async Task ForceSyncAsync()
    {
        StatusText = "Stahuji data ze serveru...";
        var result = await _sync.ForceSyncAsync();

        if (result.Success)
        {
            Points = await _db.GetAllPointsAsync();
            UpdateCountText();
            StatusText = $"Stazeno {result.PointCount} bodu";
            DataRefreshed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            StatusText = $"Chyba: {result.Error}";
        }
    }

    private void UpdateCountText()
    {
        PointCountText = _points.Count > 0 ? $"{_points.Count} bodu" : "Zadna data";
    }
}
