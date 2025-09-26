namespace ATBS.API.Flights.Views;

public sealed class FlightImportResult
{
    public int RowsProcessed { get; set; }
    public List<(int RowNumber, string Error)> Errors { get; } = new();
}