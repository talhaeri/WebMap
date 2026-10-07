<?xml version="1.0" encoding="UTF-8"?>
<!-- Konut poligonu. map.js STIL.Konut ile ayni: #2ecc71, kenar 3, dolgu %20 (Leaflet varsayilani). -->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xlink="http://www.w3.org/1999/xlink"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>webmap_konut</Name>
    <UserStyle>
      <Title>Konut</Title>
      <FeatureTypeStyle>
        <Rule>
          <Title>Konut</Title>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#2ecc71</CssParameter>
              <CssParameter name="fill-opacity">0.2</CssParameter>
            </Fill>
            <Stroke>
              <CssParameter name="stroke">#2ecc71</CssParameter>
              <CssParameter name="stroke-width">3</CssParameter>
            </Stroke>
          </PolygonSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
