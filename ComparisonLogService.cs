using System.Text;

namespace HashCode;

public sealed class ComparisonLogService
{
    private readonly object _syncRoot = new();

    public string Append(AppSettings settings, ComparisonSummary summary)
    {
        lock (_syncRoot)
        {
            Directory.CreateDirectory(settings.LogDirectory);
            var filePath = GetLogFilePath(settings);
            var nextIndex = GetNextIndex(filePath);
            var date = DateTime.Now.ToString("yyyyMMdd");
            var time = DateTime.Now.ToString("HH:mm:ss");
            var result = summary.IsSame ? "OK" : "NG";

            EnsureLogHeader(filePath);

            var line = string.Join(',',
                nextIndex,
                date,
                time,
                summary.GoldenPackageHashCode,
                summary.TargetPackageHashCode,
                result);
            File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            return filePath;
        }
    }

    public static string GetLogFilePath(AppSettings settings)
    {
        var prefix = string.IsNullOrWhiteSpace(settings.LogNamePrefix)
            ? "log"
            : SanitizeFileName(settings.LogNamePrefix.Trim());
        return Path.Combine(settings.LogDirectory, $"{prefix}_{DateTime.Now:yyyyMMdd}.csv");
    }

    private static int GetNextIndex(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return 1;
        }

        var dataLineCount = File.ReadLines(filePath)
            .Skip(1)
            .Count(line => !string.IsNullOrWhiteSpace(line));
        return dataLineCount + 1;
    }

    private static void EnsureLogHeader(string filePath)
    {
        const string header = "Index,Date,Time,GoldenHashCode,unCheckGoldenHashCode,Result";

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, header + Environment.NewLine, Encoding.UTF8);
            return;
        }

        var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
        if (lines.Count == 0)
        {
            File.WriteAllText(filePath, header + Environment.NewLine, Encoding.UTF8);
            return;
        }

        if (string.Equals(lines[0], header, StringComparison.Ordinal))
        {
            return;
        }

        lines[0] = header;
        File.WriteAllLines(filePath, lines, Encoding.UTF8);
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }
}
