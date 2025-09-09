using ATBS.Domain.Enums;

namespace ATBS.Domain.Entities;

public class FlightClass
{
    public Guid FlightId { get; set; }
    public TravelClass Class { get; set; }
    public decimal Price { get; set; }
    public int CapacityTotal { get; set; }
    public int SeatsAvailable { get; set; }
}