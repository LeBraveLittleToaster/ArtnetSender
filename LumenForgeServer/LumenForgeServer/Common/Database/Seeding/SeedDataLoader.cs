using System.Reflection;

namespace LumenForgeServer.Common.Database.Seeding;

/// <summary>
/// Reads semicolon-delimited seed data from CSV files embedded in the assembly.
/// The first line of every file is treated as a header and skipped.
/// Fields are split on <c>;</c> — use a different delimiter character in values if needed.
/// </summary>
internal static class SeedDataLoader
{
    private static readonly Assembly Assembly = typeof(SeedDataLoader).Assembly;
    private const string ResourcePrefix = "LumenForgeServer.Common.Database.Seeding.Data.";

    /// <summary>
    /// Returns all data rows from the embedded CSV file <paramref name="fileName"/>.
    /// Each row is a string array of field values, split on <c>;</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the embedded resource cannot be found.</exception>
    internal static IReadOnlyList<string[]> Load(string fileName)
    {
        var resourceName = ResourcePrefix + fileName;

        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded seed resource '{resourceName}' not found. " +
                $"Ensure '{fileName}' is marked as EmbeddedResource in the project file.");

        using var reader = new StreamReader(stream);

        reader.ReadLine(); // skip header

        var rows = new List<string[]>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                rows.Add(line.Split(';'));
        }

        return rows;
    }
}
