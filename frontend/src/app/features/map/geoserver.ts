/**
 * GeoServer publishes the same database as OGC web services (see docs/geoserver.md).
 * WMS returns ready-made map images; GetFeatureInfo asks "what is under this pixel?".
 * The dev server proxies /geoserver to http://localhost:8080 (proxy.conf.json), so there is no CORS issue.
 */
export const geoServerWmsUrl = '/geoserver/kultur/wms';

/** Drawn bottom to top: province choropleth, then places. Styles come from the SLD files on the server. */
export const geoServerLayers = 'kultur:provinces,kultur:places';

/** Attributes of the two GeoServer layers, as returned in GeoJSON (view column names). */
export interface WmsFeature {
  id?: string;
  properties: Record<string, unknown>;
}

export interface FeatureInfoRequest {
  /** Visible map extent in Web Mercator metres (EPSG:3857, the projection Leaflet draws in). */
  bbox: { minX: number; minY: number; maxX: number; maxY: number };
  size: { width: number; height: number };
  /** The clicked pixel, from the top left corner of the map. */
  point: { x: number; y: number };
}

/** Builds a WMS 1.1.1 GetFeatureInfo request. The server renders the same view and reports what is under the pixel. */
export function featureInfoParams(request: FeatureInfoRequest): Record<string, string> {
  const { bbox, size, point } = request;
  return {
    service: 'WMS',
    version: '1.1.1',
    request: 'GetFeatureInfo',
    layers: geoServerLayers,
    query_layers: geoServerLayers,
    styles: '',
    srs: 'EPSG:3857',
    bbox: [bbox.minX, bbox.minY, bbox.maxX, bbox.maxY].join(','),
    width: String(Math.round(size.width)),
    height: String(Math.round(size.height)),
    x: String(Math.round(point.x)),
    y: String(Math.round(point.y)),
    info_format: 'application/json',
    // A few pixels of tolerance so small place symbols are easy to hit
    buffer: '6',
    feature_count: '5',
  };
}

/** One line per feature: a place shows its name in the chosen language, a province its place count. */
export function describeFeature(feature: WmsFeature, language: string): string {
  const p = feature.properties;
  if (typeof p['PlaceCount'] === 'number') {
    return `${p['Name']}: ${p['PlaceCount']}`;
  }
  const name = language === 'en' ? (p['NameEn'] ?? p['NameTr']) : (p['NameTr'] ?? p['NameEn']);
  return String(name ?? feature.id ?? '');
}
