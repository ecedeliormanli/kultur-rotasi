import type { MultiPolygon, Polygon } from 'geojson';

/** Shapes returned by the backend (see KulturRotasi.Application DTOs). */

export const placeTypes = ['Museum', 'ArchaeologicalSite', 'Monument', 'Palace'] as const;
export type PlaceType = (typeof placeTypes)[number];

export interface PlaceDetails {
  id: number;
  slug: string;
  language: string;
  name: string;
  description: string;
  type: PlaceType;
  latitude: number;
  longitude: number;
  provinceId: number;
  provinceName: string;
  isUnescoWorldHeritage: boolean;
}

export interface NearbyPlace {
  id: number;
  slug: string;
  name: string;
  type: PlaceType;
  latitude: number;
  longitude: number;
  provinceName: string;
  isUnescoWorldHeritage: boolean;
  distanceKm: number;
}

/** Properties of each feature in GET /api/places/geojson. */
export interface PlaceFeatureProperties {
  id: number;
  slug: string;
  name: string;
  type: PlaceType;
  provinceName: string;
  isUnescoWorldHeritage: boolean;
}

export interface PlaceFeatureCollection {
  type: 'FeatureCollection';
  features: {
    type: 'Feature';
    geometry: { type: 'Point'; coordinates: [longitude: number, latitude: number] };
    properties: PlaceFeatureProperties;
  }[];
}

export interface PlaceFilter {
  type: PlaceType | null;
  unescoOnly: boolean;
  search: string;
}

export interface GeoPoint {
  latitude: number;
  longitude: number;
}

/** Turns the filter form into query parameters, leaving out the empty ones. */
export function toQueryParams(filter: PlaceFilter): Record<string, string> {
  const params: Record<string, string> = {};
  if (filter.type) {
    params['type'] = filter.type;
  }
  if (filter.unescoOnly) {
    params['unesco'] = 'true';
  }
  const search = filter.search.trim();
  if (search) {
    params['search'] = search;
  }
  return params;
}

/** Marker colours per place type, also used in the legend. */
export const placeTypeColors: Record<PlaceType, string> = {
  Museum: '#2563eb',
  ArchaeologicalSite: '#b45309',
  Monument: '#7c3aed',
  Palace: '#dc2626',
};

/** Properties of each feature in GET /api/provinces/geojson. */
export interface ProvinceFeatureProperties {
  plateCode: number;
  name: string;
  placeCount: number;
}

export interface ProvinceFeatureCollection {
  type: 'FeatureCollection';
  features: {
    type: 'Feature';
    geometry: Polygon | MultiPolygon;
    properties: ProvinceFeatureProperties;
  }[];
}

/**
 * Classes for the thematic (choropleth) map: provinces are coloured by how many places lie inside them.
 * Few, clearly separated classes read better on a map than a continuous scale.
 */
export const densityClasses = [
  { min: 0, max: 0, color: '#f1f5f9' },
  { min: 1, max: 1, color: '#fde68a' },
  { min: 2, max: 3, color: '#fbbf24' },
  { min: 4, max: 6, color: '#d97706' },
  { min: 7, max: Infinity, color: '#92400e' },
] as const;

export function densityColor(placeCount: number): string {
  return (
    densityClasses.find((c) => placeCount >= c.min && placeCount <= c.max) ?? densityClasses[0]
  ).color;
}

export function densityLabel(densityClass: (typeof densityClasses)[number]): string {
  if (densityClass.max === Infinity) {
    return `${densityClass.min}+`;
  }
  return densityClass.min === densityClass.max
    ? `${densityClass.min}`
    : `${densityClass.min}–${densityClass.max}`;
}
