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
import type { FeatureCollection } from 'geojson';
import L from './leaflet';
import {
  GeoPoint,
  PlaceFeatureCollection,
  ProvinceFeatureCollection,
  densityColor,
  placeTypeColors,
} from '../../core/places.models';

/** Centre of Türkiye and a zoom level that shows the whole country. */
const turkeyCenter: L.LatLngTuple = [39.0, 35.2];
const turkeyZoom = 6;

export interface MapLayers {
  markers: boolean;
  heatmap: boolean;
  provinces: boolean;
}

/**
 * A thin Angular wrapper around a Leaflet map. Leaflet works directly with the DOM, so the map is created
 * after the first render, and effects redraw a layer whenever its inputs change.
 * Layers from bottom to top: tiles, province choropleth, heatmap, imported file, search circle, markers.
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
  readonly provinces = input<ProvinceFeatureCollection | undefined>();
  readonly imported = input<FeatureCollection | null>(null);
  readonly layers = input<MapLayers>({ markers: true, heatmap: false, provinces: false });
  readonly selectedSlug = input<string | null>(null);
  /** When set, a circle shows the area searched around a point. */
  readonly searchArea = input<{ center: GeoPoint; radiusKm: number } | null>(null);

  readonly placeClick = output<string>();
  readonly mapClick = output<GeoPoint>();

  private readonly container = viewChild.required<ElementRef<HTMLElement>>('map');
  private map?: L.Map;
  private readonly provinceLayer = L.layerGroup();
  private readonly heatLayer = L.layerGroup();
  private readonly importedLayer = L.layerGroup();
  private readonly searchLayer = L.layerGroup();
  private readonly markerLayer = L.layerGroup();

  constructor() {
    afterNextRender(() => this.createMap());
    inject(DestroyRef).onDestroy(() => this.map?.remove());

    effect(() => this.drawPlaces());
    effect(() => this.drawHeatmap());
    effect(() => this.drawProvinces());
    effect(() => this.drawImported());
    effect(() => this.drawSearchArea());
  }

  private createMap(): void {
    this.map = L.map(this.container().nativeElement).setView(turkeyCenter, turkeyZoom);
    // Province polygons get their own pane below the default overlay pane (z-index 400),
    // so markers and other shapes always stay clickable on top of them
    this.map.createPane('provinces').style.zIndex = '350';

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    for (const layer of [
      this.provinceLayer,
      this.heatLayer,
      this.importedLayer,
      this.searchLayer,
      this.markerLayer,
    ]) {
      layer.addTo(this.map);
    }
    this.map.on('click', (event: L.LeafletMouseEvent) =>
      this.mapClick.emit({ latitude: event.latlng.lat, longitude: event.latlng.lng }),
    );

    // Effects may have run before the map existed
    this.drawPlaces();
    this.drawHeatmap();
    this.drawProvinces();
    this.drawImported();
    this.drawSearchArea();
  }

  private drawPlaces(): void {
    const places = this.places();
    const selectedSlug = this.selectedSlug();
    const visible = this.layers().markers;
    if (!this.map) {
      return;
    }

    this.markerLayer.clearLayers();
    if (!visible) {
      return;
    }

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
        .addTo(this.markerLayer);
    }
  }

  /** Density of places: each place adds heat around it, overlapping places glow brighter. */
  private drawHeatmap(): void {
    const places = this.places();
    const visible = this.layers().heatmap;
    if (!this.map) {
      return;
    }

    this.heatLayer.clearLayers();
    if (!visible || !places) {
      return;
    }

    const points = places.features.map((f): L.HeatLatLngTuple => [
      f.geometry.coordinates[1],
      f.geometry.coordinates[0],
      1,
    ]);
    L.heatLayer(points, { radius: 30, blur: 25, maxZoom: 8, minOpacity: 0.35 }).addTo(
      this.heatLayer,
    );
  }

  /** Thematic (choropleth) map: each province is filled according to its number of places. */
  private drawProvinces(): void {
    const provinces = this.provinces();
    const visible = this.layers().provinces;
    if (!this.map) {
      return;
    }

    this.provinceLayer.clearLayers();
    if (!visible || !provinces) {
      return;
    }

    L.geoJSON(provinces as unknown as FeatureCollection, {
      pane: 'provinces',
      style: (feature) => ({
        color: '#78716c',
        weight: 0.8,
        fillColor: densityColor(feature?.properties.placeCount ?? 0),
        fillOpacity: 0.7,
      }),
      onEachFeature: (feature, layer) =>
        layer.bindTooltip(`${feature.properties.name}: ${feature.properties.placeCount}`, {
          sticky: true,
        }),
    }).addTo(this.provinceLayer);
  }

  /** A GeoJSON or KML file the user uploaded, drawn on top so it is easy to compare with the places. */
  private drawImported(): void {
    const imported = this.imported();
    if (!this.map) {
      return;
    }

    this.importedLayer.clearLayers();
    if (!imported) {
      return;
    }

    const layer = L.geoJSON(imported, {
      style: { color: '#7c3aed', weight: 3, dashArray: '6 4', fillOpacity: 0.1 },
      pointToLayer: (_, latlng) =>
        L.circleMarker(latlng, { radius: 6, color: '#7c3aed', fillOpacity: 0.6 }),
      onEachFeature: (feature, featureLayer) => {
        const name = feature.properties?.['name'];
        if (typeof name === 'string' && name) {
          featureLayer.bindTooltip(name);
        }
      },
    }).addTo(this.importedLayer);
    this.map.fitBounds(layer.getBounds(), { padding: [24, 24], maxZoom: 12 });
  }

  private drawSearchArea(): void {
    const area = this.searchArea();
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
