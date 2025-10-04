namespace ATBS.Utils;

public static class StringUtils
{
    public static string? Normalize(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static bool EqualsIgnoreCase(string s1, string s2) =>
        string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
}