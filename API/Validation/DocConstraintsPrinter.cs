using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ATBS.Domain.Attributes;

namespace ATBS.API.Validation;

public static class DocConstraintsPrinter
{
    public static void PrintFor(string title, params Type[] types)
    {
        Console.WriteLine($"\n {title}:");

        foreach (var t in types)
        {
            var props = t.GetProperties()
                .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null)
                .ToList();

            foreach (var p in props)
            {
                string label = p.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? p.Name;
                string typeLabel = GetTypeLabel(p);

                var parts = new List<string> { "Required" };
                if (p.GetCustomAttribute<RangeAttribute>() is { } range)
                    parts.Add($"Range: {range.Minimum} -> {range.Maximum}");

                if (p.GetCustomAttribute<DocConstraintAttribute>() is { } doc)
                    parts.Add(doc.Text);

                var constraintsLine = string.Join(", ", parts);

                Console.WriteLine($"  - {label}:");
                Console.WriteLine($"    - Type: {typeLabel}");
                Console.WriteLine($"    - Constraints: {constraintsLine}");
            }
        }
    }

    private static string GetTypeLabel(PropertyInfo p)
    {
        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        if (t == typeof(string)) return "Free Text";
        if (t == typeof(DateTime)) return "Date Time";
        if (t == typeof(int)) return "Number (integer)";
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return "Number (decimal)";
        return t.Name;
    }
}