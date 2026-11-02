using System.Globalization;
using System.Text;
using System.Xml;
using KulturRotasi.Application.Places;

namespace KulturRotasi.Api.GeoFormats;

/// <summary>
/// Writes places as KML 2.2 (OGC standard), the format Google Earth and many GIS tools open directly.
/// Unlike GeoJSON, KML is XML and can carry styling; here each place becomes a Placemark with a Point.
/// </summary>
internal static class KmlWriter
{
    public const string ContentType = "application/vnd.google-earth.kml+xml";
    private const string Namespace = "http://www.opengis.net/kml/2.2";

    public static byte[] Write(IEnumerable<PlaceSummaryDto> places, string documentName)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) };
        using (var xml = XmlWriter.Create(stream, settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("kml", Namespace);
            xml.WriteStartElement("Document", Namespace);
            xml.WriteElementString("name", Namespace, documentName);

            foreach (var place in places)
            {
                xml.WriteStartElement("Placemark", Namespace);
                xml.WriteAttributeString("id", place.Slug);
                xml.WriteElementString("name", Namespace, place.Name);
                xml.WriteElementString("description", Namespace, $"{place.Type} · {place.ProvinceName}");

                // ExtendedData keeps the attributes machine-readable, like GeoJSON properties
                xml.WriteStartElement("ExtendedData", Namespace);
                WriteData(xml, "slug", place.Slug);
                WriteData(xml, "type", place.Type.ToString());
                WriteData(xml, "provinceId", place.ProvinceId.ToString(CultureInfo.InvariantCulture));
                WriteData(xml, "isUnescoWorldHeritage", place.IsUnescoWorldHeritage ? "true" : "false");
                xml.WriteEndElement();

                // KML coordinates are "longitude,latitude", the same order as GeoJSON
                xml.WriteStartElement("Point", Namespace);
                xml.WriteElementString(
                    "coordinates",
                    Namespace,
                    string.Create(CultureInfo.InvariantCulture, $"{place.Longitude},{place.Latitude}"));
                xml.WriteEndElement();

                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
        }

        return stream.ToArray();
    }

    private static void WriteData(XmlWriter xml, string name, string value)
    {
        xml.WriteStartElement("Data", Namespace);
        xml.WriteAttributeString("name", name);
        xml.WriteElementString("value", Namespace, value);
        xml.WriteEndElement();
    }
}
