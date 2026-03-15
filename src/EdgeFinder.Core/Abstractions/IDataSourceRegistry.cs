namespace EdgeFinder.Core.Abstractions;

public interface IDataSourceRegistry
{
    void Register(IDataSource source);
    IReadOnlyList<IDataSource> GetSourcesForRequirement(DataRequirement requirement);
    IReadOnlyList<IDataSource> All { get; }
}
