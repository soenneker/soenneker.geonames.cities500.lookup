[![](https://img.shields.io/nuget/v/soenneker.geonames.cities500.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.geonames.cities500.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.geonames.cities500.lookup/build-and-test.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.geonames.cities500.lookup/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/nuget/dt/soenneker.geonames.cities500.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.geonames.cities500.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.geonames.cities500.lookup/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.geonames.cities500.lookup/actions/workflows/publish-package.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.geonames.cities500.lookup/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.geonames.cities500.lookup/actions/workflows/codeql.yml)

# Soenneker.GeoNames.Cities500.Lookup

A lookup util for US GeoNames cities500 data.

## Install

```bash
dotnet add package Soenneker.GeoNames.Cities500.Lookup
```

## Quick start

```csharp
using Soenneker.GeoNames.Cities500.Lookup.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddGeonamesCities500LookupAsSingleton();
```

Adds `IGeonamesCities500Lookup` as a singleton service.

## What you get

- `IGeonamesCities500Lookup` — A lookup util for US GeoNames cities500 data.
- `GeonamesCities500LookupRegistrar` — A lookup util for GeoNames cities500 data, provided by GeoNames and updated daily.
- `GeoNamesCoordinates` — Latitude and longitude coordinates for a GeoNames record.
- `GeoNamesRecord` — A US city coordinate record.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IGeonamesCities500Lookup.GetAll(cancellationToken)` | Gets all US city records. | The matching records as a materialized collection. |
| `IGeonamesCities500Lookup.GetByCity(city, cancellationToken)` | Gets all US city records matching the provided city. | The matching records as a materialized collection. |
| `IGeonamesCities500Lookup.GetByState(state, cancellationToken)` | Gets all US city records in the provided state. State may be a two-letter abbreviation or full state name. | The matching records as a materialized collection. |
| `IGeonamesCities500Lookup.GetByCityAndState(city, state, cancellationToken)` | Gets all US city records matching the provided city and state. State may be a two-letter abbreviation or full state name. | The matching records as a materialized collection. |
| `GeonamesCities500LookupRegistrar.AddGeonamesCities500LookupAsSingleton(services)` | Adds `IGeonamesCities500Lookup` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `GeonamesCities500LookupRegistrar.AddGeonamesCities500LookupAsScoped(services)` | Adds `IGeonamesCities500Lookup` as a scoped service. | The same service collection, so additional registrations can be chained. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
