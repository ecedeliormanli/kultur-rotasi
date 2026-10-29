# Entity–relationship diagram

Database schema after milestone M1. Generated from the EF Core model in
`backend/src/KulturRotasi.Infrastructure/Persistence`.

```mermaid
erDiagram
    Provinces ||--o{ Places : "contains"
    Places ||--|{ PlaceTranslations : "has text in"

    Provinces {
        int Id PK "licence plate code, 1-81"
        nvarchar(50) Name
        geography Boundary "polygon / multipolygon, SRID 4326"
    }

    Places {
        int Id PK "identity"
        nvarchar(100) Slug UK "e.g. ephesus"
        nvarchar(30) Type "Museum | ArchaeologicalSite | Monument | Palace"
        geography Location "point, SRID 4326"
        int ProvinceId FK
        bit IsUnescoWorldHeritage
    }

    PlaceTranslations {
        int PlaceId PK, FK
        varchar(2) LanguageCode PK "tr | en"
        nvarchar(200) Name
        nvarchar(2000) Description
    }
```

## Design notes

- **`geography` instead of `geometry`.** `geography` works on the curved earth, so `STDistance`
  returns metres and buffers are real circles. `geometry` treats coordinates as a flat plane,
  which is fine for small areas but distorts distances across a country.
- **Spatial indexes** on `Places.Location` and `Provinces.Boundary` are created in the
  migration with plain SQL, because EF Core cannot model them.
- **Translations in their own table.** Adding a language means adding rows, not columns.
  The primary key `(PlaceId, LanguageCode)` guarantees one text per language.
- **Province id is the plate code.** It is a natural, stable key that everyone in Türkiye knows,
  so the database does not generate it.
