using ATBS.Domain.Enums;

namespace ATBS.API.Flights.Queries;

public sealed class FlightSearchQuery
{
    public string? DepartureCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public DateOnly? DepartureDateUtc { get; set; }
    public TravelClass? Class { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyWithSeats { get; set; } = true;
}