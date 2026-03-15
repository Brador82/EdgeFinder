using System.Text.Json;
using EdgeFinder.Core.Abstractions;
using EdgeFinder.Core.Models;

namespace EdgeFinder.DataSources.Persistence;

public class JsonStateStore : IStateStore
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JsonStateStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "edgefinder_data.json");
    }

    public AppState Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppState>(json, JsonOpts) ?? new AppState();
            }
        }
        catch { }

        return new AppState();
    }

    public void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOpts);
        File.WriteAllText(_filePath, json);
    }
}
