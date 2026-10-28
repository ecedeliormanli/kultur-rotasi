using KulturRotasi.Domain.Common;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Domain.Provinces;

/// <summary>One of Türkiye's 81 provinces (il). The id is the licence plate code, e.g. 6 for Ankara.</summary>
public sealed class Province
{
    public const int NameMaxLength = 50;

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>Province border as a (multi)polygon in WGS 84.</summary>
    public Geometry Boundary { get; private set; } = null!;

    private Province()
    {
        // Required by EF Core
    }

    public Province(int plateCode, string name, Geometry boundary)
    {
        if (!IsValidPlateCode(plateCode))
        {
            throw new DomainException($"Province plate code must be between 1 and 81, got {plateCode}.");
        }

        if (boundary is not (Polygon or MultiPolygon))
        {
            throw new DomainException("Province boundary must be a polygon or multipolygon.");
        }

        Id = plateCode;
        Name = name;
        Boundary = boundary;
    }

    public static bool IsValidPlateCode(int plateCode) => plateCode is >= 1 and <= 81;
}
