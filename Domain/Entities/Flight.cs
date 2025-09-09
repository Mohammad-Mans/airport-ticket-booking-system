using ATBS.Domain.Enums;

namespace ATBS.Domain.Entities;

public class Flight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FlightNumber { get; set; } = null!;
    public string DepartureAirport { get; set; } = null!;
    public string DepartureCountry { get; set; } = null!;
    public string ArrivalAirport { get; set; } = null!;
    public string DestinationCountry { get; set; } = null!;
    public DateTime DepartureDate { get; set; }
    public DateTime ArrivalDate { get; set; }
}