using SQLite;

namespace EuroklicMapMobile.Models;

/// <summary>
/// Jeden bod na mapě – zrcadlí model ze serveru.
/// Ukládá se lokálně do SQLite přes sqlite-net-pcl.
/// </summary>
[Table("Points")]
public class EuroklicPoint
{
    [PrimaryKey]
    public int    Id          { get; set; }
    public double Latitude    { get; set; }
    public double Longitude   { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type        { get; set; }
    public string? IconUrl     { get; set; }
    public string? Address     { get; set; }

    /// <summary>Vzdálenost od referenčního bodu v km – nepersistováno, počítáno za běhu.</summary>
    [Ignore] public double? DistanceKm { get; set; }

    [Ignore] public string DistanceText => DistanceKm.HasValue
        ? (DistanceKm.Value < 1
            ? $"{(int)(DistanceKm.Value * 1000)} m"
            : $"{DistanceKm.Value:F1} km")
        : string.Empty;

    [Ignore] public bool HasDistance => DistanceKm.HasValue;
}
