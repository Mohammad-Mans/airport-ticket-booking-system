namespace ATBS.Domain.Utils;

public static class PassengerUtils
{
    public static (string normalizedFirst, string normalizedLast) NormalizeNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        return (firstName.Trim(), lastName.Trim());
    }
}
