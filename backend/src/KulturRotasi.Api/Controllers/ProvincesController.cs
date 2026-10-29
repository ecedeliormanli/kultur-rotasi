using KulturRotasi.Api.Infrastructure;
using KulturRotasi.Application.Abstractions;
using KulturRotasi.Application.Places;
using KulturRotasi.Application.Provinces;
using KulturRotasi.Application.Provinces.GetPlacesInProvince;
using KulturRotasi.Application.Provinces.GetProvinceBoundaries;
using KulturRotasi.Application.Provinces.GetProvinces;
using Microsoft.AspNetCore.Mvc;

namespace KulturRotasi.Api.Controllers;

/// <summary>Türkiye's 81 provinces and their borders.</summary>
[ApiController]
[Route("api/provinces")]
[Produces("application/json")]
public sealed class ProvincesController : ControllerBase
{
    /// <summary>Lists all provinces with the number of places inside each border.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProvinceDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProvinceDto>> List(
        [FromServices] IQueryHandler<GetProvincesQuery, IReadOnlyList<ProvinceDto>> handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(new GetProvincesQuery(), cancellationToken);

    /// <summary>Returns province borders as a GeoJSON FeatureCollection, with a place count per province.</summary>
    /// <param name="toleranceDegrees">How much to simplify the borders (0–0.1 degrees). 0 returns full detail.</param>
    [HttpGet("geojson")]
    [Produces(GeoJsonMapper.ContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GeoJson(
        [FromServices] IQueryHandler<GetProvinceBoundariesQuery, IReadOnlyList<ProvinceBoundaryDto>> handler,
        CancellationToken cancellationToken,
        double toleranceDegrees = 0.005)
    {
        var provinces = await handler.HandleAsync(new GetProvinceBoundariesQuery(toleranceDegrees), cancellationToken);
        return Ok(GeoJsonMapper.ToFeatureCollection(provinces));
    }

    /// <summary>Lists the places whose location lies inside the province border (point-in-polygon).</summary>
    /// <param name="plateCode">Licence plate code of the province, e.g. 6 for Ankara.</param>
    [HttpGet("{plateCode:int}/places")]
    [ProducesResponseType<IReadOnlyList<PlaceSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PlaceSummaryDto>>> Places(
        [FromServices] IQueryHandler<GetPlacesInProvinceQuery, IReadOnlyList<PlaceSummaryDto>?> handler,
        int plateCode,
        CancellationToken cancellationToken)
    {
        var places = await handler.HandleAsync(new GetPlacesInProvinceQuery(plateCode, RequestLanguage.Current), cancellationToken);
        return places is null ? NotFound() : Ok(places);
    }
}
