using NetTopologySuite.Geometries;

namespace KulturRotasi.Application.Provinces;

/// <summary>A province and how many places lie inside its border.</summary>
public sealed record ProvinceDto(int PlateCode, string Name, int PlaceCount);

/// <summary>A province with its border, for drawing a thematic (choropleth) map.</summary>
public sealed record ProvinceBoundaryDto(int PlateCode, string Name, int PlaceCount, Geometry Boundary);
