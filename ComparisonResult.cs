namespace HashCode;

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
