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
