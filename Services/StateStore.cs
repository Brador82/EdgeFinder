using System.Text.Json;
using EdgeFinder.Models;

namespace EdgeFinder.Services;

public static class StateStore
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "edgefinder_data.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static AppState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppState>(json, JsonOpts) ?? new AppState();
            }
        }
        catch { }

        return new AppState();
    }

    public static void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOpts);
        File.WriteAllText(FilePath, json);
    }
}
