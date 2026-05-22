using SQLite;
using EuroklicMapMobile.Models;

namespace EuroklicMapMobile.Services;

/// <summary>
/// Lokální SQLite databáze uložená v privátním adresáři aplikace.
/// Slouží jako offline cache bodů stažených z API.
///
/// Soubor: {AppDataDirectory}/euroklic.db3
/// </summary>
public class LocalDatabaseService : ILocalDatabaseService
{
    private SQLiteAsyncConnection? _db;
    private readonly string        _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public LocalDatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "euroklic.db3");
    }

    /// <summary>Inicializuje databázi (vytvoří tabulky, pokud neexistují). Bezpečné pro vícenásobné volání.</summary>
    public async Task InitAsync()
    {
        if (_db is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return; // double-check po získání zámku
            _db = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _db.CreateTableAsync<EuroklicPoint>();
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Atomicky nahradí všechny body novými daty.
    /// Probíhá v transakci – buď se uloží vše, nebo nic.
    /// </summary>
    public async Task SavePointsAsync(IEnumerable<EuroklicPoint> points)
    {
        await InitAsync();
        var list = points.ToList();

        await _db!.RunInTransactionAsync(conn =>
        {
            conn.DeleteAll<EuroklicPoint>();
            conn.InsertAll(list);
        });
    }

    public async Task<List<EuroklicPoint>> GetAllPointsAsync()
    {
        await InitAsync();
        return await _db!.Table<EuroklicPoint>().ToListAsync();
    }

    public async Task<int> GetPointCountAsync()
    {
        await InitAsync();
        return await _db!.Table<EuroklicPoint>().CountAsync();
    }
}
