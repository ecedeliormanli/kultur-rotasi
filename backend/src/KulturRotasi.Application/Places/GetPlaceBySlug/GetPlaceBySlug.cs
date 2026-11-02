using KulturRotasi.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Places.GetPlaceBySlug;

/// <summary>One place with its description. Returns null when the slug does not exist.</summary>
public sealed record GetPlaceBySlugQuery(string Slug, string Language);

public sealed class GetPlaceBySlugHandler(IAppDbContext db) : IQueryHandler<GetPlaceBySlugQuery, PlaceDetailsDto?>
{
    public async Task<PlaceDetailsDto?> HandleAsync(GetPlaceBySlugQuery query, CancellationToken cancellationToken)
    {
        var place = await db.Places.AsNoTracking()
            .Include(p => p.Province)
            .SingleOrDefaultAsync(p => p.Slug == query.Slug, cancellationToken);

        // The domain decides which text to show (requested language, otherwise Turkish)
        var text = place?.GetTranslation(query.Language);
        if (place is null || text is null)
        {
            return null;
        }

        return new PlaceDetailsDto(
            place.Id,
            place.Slug,
            text.LanguageCode,
            text.Name,
            text.Description,
            place.Type,
            place.Latitude,
            place.Longitude,
            place.ProvinceId,
            place.Province.Name,
            place.IsUnescoWorldHeritage);
    }
}
