import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguageService } from '../../core/language.service';
import {
  GeoPoint,
  NearbyPlace,
  PlaceDetails,
  PlaceFeatureCollection,
  PlaceFilter,
  PlaceType,
  placeTypeColors,
  placeTypes,
  toQueryParams,
} from '../../core/places.models';
import { PlaceMap } from './place-map';

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

  protected readonly placeTypes = placeTypes;
  protected readonly placeTypeColors = placeTypeColors;

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

  protected toggleNearbyMode(): void {
    this.nearbyMode.update((on) => !on);
  }
}
