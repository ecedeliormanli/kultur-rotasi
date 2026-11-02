# GeoServer: WMS, WFS and WMTS

The API (`/api/...`) is this project's own REST interface. GeoServer publishes **the same SQL Server data**
through open OGC standards instead, so any GIS client (QGIS, ArcGIS, OpenLayers, Leaflet, a ministry portal)
can use it without knowing anything about our API.

```
SQL Server ──(views dbo.MapPlaces, dbo.MapProvinces)──► GeoServer ──► WMS  (map images)
   read-only login geoserver_reader                              ├──► WFS  (vector features: GeoJSON, GML, KML)
                                                                 └──► WMTS (cached tiles, GeoWebCache)
```

Start it with `docker compose --profile gis up -d`, then run `./geoserver/setup.sh` (see the README).
All URLs below assume `http://localhost:8080/geoserver`. The web UI is at `/geoserver/web/`.

## The standards in one table

| Standard | Answers | Returns | Main requests |
| --- | --- | --- | --- |
| **WMS** (Web Map Service) | "Draw me a map of this area" | An image (PNG, JPEG) | `GetCapabilities`, `GetMap`, `GetFeatureInfo`, `GetLegendGraphic` |
| **WFS** (Web Feature Service) | "Give me the features themselves" | Vector data with attributes (GML, GeoJSON, KML, CSV…) | `GetCapabilities`, `DescribeFeatureType`, `GetFeature` (WFS-T adds `Transaction` for edits) |
| **WMTS** (Web Map Tile Service) | "Give me tile z/x/y" | Pre-rendered, cacheable tiles | `GetCapabilities`, `GetTile` |
| **SLD** (Styled Layer Descriptor) | "How should features look?" | XML style rules used by WMS | – |
| **CQL / ECQL** | "Only features where…" | A filter language (GeoServer extension, simpler than OGC Filter XML) | `CQL_FILTER=` parameter |

When to use which: WMS when the client only needs to *see* the map (light for the browser, works with
millions of features, styling stays on the server). WFS when the client needs the *data* (attributes,
geometry, analysis, download). WMTS when the same map is viewed by many users and speed matters.

## What setup.sh configures

| GeoServer concept | Here | Comparable to |
| --- | --- | --- |
| Workspace | `kultur` | A namespace / package; layer names become `kultur:places` |
| Data store | `kulturrotasi-db` (SQL Server, host `db`, schema `dbo`) | A JDBC connection pool |
| Feature type / layer | `places` ← `dbo.MapPlaces`, `provinces` ← `dbo.MapProvinces` | A table published as a service |
| Style | `places.sld` (colour by `Type`), `provinces.sld` (choropleth by `PlaceCount`) | CSS for maps |

Design decisions:

- **Views, not tables.** The views flatten the data for maps (Turkish and English names as columns, place
  count per province) and keep GeoServer independent of the EF Core table design.
- **`geography` → `geometry`.** The tables use `geography` (round-earth distances in metres, used by the API).
  GeoServer's SQL Server support works best with planar `geometry`, so the views convert with
  `geometry::STGeomFromWKB(Location.STAsBinary(), 4326)`.
- **Least privilege.** GeoServer connects as `geoserver_reader` (`db_datareader` only), never as `sa`.
- **Stable feature ids.** Views have no primary key, so `geoserver.gt_pk_metadata` tells GeoServer to use
  `Id` / `PlateCode`. Feature ids are `places.5`, not random values.
- **Configuration as code.** Everything goes through the REST API (`/geoserver/rest`), so a fresh
  environment is configured by one script, and the script is safe to run again.

## WMS examples

Capabilities (what layers, styles, projections and formats the server offers):

```
/geoserver/kultur/wms?service=WMS&request=GetCapabilities
```

A map of Türkiye with the choropleth below and places on top (open it in the browser, it is a PNG):

```
/geoserver/kultur/wms?service=WMS&version=1.1.1&request=GetMap
  &layers=kultur:provinces,kultur:places&styles=
  &bbox=25.5,35.8,45,42.2&width=1000&height=330
  &srs=EPSG:4326&format=image/png&transparent=true
```

Only UNESCO sites, filtered on the server:

```
...&request=GetMap&layers=kultur:places&CQL_FILTER=IsUnescoWorldHeritage=true&...
```

What is under a pixel (`x`, `y` count from the top left of the image), as GeoJSON. The Angular app sends
this when you click the map with the **GeoServer WMS** layer switched on:

```
/geoserver/kultur/wms?service=WMS&version=1.1.1&request=GetFeatureInfo
  &layers=kultur:provinces&query_layers=kultur:provinces
  &bbox=25.5,35.8,45,42.2&width=1000&height=330&srs=EPSG:4326
  &x=266&y=273&info_format=application/json
```

Legend image built from the SLD rules:

```
/geoserver/kultur/wms?service=WMS&version=1.1.1&request=GetLegendGraphic&layer=kultur:provinces&format=image/png
```

## WFS examples

The attribute schema of a layer:

```
/geoserver/kultur/wfs?service=WFS&version=2.0.0&request=DescribeFeatureType&typeNames=kultur:places
```

Features as GeoJSON (`count` limits the result, like `TOP` / `LIMIT`):

```
/geoserver/kultur/wfs?service=WFS&version=2.0.0&request=GetFeature
  &typeNames=kultur:places&outputFormat=application/json&count=5
```

Attribute filter with CQL (URL-encode spaces and quotes, for example with `curl -G --data-urlencode`):

```
...&request=GetFeature&typeNames=kultur:places&outputFormat=application/json
  &CQL_FILTER=IsUnescoWorldHeritage = true AND Type = 'ArchaeologicalSite'
```

Provinces with at least 4 places, without geometry (smaller response):

```
...&typeNames=kultur:provinces&outputFormat=application/json
  &propertyName=Name,PlaceCount&CQL_FILTER=PlaceCount >= 4
```

Spatial filters. The `SRID=4326;` prefix matters: SQL Server returns no rows when two geometries have
different SRIDs, and a plain WKT literal has none.

```
CQL_FILTER=BBOX(Geom, 29, 36, 32.7, 37.5, 'EPSG:4326')                         -- Antalya region (lon/lat)
CQL_FILTER=INTERSECTS(Geom, SRID=4326;POLYGON((29 36, 32.7 36, 32.7 37.5, 29 37.5, 29 36)))
CQL_FILTER=DWITHIN(Geom, SRID=4326;POINT(32.85 39.92), 0.03, meters)          -- see the note below
```

> **Note on distances.** Because the views expose planar `geometry` in EPSG:4326, `DWITHIN` measures in
> **degrees**, whatever unit is written (0.03° is about 3 km in Ankara, and less east–west further north).
> For correct distances in kilometres use the API (`/api/places/nearby`), which uses `geography`.
> This is the classic geometry vs. geography trade-off.

The standard `bbox` parameter works too. With the short code `EPSG:4326` the order is lon/lat; with
`urn:ogc:def:crs:EPSG::4326` WFS 2.0 expects lat/lon (axis order is a common source of empty results):

```
...&request=GetFeature&typeNames=kultur:places&outputFormat=application/json&bbox=29,36,32.7,37.5,EPSG:4326
```

Other output formats: `outputFormat=KML` (Google Earth), `outputFormat=csv`, `outputFormat=shape-zip` (Shapefile).

## WMTS (tiles)

GeoWebCache, built into GeoServer, offers both layers as cached tiles:

```
/geoserver/gwc/service/wmts?service=WMTS&request=GetCapabilities
```

## Styling with SLD

An SLD file is a list of **rules**. Each rule has an optional **filter** (which features), optional
**scale limits** (at which zoom levels) and **symbolizers** (how to draw: `PointSymbolizer`,
`LineSymbolizer`, `PolygonSymbolizer`, `TextSymbolizer`).

- `geoserver/styles/places.sld`: one rule per place type (`PropertyIsEqualTo` on `Type`), plus a label
  rule that only applies below 1:2,000,000 (`MaxScaleDenominator`) so names do not overlap at country level.
- `geoserver/styles/provinces.sld`: a choropleth with five classes on `PlaceCount`
  (`PropertyIsBetween`): 0, 1, 2–3, 4–6, 7+. The colours match the Angular thematic layer, so the
  browser-drawn and server-drawn maps look the same.

After editing an SLD file, run `./geoserver/setup.sh` again to upload it.
