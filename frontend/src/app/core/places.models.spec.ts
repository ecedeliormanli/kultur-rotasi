import { toQueryParams } from './places.models';

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
