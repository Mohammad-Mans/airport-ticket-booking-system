using System.ComponentModel.DataAnnotations;
using ATBS.Domain.Attributes;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Entities;

public class FlightClass
{
    public Guid FlightId { get; set; }
    public TravelClass Class { get; set; }

    [Display(Name = "Class Price")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [DocConstraint("*Required for Economy, optional for Business/First*")]
    [Required]
    public decimal Price { get; set; }

    [Display(Name = "Class Capacity")]
    [Range(0, int.MaxValue)]
    [DocConstraint("*Required for Economy, optional for Business/First*")]
    [Required]
    public int CapacityTotal { get; set; }

    public int SeatsAvailable { get; set; }
}