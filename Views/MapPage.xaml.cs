using EuroklicMapMobile.ViewModels;

namespace EuroklicMapMobile.Views;

/// <summary>
/// Zobrazuje interaktivní Leaflet mapu v WebView.
///
/// Tok dat:
///   1. OnAppearing → MapViewModel.InitializeAsync() načte lokální body + spustí sync na pozadí.
///   2. OnMapNavigated → po načtení HTML stránky injektuje body do JavaScriptu.
///   3. MapViewModel.DataRefreshed → při aktualizaci dat reinjeketuje body do mapy.
/// </summary>
public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private bool _mapLoaded;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Když se obnoví data ze serveru, aktualizuj mapu
        vm.DataRefreshed += async (_, _) => await InjectPointsAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _vm.InitializeAsync();

        // Načti HTML mapu (jen poprvé nebo pokud WebView nemá zdroj)
        if (MapWebView.Source is null)
        {
            MapWebView.Source = new HtmlWebViewSource
            {
                Html = await LoadMapHtmlAsync()
            };
        }
    }

    /// <summary>Voláno WebView po dokončení navigace (tj. po načtení HTML).</summary>
    private async void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success) return;
        _mapLoaded = true;
        await InjectPointsAsync();
    }

    /// <summary>Injektuje pole bodů do Leaflet mapy přes JavaScript funkci loadPoints().</summary>
    private async Task InjectPointsAsync()
    {
        if (!_mapLoaded) return;

        var json = _vm.GetPointsJson();
        // Escapujeme jednoduché apostrofy – JSON používá uvozovky, ale pro jistotu
        var safeJson = json.Replace("\\", "\\\\").Replace("'", "\\'");

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await MapWebView.EvaluateJavaScriptAsync($"loadPoints('{safeJson}')");
        });
    }

    /// <summary>Načte map.html z MauiAsset (Resources/Raw/map.html).</summary>
    private static async Task<string> LoadMapHtmlAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
