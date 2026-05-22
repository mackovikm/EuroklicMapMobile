using EuroklicMapMobile.ViewModels;

namespace EuroklicMapMobile.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private bool _mapLoaded;
    private CancellationTokenSource? _searchCts;

    // Barvy chipů
    private static readonly Color ChipActiveBg   = Color.FromArgb("#1565C0");
    private static readonly Color ChipInactiveBg  = Color.FromArgb("#FFFFFF");
    private static readonly Color ChipActiveText  = Color.FromArgb("#FFFFFF");
    private static readonly Color ChipInactiveText = Color.FromArgb("#333333");
    private static readonly Color ChipActiveBorder   = Color.FromArgb("#1565C0");
    private static readonly Color ChipInactiveBorder  = Color.FromArgb("#BDBDBD");

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        vm.DataRefreshed += async (_, _) => await RefreshAll();
        vm.TypesLoaded   += (_, _) => MainThread.BeginInvokeOnMainThread(RebuildChips);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();

        if (MapWebView.Source is null)
        {
            MapWebView.Source = new HtmlWebViewSource
            {
                Html = await LoadMapHtmlAsync()
            };
        }
    }

    // ── WebView ──────────────────────────────────────────────────────────────

    private void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("maui://", StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;

        // maui://bounds?n=...&s=...&e=...&w=...  → aktualizuj seznam viditelných bodů
        if (e.Url.StartsWith("maui://bounds?", StringComparison.OrdinalIgnoreCase))
        {
            var query = e.Url["maui://bounds?".Length..];
            var dict = query.Split('&')
                .Select(part => part.Split('='))
                .Where(kv => kv.Length == 2)
                .ToDictionary(kv => kv[0], kv => kv[1]);

            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (double.TryParse(dict.GetValueOrDefault("n"), System.Globalization.NumberStyles.Float, ci, out var north) &&
                double.TryParse(dict.GetValueOrDefault("s"), System.Globalization.NumberStyles.Float, ci, out var south) &&
                double.TryParse(dict.GetValueOrDefault("e"), System.Globalization.NumberStyles.Float, ci, out var east)  &&
                double.TryParse(dict.GetValueOrDefault("w"), System.Globalization.NumberStyles.Float, ci, out var west))
            {
                _vm.SetBounds(north, south, east, west);
            }
        }
    }

    private async void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success) return;
        _mapLoaded = true;
        await RefreshAll();
        await TryCenterOnGpsAsync();
    }

    // ── Vyhledávání ──────────────────────────────────────────────────────────

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        try
        {
            await Task.Delay(400, token);
            await RefreshAll();
        }
        catch (OperationCanceledException) { }
    }

    private async void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        _searchCts?.Cancel();
        await RefreshAll();
    }

    // ── GPS ──────────────────────────────────────────────────────────────────

    private async void OnGpsClicked(object? sender, EventArgs e)
        => await TryCenterOnGpsAsync();

    private async Task TryCenterOnGpsAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;

            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(8)
            });
            if (location is null) return;

            var lat = location.Latitude .ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var lng = location.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

            await MainThread.InvokeOnMainThreadAsync(async () =>
                await MapWebView.EvaluateJavaScriptAsync($"centerOnGps({lat}, {lng})"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GPS chyba: {ex.Message}");
        }
    }

    // ── Chipy typů ───────────────────────────────────────────────────────────

    /// <summary>
    /// Vytvoří lištu chipů: "Vše" + jeden chip na každý dostupný typ.
    /// Volá se po načtení/změně typů z dat.
    /// </summary>
    private void RebuildChips()
    {
        ChipsLayout.Children.Clear();

        // Chip „Vše"
        ChipsLayout.Children.Add(MakeChip("Vše", null));

        // Chip pro každý typ
        foreach (var type in _vm.AvailableTypes)
            ChipsLayout.Children.Add(MakeChip(GetTypeLabel(type), type));

        RefreshChipStyles();
    }

    private Border MakeChip(string label, string? typeKey)
    {
        var btn = new Button
        {
            Text            = label,
            FontSize        = 13,
            Padding         = new Thickness(14, 0),
            HeightRequest   = 34,
            CornerRadius    = 17,
            BackgroundColor = ChipInactiveBg,
            TextColor       = ChipInactiveText,
            CommandParameter = typeKey
        };

        btn.Clicked += async (_, _) =>
        {
            // Klik na uz aktivni chip → zobraz vse; jinak vyber tento typ
            _vm.SelectedType = (_vm.SelectedType == typeKey) ? null : typeKey;
            RefreshChipStyles();
            await RefreshAll();
        };

        // Wrapper Border pro ostrejsi obrys
        return new Border
        {
            Content         = btn,
            Stroke          = new SolidColorBrush(ChipInactiveBorder),
            StrokeThickness = 1,
            StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 17 },
            Padding         = 0,
            BackgroundColor = Colors.Transparent
        };
    }

    private void RefreshChipStyles()
    {
        foreach (var child in ChipsLayout.Children)
        {
            if (child is not Border border || border.Content is not Button btn) continue;

            var typeKey = btn.CommandParameter as string; // null = "Vše"
            var isActive = typeKey is null
                ? _vm.SelectedType is null      // chip "Vše" je aktivní když není vybraný žádný typ
                : _vm.SelectedType == typeKey;

            btn.BackgroundColor    = isActive ? ChipActiveBg   : ChipInactiveBg;
            btn.TextColor          = isActive ? ChipActiveText  : ChipInactiveText;
            border.Stroke          = new SolidColorBrush(isActive ? ChipActiveBorder : ChipInactiveBorder);
        }
    }

    // ── Seznam bodů ─────────────────────────────────────────────────────────

    private async void OnPointSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not EuroklicMapMobile.Models.EuroklicPoint point)
            return;

        // Odznačit výběr okamžitě (vizuální reset)
        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        // Zaměřit bod na mapě a otevřít jeho popup
        await MainThread.InvokeOnMainThreadAsync(async () =>
            await MapWebView.EvaluateJavaScriptAsync($"focusPoint({point.Id})"));
    }

    // ── Spodní panel ────────────────────────────────────────────────────────

    private void OnTogglePanelTapped(object? sender, TappedEventArgs e)
    {
        _vm.IsPanelExpanded = !_vm.IsPanelExpanded;
        PanelArrow.Text = _vm.IsPanelExpanded ? "▼" : "▲";
    }

    // ── Refresh mapy + seznamu ───────────────────────────────────────────────

    private async Task RefreshAll()
    {
        if (!_mapLoaded) return;

        _vm.RefreshFilter();

        var json     = _vm.GetFilteredPointsJson();
        var safeJson = json.Replace("\\", "\\\\").Replace("'", "\\'");

        await MainThread.InvokeOnMainThreadAsync(async () =>
            await MapWebView.EvaluateJavaScriptAsync($"loadPoints('{safeJson}')"));
    }

    // ── Pomocné ──────────────────────────────────────────────────────────────

    // Stejne hodnoty jako ICON_TYPES v app.js – emoji + cesky popisek pro chip
    private static string GetTypeLabel(string type) => type switch
    {
        "default"      => "Obecný",
        "monument"     => "★ Památka",
        "restaurant"   => "🍴 Restaurace",
        "hotel"        => "🛏 Ubytování",
        "nature"       => "🌲 Příroda",
        "transport"    => "🚌 Doprava",
        "shop"         => "🛍 Obchod",
        "WC"           => "🚻 Toaleta",
        "Plošina"      => "♿ Plošina",
        "Plošina + WC" => "🚻♿ Toaleta+Plošina",
        "Výtah"        => "🛗 Výtah",
        "Brána"        => "⛩️ Brána",
        "Parkoviště"   => "🅿️ Parkoviště",
        "Dveře"        => "🚪 Dveře",
        "Sprcha"       => "🚿 Sprcha",
        "Závora"       => "🚧 Závora",
        "other"        => "? Ostatní",
        _              => char.ToUpperInvariant(type[0]) + type[1..]
    };

    private static async Task<string> LoadMapHtmlAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
