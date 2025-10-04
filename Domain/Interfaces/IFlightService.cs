using ATBS.API.Flights.Queries;
using ATBS.API.Flights.DTOs;

namespace ATBS.Domain.Interfaces;

public interface IFlightService
{
    Task<FlightImportResult> ImportFromCsvAsync(string csvPath);
    Task<IReadOnlyList<FlightSearchView>> SearchAsync(FlightSearchQuery query);
}