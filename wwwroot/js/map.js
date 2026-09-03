// ===== Harita kurulumu =====
const map = L.map('map').setView([39.9588, 32.8665], 15);
map.zoomControl.setPosition('bottomleft');

L.tileLayer('http://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}', {
    maxZoom: 20,
    subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
    attribution: '&copy; Google Maps'
}).addTo(map);

const katmanlar = L.layerGroup().addTo(map);
// Leaflet.Snap "guide" grubu: sadece fiber-disi nesneler (haritaya eklenmez, sadece snap hedefi)
const snapKatmanlari = L.layerGroup();

// ===== WKT <-> GeoJSON =====
const gjToWkt = (g) => wellknown.stringify(g);
const wktToGj = (w) => wellknown.parse(w);

// ===== Tip tablolari =====
const API = {
    Menhol: '/api/menholler', Kabin: '/api/kabinler',
    Konut: '/api/konutlar', Ticari: '/api/ticariler', Fiber: '/api/fiberler'
};

const IKON = {
    Menhol: L.divIcon({ className: 'ikon ikon-menhol', iconSize: [16, 16] }),
    Kabin: L.divIcon({ className: 'ikon ikon-kabin', iconSize: [16, 16] })
};

const STIL = {
    Konut: { color: '#2ecc71' },
    Ticari: { color: '#e67e22' },
    Fiber: { color: '#e74c3c', weight: 3 }
};

// Popup'ta gosterilecek alanlar + etiketleri
const BILGI = {
    Menhol: { kod: 'Kod', derinlik: 'Derinlik (m)' },
    Kabin: { kod: 'Kod', kabinTipi: 'Tip', kabinKapasitesi: 'Kapasite', bosPort: 'Bos Port' },
    Konut: { uavtKod: 'UAVT Kod', bbKsayi: 'BBK Sayisi' },
    Ticari: { uavtKod: 'UAVT Kod', isyeriSayisi: 'Isyeri Sayisi' },
    Fiber: { baslangicId: 'Baslangic', bitisId: 'Bitis' }
};

// Kayit govdesindeki geometri alani (tipe gore)
const GEO_ALAN = { Menhol: 'konum', Kabin: 'konum', Konut: 'geometri', Ticari: 'geometri', Fiber: 'guzergah' };

// ===== Formlar (tip basina duz HTML) =====
const FORMLAR = {
    Menhol: `
        <label>Kod<br><input name="kod" value="MNHL-" required></label><br>
        <label>Derinlik (m)<br><input name="derinlik" type="number" step="0.1" min="0" value="1.5" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Kabin: `
        <label>Kod<br><input name="kod" value="KBN-" required></label><br>
        <label>Tip<br><input name="kabinTipi" value="Saha" required></label><br>
        <label>Kapasite<br><input name="kabinKapasitesi" type="number" min="1" value="288" required></label><br>
        <label>Bos Port<br><input name="bosPort" type="number" min="0" value="288" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Konut: `
        <label>UAVT Kod<br><input name="uavtKod" type="number" min="0" value="0" required></label><br>
        <label>BBK Sayisi<br><input name="bbKsayi" type="number" min="0" value="1" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Ticari: `
        <label>UAVT Kod<br><input name="uavtKod" type="number" min="0" value="0" required></label><br>
        <label>Isyeri Sayisi<br><input name="isyeriSayisi" type="number" min="0" value="1" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`
};

// ===== Cizim / secim durumu =====
let eklemeTuru = null;     // 'Menhol' | 'Kabin'   -> haritaya tikla
let polygonHedef = null;   // 'Konut' | 'Ticari'   -> L.Draw.Polygon
let fiberCizim = null;     // aktif L.Draw.Polyline handler'i

// ===== POST: kaydet, haritaya ekle (SAYFA YENILENMEZ) =====
async function kaydet(tur, govde) {
    const r = await fetch(API[tur], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(govde)
    });
    if (!r.ok) { alert(await r.text()); return null; }

    const kayit = await r.json();   // sunucu tam kaydi doner
    ciz(tur, kayit);                // yeni nesneyi haritaya ekle
    map.closePopup();               // acik form popup'ini kapat
    return kayit;
}

// ===== DELETE: sunucudan sil, katmani (ve bagli fiberleri) haritadan kaldir =====
async function sil(tur, id, katman) {
    const onay = confirm(`Bu veriyi (${tur}) silmek istediðinize emin misiniz?`);
    if (onay) {
        await fetch(`${API[tur]}/${id}`, { method: 'DELETE' });
        katmanlar.removeLayer(katman);
        snapKatmanlari.removeLayer(katman);
        if (katman._snapMerkez) snapKatmanlari.removeLayer(katman._snapMerkez);

        // Bir nesne silinince ona bagli fiberler de gider (sunucu da siliyor).
        if (tur !== 'Fiber') {
            [...katmanlar.getLayers()]
                .filter(l => l._veri?.tur === 'Fiber'
                    && (l._veri.kayit.baslangicId === id || l._veri.kayit.bitisId === id))
                .forEach(l => katmanlar.removeLayer(l));
        }
        alert("Bu veri basari ile silindi !");
    }
    else {
        alert("Silme islemi basari ile iptal edildi !");
    }
    map.closePopup();
}

// ===== Popup icerigi: sadece ilgili bilgiler + Sil butonu (id gosterilmez) =====
function popupIcerik(tur, kayit, katman) {
    const kutu = document.createElement('div');
    const satirlar = Object.entries(BILGI[tur])
        .map(([alan, etiket]) => `<div><b>${etiket}:</b> ${kayit[alan]}</div>`)
        .join('');
    kutu.innerHTML = `${satirlar}<div><button type="button">Sil</button></div>`;
    kutu.querySelector('button').onclick = () => sil(tur, kayit.id, katman);
    return kutu;
}

// ===== Bir kaydi haritaya ciz =====
function ciz(tur, kayit) {
    const gj = wktToGj(kayit[GEO_ALAN[tur]]);
    const katman = gj.type === 'Point'
        ? L.marker([gj.coordinates[1], gj.coordinates[0]], { icon: IKON[tur] })
        : L.geoJSON(gj, { style: STIL[tur] });

    katman._veri = { tur, kayit };
    // Leaflet.Snap ic katmana (poligon/cizgi) yapisir; _veri'yi ona da tasi
    if (katman.eachLayer) katman.eachLayer(l => { l._veri = katman._veri; });

    katman.on('click', () => tikla(katman));
    katman.addTo(katmanlar);

    if (tur !== 'Fiber') {                      // fiberler snap hedefi degil
        snapKatmanlari.addLayer(katman);       // marker: kendisi ; poligon: kenar
        if (!katman.getLatLng) {               // poligon: merkezine gorunmez snap-hedefi
            const merkez = L.marker(katman.getBounds().getCenter(), { opacity: 0, interactive: false });
            merkez._veri = katman._veri;
            katman._snapMerkez = merkez;
            snapKatmanlari.addLayer(merkez);
        }
    }
    return katman;
}

// ===== Nesneye tiklama -> bilgi popup'i =====
function tikla(katman) {
    const { tur, kayit } = katman._veri;
    const nokta = katman.getLatLng ? katman.getLatLng() : katman.getBounds().getCenter();
    L.popup().setLatLng(nokta).setContent(popupIcerik(tur, kayit, katman)).openOn(map);
}

// ===== Popup icinde form ac; Kaydet'e basinca POST =====
function formPopupAc(tur, latlng, ekVeri) {
    const form = document.createElement('form');
    form.innerHTML = FORMLAR[tur];
    L.popup().setLatLng(latlng).setContent(form).openOn(map);

    form.querySelector('[data-kaydet]').onclick = () => {
        if (!form.reportValidity()) return;                     // native HTML5 dogrulama
        const veri = Object.fromEntries(new FormData(form));    // { kod: "...", derinlik: "1.5", ... }
        kaydet(tur, Object.assign(veri, ekVeri));
    };
}

// ===== Baslangicta kayitli verileri yukle =====
async function yukle() {
    for (const tur of ['Menhol', 'Kabin', 'Konut', 'Ticari', 'Fiber']) {
        const liste = await (await fetch(API[tur])).json();
        liste.forEach(kayit => ciz(tur, kayit));
    }
}
yukle();

// ===== Leaflet.Snap: fiber koseleri mevcut nesnelere yapisir =====
let sonSnap = null;         // imlecin su an yapistigi katman (veya null)
let fiberKoseSnap = [];     // eklenen her koseye karsilik gelen nesne katmani

// ===== Toolbar =====

// Menhol / Kabin: butona bas -> haritaya tikla
document.querySelectorAll('[data-add-type]').forEach(b => {
    b.onclick = () => { eklemeTuru = b.dataset.addType; polygonHedef = null; fiberCizim?.disable(); };
});

// Konut / Ticari: butona bas -> poligon ciz
document.querySelectorAll('[data-poly-type]').forEach(b => {
    b.onclick = () => {
        polygonHedef = b.dataset.polyType;
        eklemeTuru = null;
        fiberCizim?.disable();
        new L.Draw.Polygon(map).enable();
    };
});

// Fiber: Leaflet.Draw polyline + Leaflet.Snap (koseler nesnelere yapisir)
document.getElementById('fiber-btn').onclick = () => {
    eklemeTuru = null;
    polygonHedef = null;
    fiberCizim?.disable();

    sonSnap = null;
    fiberKoseSnap = [];

    fiberCizim = new L.Draw.Polyline(map, {
        shapeOptions: { color: '#e74c3c', weight: 3 },
        guideLayers: [snapKatmanlari]     // Leaflet.Snap: fiber-disi nesnelere yapis
    });
    fiberCizim.enable();
    fiberCizim._snap_on_enabled();        // leaflet-snap L.Draw baglayicisi elle tetiklenir

    // hangi nesneye yapistigimizi takip et
    fiberCizim._mouseMarker
        .on('snap', (e) => { sonSnap = e.layer; })
        .on('unsnap', () => { sonSnap = null; });
};

// Her kose eklendiginde o anki snap'i kaydet
map.on(L.Draw.Event.DRAWVERTEX, () => {
    if (fiberCizim && fiberCizim.enabled()) fiberKoseSnap.push(sonSnap);
});

// Esc -> aktif fiber cizimini iptal et
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') fiberCizim?.disable();
});

// Haritaya (bos alana) tiklama -> Menhol / Kabin ekleme
map.on('click', (e) => {
    if (!eklemeTuru) return;
    const tur = eklemeTuru;
    eklemeTuru = null;
    const konum = gjToWkt({ type: 'Point', coordinates: [e.latlng.lng, e.latlng.lat] });
    formPopupAc(tur, e.latlng, { konum });
});

// Cizim bitince: poligon -> form (Konut/Ticari) ; polyline -> fiber
map.on(L.Draw.Event.CREATED, (e) => {
    if (e.layerType === 'polygon' && polygonHedef) {
        const geometri = gjToWkt(e.layer.toGeoJSON().geometry);
        const hedef = polygonHedef;
        polygonHedef = null;
        formPopupAc(hedef, e.layer.getBounds().getCenter(), { geometri });
        return;
    }
    if (e.layerType !== 'polyline') return;

    // son kose DRAWVERTEX'e dusmediyse anlik snap'i ekle
    if (fiberKoseSnap.length < e.layer.getLatLngs().length) fiberKoseSnap.push(sonSnap);

    const bas = fiberKoseSnap[0]?._veri;                          // ilk kose hangi nesneye yapisti
    const bit = fiberKoseSnap[fiberKoseSnap.length - 1]?._veri;   // son kose

    if (!bas || (bas.tur !== 'Menhol' && bas.tur !== 'Kabin'))
        return alert('Fiber baslangici bir menhol veya kabin uzerinde olmali.');
    if (!bit || bit.tur === 'Fiber')
        return alert('Fiber bitisi bir nesne veya alan olmali.');

    kaydet('Fiber', {
        baslangicId: bas.kayit.id,
        bitisId: bit.kayit.id,
        guzergah: gjToWkt(e.layer.toGeoJSON().geometry)
    });
});
