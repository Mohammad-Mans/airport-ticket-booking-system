using System.Globalization;

namespace ATBS.Utils;

public static class ParsingUtils
{
    public static DateTime ParseUtc(string s, string name)
    {
        var styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        if (DateTime.TryParseExact(s, "O", CultureInfo.InvariantCulture, styles, out var dt)) return dt;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, styles, out dt)) return dt;
        throw new FormatException($"{name} is not a valid UTC/ISO date.");
    }

    public static decimal ParseDecimalNonNegative(string s, string name)
    {
        if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            throw new FormatException($"{name} is not a valid number.");
        if (d < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
        return d;
    }

    public static int ParseIntNonNegative(string s, string name)
    {
        if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            throw new FormatException($"{name} is not a valid integer.");
        if (i < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
        return i;
    }
}