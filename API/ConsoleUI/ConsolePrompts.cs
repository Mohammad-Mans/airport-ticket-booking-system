using System.Globalization;
using ATBS.Domain.Enums;

namespace ATBS.API.ConsoleUI;

public static class ConsolePrompts
{
    public static string? Optional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    public static string Required(string label)
    {
        while (true)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            Console.WriteLine("This field is required. Please try again.");
        }
    }

    public static int Index(string label, int minInclusive, int maxInclusive)
    {
        while (true)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            if (int.TryParse(s, out var n) && n >= minInclusive && n <= maxInclusive) return n;
            Console.WriteLine($"Enter a number between {minInclusive} and {maxInclusive}.");
        }
    }

    public static bool Confirm(string label)
    {
        Console.Write(label);
        var s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
        return s is "y" or "yes";
    }

    public static DateOnly? DateOnlyOptional(string label, string format = "dd-MM-yyyy")
    {
        Console.Write(label);
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateOnly.TryParseExact(s.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        Console.WriteLine($"Invalid date format (expected {format}). Ignoring.");
        return null;
    }

    private static bool TryParseDecimal(string? s, out decimal value)
    {
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return true;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
        value = 0;
        return false;
    }

    public static decimal? DecimalOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (TryParseDecimal(s, out var d)) return d;

        Console.WriteLine("Invalid number. Ignoring.");
        return null;
    }

    public static decimal DecimalWithDefault(string label, decimal defaultValue)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        return TryParseDecimal(s, out var d) ? d : defaultValue;
    }

    public static decimal PositiveDecimal(string label)
    {
        while (true)
        {
            Console.Write(label);
            var s = Console.ReadLine();

            if (TryParseDecimal(s, out var d) && d > 0m)
                return d;

            Console.WriteLine("Please enter a positive amount.");
        }
    }

    private static bool TryParseClass(string? input, out TravelClass value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        switch (input.Trim().ToLowerInvariant())
        {
            case "e":
            case "economy":
                value = TravelClass.Economy;
                return true;
            case "b":
            case "business":
                value = TravelClass.Business;
                return true;
            case "f":
            case "first":
                value = TravelClass.First;
                return true;
            default:
                return false;
        }
    }

    public static TravelClass? ClassOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();

        if (TryParseClass(s, out var cls)) return cls;

        if (!string.IsNullOrWhiteSpace(s))
            Console.WriteLine("Unknown class. Ignoring.");
        return null;
    }

    public static TravelClass ClassRequired(string label, TravelClass currentClass)
    {
        while (true)
        {
            Console.Write(label);
            var s = Console.ReadLine();

            if (!TryParseClass(s, out var cls))
            {
                Console.WriteLine("Unknown class. Please enter Economy/Business/First.");
                continue;
            }

            if (cls == currentClass)
            {
                Console.WriteLine("New class must be different from current class.");
                continue;
            }

            return cls;
        }
    }
}