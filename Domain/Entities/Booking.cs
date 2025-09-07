using ATBS.Domain.Enums;

namespace ATBS.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FlightId { get; set; }
    public Guid PassengerId { get; set; }
    public decimal Price { get; set; }
    public TravelClass Class { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}