using System.ComponentModel.DataAnnotations;
using ATBS.Domain.Attributes;

namespace ATBS.Domain.Entities;

public class Flight
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Display(Name = "Flight Number")]
    [Required]
    public string FlightNumber { get; set; } = null!;

    [Display(Name = "Departure Airport")]
    [Required]
    public string DepartureAirport { get; set; } = null!;

    [Display(Name = "Departure Country")]
    [Required]
    public string DepartureCountry { get; set; } = null!;

    [Display(Name = "Departure Date")]
    [DocConstraint("Must be UTC, Allowed Range: today -> future")]
    [Required]
    public DateTime DepartureDate { get; set; }

    [Display(Name = "Arrival Airport")]
    [Required]
    public string ArrivalAirport { get; set; } = null!;

    [Display(Name = "Destination Country")]
    [Required]
    public string DestinationCountry { get; set; } = null!;

    [Display(Name = "Arrival Date")]
    [DocConstraint("Must be UTC, Must be after DepartureDate")]
    [Required]
    public DateTime ArrivalDate { get; set; }
}