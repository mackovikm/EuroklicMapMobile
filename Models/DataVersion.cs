namespace EuroklicMapMobile.Models;

/// <summary>
/// Odpověď z GET /api/version.
/// DbLastUpdate slouží jako klíč pro detekci změn dat.
/// </summary>
public class DataVersion
{
    public string? Version     { get; set; }
    public string? BuildDate   { get; set; }
    public string? Kontakt     { get; set; }

    /// <summary>
    /// Datum a čas poslední změny dat v databázi serveru.
    /// Formát: "yyyy-MM-dd HH:mm:ss GMT zzz"
    /// Pokud se tato hodnota liší od lokálně uložené → data jsou zastaralá.
    /// </summary>
    public string? DbLastUpdate { get; set; }
}
