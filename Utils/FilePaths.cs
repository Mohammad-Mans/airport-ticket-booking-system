namespace ATBS.Utils;

public static class FilePaths
{
    public static readonly string PassengerFilePath = GetFilePath("Passengers");

    // Build an absolute path to Seed/<fileName>.csv under the *project* folder.
    // When running the project, the working directory is usually:
    //   <project>\bin\<config>\netX.Y\
    // So we walk up three directories to reach the project root, then append "Seed/<file>.csv".
    private static string GetFilePath(string fileName)
    {
        var parentPath = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.ToString();

        if (parentPath == null)
            throw new DirectoryNotFoundException("Could not find project root directory");

        return Path.Combine(parentPath, "Seed", $"{fileName}.csv");
    }
}