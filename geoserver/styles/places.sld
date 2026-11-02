<?xml version="1.0" encoding="UTF-8"?>
<!-- Places as circles coloured by type (same colours as the Angular map), names shown when zoomed in. -->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld" xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>places</Name>
    <UserStyle>
      <Title>Places by type</Title>
      <FeatureTypeStyle>
        <Rule>
          <Name>Museum</Name>
          <Title>Museum / Müze</Title>
          <ogc:Filter><ogc:PropertyIsEqualTo><ogc:PropertyName>Type</ogc:PropertyName><ogc:Literal>Museum</ogc:Literal></ogc:PropertyIsEqualTo></ogc:Filter>
          <PointSymbolizer><Graphic><Mark><WellKnownName>circle</WellKnownName>
            <Fill><CssParameter name="fill">#2563eb</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#ffffff</CssParameter><CssParameter name="stroke-width">1.5</CssParameter></Stroke>
          </Mark><Size>12</Size></Graphic></PointSymbolizer>
        </Rule>
        <Rule>
          <Name>ArchaeologicalSite</Name>
          <Title>Archaeological site / Ören yeri</Title>
          <ogc:Filter><ogc:PropertyIsEqualTo><ogc:PropertyName>Type</ogc:PropertyName><ogc:Literal>ArchaeologicalSite</ogc:Literal></ogc:PropertyIsEqualTo></ogc:Filter>
          <PointSymbolizer><Graphic><Mark><WellKnownName>circle</WellKnownName>
            <Fill><CssParameter name="fill">#b45309</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#ffffff</CssParameter><CssParameter name="stroke-width">1.5</CssParameter></Stroke>
          </Mark><Size>12</Size></Graphic></PointSymbolizer>
        </Rule>
        <Rule>
          <Name>Monument</Name>
          <Title>Monument / Anıt</Title>
          <ogc:Filter><ogc:PropertyIsEqualTo><ogc:PropertyName>Type</ogc:PropertyName><ogc:Literal>Monument</ogc:Literal></ogc:PropertyIsEqualTo></ogc:Filter>
          <PointSymbolizer><Graphic><Mark><WellKnownName>circle</WellKnownName>
            <Fill><CssParameter name="fill">#7c3aed</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#ffffff</CssParameter><CssParameter name="stroke-width">1.5</CssParameter></Stroke>
          </Mark><Size>12</Size></Graphic></PointSymbolizer>
        </Rule>
        <Rule>
          <Name>Palace</Name>
          <Title>Palace / Saray</Title>
          <ogc:Filter><ogc:PropertyIsEqualTo><ogc:PropertyName>Type</ogc:PropertyName><ogc:Literal>Palace</ogc:Literal></ogc:PropertyIsEqualTo></ogc:Filter>
          <PointSymbolizer><Graphic><Mark><WellKnownName>circle</WellKnownName>
            <Fill><CssParameter name="fill">#dc2626</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#ffffff</CssParameter><CssParameter name="stroke-width">1.5</CssParameter></Stroke>
          </Mark><Size>12</Size></Graphic></PointSymbolizer>
        </Rule>
        <Rule>
          <Name>Labels</Name>
          <!-- Only when zoomed in closer than about 1:2,000,000, so labels do not overlap at country level -->
          <MaxScaleDenominator>2000000</MaxScaleDenominator>
          <TextSymbolizer>
            <Label><ogc:PropertyName>NameTr</ogc:PropertyName></Label>
            <Font><CssParameter name="font-family">SansSerif</CssParameter><CssParameter name="font-size">11</CssParameter></Font>
            <LabelPlacement><PointPlacement><Displacement><DisplacementX>8</DisplacementX><DisplacementY>0</DisplacementY></Displacement></PointPlacement></LabelPlacement>
            <Halo><Radius>1.5</Radius><Fill><CssParameter name="fill">#ffffff</CssParameter></Fill></Halo>
            <Fill><CssParameter name="fill">#1f2937</CssParameter></Fill>
          </TextSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
