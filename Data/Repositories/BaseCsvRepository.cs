using System.Text;

namespace ATBS.Data.Repositories;

public abstract class BaseCsvRepository<T> where T : class
{
    protected readonly string _filePath;

    protected BaseCsvRepository(string filePath)
    {
        _filePath = filePath;
        EnsureFileExists();
    }

    protected abstract string Header { get; }
    protected abstract T Parse(string line);
    protected abstract string Serialize(T item);

    private void EnsureFileExists()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(_filePath))
        {
            using var sw = new StreamWriter(_filePath, false);
            sw.WriteLine(Header);
        }
    }

    protected async Task<List<T>> ReadAllAsync()
    {
        var list = new List<T>();
        using var sr = new StreamReader(_filePath);

        string? line;
        bool isHeader = true;
        while ((line = await sr.ReadLineAsync()) != null)
        {
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                list.Add(Parse(line));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing CSV line: {line}", ex);
            }
        }

        return list;
    }

    protected async Task WriteAllAsync(IEnumerable<T> items)
    {
        await using var sw = new StreamWriter(_filePath, append: false);
        await sw.WriteLineAsync(Header);
        foreach (var item in items)
            await sw.WriteLineAsync(Serialize(item));
    }
}