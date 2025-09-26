using ATBS.Domain.Enums;

namespace ATBS.API.Flights.Views;

public sealed record FlightClassOption(TravelClass Class, decimal Price, int SeatsAvailable);