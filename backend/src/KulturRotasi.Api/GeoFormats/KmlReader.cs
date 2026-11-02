using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using KulturRotasi.Domain.Common;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Api.GeoFormats;

/// <summary>
/// Reads the Placemarks of a KML file into features. Supports Point, LineString, Polygon (with holes)
/// and MultiGeometry; styles and other elements are ignored.
/// </summary>
internal static class KmlReader
{
    private static readonly XNamespace Kml = "http://www.opengis.net/kml/2.2";

    // Security (OWASP: XML External Entities): DTDs are refused, so a file cannot make the server
    // read local files or call URLs through entity declarations.
    private static readonly XmlReaderSettings SafeSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
    };

    public static FeatureCollection Read(Stream stream)
    {
        XDocument document;
        try
        {
            using var reader = XmlReader.Create(stream, SafeSettings);
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new DomainException($"The file is not valid KML: {exception.Message}");
        }

        var collection = new FeatureCollection();
        foreach (var placemark in document.Descendants(Kml + "Placemark"))
        {
            var geometry = ReadGeometry(placemark);
            if (geometry is null)
            {
                continue;
            }

            var attributes = new AttributesTable { { "name", placemark.Element(Kml + "name")?.Value.Trim() ?? string.Empty } };
            collection.Add(new Feature(geometry, attributes));
        }

        return collection;
    }

    private static Geometry? ReadGeometry(XElement parent)
    {
        var geometries = parent.Elements()
            .Select(element => element.Name.LocalName switch
            {
                "Point" => Geo.Factory.CreatePoint(ReadCoordinates(element).First()),
                "LineString" => Geo.Factory.CreateLineString(ReadCoordinates(element)),
                "Polygon" => ReadPolygon(element),
                "MultiGeometry" => ReadGeometry(element),
                _ => null,
            })
            .OfType<Geometry>()
            .ToArray();

        return geometries.Length switch
        {
            0 => null,
            1 => geometries[0],
            _ => Geo.Factory.CreateGeometryCollection(geometries),
        };
    }

    private static Polygon ReadPolygon(XElement polygon)
    {
        LinearRing Ring(XElement boundary) => Geo.Factory.CreateLinearRing(ReadCoordinates(boundary));

        var shell = polygon.Element(Kml + "outerBoundaryIs")
            ?? throw new DomainException("A KML Polygon needs an outerBoundaryIs element.");
        var holes = polygon.Elements(Kml + "innerBoundaryIs").Select(Ring).ToArray();
        return Geo.Factory.CreatePolygon(Ring(shell), holes);
    }

    /// <summary>KML coordinates are tuples "lon,lat[,alt]" separated by whitespace.</summary>
    private static Coordinate[] ReadCoordinates(XElement element)
    {
        var text = element.Descendants(Kml + "coordinates").FirstOrDefault()?.Value
            ?? throw new DomainException($"A KML {element.Name.LocalName} has no coordinates.");

        try
        {
            return text
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(tuple => tuple.Split(','))
                .Select(parts => new Coordinate(
                    double.Parse(parts[0], CultureInfo.InvariantCulture),
                    double.Parse(parts[1], CultureInfo.InvariantCulture)))
                .ToArray();
        }
        catch (Exception exception) when (exception is FormatException or IndexOutOfRangeException)
        {
            throw new DomainException($"Invalid KML coordinates: '{text.Trim()}'.");
        }
    }
}
