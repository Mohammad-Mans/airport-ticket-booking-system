using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public sealed class FlightImportResult
{
    public int RowsProcessed { get; set; }
    public List<(int RowNumber, string Error)> Errors { get; } = new();
}

public sealed class FlightSearchQuery
{
    public string? DepartureCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public DateOnly? DepartureDateUtc { get; set; }
    public TravelClass? Class { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyWithSeats { get; set; } = true;
}

public sealed record FlightClassOption(TravelClass Class, decimal Price, int SeatsAvailable);

public sealed record FlightOption(
    Flight Flight,
    IReadOnlyList<FlightClassOption> ClassOptions,
    int TotalSeats);

public interface IFlightService
{
    Task<FlightImportResult> ImportFromCsvAsync(string csvPath);
    Task<IReadOnlyList<FlightOption>> SearchAsync(FlightSearchQuery query);
}