import { describeFeature, featureInfoParams, geoServerLayers } from './geoserver';

describe('GeoServer helpers', () => {
  it('builds a WMS GetFeatureInfo request for the clicked pixel', () => {
    const params = featureInfoParams({
      bbox: { minX: 2800000, minY: 4300000, maxX: 5000000, maxY: 5100000 },
      size: { width: 800.4, height: 400 },
      point: { x: 120.6, y: 33 },
    });

    expect(params['request']).toBe('GetFeatureInfo');
    expect(params['query_layers']).toBe(geoServerLayers);
    expect(params['srs']).toBe('EPSG:3857');
    expect(params['bbox']).toBe('2800000,4300000,5000000,5100000');
    expect(params['width']).toBe('800');
    expect(params['x']).toBe('121');
    expect(params['info_format']).toBe('application/json');
  });

  it('describes a province with its place count', () => {
    const text = describeFeature({ properties: { Name: 'Antalya', PlaceCount: 9 } }, 'tr');
    expect(text).toBe('Antalya: 9');
  });

  it('describes a place in the chosen language', () => {
    const place = { properties: { NameTr: 'Topkapı Sarayı', NameEn: 'Topkapı Palace' } };
    expect(describeFeature(place, 'tr')).toBe('Topkapı Sarayı');
    expect(describeFeature(place, 'en')).toBe('Topkapı Palace');
  });

  it('falls back to the other language when a name is missing', () => {
    expect(describeFeature({ properties: { NameTr: 'Efes' } }, 'en')).toBe('Efes');
  });
});
