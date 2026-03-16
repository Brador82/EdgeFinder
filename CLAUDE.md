# EdgeFinder

## What is this?

A .NET 10 console application that finds +EV (positive expected value) sports bets and provides a natural language probability engine powered by Claude. It does NOT place trades — it's a decision support tool.

## Solution structure

```
EdgeFinder.slnx
src/
  EdgeFinder.Core/          ← Shared: models, math, abstractions, pipeline
  EdgeFinder.DataSources/   ← OddsService, JsonStateStore, OddsApiSource, DataSourceRegistry
  EdgeFinder.Claude/        ← Claude API client, QueryAnalyzer, ProbabilityEngine
  EdgeFinder.Console/       ← Console app entry point and UI
```

## Key architecture

- **IDataSource** plugin interface — any data provider implements this to plug into the pipeline
- **QueryPipeline**: question → ClaudeQueryAnalyzer → DataSourceRegistry → fetch → ClaudeProbabilityEngine → result
- **EdgeScanner**: deterministic sports edge detection using multiplicative devigging + Kelly criterion
- **MathEngine**: pure static math (AmToDec, AmToImpl, CalcKelly, CalcEdge, GetConsensusProb, GetBestLine)

## Build & run

```bash
cd src/EdgeFinder.Console
dotnet run
```

## Configuration

API keys go in `src/EdgeFinder.Console/appsettings.json` (gitignored):
```json
{
  "OddsApiKey": "your-odds-api-key",
  "AnthropicApiKey": "your-anthropic-api-key"
}
```

Or via environment variables: `ODDS_API_KEY`, `ANTHROPIC_API_KEY`.

## External APIs

- **The Odds API** (https://the-odds-api.com) — live sports odds, h2h markets, American format
- **Anthropic Claude API** — query analysis and probability interpretation (uses claude-sonnet-4-20250514)

## Conventions

- Target framework: net10.0
- Namespaces follow folder structure: `EdgeFinder.Core.Math`, `EdgeFinder.DataSources.Sports`, etc.
- Abstractions live in `EdgeFinder.Core.Abstractions/`
- No third-party Claude SDK — raw HttpClient + System.Text.Json
- State persisted to `edgefinder_data.json` via JsonStateStore
- `appsettings.json` and `edgefinder_data.json` are gitignored
