using System.Net.Http.Json;
using EuroklicMapMobile.Models;

namespace EuroklicMapMobile.Services;

/// <summary>
/// Implementace komunikace s EuroklicMap REST API.
///
/// Autentizace probíhá přes HTTP header "X-Api-Key" – stejný mechanismus
/// jako ve webové aplikaci (ApiKeyAuthenticationHandler na serveru).
///
/// Endpoint pro verzi (/api/version) je veřejný – API klíč není nutný.
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _http;

    private string _baseUrl = string.Empty;
    private bool   _configured;

    public bool IsConfigured => _configured;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public void Configure(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl.TrimEnd('/');

        // Odstraň starý klíč, nastav nový
        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey.Trim());

        _configured = !string.IsNullOrWhiteSpace(_baseUrl)
                   && !string.IsNullOrWhiteSpace(apiKey);
    }

    /// <inheritdoc />
    public async Task<DataVersion?> GetVersionAsync()
    {
        if (string.IsNullOrEmpty(_baseUrl))
            throw new InvalidOperationException("API URL není nastaveno.");

        // Verze endpoint je veřejný – dočasně odebereme klíč z požadavku
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/version");
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataVersion>();
    }

    /// <inheritdoc />
    public async Task<List<EuroklicPoint>> GetAllPointsAsync()
    {
        if (!_configured)
            throw new InvalidOperationException("API není nakonfigurováno.");

        var response = await _http.GetAsync($"{_baseUrl}/api/points");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<EuroklicPoint>>();
        return result ?? [];
    }
}
