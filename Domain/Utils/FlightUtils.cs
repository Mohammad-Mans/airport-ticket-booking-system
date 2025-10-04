using ATBS.Domain.Entities;

namespace ATBS.Domain.Utils;

public static class FlightUtils
{
    public static string ComputeFlightKey(Flight flight)
        => $"{flight.FlightNumber}|{flight.DepartureAirport}|{flight.ArrivalAirport}|{flight.DepartureDate:O}";

    public static string RequireNonEmpty(string value, string fieldName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{fieldName} is required.") : value.Trim();
}