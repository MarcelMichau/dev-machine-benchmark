# Benchmark Results — MARCEL-PC

_2026-03-16 05:17:35 UTC · 5 iterations (+1 warm-up discarded)_

## Machine

| | |
|---|---|
| **Hostname** | MARCEL-PC |
| **OS** | Microsoft Windows 10.0.26200 |
| **CPU** | AMD Ryzen 7 9800X3D 8-Core Processor (16 cores) |
| **RAM** | 31.2 GB |
| **Disk** | SSD |
| **.NET SDK** | 10.0.201 |
| **Node** | v25.8.1 |
| **Docker** | Docker version 29.2.1, build a5c7197 |
| **CPU Load at Start** | 7.0% |
| **Memory Used at Start** | 44.2% |

## Results

### fake-survey-generator

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| git clone | NET | 5.28s | 1.12s | 24.5% | 3.30s | 5.47s | [3.16s, 5.93s] |
| dotnet restore fake-survey-generator/FakeSurveyGenerator.slnx | NET | 847ms | 135ms | 14.9% | 829ms | 1.14s | [737ms, 1.07s] |
| dotnet build fake-survey-generator/FakeSurveyGenerator.slnx | CPU | 2.91s | 498ms | 16.4% | 2.67s | 3.88s | [2.43s, 3.66s] |
| dotnet clean + build fake-survey-generator/FakeSurveyGenerator.slnx | CPU | 3.45s | 262ms | 7.4% | 3.41s | 4.03s | [3.23s, 3.88s] |
| dotnet test (unit): FakeSurveyGenerator.Application.Tests | MIX | 2.60s | 30ms | 1.1% | 2.59s | 2.67s | [2.58s, 2.65s] |
| dotnet test (integration): FakeSurveyGenerator.Api.Tests.Integration | MIX | 12.50s | 107ms | 0.9% | 12.35s | 12.62s | [12.34s, 12.60s] |
| dotnet test (acceptance): FakeSurveyGenerator.Acceptance.Tests | MIX | 42.66s | 1.73s | 4.0% | 41.01s | 45.51s | [41.01s, 45.29s] |
| npm ci (fake-survey-generator\src/client/ui) | NET | 6.02s | 415ms | 6.9% | 5.69s | 6.73s | [5.54s, 6.57s] |
| npm run build (fake-survey-generator\src/client/ui) | CPU | 1.73s | 16ms | 0.9% | 1.71s | 1.76s | [1.71s, 1.75s] |
| npx vitest run (fake-survey-generator\src/client/ui) | CPU | 2.68s | 43ms | 1.6% | 2.66s | 2.76s | [2.64s, 2.75s] |
| git add + commit (with changes) | I/O | 101ms | 9ms | 9.7% | 80ms | 103ms | [85ms, 109ms] |

### dotnet-starter-project-template

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| git clone | NET | 1.06s | 87ms | 8.5% | 879ms | 1.10s | [918ms, 1.13s] |
| dotnet restore dotnet-starter-project-template/DotNetStarterProjectTemplate.slnx | NET | 814ms | 11ms | 1.3% | 797ms | 827ms | [798ms, 825ms] |
| dotnet build dotnet-starter-project-template/DotNetStarterProjectTemplate.slnx | CPU | 1.84s | 178ms | 9.3% | 1.80s | 2.23s | [1.69s, 2.13s] |
| dotnet clean + build dotnet-starter-project-template/DotNetStarterProjectTemplate.slnx | CPU | 2.29s | 90ms | 3.9% | 2.25s | 2.48s | [2.22s, 2.45s] |
| dotnet test (aspire): DotNetStarterProjectTemplate.AppHost.Tests | MIX | 36.46s | 197ms | 0.5% | 36.36s | 36.82s | [36.30s, 36.79s] |
| git add + commit (with changes) | I/O | 78ms | 4ms | 4.4% | 78ms | 86ms | [75ms, 84ms] |

### home-page

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| git clone | NET | 1.09s | 171ms | 15.2% | 973ms | 1.39s | [911ms, 1.34s] |
| pnpm install (home-page) | NET | 1.04s | 88ms | 8.8% | 853ms | 1.06s | [888ms, 1.11s] |
| pnpm run build (home-page) | CPU | 507ms | 3ms | 0.6% | 507ms | 514ms | [505ms, 513ms] |
| playwright install (home-page) | NET | 624ms | 13ms | 2.0% | 616ms | 647ms | [614ms, 645ms] |
| playwright test (home-page) | MIX | 17.61s | 20ms | 0.1% | 17.58s | 17.63s | [17.58s, 17.63s] |
| git add + commit (with changes) | I/O | 80ms | 2ms | 2.5% | 78ms | 83ms | [77ms, 82ms] |

### Container Image Pulls

| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |
|------|:----:|-------:|-------:|----:|----:|----:|-------:|
| docker pull mcr.microsoft.com/dotnet/sdk:10.0 | NET | 396ms | 16ms | 4.1% | 378ms | 419ms | [375ms, 415ms] |
| docker pull mcr.microsoft.com/dotnet/aspnet:10.0 | NET | 1.03s | 41ms | 3.9% | 1.00s | 1.09s | [991ms, 1.09s] |
| docker pull node:22-alpine | NET | 9.37s | 365ms | 3.9% | 8.79s | 9.80s | [8.90s, 9.81s] |
| docker pull postgres:17 | NET | 14.66s | 132ms | 0.9% | 14.54s | 14.87s | [14.50s, 14.83s] |
| docker pull redis:7-alpine | NET | 5.98s | 144ms | 2.4% | 5.77s | 6.15s | [5.78s, 6.14s] |

### Legend

| Type | Description |
|:----:|-------------|
| NET | Network-dependent (clone, pull, install) |
| CPU | CPU-bound (build, compile) |
| I/O | Disk I/O-bound (git commit) |
| MIX | Mixed workload (tests) |

## Notes

> Clone and Docker pull times are network-dependent. Git commit times are heavily impacted by DLP/endpoint protection.

