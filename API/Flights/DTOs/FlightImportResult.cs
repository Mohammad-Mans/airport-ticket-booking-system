namespace ATBS.API.Flights.DTOs;

public sealed class FlightImportResult
{
    public int RowsProcessed { get; set; }
    public List<(int RowNumber, string Error)> Errors { get; } = new();
}