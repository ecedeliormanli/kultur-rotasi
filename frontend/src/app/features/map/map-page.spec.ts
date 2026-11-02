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
});
