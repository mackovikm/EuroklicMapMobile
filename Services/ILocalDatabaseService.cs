using EuroklicMapMobile.Models;

namespace EuroklicMapMobile.Services;

/// <summary>Lokální SQLite databáze pro offline přístup k datům.</summary>
public interface ILocalDatabaseService
{
    Task InitAsync();
    Task SavePointsAsync(IEnumerable<EuroklicPoint> points);
    Task<List<EuroklicPoint>> GetAllPointsAsync();
    Task<int> GetPointCountAsync();
}
