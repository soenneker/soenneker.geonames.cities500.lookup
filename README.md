[![](https://img.shields.io/nuget/v/soenneker.geonames.cities500.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.geonames.cities500.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.geonames.cities500.lookup/build-and-test.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.geonames.cities500.lookup/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/nuget/dt/soenneker.geonames.cities500.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.geonames.cities500.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.geonames.cities500.lookup/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.geonames.cities500.lookup/actions/workflows/publish-package.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.geonames.cities500.lookup/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.geonames.cities500.lookup/actions/workflows/codeql.yml)

# Soenneker.GeoNames.Cities500.Lookup

A normalized, in-memory lookup for the US city and coordinate extract packaged by `Soenneker.GeoNames.Cities500.Data`.

## Install

```bash
dotnet add package Soenneker.GeoNames.Cities500.Lookup
```

## Registration

```csharp
using Soenneker.GeoNames.Cities500.Lookup.Registrars;
using Microsoft.Extensions.DependencyInjection;

services.AddGeonamesCities500LookupAsSingleton();
```

Singleton registration is recommended: the data file is loaded once on first use and the immutable indexes are shared by every scope. `AddGeonamesCities500LookupAsScoped()` is also available when each scope should load and own a separate index.

## Look up a city

```csharp
IGeonamesCities500Lookup lookup = serviceProvider
    .GetRequiredService<IGeonamesCities500Lookup>();

GeoNamesCoordinates? coordinates =
    await lookup.GetCoordinatesByCityAndState("Ft. Lauderdale", "Florida");

if (coordinates is { } value)
    Console.WriteLine($"{value.Latitude}, {value.Longitude}");
```

City matching is case-insensitive, ignores accents and punctuation, and expands common tokens such as `Ft` to `Fort`, `St` to `Saint`, and `N` to `North`. State arguments accept either a postal abbreviation or full name. Blank, unknown, and unmatched inputs return an empty list or `null`; they are not exceptional.

## Available lookups

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `GetAll()` | Returns every packaged record. | Source-file order. |
| `GetByCity(city)` | Finds a normalized city name across all states. | May return multiple places. |
| `GetByState(state)` | Returns every record in a state. | Accepts code or full name. |
| `GetByCityAndState(city, state)` | Finds every matching place in one state. | Preserves source-file order. |
| `GetBestByCityAndState(city, state)` | Returns one matching record. | This is the first source-file match, not a population ranking. |
| `GetCoordinatesByCityAndState(city, state)` | Returns the coordinates of the first match. | `null` when no match exists. |

The lookup package references the data package, whose `Resources/cities500.txt` file is copied to the application output. The index is initialized lazily; cancellation can interrupt that initial file read. Later calls use the completed in-memory index.

The source data is derived from [GeoNames](https://www.geonames.org/) and is subject to the attribution terms included by the data package.
