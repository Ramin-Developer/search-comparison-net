# SearchComparisonNet

[![CI](https://github.com/Ramin-Developer/SearchComparisonNet/actions/workflows/ci.yml/badge.svg)](https://github.com/Ramin-Developer/SearchComparisonNet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

A .NET 10 WPF application that compares the efficiency of **linear search** and **binary search**
over a generated, sorted integer dataset. It runs many randomized lookups with each strategy and
reports the average number of iterations and elapsed time side by side, with a WPF UI for driving
simulations and inspecting results.

## What it does

- Generates a sorted dataset of a configurable size (No. of Entries).
- Runs a configurable number of randomized searches (No. of Searches) with both the linear and
  binary strategies over the same data.
- Reports per-strategy averages (iteration count and elapsed time) so the two approaches can be
  compared directly.
- Supports a single on-demand lookup for a specific target value, showing its index (or `-1` with a
  "Not Found" hint when the value is absent).
- Shows a compact preview of the generated collection (first, middle, and last values).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (the GUI targets `net10.0-windows`)
- Visual Studio 2026 **or** any editor with C# support

## Solution layout

| Project | Description |
| --- | --- |
| `SearchComparisonNet.Kernel` | Core search algorithms (`LinearSearch`, `BinarySearch`), data generation, and shared models/interfaces. No UI dependencies. |
| `SearchComparisonNet.GUI` | WPF (MVVM) front end. Uses CommunityToolkit.Mvvm, FluentValidation, and Microsoft.Extensions.DependencyInjection. |
| `SearchComparisonNet.Tests` | Unit tests for the Kernel (algorithms, data generation, edge cases). |
| `SearchComparisonNet.ViewModelTests` | Unit tests for the GUI view models, converters, and validation. |
| `BenchmarkSuite1` | BenchmarkDotNet performance benchmarks for the search and data-generation paths. |

## Tech stack

- **.NET 10** (`net10.0` / `net10.0-windows`)
- **WPF** with the MVVM pattern
- **xUnit v3** for tests, **BenchmarkDotNet** for benchmarks
- Central Package Management (`Directory.Packages.props`); shared build settings in
  `Directory.Build.props` (`Nullable`, `ImplicitUsings`, warnings-as-errors)

## Build, test, and run

From the repository root:

```powershell
# Restore and build the whole solution
dotnet build SearchComparisonNet.slnx

# Run all tests
dotnet test SearchComparisonNet.slnx

# Run the WPF application
dotnet run --project SearchComparisonNet.GUI
```

> The solution can also be opened directly in Visual Studio 2026 via `SearchComparisonNet.slnx`.

## Contributing

This repository follows the shared tooling and editor baseline from
https://github.com/Ramin-Developer/developer-workflow. Those workflow-alignment changes are
implemented in the repo-level `.editorconfig`, `.gitignore`, and `.vscode/settings.json` files,
while the project-specific .NET solution and application code remain intact.

All changes—including documentation-only changes—go through pull requests into `main` rather than
being pushed directly. Each change ships on its own focused branch. See
[`TODO.md`](TODO.md) for the current backlog and
[`docs/review/solution-review.md`](docs/review/solution-review.md) for the standing code review.

## License

This project is licensed under the [MIT License](LICENSE.txt).
