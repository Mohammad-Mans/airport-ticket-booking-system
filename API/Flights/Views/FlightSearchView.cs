using ATBS.Domain.Entities;

namespace ATBS.API.Flights.Views;

public sealed record FlightSearchView(Flight Flight, IReadOnlyList<FlightClassOption> ClassOptions);