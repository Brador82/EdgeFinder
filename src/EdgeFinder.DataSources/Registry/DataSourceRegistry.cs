using EdgeFinder.Core.Abstractions;

namespace EdgeFinder.DataSources.Registry;

public class DataSourceRegistry : IDataSourceRegistry
{
    private readonly List<IDataSource> _sources = [];

    public IReadOnlyList<IDataSource> All => _sources.AsReadOnly();

    public void Register(IDataSource source)
    {
        _sources.Add(source);
    }

    public IReadOnlyList<IDataSource> GetSourcesForRequirement(DataRequirement requirement)
    {
        return _sources
            .Where(s => s.Domain == requirement.Domain &&
                        s.SupportedDataTypes.Contains(requirement.DataType))
            .ToList()
            .AsReadOnly();
    }
}
