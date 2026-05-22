namespace EuroklicMapMobile.Models;

/// <summary>Výsledek synchronizace dat se serverem.</summary>
public class SyncResult
{
    public bool    Success      { get; set; }
    public bool    DataUpdated  { get; set; }
    public int     PointCount   { get; set; }
    public string? Error        { get; set; }
    public string? ServerVersion { get; set; }
}
