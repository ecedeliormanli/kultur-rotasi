<?xml version="1.0" encoding="UTF-8"?>
<!-- Thematic (choropleth) map: provinces filled by the number of places inside them.
     Same classes and colours as the Angular map, so both renderings match. -->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld" xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>provinces</Name>
    <UserStyle>
      <Title>Places per province</Title>
      <FeatureTypeStyle>
        <Rule>
          <Name>count-0-0</Name>
          <Title>0</Title>
          <ogc:Filter><ogc:PropertyIsBetween><ogc:PropertyName>PlaceCount</ogc:PropertyName><ogc:LowerBoundary><ogc:Literal>0</ogc:Literal></ogc:LowerBoundary><ogc:UpperBoundary><ogc:Literal>0</ogc:Literal></ogc:UpperBoundary></ogc:PropertyIsBetween></ogc:Filter>
          <PolygonSymbolizer>
            <Fill><CssParameter name="fill">#f1f5f9</CssParameter><CssParameter name="fill-opacity">0.75</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#78716c</CssParameter><CssParameter name="stroke-width">0.6</CssParameter></Stroke>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>count-1-1</Name>
          <Title>1</Title>
          <ogc:Filter><ogc:PropertyIsBetween><ogc:PropertyName>PlaceCount</ogc:PropertyName><ogc:LowerBoundary><ogc:Literal>1</ogc:Literal></ogc:LowerBoundary><ogc:UpperBoundary><ogc:Literal>1</ogc:Literal></ogc:UpperBoundary></ogc:PropertyIsBetween></ogc:Filter>
          <PolygonSymbolizer>
            <Fill><CssParameter name="fill">#fde68a</CssParameter><CssParameter name="fill-opacity">0.75</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#78716c</CssParameter><CssParameter name="stroke-width">0.6</CssParameter></Stroke>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>count-2-3</Name>
          <Title>2–3</Title>
          <ogc:Filter><ogc:PropertyIsBetween><ogc:PropertyName>PlaceCount</ogc:PropertyName><ogc:LowerBoundary><ogc:Literal>2</ogc:Literal></ogc:LowerBoundary><ogc:UpperBoundary><ogc:Literal>3</ogc:Literal></ogc:UpperBoundary></ogc:PropertyIsBetween></ogc:Filter>
          <PolygonSymbolizer>
            <Fill><CssParameter name="fill">#fbbf24</CssParameter><CssParameter name="fill-opacity">0.75</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#78716c</CssParameter><CssParameter name="stroke-width">0.6</CssParameter></Stroke>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>count-4-6</Name>
          <Title>4–6</Title>
          <ogc:Filter><ogc:PropertyIsBetween><ogc:PropertyName>PlaceCount</ogc:PropertyName><ogc:LowerBoundary><ogc:Literal>4</ogc:Literal></ogc:LowerBoundary><ogc:UpperBoundary><ogc:Literal>6</ogc:Literal></ogc:UpperBoundary></ogc:PropertyIsBetween></ogc:Filter>
          <PolygonSymbolizer>
            <Fill><CssParameter name="fill">#d97706</CssParameter><CssParameter name="fill-opacity">0.75</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#78716c</CssParameter><CssParameter name="stroke-width">0.6</CssParameter></Stroke>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>count-7-1000</Name>
          <Title>7+</Title>
          <ogc:Filter><ogc:PropertyIsBetween><ogc:PropertyName>PlaceCount</ogc:PropertyName><ogc:LowerBoundary><ogc:Literal>7</ogc:Literal></ogc:LowerBoundary><ogc:UpperBoundary><ogc:Literal>1000</ogc:Literal></ogc:UpperBoundary></ogc:PropertyIsBetween></ogc:Filter>
          <PolygonSymbolizer>
            <Fill><CssParameter name="fill">#92400e</CssParameter><CssParameter name="fill-opacity">0.75</CssParameter></Fill>
            <Stroke><CssParameter name="stroke">#78716c</CssParameter><CssParameter name="stroke-width">0.6</CssParameter></Stroke>
          </PolygonSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
