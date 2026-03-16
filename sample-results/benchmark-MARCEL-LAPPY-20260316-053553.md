# Benchmark Results — MARCEL-LAPPY

_2026-03-16 05:35:53 UTC · 5 iterations (+1 warm-up discarded)_

## Machine

| | |
|---|---|
| **Hostname** | MARCEL-LAPPY |
| **OS** | Microsoft Windows 10.0.26200 |
| **CPU** | Intel(R) Core(TM) Ultra 7 155H (22 cores) |
| **RAM** | 15.4 GB |
| **Disk** | SSD |
| **.NET SDK** | 10.0.201 |
| **Node** | v25.8.1 |
| **Docker** | Docker version 29.2.1, build a5c7197 |
| **CPU Load at Start** | 0.0% |
| **Memory Used at Start** | 66.7% |

## Results

### fake-survey-generator

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| git clone | NET | 3.76s | 436ms | 11.1% | 3.40s | 4.42s | [3.37s, 4.45s] |
| dotnet restore fake-survey-generator/FakeSurveyGenerator.slnx | NET | 1.61s | 144ms | 9.1% | 1.33s | 1.68s | [1.40s, 1.76s] |
| dotnet build fake-survey-generator/FakeSurveyGenerator.slnx | CPU | 6.40s | 784ms | 12.0% | 5.53s | 7.67s | [5.54s, 7.48s] |
| dotnet clean + build fake-survey-generator/FakeSurveyGenerator.slnx | CPU | 7.81s | 414ms | 5.2% | 7.50s | 8.56s | [7.40s, 8.43s] |
| dotnet test (unit): FakeSurveyGenerator.Application.Tests | MIX | 5.96s | 314ms | 5.2% | 5.65s | 6.48s | [5.60s, 6.38s] |
| dotnet test (integration): FakeSurveyGenerator.Api.Tests.Integration | MIX | 18.99s | 582ms | 3.1% | 18.26s | 19.89s | [18.34s, 19.78s] |
| dotnet test (acceptance): FakeSurveyGenerator.Acceptance.Tests | MIX | 72.49s | 3.02s | 4.1% | 71.22s | 78.17s | [70.11s, 77.59s] |
| npm ci (fake-survey-generator\src/client/ui) | NET | 5.67s | 366ms | 6.4% | 5.25s | 6.13s | [5.30s, 6.21s] |
| npm run build (fake-survey-generator\src/client/ui) | CPU | 2.30s | 171ms | 7.2% | 2.26s | 2.67s | [2.16s, 2.58s] |
| npx vitest run (fake-survey-generator\src/client/ui) | CPU | 5.90s | 347ms | 5.8% | 5.62s | 6.53s | [5.59s, 6.45s] |
| git add + commit (with changes) | I/O | 128ms | 33ms | 21.9% | 127ms | 194ms | [110ms, 193ms] |

### dotnet-starter-project-template

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| git clone | NET | 1.19s | 223ms | 18.9% | 934ms | 1.52s | [906ms, 1.46s] |
| dotnet restore dotnet-starter-project-template/DotNetStarterProjectTemplate.slnx | NET | 1.21s | 17ms | 1.4% | 1.19s | 1.23s | [1.19s, 1.23s] |
| dotnet build dotnet-starter-project-template/DotNetStarterProjectTemplate.slnx | CPU | 2.97s | 90ms | 3.0% | 2.86s | 3.08s | [2.88s, 3.10s] |
| dotnet clean + build dotnet-starter-project-template/DotNetStarterProjectTemplate.slnx | CPU | 3.65s | 50ms | 1.4% | 3.58s | 3.69s | [3.57s, 3.69s] |
| dotnet test (aspire): DotNetStarterProjectTemplate.AppHost.Tests | MIX | 44.00s | 1.22s | 2.8% | 42.44s | 45.17s | [42.47s, 45.49s] |
| git add + commit (with changes) | I/O | 103ms | 4ms | 4.1% | 97ms | 109ms | [98ms, 108ms] |

### home-page

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| git clone | NET | 1.22s | 219ms | 17.8% | 973ms | 1.54s | [956ms, 1.50s] |
| pnpm install (home-page) | NET | 1.29s | 215ms | 15.7% | 1.17s | 1.68s | [1.10s, 1.64s] |
| pnpm run build (home-page) | CPU | 800ms | 72ms | 8.8% | 724ms | 923ms | [725ms, 902ms] |
| playwright install (home-page) | NET | 877ms | 12ms | 1.3% | 874ms | 901ms | [869ms, 898ms] |
| playwright test (home-page) | MIX | 24.04s | 212ms | 0.9% | 23.98s | 24.44s | [23.90s, 24.43s] |
| git add + commit (with changes) | I/O | 103ms | 11ms | 10.6% | 97ms | 125ms | [94ms, 122ms] |

### Container Image Pulls

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| docker pull mcr.microsoft.com/dotnet/sdk:10.0 | NET | 14.52s | 1.69s | 10.9% | 14.22s | 18.02s | [13.40s, 17.60s] |
| docker pull mcr.microsoft.com/dotnet/aspnet:10.0 | NET | 1.15s | 40ms | 3.5% | 1.10s | 1.21s | [1.10s, 1.20s] |
| docker pull node:22-alpine | NET | 8.39s | 225ms | 2.6% | 8.30s | 8.84s | [8.22s, 8.78s] |
| docker pull postgres:17 ⚠️ | NET | 15.51s | 77.63s | 157.2% | 13.60s | 188.22s | [-47001ms, 145.74s] |
| docker pull redis:7-alpine | NET | 6.15s | 75ms | 1.2% | 6.08s | 6.28s | [6.07s, 6.26s] |

### Legend

| Type | Description |
|:----:|-------------|
| NET | Network-dependent (clone, pull, install) |
| CPU | CPU-bound (build, compile) |
| I/O | Disk I/O-bound (git commit) |
| MIX | Mixed workload (tests) |

## Notes

> Clone and Docker pull times are network-dependent. Git commit times are heavily impacted by DLP/endpoint protection.

