using ATBS.Domain.Enums;

namespace ATBS.API.Flights.DTOs;

public sealed record FlightClassOption(TravelClass Class, decimal Price, int SeatsAvailable);