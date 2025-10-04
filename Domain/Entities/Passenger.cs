namespace ATBS.Domain.Entities;

public class Passenger
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public decimal Balance { get; set; }
}