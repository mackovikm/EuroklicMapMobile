using EuroklicMapMobile.Models;

namespace EuroklicMapMobile.Services;

/// <summary>Synchronizace lokálních dat se serverem.</summary>
public interface ISyncService
{
    /// <summary>
    /// Zkontroluje verzi dat na serveru.
    /// Pokud je server novější (nebo lokální DB je prázdná) → stáhne a uloží data.
    /// Pokud jsou data aktuální → nedělá nic.
    /// </summary>
    Task<SyncResult> SyncIfNeededAsync();

    /// <summary>Vždy stáhne čerstvá data ze serveru bez ohledu na verzi.</summary>
    Task<SyncResult> ForceSyncAsync();

    /// <summary>Událost vyvolaná po dokončení synchronizace (úspěch i chyba).</summary>
    event EventHandler<SyncResult>? SyncCompleted;
}
