using FluentValidation;
using KulturRotasi.Application.Abstractions;
using KulturRotasi.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Places.SearchPlaces;

/// <summary>Lists places page by page, sorted by name, with optional filters.</summary>
public sealed record SearchPlacesQuery(PlaceFilter Filter, string Language, int Page = 1, int PageSize = 20);

public sealed class SearchPlacesQueryValidator : AbstractValidator<SearchPlacesQuery>
{
    public const int MaxPageSize = 100;

    public SearchPlacesQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Filter.Search).MaximumLength(100).OverridePropertyName("search");
    }
}

public sealed class SearchPlacesHandler(IAppDbContext db) : IQueryHandler<SearchPlacesQuery, PagedResult<PlaceSummaryDto>>
{
    public async Task<PagedResult<PlaceSummaryDto>> HandleAsync(SearchPlacesQuery query, CancellationToken cancellationToken)
    {
        var places = db.Places.AsNoTracking().ApplyFilter(query.Filter);

        var totalCount = await places.CountAsync(cancellationToken);
        var items = await places
            .OrderBy(PlaceQueries.NameIn(query.Language))
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(PlaceQueries.ToSummary(query.Language))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlaceSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }
}
