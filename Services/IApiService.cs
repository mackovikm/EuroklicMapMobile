using EuroklicMapMobile.Models;

namespace EuroklicMapMobile.Services;

/// <summary>
/// Komunikace se serverovým REST API.
/// Autentizace: HTTP header X-Api-Key.
/// </summary>
public interface IApiService
{
    /// <summary>
    /// Nastaví základní URL a API klíč.
    /// Voláno při startu (z MauiProgram) a po uložení nastavení.
    /// </summary>
    void Configure(string baseUrl, string apiKey);

    /// <summary>GET /api/version – veřejný endpoint, nevyžaduje API klíč.</summary>
    Task<DataVersion?> GetVersionAsync();

    /// <summary>GET /api/points – vrátí všechny body (vyžaduje API klíč).</summary>
    Task<List<EuroklicPoint>> GetAllPointsAsync();

    /// <summary>Vrátí true, pokud je API nakonfigurováno (URL + klíč).</summary>
    bool IsConfigured { get; }
}
