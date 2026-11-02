import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import type { FeatureCollection } from 'geojson';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguageService } from '../../core/language.service';
import {
  GeoPoint,
  NearbyPlace,
  PlaceDetails,
  PlaceFeatureCollection,
  PlaceFilter,
  PlaceType,
  ProvinceFeatureCollection,
  densityClasses,
  densityLabel,
  placeTypeColors,
  placeTypes,
  toQueryParams,
} from '../../core/places.models';
import { MapLayers, PlaceMap } from './place-map';

/**
 * The main page: filters and details on the left, the map on the right.
 * State lives in signals; httpResource re-runs a request whenever a signal it reads changes
 * (filters, the selected place, the language), much like a computed value that comes from the API.
 * Every request sends Accept-Language, so the backend returns names and descriptions in the chosen language.
 */
@Component({
  selector: 'app-map-page',
  imports: [PlaceMap, TranslocoPipe],
  templateUrl: './map-page.html',
  styleUrl: './map-page.scss',
})
export class MapPage {
  private readonly language = inject(LanguageService);
  private readonly http = inject(HttpClient);

  protected readonly placeTypes = placeTypes;
  protected readonly placeTypeColors = placeTypeColors;
  protected readonly densityClasses = densityClasses;
  protected readonly densityLabel = densityLabel;

  protected readonly layerNames: (keyof MapLayers)[] = ['markers', 'heatmap', 'provinces'];
  protected readonly layers = signal<MapLayers>({
    markers: true,
    heatmap: false,
    provinces: false,
  });
  protected readonly imported = signal<{ name: string; features: FeatureCollection } | null>(null);
  protected readonly importError = signal<string | null>(null);

  protected readonly filter = signal<PlaceFilter>({ type: null, unescoOnly: false, search: '' });
  protected readonly selectedSlug = signal<string | null>(null);

  protected readonly nearbyMode = signal(false);
  protected readonly nearbyRadiusKm = signal(50);
  /** The point clicked in nearby mode. Cleared whenever the mode is switched. */
  protected readonly nearbyCenter = linkedSignal<boolean, GeoPoint | null>({
    source: this.nearbyMode,
    computation: () => null,
  });

  protected readonly places = httpResource<PlaceFeatureCollection>(() => ({
    url: '/api/places/geojson',
    params: toQueryParams(this.filter()),
    headers: { 'Accept-Language': this.language.current() },
  }));

  /** Province borders are only downloaded once the thematic layer is switched on. */
  protected readonly provinces = httpResource<ProvinceFeatureCollection>(() =>
    this.layers().provinces
      ? { url: '/api/provinces/geojson', params: { toleranceDegrees: 0.01 } }
      : undefined,
  );

  /** Export links use ?culture= because a plain link cannot send an Accept-Language header. */
  protected readonly downloadParams = computed(() =>
    new URLSearchParams({
      ...toQueryParams(this.filter()),
      culture: this.language.current(),
    }).toString(),
  );

  protected readonly details = httpResource<PlaceDetails>(() => {
    const slug = this.selectedSlug();
    // Returning undefined means "no request": the panel is closed
    return slug
      ? {
          url: `/api/places/${encodeURIComponent(slug)}`,
          headers: { 'Accept-Language': this.language.current() },
        }
      : undefined;
  });

  protected readonly nearby = httpResource<NearbyPlace[]>(() => {
    const center = this.nearbyCenter();
    return center
      ? {
          url: '/api/places/nearby',
          params: {
            latitude: center.latitude,
            longitude: center.longitude,
            radiusKm: this.nearbyRadiusKm(),
            limit: 50,
          },
          headers: { 'Accept-Language': this.language.current() },
        }
      : undefined;
  });

  protected readonly searchArea = computed(() => {
    const center = this.nearbyCenter();
    return center ? { center, radiusKm: this.nearbyRadiusKm() } : null;
  });

  protected readonly placeCount = computed(() => this.places.value()?.features.length ?? 0);

  protected setType(value: string): void {
    this.filter.update((f) => ({ ...f, type: value ? (value as PlaceType) : null }));
  }

  protected setUnescoOnly(value: boolean): void {
    this.filter.update((f) => ({ ...f, unescoOnly: value }));
  }

  protected setSearch(value: string): void {
    this.filter.update((f) => ({ ...f, search: value }));
  }

  protected onMapClick(point: GeoPoint): void {
    if (this.nearbyMode()) {
      this.nearbyCenter.set(point);
    }
  }

  protected setLayer(layer: keyof MapLayers, visible: boolean): void {
    this.layers.update((layers) => ({ ...layers, [layer]: visible }));
  }

  /** Sends the file to the API, which converts KML or GeoJSON into GeoJSON for the map. */
  protected upload(input: HTMLInputElement): void {
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    const form = new FormData();
    form.append('file', file);
    this.importError.set(null);
    this.http.post<FeatureCollection>('/api/import/preview', form).subscribe({
      next: (features) => this.imported.set({ name: file.name, features }),
      error: (error: HttpErrorResponse) => {
        this.imported.set(null);
        this.importError.set(error.error?.detail ?? error.error?.title ?? error.message);
      },
    });
  }

  protected toggleNearbyMode(): void {
    this.nearbyMode.update((on) => !on);
  }
}
