using EuroklicMapMobile.Models;

namespace EuroklicMapMobile.Services;

/// <summary>
/// Implementace synchronizace dat.
///
/// Logika verzování:
///   1. Zavolá GET /api/version (veřejný endpoint).
///   2. Porovná DbLastUpdate ze serveru s hodnotou uloženou v Preferences.
///   3. Pokud se liší (nebo je lokální DB prázdná) → stáhne všechny body a přepíše lokální DB.
///   4. Uloží nové DbLastUpdate do Preferences.
///
/// Klíč v Preferences: "sync_db_last_update"
/// </summary>
public class SyncService : ISyncService
{
    private const string PrefKey = "sync_db_last_update";

    private readonly IApiService           _api;
    private readonly ILocalDatabaseService _db;

    public event EventHandler<SyncResult>? SyncCompleted;

    public SyncService(IApiService api, ILocalDatabaseService db)
    {
        _api = api;
        _db  = db;
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncIfNeededAsync()
    {
        if (!_api.IsConfigured)
            return Fail("API není nakonfigurováno. Zadejte URL a API klíč v Nastavení.");

        try
        {
            var version = await _api.GetVersionAsync();
            if (version is null)
                return Fail("Server nevrátil informaci o verzi.");

            var storedUpdate = Preferences.Get(PrefKey, string.Empty);
            var localCount   = await _db.GetPointCountAsync();

            // Data jsou aktuální – není třeba nic stahovat
            if (storedUpdate == version.DbLastUpdate && localCount > 0)
            {
                return new SyncResult
                {
                    Success       = true,
                    DataUpdated   = false,
                    PointCount    = localCount,
                    ServerVersion = version.DbLastUpdate
                };
            }

            // Data jsou zastaralá nebo chybí → stáhneme
            return await DownloadAndSaveAsync(version);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> ForceSyncAsync()
    {
        if (!_api.IsConfigured)
            return Fail("API není nakonfigurováno. Zadejte URL a API klíč v Nastavení.");

        try
        {
            var version = await _api.GetVersionAsync();
            return await DownloadAndSaveAsync(version);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    // ── Privátní ───────────────────────────────────────────────────────────

    private async Task<SyncResult> DownloadAndSaveAsync(DataVersion? version)
    {
        var points = await _api.GetAllPointsAsync();
        await _db.SavePointsAsync(points);

        // Ulož verzi tak, aby příští spuštění vědělo, co je aktuální
        Preferences.Set(PrefKey, version?.DbLastUpdate ?? string.Empty);

        var result = new SyncResult
        {
            Success       = true,
            DataUpdated   = true,
            PointCount    = points.Count,
            ServerVersion = version?.DbLastUpdate
        };
        RaiseSyncCompleted(result);
        return result;
    }

    private SyncResult Fail(string message)
    {
        var result = new SyncResult { Success = false, Error = message };
        RaiseSyncCompleted(result);
        return result;
    }

    private void RaiseSyncCompleted(SyncResult result)
        => SyncCompleted?.Invoke(this, result);
}
