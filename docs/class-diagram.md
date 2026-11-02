# UML class diagram

Main classes of the backend after M2. Rendered by GitHub from the Mermaid code below.

## Domain

```mermaid
classDiagram
    direction LR

    class Place {
        +int Id
        +string Slug
        +PlaceType Type
        +Point Location
        +int ProvinceId
        +bool IsUnescoWorldHeritage
        +double Latitude
        +double Longitude
        +Create(slug, type, latitude, longitude, provinceId, isUnesco)$ Place
        +SetTranslation(languageCode, name, description) void
        +GetTranslation(languageCode) PlaceTranslation
    }

    class PlaceTranslation {
        +int PlaceId
        +string LanguageCode
        +string Name
        +string Description
    }

    class Province {
        +int Id
        +string Name
        +Geometry Boundary
        +IsValidPlateCode(plateCode)$ bool
    }

    class PlaceType {
        <<enumeration>>
        Museum
        ArchaeologicalSite
        Monument
        Palace
    }

    class Geo {
        <<static>>
        +int Wgs84
        +CreatePoint(latitude, longitude)$ Point
        +PrepareForGeography(geometry)$ Geometry
    }

    Place "1" *-- "1..*" PlaceTranslation : translations
    Place "*" --> "1" Province : ProvinceId
    Place --> PlaceType
    Place ..> Geo : uses
```

- `Place` owns its translations (composition): they are created, changed and deleted only through `Place`.
- `Place` refers to `Province` by id. Spatial queries also check the real border (`Boundary.Contains(Location)`).

## Application: hand-written CQRS

```mermaid
classDiagram
    direction TB

    class IQueryHandler~TQuery, TResult~ {
        <<interface>>
        +HandleAsync(query, cancellationToken) Task~TResult~
    }

    class ValidationDecorator~TQuery, TResult~ {
        -IQueryHandler inner
        -IEnumerable~IValidator~ validators
        +HandleAsync(query, cancellationToken) Task~TResult~
    }

    class GetNearbyPlacesHandler {
        +HandleAsync(GetNearbyPlacesQuery, cancellationToken) Task~IReadOnlyList~NearbyPlaceDto~~
    }

    class GetNearbyPlacesQuery {
        <<record>>
        +double Latitude
        +double Longitude
        +double? RadiusKm
        +int Limit
    }

    class GetNearbyPlacesQueryValidator
    class AbstractValidator~T~ {
        <<FluentValidation>>
    }

    class IAppDbContext {
        <<interface>>
        +DbSet~Place~ Places
        +DbSet~Province~ Provinces
    }

    class AppDbContext {
        <<Infrastructure>>
    }

    class PlacesController {
        <<Api>>
        +Nearby(latitude, longitude, radiusKm, type, limit)
    }

    IQueryHandler <|.. ValidationDecorator
    IQueryHandler <|.. GetNearbyPlacesHandler
    ValidationDecorator o-- IQueryHandler : wraps
    ValidationDecorator ..> GetNearbyPlacesQueryValidator : runs
    AbstractValidator <|-- GetNearbyPlacesQueryValidator
    GetNearbyPlacesHandler ..> GetNearbyPlacesQuery : handles
    GetNearbyPlacesHandler --> IAppDbContext
    IAppDbContext <|.. AppDbContext
    PlacesController --> IQueryHandler : depends on
```

One example use case is shown; every other query follows the same shape:

| Query | Result | What it does |
| --- | --- | --- |
| `SearchPlacesQuery` | `PagedResult<PlaceSummaryDto>` | Filter, search and page |
| `ListPlacesQuery` | `IReadOnlyList<PlaceSummaryDto>` | All matching places, for GeoJSON |
| `GetPlaceBySlugQuery` | `PlaceDetailsDto?` | One place with its description |
| `GetNearbyPlacesQuery` | `IReadOnlyList<NearbyPlaceDto>` | Nearest places, optional radius |
| `GetPlacesWithinGeometryQuery` | `IReadOnlyList<PlaceSummaryDto>` | Inside a shape or within a buffer of it |
| `GetProvincesQuery` | `IReadOnlyList<ProvinceDto>` | Provinces with place counts |
| `GetProvinceBoundariesQuery` | `IReadOnlyList<ProvinceBoundaryDto>` | Simplified borders, for GeoJSON |
| `GetPlacesInProvinceQuery` | `IReadOnlyList<PlaceSummaryDto>?` | Point-in-polygon for one province |

**Why no MediatR?** The pattern needs only two small types here (`IQueryHandler` and `ValidationDecorator`),
registered by scanning the assembly. Controllers ask the DI container for `IQueryHandler<TQuery, TResult>`
and receive the handler already wrapped in validation, the same job a MediatR pipeline behaviour does.
