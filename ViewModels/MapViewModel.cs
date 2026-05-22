using System.Text.Json;
using EuroklicMapMobile.Models;
using EuroklicMapMobile.Services;

namespace EuroklicMapMobile.ViewModels;

public class MapViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _db;
    private readonly ISyncService          _sync;

    private List<EuroklicPoint> _points         = [];
    private List<EuroklicPoint> _filteredPoints  = [];
    private List<string>        _availTypes      = [];
    private string              _searchText      = string.Empty;
    private string?             _selectedType;
    private string              _pointCountText  = string.Empty;
    private bool                _isPanelExpanded;
    private bool                _initialized;

    // ── Verejne vlastnosti ──────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public string? SelectedType
    {
        get => _selectedType;
        set => SetField(ref _selectedType, value);
    }

    public string PointCountText
    {
        get => _pointCountText;
        private set => SetField(ref _pointCountText, value);
    }

    public bool IsPanelExpanded
    {
        get => _isPanelExpanded;
        set => SetField(ref _isPanelExpanded, value);
    }

    public List<string>        AvailableTypes  => _availTypes;
    public bool                HasTypes        => _availTypes.Count > 0;
    public List<EuroklicPoint> FilteredPoints  => _filteredPoints;

    /// <summary>Pocet filtrovaných / celkem, napr. "12 / 4035".</summary>
    public string FilteredCountText => $"{_filteredPoints.Count} / {_points.Count}";

    public event EventHandler? DataRefreshed;
    public event EventHandler? TypesLoaded;

    public Command RefreshCommand { get; }

    public MapViewModel(ILocalDatabaseService db, ISyncService sync)
    {
        _db   = db;
        _sync = sync;
        RefreshCommand = CreateBusyCommand(ForceSyncAsync);
    }

    // ── Inicializace ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        await _db.InitAsync();
        SetPoints(await _db.GetAllPointsAsync());
        _ = BackgroundSyncAsync();
    }

    // ── Filtrovani ──────────────────────────────────────────────────────────

    /// <summary>
    /// Prepocita seznam filtrovaných bodů (SearchText + SelectedType).
    /// Volá se z View po každé změně filtru (debounce pro hledání).
    /// </summary>
    public void RefreshFilter()
    {
        IEnumerable<EuroklicPoint> result = _points;

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var q = _searchText.ToLowerInvariant();
            result = result.Where(p =>
                p.Name.ToLowerInvariant().Contains(q) ||
                (p.Description?.ToLowerInvariant().Contains(q) ?? false) ||
                (p.Address?.ToLowerInvariant().Contains(q) ?? false));
        }

        if (!string.IsNullOrEmpty(_selectedType))
            result = result.Where(p => (p.Type ?? "default") == _selectedType);

        _filteredPoints = result.ToList();
        OnPropertyChanged(nameof(FilteredPoints));
        OnPropertyChanged(nameof(FilteredCountText));
    }

    /// <summary>Vrátí JSON filtrovaných bodů pro Leaflet.</summary>
    public string GetFilteredPointsJson()
        => JsonSerializer.Serialize(_filteredPoints,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    // ── Privatní ────────────────────────────────────────────────────────────

    private void SetPoints(List<EuroklicPoint> points)
    {
        _points = points;
        RefreshFilter();
        UpdateCountText();
        ExtractTypes();
    }

    private void ExtractTypes()
    {
        var types = _points
            .Select(p => p.Type)
            .Where(w => !string.IsNullOrEmpty(w))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        if (types.SequenceEqual(_availTypes)) return;
        _availTypes = types;
        OnPropertyChanged(nameof(AvailableTypes));
        OnPropertyChanged(nameof(HasTypes));
        TypesLoaded?.Invoke(this, EventArgs.Empty);
    }

    private async Task BackgroundSyncAsync()
    {
        StatusText = "Kontroluji aktualizace...";
        try
        {
            var r = await _sync.SyncIfNeededAsync();
            if (r.DataUpdated)
            {
                SetPoints(await _db.GetAllPointsAsync());
                StatusText = $"Aktualizovano: {r.PointCount} bodu";
                DataRefreshed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusText = r.Success
                    ? $"Data jsou aktualni ({r.PointCount} bodu)"
                    : $"Offline - {_points.Count} bodu ze zalohy";
            }
        }
        catch { StatusText = $"Offline - {_points.Count} bodu ze zalohy"; }
    }

    private async Task ForceSyncAsync()
    {
        StatusText = "Stahuji data ze serveru...";
        var r = await _sync.ForceSyncAsync();
        if (r.Success)
        {
            SetPoints(await _db.GetAllPointsAsync());
            StatusText = $"Stazeno {r.PointCount} bodu";
            DataRefreshed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            StatusText = $"Chyba: {r.Error}";
        }
    }

    private void UpdateCountText()
        => PointCountText = _points.Count > 0 ? $"{_points.Count} bodu" : "Zadna data";
}
