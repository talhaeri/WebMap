<?xml version="1.0" encoding="UTF-8"?>
<!-- Proje siniri. Renk proje durumuna gore (Durum kolonu, gs.Projeler).
     Kural basliklari (Title) lejantta gorunur. Proje adi etiketi sadece orta/yakin zoomda. -->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xlink="http://www.w3.org/1999/xlink"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>webmap_proje</Name>
    <UserStyle>
      <Title>Proje sınırı (duruma göre)</Title>
      <FeatureTypeStyle>

        <Rule>
          <Title>Planlama</Title>
          <ogc:Filter>
            <ogc:PropertyIsEqualTo>
              <ogc:PropertyName>Durum</ogc:PropertyName>
              <ogc:Literal>Planlama</ogc:Literal>
            </ogc:PropertyIsEqualTo>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#6c757d</CssParameter>
              <CssParameter name="fill-opacity">0.05</CssParameter>
            </Fill>
            <Stroke>
              <CssParameter name="stroke">#6c757d</CssParameter>
              <CssParameter name="stroke-width">3</CssParameter>
            </Stroke>
          </PolygonSymbolizer>
        </Rule>

        <Rule>
          <Title>Onay bekliyor</Title>
          <ogc:Filter>
            <ogc:PropertyIsEqualTo>
              <ogc:PropertyName>Durum</ogc:PropertyName>
              <ogc:Literal>OnayBekliyor</ogc:Literal>
            </ogc:PropertyIsEqualTo>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#e0a800</CssParameter>
              <CssParameter name="fill-opacity">0.05</CssParameter>
            </Fill>
            <Stroke>
              <CssParameter name="stroke">#e0a800</CssParameter>
              <CssParameter name="stroke-width">3</CssParameter>
            </Stroke>
          </PolygonSymbolizer>
        </Rule>

        <Rule>
          <Title>Onaylandı</Title>
          <ogc:Filter>
            <ogc:PropertyIsEqualTo>
              <ogc:PropertyName>Durum</ogc:PropertyName>
              <ogc:Literal>Onaylandi</ogc:Literal>
            </ogc:PropertyIsEqualTo>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#198754</CssParameter>
              <CssParameter name="fill-opacity">0.05</CssParameter>
            </Fill>
            <Stroke>
              <CssParameter name="stroke">#198754</CssParameter>
              <CssParameter name="stroke-width">3</CssParameter>
            </Stroke>
          </PolygonSymbolizer>
        </Rule>

        <!-- 1:70000'den yakin (~zoom 13+) proje adi -->
        <Rule>
          <Title>Proje adı</Title>
          <MaxScaleDenominator>70000</MaxScaleDenominator>
          <TextSymbolizer>
            <Label><ogc:PropertyName>ProjeAdi</ogc:PropertyName></Label>
            <Font>
              <CssParameter name="font-family">Arial</CssParameter>
              <CssParameter name="font-size">12</CssParameter>
              <CssParameter name="font-weight">bold</CssParameter>
            </Font>
            <LabelPlacement>
              <PointPlacement>
                <AnchorPoint><AnchorPointX>0.5</AnchorPointX><AnchorPointY>0.5</AnchorPointY></AnchorPoint>
              </PointPlacement>
            </LabelPlacement>
            <Halo>
              <Radius>2</Radius>
              <Fill><CssParameter name="fill">#FFFFFF</CssParameter></Fill>
            </Halo>
            <Fill><CssParameter name="fill">#051650</CssParameter></Fill>
          </TextSymbolizer>
        </Rule>

      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
