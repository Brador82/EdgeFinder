using System.Text.Json;
using EdgeFinder.Core.Abstractions;

namespace EdgeFinder.DataSources.Finance;

public class AlphaVantageSource : IDataSource
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    private const string BaseUrl = "https://www.alphavantage.co/query";

    public string Name => "Alpha Vantage";
    public string Domain => "finance";
    public string[] SupportedDataTypes =>
        ["stock_price", "historical_prices", "forex", "crypto", "intraday"];

    public AlphaVantageSource(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public Task<bool> CanFulfillAsync(DataRequirement requirement)
    {
        var canFulfill = requirement.Domain == "finance"
                      && SupportedDataTypes.Contains(requirement.DataType);
        return Task.FromResult(canFulfill);
    }

    public async Task<DataPayload> FetchAsync(DataRequirement requirement, CancellationToken ct = default)
    {
        var url = requirement.DataType switch
        {
            "stock_price" => BuildQuoteUrl(requirement),
            "historical_prices" => BuildDailyUrl(requirement),
            "intraday" => BuildIntradayUrl(requirement),
            "forex" => BuildForexUrl(requirement),
            "crypto" => BuildCryptoUrl(requirement),
            _ => BuildQuoteUrl(requirement)
        };

        try
        {
            var response = await _http.GetStringAsync(url, ct);

            // AlphaVantage returns error messages in JSON — check for them
            if (response.Contains("\"Error Message\"") || response.Contains("\"Note\""))
            {
                return new DataPayload(Name, requirement.DataType, response, DateTimeOffset.UtcNow);
            }

            // Return raw JSON — Claude will interpret it
            return new DataPayload(Name, requirement.DataType, response, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            var error = JsonSerializer.Serialize(new { error = ex.Message });
            return new DataPayload(Name, requirement.DataType, error, DateTimeOffset.UtcNow);
        }
    }

    private string BuildQuoteUrl(DataRequirement req)
    {
        var symbol = GetSymbol(req);
        return $"{BaseUrl}?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
    }

    private string BuildDailyUrl(DataRequirement req)
    {
        var symbol = GetSymbol(req);
        var compact = req.Parameters.GetValueOrDefault("full", "false") == "true"
            ? "full" : "compact";
        return $"{BaseUrl}?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize={compact}&apikey={_apiKey}";
    }

    private string BuildIntradayUrl(DataRequirement req)
    {
        var symbol = GetSymbol(req);
        var interval = req.Parameters.GetValueOrDefault("interval", "15min");
        return $"{BaseUrl}?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval={interval}&apikey={_apiKey}";
    }

    private string BuildForexUrl(DataRequirement req)
    {
        var fromCurrency = req.Parameters.GetValueOrDefault("from_currency", "EUR");
        var toCurrency = req.Parameters.GetValueOrDefault("to_currency", "USD");
        return $"{BaseUrl}?function=CURRENCY_EXCHANGE_RATE&from_currency={fromCurrency}&to_currency={toCurrency}&apikey={_apiKey}";
    }

    private string BuildCryptoUrl(DataRequirement req)
    {
        var symbol = GetSymbol(req);
        var market = req.Parameters.GetValueOrDefault("market", "USD");
        return $"{BaseUrl}?function=DIGITAL_CURRENCY_DAILY&symbol={symbol}&market={market}&apikey={_apiKey}";
    }

    private static string GetSymbol(DataRequirement req)
    {
        if (req.Parameters.TryGetValue("symbol", out var symbol))
            return symbol.ToUpperInvariant();

        // Try to resolve common names to tickers
        var entity = req.Parameters.GetValueOrDefault("entity", "");
        return ResolveSymbol(entity);
    }

    private static string ResolveSymbol(string entity)
    {
        return entity.ToUpperInvariant() switch
        {
            "S&P 500" or "S&P" or "SPX" or "SP500" => "SPY",
            "NASDAQ" or "NQ" or "QQQ" => "QQQ",
            "DOW" or "DJI" or "DJIA" => "DIA",
            "RUSSELL" or "IWM" or "RUT" => "IWM",
            "BITCOIN" or "BTC" => "BTC",
            "ETHEREUM" or "ETH" => "ETH",
            "GOLD" or "XAUUSD" => "GLD",
            "CRUDE" or "OIL" or "CL" => "USO",
            var s when !string.IsNullOrWhiteSpace(s) => s,
            _ => "SPY"
        };
    }
}
