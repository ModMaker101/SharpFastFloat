# Developing SharpFastFloat

## Build and test

Use the .NET 9 SDK and install the .NET 8 and .NET 9 runtimes:

```sh
dotnet restore SharpFastFloat.sln
dotnet build SharpFastFloat.sln -c Release
dotnet test SharpFastFloat.Tests/SharpFastFloat.Tests.csproj -c Release -f net8.0
dotnet test SharpFastFloat.Tests/SharpFastFloat.Tests.csproj -c Release -f net9.0
```

The library targets .NET 8, .NET 9, and .NET Standard 2.0. The additional
.NET Framework 4.7.2 test target exercises the Standard implementation and requires
Windows or Mono. Some approval tests are skipped when a diff tool is unavailable.
CI also runs scanner and arithmetic tests on x64 and ARM64 with intrinsics enabled
and disabled.

## Formatting

```sh
dotnet format SharpFastFloat.sln
dotnet format SharpFastFloat.sln --verify-no-changes
```

EditorConfig defines PascalCase classes/methods/constants, camelCase locals and
parameters, `_camelCase` private instance fields, and `s_camelCase` private static
fields. CI checks formatting.

## Benchmarks

The BenchmarkDotNet project includes strict and regular scanning, digit chunks,
and exact midpoint rounding:

```sh
dotnet run -c Release --project SharpFastFloat.Benchmarks -- --filter '*MidpointRoundingBenchmark*' --job short
```

See [measured results and detailed reproduction commands](docs/benchmarks.md).
Run comparisons sequentially on the same machine and distinguish token-level
microbenchmarks from application throughput.

## Publishing

`.github/workflows/publish.yml` uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
and `NuGet/login@v1`. No stored API key is needed. The policy matches owner
`ModMaker101`, repository `SharpFastFloat`, workflow filename `publish.yml`, and
NuGet user `ModMaker101`, without an environment restriction.

After merging the release changes, tag the intended commit and push that tag:

```sh
git tag v0.1.0-preview.2
git push origin v0.1.0-preview.2
```

Use a new version for each release. The tag controls the package version. The
workflow validates the tag, restores, checks formatting, builds, runs .NET 8/9
tests, and packs before requesting a short-lived publishing key. It uploads the
package and symbols to NuGet.org and retains the files as GitHub artifacts.
Branch pushes and pull requests do not publish packages.

The root `README.md` is the canonical package documentation and is packed directly
into the NuGet package. Keep its installation version aligned with the release.
