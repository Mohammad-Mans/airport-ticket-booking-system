using ATBS.Domain.Entities;

namespace ATBS.API.Flights.DTOs;

public sealed record FlightSearchView(Flight Flight, IReadOnlyList<FlightClassOption> ClassOptions);