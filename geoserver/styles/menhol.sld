<?xml version="1.0" encoding="UTF-8"?>
<!-- Menhol: saddlebrown daire, beyaz kenar (map.css .ikon-menhol ile ayni gorunum).
     Kod etiketi sadece yakin zoomda (1:5000'den yakin, ~zoom 17+), simgenin sagina. -->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xlink="http://www.w3.org/1999/xlink"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>webmap_menhol</Name>
    <UserStyle>
      <Title>Menhol</Title>
      <FeatureTypeStyle>
        <Rule>
          <Title>Menhol</Title>
          <PointSymbolizer>
            <Graphic>
              <Mark>
                <WellKnownName>circle</WellKnownName>
                <Fill><CssParameter name="fill">#8B4513</CssParameter></Fill>
                <Stroke>
                  <CssParameter name="stroke">#FFFFFF</CssParameter>
                  <CssParameter name="stroke-width">2</CssParameter>
                </Stroke>
              </Mark>
              <Size>18</Size>
            </Graphic>
          </PointSymbolizer>
        </Rule>
        <Rule>
          <Title>Menhol kodu</Title>
          <MaxScaleDenominator>5000</MaxScaleDenominator>
          <TextSymbolizer>
            <Label><ogc:PropertyName>Kod</ogc:PropertyName></Label>
            <Font>
              <CssParameter name="font-family">Arial</CssParameter>
              <CssParameter name="font-size">11</CssParameter>
            </Font>
            <LabelPlacement>
              <PointPlacement>
                <AnchorPoint><AnchorPointX>0</AnchorPointX><AnchorPointY>0.5</AnchorPointY></AnchorPoint>
                <Displacement><DisplacementX>12</DisplacementX><DisplacementY>0</DisplacementY></Displacement>
              </PointPlacement>
            </LabelPlacement>
            <Halo>
              <Radius>1.5</Radius>
              <Fill><CssParameter name="fill">#FFFFFF</CssParameter></Fill>
            </Halo>
            <Fill><CssParameter name="fill">#333333</CssParameter></Fill>
          </TextSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
