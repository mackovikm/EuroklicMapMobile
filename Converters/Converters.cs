using System.Globalization;

namespace EuroklicMapMobile.Converters;

// ── Sdilena data typu - zrcadli ICON_TYPES z weboveho app.js ──────────────────
internal static class TypeMeta
{
    internal record Meta(string Label, string ColorHex, string Emoji);

    private static readonly Dictionary<string, Meta> Known = new()
    {
        { "default",      new("Obecný",              "#3b82f6", ""    ) },
        { "monument",     new("Památka",             "#7c3aed", "★"  ) },
        { "restaurant",   new("Restaurace",          "#ea580c", "🍴" ) },
        { "hotel",        new("Ubytování",           "#facc15", "🛏" ) },
        { "nature",       new("Příroda",             "#16a34a", "🌲" ) },
        { "transport",    new("Doprava",             "#0891b2", "🚌" ) },
        { "shop",         new("Obchod",              "#db2777", "🛍" ) },
        { "WC",           new("Toaleta",             "#38bdf8", "🚻" ) },
        { "Plošina",      new("Plošina",             "#f97316", "♿"  ) },
        { "Plošina + WC", new("Toaleta a plošina",   "#38f8bb", "🚻♿") },
        { "Výtah",        new("Výtah",               "#a855f7", "🛗" ) },
        { "Brána",        new("Brána",               "#92400e", "⛩️"  ) },
        { "Parkoviště",   new("Parkoviště",          "#1d4ed8", "🅿️" ) },
        { "Dveře",        new("Dveře",               "#78716c", "🚪" ) },
        { "Sprcha",       new("Sprcha",              "#0e7490", "🚿" ) },
        { "Závora",       new("Závora",              "#f59e0b", "🚧" ) },
        { "other",        new("Ostatní",             "#6b7280", "?"  ) },
    };

    internal static Meta For(string? type)
    {
        if (string.IsNullOrEmpty(type)) return Known["default"];
        if (Known.TryGetValue(type, out var m)) return m;
        // Neznamy typ: label = typ samotny, barva = hash (mirror colorFromString z app.js)
        var color = ColorFromString(type);
        return new Meta(
            char.ToUpperInvariant(type[0]) + type[1..],
            color,
            "?"
        );
    }

    /// <summary>
    /// Deterministicka barva z retezce – zrcadli colorFromString + hslToHex z app.js.
    /// </summary>
    private static string ColorFromString(string str)
    {
        int hash = 0;
        unchecked
        {
            foreach (char c in str)
                hash = (hash << 5) - hash + c;
        }
        int hue = Math.Abs(hash) % 360;
        return HslToHex(hue, 65, 45);
    }

    private static string HslToHex(int h, int s, int l)
    {
        double sd = s / 100.0, ld = l / 100.0;
        double K(double n) => (n + h / 30.0) % 12;
        double a = sd * Math.Min(ld, 1 - ld);
        byte Channel(double n)
        {
            double c = ld - a * Math.Max(-1, Math.Min(Math.Min(K(n) - 3, 9 - K(n)), 1));
            return (byte)Math.Round(255 * c);
        }
        return $"#{Channel(0):X2}{Channel(8):X2}{Channel(4):X2}";
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Typ bodu → Color (shoduje se s ICON_TYPES.color v app.js).</summary>
public class TypeColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Color.FromArgb(TypeMeta.For(value as string).ColorHex);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Typ bodu → cesky popisek (ICON_TYPES.label).</summary>
public class TypeLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => TypeMeta.For(value as string).Label;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Typ bodu → emoji ikona (ICON_TYPES.emoji).</summary>
public class TypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => TypeMeta.For(value as string).Emoji;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── Obecne konvertory ─────────────────────────────────────────────────────────

/// <summary>Invertuje bool.</summary>
public class InvertBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

/// <summary>Vrátí true pokud string není prázdný.</summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s);
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>bool → Color: true = zelená, false = červená.</summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
