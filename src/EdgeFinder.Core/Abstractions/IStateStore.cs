using EdgeFinder.Core.Models;

namespace EdgeFinder.Core.Abstractions;

public interface IStateStore
{
    AppState Load();
    void Save(AppState state);
}
