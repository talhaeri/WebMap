<?xml version="1.0" encoding="UTF-8"?>
<!-- Santral poligonu. map.js STIL.Santral ile ayni: #9b59b6, kenar 3, dolgu %20.
     Kod etiketi sadece yakin zoomda (1:5000'den yakin, ~zoom 17+). -->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xlink="http://www.w3.org/1999/xlink"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>webmap_santral</Name>
    <UserStyle>
      <Title>Santral</Title>
      <FeatureTypeStyle>
        <Rule>
          <Title>Santral</Title>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#9b59b6</CssParameter>
              <CssParameter name="fill-opacity">0.2</CssParameter>
            </Fill>
            <Stroke>
              <CssParameter name="stroke">#9b59b6</CssParameter>
              <CssParameter name="stroke-width">3</CssParameter>
            </Stroke>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Title>Santral kodu</Title>
          <MaxScaleDenominator>5000</MaxScaleDenominator>
          <TextSymbolizer>
            <Label><ogc:PropertyName>Kod</ogc:PropertyName></Label>
            <Font>
              <CssParameter name="font-family">Arial</CssParameter>
              <CssParameter name="font-size">11</CssParameter>
            </Font>
            <LabelPlacement>
              <PointPlacement>
                <AnchorPoint><AnchorPointX>0.5</AnchorPointX><AnchorPointY>0.5</AnchorPointY></AnchorPoint>
              </PointPlacement>
            </LabelPlacement>
            <Halo>
              <Radius>1.5</Radius>
              <Fill><CssParameter name="fill">#FFFFFF</CssParameter></Fill>
            </Halo>
            <Fill><CssParameter name="fill">#4a235a</CssParameter></Fill>
          </TextSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
