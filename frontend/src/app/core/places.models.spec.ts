import { densityClasses, densityColor, densityLabel, toQueryParams } from './places.models';

describe('toQueryParams', () => {
  it('leaves out empty filters', () => {
    expect(toQueryParams({ type: null, unescoOnly: false, search: '  ' })).toEqual({});
  });

  it('maps every filter to the API parameter name', () => {
    expect(toQueryParams({ type: 'Museum', unescoOnly: true, search: ' efes ' })).toEqual({
      type: 'Museum',
      unesco: 'true',
      search: 'efes',
    });
  });
});

describe('density classes', () => {
  it('colours provinces by their place count', () => {
    expect(densityColor(0)).toBe('#f1f5f9');
    expect(densityColor(3)).toBe('#fbbf24');
    expect(densityColor(25)).toBe('#92400e');
  });

  it('labels each class for the legend', () => {
    expect(densityClasses.map(densityLabel)).toEqual(['0', '1', '2–3', '4–6', '7+']);
  });
});
