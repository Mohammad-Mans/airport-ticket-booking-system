namespace ATBS.Domain.Interfaces;

public sealed class FlightImportResult
{
    public int RowsProcessed { get; set; }
    public List<(int RowNumber, string Error)> Errors { get; } = new();
}

public interface IFlightService
{
    Task<FlightImportResult> ImportFromCsvAsync(string csvPath);
}