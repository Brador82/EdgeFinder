namespace EdgeFinder.Core.Abstractions;

public interface IDataSource
{
    string Name { get; }
    string Domain { get; }
    string[] SupportedDataTypes { get; }

    Task<bool> CanFulfillAsync(DataRequirement requirement);
    Task<DataPayload> FetchAsync(DataRequirement requirement, CancellationToken ct = default);
}

public record DataRequirement(
    string DataType,
    string Domain,
    Dictionary<string, string> Parameters,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

public record DataPayload(
    string SourceName,
    string DataType,
    string RawData,
    DateTimeOffset Timestamp);
