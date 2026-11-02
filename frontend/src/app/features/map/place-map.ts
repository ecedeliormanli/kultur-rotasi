import {
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';
import { GeoPoint, PlaceFeatureCollection, placeTypeColors } from '../../core/places.models';

/** Centre of Türkiye and a zoom level that shows the whole country. */
const turkeyCenter: L.LatLngTuple = [39.0, 35.2];
const turkeyZoom = 6;

/**
 * A thin Angular wrapper around a Leaflet map. Leaflet works directly with the DOM, so the map is created
 * after the first render, and effects redraw the layers whenever the inputs change.
 */
@Component({
  selector: 'app-place-map',
  template: `<div #map class="map"></div>`,
  styles: `
    :host,
    .map {
      display: block;
      height: 100%;
      width: 100%;
    }
  `,
})
export class PlaceMap {
  readonly places = input<PlaceFeatureCollection | undefined>();
  readonly selectedSlug = input<string | null>(null);
  /** When set, a circle shows the area searched around a point. */
  readonly searchArea = input<{ center: GeoPoint; radiusKm: number } | null>(null);

  readonly placeClick = output<string>();
  readonly mapClick = output<GeoPoint>();

  private readonly container = viewChild.required<ElementRef<HTMLElement>>('map');
  private map?: L.Map;
  private readonly markers = L.layerGroup();
  private readonly searchLayer = L.layerGroup();

  constructor() {
    afterNextRender(() => this.createMap());
    inject(DestroyRef).onDestroy(() => this.map?.remove());

    effect(() => this.drawPlaces(this.places(), this.selectedSlug()));
    effect(() => this.drawSearchArea(this.searchArea()));
  }

  private createMap(): void {
    this.map = L.map(this.container().nativeElement, { zoomControl: true }).setView(
      turkeyCenter,
      turkeyZoom,
    );

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    this.searchLayer.addTo(this.map);
    this.markers.addTo(this.map);
    this.map.on('click', (event: L.LeafletMouseEvent) =>
      this.mapClick.emit({ latitude: event.latlng.lat, longitude: event.latlng.lng }),
    );

    // Effects may have run before the map existed
    this.drawPlaces(this.places(), this.selectedSlug());
    this.drawSearchArea(this.searchArea());
  }

  private drawPlaces(
    places: PlaceFeatureCollection | undefined,
    selectedSlug: string | null,
  ): void {
    if (!this.map) {
      return;
    }

    this.markers.clearLayers();
    for (const feature of places?.features ?? []) {
      const [longitude, latitude] = feature.geometry.coordinates;
      const { slug, name, type } = feature.properties;
      const selected = slug === selectedSlug;

      // Circle markers are drawn as SVG, so no marker image files are needed
      L.circleMarker([latitude, longitude], {
        radius: selected ? 11 : 7,
        color: '#ffffff',
        weight: 2,
        fillColor: placeTypeColors[type],
        fillOpacity: 0.95,
      })
        .bindTooltip(name)
        .on('click', (event) => {
          L.DomEvent.stopPropagation(event);
          this.placeClick.emit(slug);
        })
        .addTo(this.markers);
    }
  }

  private drawSearchArea(area: { center: GeoPoint; radiusKm: number } | null): void {
    if (!this.map) {
      return;
    }

    this.searchLayer.clearLayers();
    if (!area) {
      return;
    }

    const center: L.LatLngTuple = [area.center.latitude, area.center.longitude];
    const circle = L.circle(center, {
      radius: area.radiusKm * 1000,
      color: '#0f766e',
      weight: 2,
      fillOpacity: 0.08,
      interactive: false,
    }).addTo(this.searchLayer);
    L.circleMarker(center, {
      radius: 4,
      color: '#0f766e',
      fillOpacity: 1,
      interactive: false,
    }).addTo(this.searchLayer);
    this.map.fitBounds(circle.getBounds(), { padding: [24, 24] });
  }
}
