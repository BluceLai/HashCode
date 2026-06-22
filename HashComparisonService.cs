using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace HashCode;

public sealed class HashComparisonService
{
    private static readonly string[] AutoIgnoredExtensions = [".plcproj", ".tmc"];
    private const string AutoIgnoredDirectory = "_Libraries";

    public IReadOnlyList<ZipEntryHash> ReadEntries(string packagePath)
    {
        EnsurePackageExists(packagePath);

        using var archive = ZipFile.OpenRead(packagePath);
        var rows = new List<ZipEntryHash>();

        foreach (var entry in archive.Entries
                     .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                     .OrderBy(entry => NormalizeEntryName(entry.FullName), StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new ZipEntryHash(
                NormalizeEntryName(entry.FullName),
                entry.Length,
                ComputeEntryMd5(entry)));
        }

        return rows;
    }

    public ComparisonSummary Compare(string goldenPath, string targetPath) =>
        Compare(goldenPath, targetPath, Enumerable.Empty<string>());

    public ComparisonSummary Compare(
        string goldenPath,
        string targetPath,
        IEnumerable<string> ignoredEntries)
    {
        var goldenEntries = ReadEntries(goldenPath)
            .Where(entry => !IsAutoIgnoredEntry(entry.EntryName))
            .ToDictionary(entry => entry.EntryName, StringComparer.OrdinalIgnoreCase);
        var targetEntries = ReadEntries(targetPath)
            .Where(entry => !IsAutoIgnoredEntry(entry.EntryName))
            .ToDictionary(entry => entry.EntryName, StringComparer.OrdinalIgnoreCase);
        var goldenPackageHash = ComputePackageHash(goldenEntries.Values.Select(entry =>
            new PackageHashPart(entry.EntryName, entry.HashCode)));
        var targetPackageHash = ComputePackageHash(targetEntries.Values
            .Select(entry => new PackageHashPart(entry.EntryName, entry.HashCode)));
        var allEntryNames = goldenEntries.Keys
            .Union(targetEntries.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(entryName => entryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<ComparisonRow>();

        foreach (var entryName in allEntryNames)
        {
            goldenEntries.TryGetValue(entryName, out var golden);
            targetEntries.TryGetValue(entryName, out var target);

            var goldenHash = golden?.HashCode ?? string.Empty;
            var targetHash = target?.HashCode ?? string.Empty;
            var result = goldenHash.Length > 0 && targetHash.Length > 0 && goldenHash == targetHash
                ? "OK"
                : "NG";
            var note = (golden, target) switch
            {
                (null, not null) => "只存在於待檢查檔案",
                (not null, null) => "待檢查檔案缺少此項目",
                _ when result == "NG" => "MD5 不一致",
                _ => string.Empty
            };

            rows.Add(new ComparisonRow(entryName, goldenHash, targetHash, result, note));
        }

        return new ComparisonSummary(
            goldenPackageHash,
            targetPackageHash,
            rows.All(row => row.Result == "OK"),
            rows);
    }

    private static void EnsurePackageExists(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            throw new InvalidOperationException("請先設定 .tpzip 檔案路徑。");
        }

        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("找不到指定的 .tpzip 檔案。", packagePath);
        }
    }

    private static string ComputeEntryMd5(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var hash = MD5.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private static string ComputePackageHash(IEnumerable<PackageHashPart> parts)
    {
        var text = string.Join(Environment.NewLine,
            parts.Select(part => $"{NormalizeEntryName(part.EntryName)}={part.HashCode}"));
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToHexString(MD5.HashData(bytes));
    }

    private static string NormalizeEntryName(string entryName) =>
        entryName.Replace('\\', '/').Trim();

    private static bool IsAutoIgnoredEntry(string entryName)
    {
        var normalizedEntryName = NormalizeEntryName(entryName);
        return HasAutoIgnoredExtension(normalizedEntryName)
               || HasAutoIgnoredDirectory(normalizedEntryName);
    }

    private static bool HasAutoIgnoredExtension(string entryName) =>
        AutoIgnoredExtensions.Contains(Path.GetExtension(entryName), StringComparer.OrdinalIgnoreCase);

    private static bool HasAutoIgnoredDirectory(string entryName) =>
        entryName.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => string.Equals(segment, AutoIgnoredDirectory, StringComparison.OrdinalIgnoreCase));

    private sealed record PackageHashPart(string EntryName, string HashCode);
}

public sealed record ZipEntryHash(string EntryName, long Size, string HashCode);

public sealed record ComparisonRow(
    string EntryName,
    string GoldenHashCode,
    string TargetHashCode,
    string Result,
    string Note);

public sealed record ComparisonSummary(
    string GoldenPackageHashCode,
    string TargetPackageHashCode,
    bool IsSame,
    IReadOnlyList<ComparisonRow> Rows);
