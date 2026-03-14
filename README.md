# Dev Machine Benchmark

A .NET 10 benchmark harness that measures real-world developer workflow performance — git clone, dotnet restore/build/test, npm/pnpm toolchains, Playwright E2E, and Docker pulls — across multiple open-source repositories.

Designed to capture end-to-end dev machine performance including the overhead of DLP/endpoint protection, not synthetic micro-benchmarks.

## What It Measures

Four benchmark suites run sequentially, modeling real developer workflows (clone -> restore -> build -> test -> commit):

| Suite | Stack | Tasks |
|-------|-------|-------|
| **fake-survey-generator** | .NET + Node.js | Clone, restore, build, unit/integration/acceptance tests, npm ci/build/test, git commit |
| **dotnet-starter-project-template** | .NET Aspire | Clone, restore, build, Aspire tests, git commit |
| **home-page** | Node.js/pnpm/Playwright | Clone, pnpm install/build, Playwright install/tests, git commit |
| **Docker Image Pulls** | Docker | Pull 5 standard images (dotnet/sdk, dotnet/aspnet, node, postgres, redis) |

Each task is tagged by type — **NET** (network), **CPU**, **I/O**, or **MIX** — so you can distinguish network-dependent results from local performance.

## Prerequisites

**Required:** [.NET 10 SDK](https://dotnet.microsoft.com/download), [Git](https://git-scm.com/)

**Optional (suites skip gracefully if missing):**
- [Node.js](https://nodejs.org/) + npm — for npm-based tasks
- [pnpm](https://pnpm.io/) — for the home-page suite
- [Docker](https://www.docker.com/) — for integration/acceptance tests and Docker pull benchmarks

## Quick Start

```bash
dotnet run --project src/DevMachineBenchmark
```

Results are saved to `./results/` as both JSON and Markdown.

## CLI Options

```
--iterations N           Measured iterations per task (default: 5)
--warmup N               Warm-up iterations to discard (default: 1)
--preflight-only         Run preflight checks and exit
--skip-docker            Skip Docker pull benchmarks
--shuffle-suites         Randomize suite execution order
--output-dir PATH        Results directory (default: ./results)
--compare A.json B.json  Compare two result files side-by-side
--from-json FILE         Re-render markdown from an existing JSON result
--help                   Show help
```

## Comparing Machines

Run the benchmark on two machines, then compare the JSON results:

```bash
dotnet run --project src/DevMachineBenchmark -- --compare results/machine-a.json results/machine-b.json
```

The comparison flags winners per task, adjusts thresholds for noisy (high-CV) measurements, and warns when sample sizes are too small for meaningful conclusions.

## Statistics

Each task reports:

| Metric | Description |
|--------|-------------|
| **Median** | Central value — robust to outliers |
| **StdDev** | Sample standard deviation (Bessel's correction) |
| **CV%** | Coefficient of variation — flags noisy measurements |
| **95% CI** | Confidence interval using t-distribution |
| **IQR** | Interquartile range (when n >= 4) |
| **Trimmed Mean** | 10%-trimmed mean (when n >= 5) |
| **Outlier flag** | Warns when Max > 2x Median |

StdDev and CV display "N/A" when n <= 1 to avoid misleading zeros.

## Tips for Reliable Results

- Use `--iterations 5` or higher (default) for meaningful statistics
- Close resource-heavy applications before running — the harness warns if CPU > 50% at start
- Use `--shuffle-suites` to eliminate suite ordering bias
- Network-dependent tasks (clone, pull, install) will always have higher variance — the **Type** column helps distinguish these
- First run after a reboot will be slower due to cold OS caches; the warm-up iteration mitigates but doesn't fully eliminate this
