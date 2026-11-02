import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MapPage } from './map-page';
import { LanguageService } from '../../core/language.service';
import { PlaceFeatureCollection } from '../../core/places.models';
import { translocoTesting } from '../../../testing/transloco-testing';

const twoPlaces: PlaceFeatureCollection = {
  type: 'FeatureCollection',
  features: [
    {
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [27.3417, 37.9395] },
      properties: {
        id: 8,
        slug: 'ephesus',
        name: 'Efes Antik Kenti',
        type: 'ArchaeologicalSite',
        provinceName: 'İzmir',
        isUnescoWorldHeritage: true,
      },
    },
    {
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [28.9834, 41.0115] },
      properties: {
        id: 5,
        slug: 'topkapi-palace',
        name: 'Topkapı Sarayı',
        type: 'Palace',
        provinceName: 'İstanbul',
        isUnescoWorldHeritage: true,
      },
    },
  ],
};

describe('MapPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.setItem('kultur-rotasi.language', 'tr');
    await TestBed.configureTestingModule({
      imports: [MapPage, translocoTesting()],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // whenStable() would wait for the pending HTTP requests, so tick() is used to run change detection
  // and effects; the test then answers the request itself through HttpTestingController.
  function render() {
    const fixture = TestBed.createComponent(MapPage);
    TestBed.tick();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  it('loads the places in the current language and shows how many there are', async () => {
    const { fixture, element } = render();

    const request = http.expectOne((r) => r.url === '/api/places/geojson');
    expect(request.request.headers.get('Accept-Language')).toBe('tr');
    request.flush(twoPlaces);
    await fixture.whenStable();

    expect(element.textContent).toContain('Haritada 2 yer');
  });

  it('sends the filters as query parameters', async () => {
    const { element } = render();
    http.expectOne('/api/places/geojson').flush(twoPlaces);

    const select = element.querySelector('select')!;
    select.value = 'Palace';
    select.dispatchEvent(new Event('change'));
    TestBed.tick();

    const request = http.expectOne((r) => r.url === '/api/places/geojson');
    expect(request.request.params.get('type')).toBe('Palace');
    request.flush({ ...twoPlaces, features: [twoPlaces.features[1]] });
  });

  it('reloads the places when the language changes', async () => {
    render();
    http.expectOne('/api/places/geojson').flush(twoPlaces);

    TestBed.inject(LanguageService).use('en');
    TestBed.tick();

    const request = http.expectOne((r) => r.url === '/api/places/geojson');
    expect(request.request.headers.get('Accept-Language')).toBe('en');
    request.flush(twoPlaces);
  });

  it('shows a hint when nearby search is switched on', async () => {
    const { fixture, element } = render();
    http.expectOne('/api/places/geojson').flush(twoPlaces);

    const nearbyButton = [...element.querySelectorAll('button')].find((b) =>
      b.textContent?.includes('Yakınımdakiler'),
    )!;
    nearbyButton.click();
    await fixture.whenStable();

    expect(element.textContent).toContain('Haritada bir noktaya tıkla');
  });

  it('downloads the province borders only when the thematic layer is switched on', async () => {
    const { element } = render();
    http.expectOne('/api/places/geojson').flush(twoPlaces);
    http.expectNone((r) => r.url === '/api/provinces/geojson');

    const checkbox = findCheckbox(element, 'İllere göre yer sayısı');
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));
    TestBed.tick();

    http
      .expectOne((r) => r.url === '/api/provinces/geojson')
      .flush({ type: 'FeatureCollection', features: [] });
  });

  it('builds export links with the filters and the language', async () => {
    const { fixture, element } = render();
    http.expectOne('/api/places/geojson').flush(twoPlaces);
    await fixture.whenStable();

    const kml = [...element.querySelectorAll('a')].find((a) => a.textContent?.trim() === 'KML')!;
    expect(kml.getAttribute('href')).toBe('/api/places/kml?culture=tr');
  });

  it('shows the reason when an uploaded file is rejected', async () => {
    const { fixture, element } = render();
    http.expectOne('/api/places/geojson').flush(twoPlaces);

    const input = element.querySelector<HTMLInputElement>('input[type="file"]')!;
    Object.defineProperty(input, 'files', { value: [new File(['x'], 'notes.txt')] });
    input.dispatchEvent(new Event('change'));

    const request = http.expectOne('/api/import/preview');
    expect(request.request.body).toBeInstanceOf(FormData);
    request.flush(
      {
        title: 'The request is not valid.',
        detail: 'Only .geojson, .json and .kml files are supported.',
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Only .geojson');
  });
});

function findCheckbox(element: HTMLElement, label: string): HTMLInputElement {
  const labelElement = [...element.querySelectorAll('label')].find((l) =>
    l.textContent?.includes(label),
  )!;
  return labelElement.querySelector('input')!;
}
