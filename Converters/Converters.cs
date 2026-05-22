using System.Globalization;

namespace EuroklicMapMobile.Converters;

/// <summary>Invertuje bool – používá se pro IsEnabled="{Binding IsBusy, Converter=...}".</summary>
public class InvertBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

/// <summary>Vrátí true pokud string není prázdný – pro zobrazení výsledkového Frame.</summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>bool → Color: true = zelená (#2E7D32), false = červená (#C62828).</summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Typ bodu → Color (shoduje se s TYPE_COLORS v map.html).
/// Pouziva se pro barevnou tecku vedle nazvu bodu v seznamu.
/// </summary>
public class TypeColorConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Colors = new()
    {
        { "default",    "#1565C0" },
        { "monument",   "#C62828" },
        { "restaurant", "#E65100" },
        { "hotel",      "#6A1B9A" },
        { "nature",     "#2E7D32" },
        { "transport",  "#0277BD" },
        { "shop",       "#EF6C00" },
        { "other",      "#546E7A" },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value as string ?? "default";
        var hex = Colors.TryGetValue(key, out var c) ? c : Colors["default"];
        return Color.FromArgb(hex);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Typ bodu → cesky popisek, napr. "monument" → "Památka".</summary>
public class TypeLabelConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        { "default",    "Výchozí"    },
        { "monument",   "Památka"    },
        { "restaurant", "Restaurace" },
        { "hotel",      "Ubytování"  },
        { "nature",     "Příroda"    },
        { "transport",  "Doprava"    },
        { "shop",       "Obchod"     },
        { "other",      "Ostatní"    },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value as string ?? "default";
        if (Labels.TryGetValue(key, out var label)) return label;
        // Nezname typy: prvni pismeno velke, zbytek tak jak je
        return key.Length > 0
            ? char.ToUpperInvariant(key[0]) + key[1..]
            : key;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
