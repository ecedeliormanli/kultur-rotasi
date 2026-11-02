using KulturRotasi.Api.Infrastructure;
using KulturRotasi.Application.Abstractions;
using KulturRotasi.Application.Common;
using KulturRotasi.Application.Places;
using KulturRotasi.Application.Places.GetNearbyPlaces;
using KulturRotasi.Application.Places.GetPlaceBySlug;
using KulturRotasi.Application.Places.GetPlacesWithinGeometry;
using KulturRotasi.Application.Places.ListPlaces;
using KulturRotasi.Application.Places.SearchPlaces;
using KulturRotasi.Domain.Places;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Api.Controllers;

/// <summary>Museums, archaeological sites and monuments, with spatial search.</summary>
/// <remarks>Texts are returned in the language from <c>Accept-Language</c> or <c>?culture=</c> (tr or en, default tr).</remarks>
[ApiController]
[Route("api/places")]
public sealed class PlacesController : ControllerBase
{
    /// <summary>Lists places page by page, sorted by name.</summary>
    /// <param name="type">Only places of this type.</param>
    /// <param name="provinceId">Only places in this province (plate code, 1–81).</param>
    /// <param name="unesco">Only UNESCO World Heritage places (true) or only others (false).</param>
    /// <param name="search">Part of the name, in Turkish or English.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page, 1–100.</param>
    [HttpGet]
    [ProducesResponseType<PagedResult<PlaceSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<PagedResult<PlaceSummaryDto>> Search(
        [FromServices] IQueryHandler<SearchPlacesQuery, PagedResult<PlaceSummaryDto>> handler,
        PlaceType? type,
        int? provinceId,
        bool? unesco,
        string? search,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20) =>
        await handler.HandleAsync(
            new SearchPlacesQuery(new PlaceFilter(type, provinceId, unesco, search), RequestLanguage.Current, page, pageSize),
            cancellationToken);

    /// <summary>Returns every matching place as a GeoJSON FeatureCollection of points, for the map.</summary>
    [HttpGet("geojson")]
    [Produces(GeoJsonMapper.ContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GeoJson(
        [FromServices] IQueryHandler<ListPlacesQuery, IReadOnlyList<PlaceSummaryDto>> handler,
        PlaceType? type,
        int? provinceId,
        bool? unesco,
        string? search,
        CancellationToken cancellationToken)
    {
        var places = await handler.HandleAsync(
            new ListPlacesQuery(new PlaceFilter(type, provinceId, unesco, search), RequestLanguage.Current),
            cancellationToken);

        return Ok(GeoJsonMapper.ToFeatureCollection(places));
    }

    /// <summary>Finds the places nearest to a point, closest first.</summary>
    /// <param name="latitude">Latitude in WGS 84, e.g. 39.92.</param>
    /// <param name="longitude">Longitude in WGS 84, e.g. 32.85.</param>
    /// <param name="radiusKm">Only places within this many kilometres (up to 500). Leave empty for the nearest places at any distance.</param>
    /// <param name="type">Only places of this type.</param>
    /// <param name="limit">Maximum number of places, 1–100.</param>
    [HttpGet("nearby")]
    [ProducesResponseType<IReadOnlyList<NearbyPlaceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<NearbyPlaceDto>> Nearby(
        [FromServices] IQueryHandler<GetNearbyPlacesQuery, IReadOnlyList<NearbyPlaceDto>> handler,
        [BindRequired] double latitude,
        [BindRequired] double longitude,
        double? radiusKm,
        PlaceType? type,
        CancellationToken cancellationToken,
        int limit = 10) =>
        await handler.HandleAsync(
            new GetNearbyPlacesQuery(latitude, longitude, RequestLanguage.Current, radiusKm, type, limit),
            cancellationToken);

    /// <summary>Finds the places inside a GeoJSON geometry, or within a buffer distance of it.</summary>
    /// <remarks>
    /// Example body: <c>{ "geometry": { "type": "LineString", "coordinates": [[29.0, 36.3], [30.6, 36.3]] }, "bufferKm": 15 }</c>.
    /// Coordinates are [longitude, latitude]. Polygons may be drawn in either direction.
    /// </remarks>
    [HttpPost("within")]
    [Consumes("application/json")]
    [ProducesResponseType<IReadOnlyList<PlaceSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<PlaceSummaryDto>> Within(
        [FromServices] IQueryHandler<GetPlacesWithinGeometryQuery, IReadOnlyList<PlaceSummaryDto>> handler,
        WithinGeometryRequest request,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(
            new GetPlacesWithinGeometryQuery(request.Geometry, RequestLanguage.Current, request.BufferKm),
            cancellationToken);

    /// <summary>Returns one place with its description.</summary>
    /// <param name="slug">URL name of the place, e.g. "ephesus".</param>
    [HttpGet("{slug}")]
    [ProducesResponseType<PlaceDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDetailsDto>> BySlug(
        [FromServices] IQueryHandler<GetPlaceBySlugQuery, PlaceDetailsDto?> handler,
        string slug,
        CancellationToken cancellationToken)
    {
        var place = await handler.HandleAsync(new GetPlaceBySlugQuery(slug, RequestLanguage.Current), cancellationToken);
        return place is null ? NotFound() : place;
    }
}

/// <summary>Body of <c>POST /api/places/within</c>.</summary>
/// <param name="Geometry">A GeoJSON geometry: Point, LineString, Polygon or a Multi variant.</param>
/// <param name="BufferKm">Also include places up to this many kilometres outside the geometry (0–100).</param>
public sealed record WithinGeometryRequest(Geometry Geometry, double BufferKm = 0);
