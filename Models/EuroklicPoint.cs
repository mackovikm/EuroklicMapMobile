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
}
