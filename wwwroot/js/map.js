// ===== Harita kurulumu =====
const map = L.map('map').setView([39.9588, 32.8665], 15);
map.zoomControl.setPosition('bottomleft');

L.tileLayer('http://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}', {
    maxZoom: 20,
    subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
    attribution: '&copy; Google Maps'
}).addTo(map);

// ===== Yetki =====
// Sunucudan gelen yetki (Views/Home/Index.cshtml icindeki data-yetki).
// Bu SADECE arayuzu kisitlar; gercek engel sunucuda ([Authorize] oznitelikleri).
const duzenleyebilir = ['yonetici', 'duzenleme']
    .includes(document.getElementById('map').dataset.yetki);

const katmanlar = L.layerGroup().addTo(map);

// Poligonlarin (Santral / Konut) merkezindeki gorunmez yapisma hedefleri.
// Fiber ucu poligonun sadece kenarina degil ortasina da yapisabilsin diye var.
// Geoman yapisma listesini map.eachLayer ile kurdugu icin haritaya EKLENMEK zorunda.
const merkezler = L.layerGroup().addTo(map);

// ===== Geoman =====
// Yapisma (snap) her cizimde acik. Fiberler snap hedefi degil: ciz() icinde
// snapIgnore ile disarida birakiliyor. Geoman'in kendi araç cubugu EKLENMEZ
// (addControls cagrilmaz), cizim modlari toolbar'dan programla aciliyor.
const SNAP_TOLERANS = 20;   // piksel: kose bu kadar yakinsa nesneye yapisir

map.pm.setLang('tr');
map.pm.setGlobalOptions({ snappable: true, snapDistance: SNAP_TOLERANS });

// ===== WKT <-> GeoJSON =====
const gjToWkt = (g) => wellknown.stringify(g);
const wktToGj = (w) => wellknown.parse(w);

// ===== Geometri kurallari =====
// Koordinatlar GeoJSON duzeninde: [lng, lat].
// Ayni kurallarin sunucu karsiligi Services/GeometriDenetimi.cs; son soz orada.

// Poligonun dis halkasi / cizginin kose dizisi
const halka = (gj) => gj.type === 'Polygon' ? gj.coordinates[0] : gj.coordinates;

// Ardisik kose ciftleri (poligon halkasi kapali geldigi icin tum kenarlari verir)
function* kenarlar(kose) {
    for (let i = 0; i < kose.length - 1; i++) yield [kose[i], kose[i + 1]];
}

// Nokta poligonun icinde mi - isin atma (ray casting)
function noktaIcinde([x, y], kose) {
    let icerde = false;
    for (let i = 0, j = kose.length - 1; i < kose.length; j = i++) {
        const [xi, yi] = kose[i], [xj, yj] = kose[j];
        if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) icerde = !icerde;
    }
    return icerde;
}

// Bir nokta herhangi bir Santral poligonunun uzerinde mi (OLT kurali).
// Cagiranlar koordinat dizisi ([lng, lat]) veriyor, GeoJSON nesnesi degil.
function santralUstunde(koordinat) {
    return katmanlar.getLayers().some(l =>
        l._veri.tur === 'Santral' && noktaIcinde(koordinat, halka(l._gj)));
}

const kabinTipleri = (koordinat) => santralUstunde(koordinat) ? ['OLT'] : ['MDU', 'Splitter'];

// Iki dogru parcasi birbirini gercekten kesiyor mu (uc uca degme kesisme sayilmaz)
function parcaKesisiyor(a, b, c, d) {
    const yon = (p, q, r) => Math.sign((q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0]));
    const d1 = yon(a, b, c), d2 = yon(a, b, d), d3 = yon(c, d, a), d4 = yon(c, d, b);
    return d1 !== d2 && d3 !== d4 && d1 !== 0 && d2 !== 0 && d3 !== 0 && d4 !== 0;
}

// Geometri (nokta / cizgi / poligon) sinir poligonunun TAMAMEN icinde mi
function icindeMi(gj, sinirGj) {
    const sinir = halka(sinirGj);
    if (gj.type === 'Point') return noktaIcinde(gj.coordinates, sinir);

    const kose = halka(gj);
    if (!kose.every(k => noktaIcinde(k, sinir))) return false;   // her kose iceride olmali
    for (const [a, b] of kenarlar(kose))                         // hicbir kenar siniri kesmemeli
        for (const [c, d] of kenarlar(sinir))
            if (parcaKesisiyor(a, b, c, d)) return false;
    return true;
}

// Iki poligonun ic alanlari cakisiyor mu (sadece kenar temasi cakisma sayilmaz)
function cakisiyorMu(aGj, bGj) {
    const a = halka(aGj), b = halka(bGj);
    if (a.some(k => noktaIcinde(k, b)) || b.some(k => noktaIcinde(k, a))) return true;
    for (const [p, q] of kenarlar(a))
        for (const [r, s] of kenarlar(b))
            if (parcaKesisiyor(p, q, r, s)) return true;
    return false;
}

// ===== Bildirim (toast) + onay kutusu - Notiflix =====
Notiflix.Notify.init({
    position: 'right-top',
    success: { background: '#2ecc71' },
    failure: { background: '#e74c3c' },
    info: { background: '#333' }
});
Notiflix.Confirm.init({
    titleColor: '#e74c3c',
    okButtonBackground: '#e74c3c',
    borderRadius: '10px'
});

// tip: 'bilgi' | 'basari' | 'hata'
function bildirGoster(mesaj, tip = 'bilgi') {
    const f = {
        basari: Notiflix.Notify.success,
        hata: Notiflix.Notify.failure,
        bilgi: Notiflix.Notify.info
    }[tip] || Notiflix.Notify.info;
    f(mesaj);
}

// Onay kutusu (silme işlemindeki) Promise<boolean> doner: onay -> true ; vazgec -> false
function onayIste(mesaj, { onayMetni = 'Sil', vazgecMetni = 'Vazgeç', baslik = 'Onay' } = {}) {
    return new Promise(resolve => {
        Notiflix.Confirm.show(
            baslik, mesaj, onayMetni, vazgecMetni,
            () => resolve(true),
            () => resolve(false)
        );
    });
}

// ===== Tip tablolari =====
const API = {
    Proje: '/api/projeler',
    Menhol: '/api/menholler', Kabin: '/api/kabinler', Santral: '/api/santraller',
    Konut: '/api/konutlar', Fiber: '/api/fiberler'
};

const IKON = {
    Menhol: L.divIcon({ className: 'ikon ikon-menhol', iconSize: [16, 16] }),
    Kabin: L.divIcon({ className: 'ikon ikon-kabin', iconSize: [16, 16] }),
};

const STIL = {
    Santral: { color: '#9b59b6' },
    Konut: { color: '#2ecc71' },
    Fiber: { color: '#e74c3c', weight: 3 },
};

// Popup'ta gosterilecek alanlar + etiketleri
const BILGI = {
    Menhol: { kod: 'Kod', derinlik: 'Derinlik (m)' },
    Kabin: { kod: 'Kod', kabinTipi: 'Tip', kabinKapasitesi: 'Kapasite', bosPort: 'Bos Port' },
    Santral: { kod: 'Kod', kapasite: 'Kapasite', bosPort: 'Bos Port' },
    Konut: { uavtKod: 'UAVT Kod', bbKsayi: 'BBK Sayisi' },
    Fiber: {},
};

// Kayit govdesindeki geometri alani (tipe gore)
const GEO_ALAN = { Menhol: 'konum', Kabin: 'konum', Santral: 'geometri', Konut: 'geometri', Fiber: 'guzergah' };

// Menhol/Kabin/Santral kodunun degistirilemeyen sabit oneki (form disinda tutulur)
const KOD_ONEK = { Menhol: 'MNHL-', Kabin: 'KBN-', Santral: 'SNTR-' };

// Tip basina Geoman cizim sekli + ayari. Toolbar butonu sadece turu soyler.
// Fiber'de finishOn:'snap' -> ikinci kose bir nesneye yapistigi anda cizim biter.
// (Ilk kose yapissa bile Geoman tek koseli cizgiyi bitirmez.)
const CIZIM = {
    Proje: { sekil: 'Polygon', ayar: { pathOptions: { color: 'yellow', weight: 4, dashArray: '6,4' } } },
    Menhol: { sekil: 'Marker', ayar: { markerStyle: { icon: IKON.Menhol } } },
    Kabin: { sekil: 'Marker', ayar: { markerStyle: { icon: IKON.Kabin } } },
    Santral: { sekil: 'Polygon', ayar: { pathOptions: STIL.Santral } },
    Konut: { sekil: 'Polygon', ayar: { pathOptions: STIL.Konut } },
    Fiber: { sekil: 'Line', ayar: { pathOptions: STIL.Fiber, finishOn: 'snap' } },
};

// UAVT adres kodu: 10 haneli
const UAVT = `type="number" min="1000000000" max="9999999999" step="1" title="10 haneli sayi" required`;

// ===== Formlar (tip basina duz HTML) =====
const FORMLAR = {
    Menhol: `
        <label>Kod<br><span class="kod-onek">MNHL-</span><input name="veriDeger" pattern="[A-Z0-9]{7}" maxlength="7" title="7 buyuk harf/rakam" required></label><br>
        <label>Derinlik (m)<br><input name="derinlik" type="number" step="0.1" min="0" max="100" value="1.5" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Kabin: `
        <label>Kod<br><span class="kod-onek">KBN-</span><input name="veriDeger" pattern="[A-Z0-9]{7}" maxlength="7" title="7 buyuk harf/rakam" required></label><br>
        <label>Tip<br>
            <select name="kabinTipi" required>
                <option value="MDU">MDU</option>
                <option value="Splitter">Splitter</option>
                <option value="OLT">OLT</option>
            </select></label><br>
        <label>Kapasite<br>
            <select name="kabinKapasitesi" required>
                <option value="8">8</option>
                <option value="16">16</option>
                <option value="32">32</option>
            </select></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Santral: `
        <label>Kod<br><span class="kod-onek">SNTR-</span><input name="veriDeger" pattern="[A-Z0-9]{7}" maxlength="7" title="7 buyuk harf/rakam" required></label><br>
        <label>Kapasite<br><input name="kapasite" type="number" min="1" max="100000" value="1" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Konut: `
        <label>UAVT Kod<br><input name="uavtKod" ${UAVT}></label><br>
        <label>BBK Sayisi<br><input name="bbKsayi" type="number" min="0" max="100000" value="1" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
    Proje: `
        <label>Proje Adi<br><input name="ad" maxlength="100" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
};

// ===== Proje durumu =====
let aktifProje = null;     // { id, ad, geometri } - su an uzerinde calisilan proje
let projeKatmani = null;   // proje sinirini gosteren, tiklanamaz/silinemez katman
let projeGj = null;        // ayni sinir, GeoJSON: icerik testleri bunun uzerinden

// ===== Cizim durumu =====
// Tek degisken: o an hangi tur cizilliyor. Sekil bilgisi CIZIM tablosunda.
let aktifArac = null;      // 'Proje' | 'Menhol' | 'Kabin' | 'Santral' | 'Konut' | 'Fiber'

// ===== Proje secilmeden nesne araclari kilitli kalir =====
function araclariAc(ac) {
    document.querySelectorAll('[data-tur]:not([data-tur="Proje"])')
        .forEach(b => b.disabled = !ac || !duzenleyebilir);
}

// ===== Arac secimi: onceki cizimi kapat, yenisini ac, vurguyu tasi =====
function aracSec(btn, tur = null) {
    map.pm.disableDraw();          // yarim kalan cizim varsa iptal
    aktifArac = tur;
    document.querySelectorAll('.toolbar-btn').forEach(b => b.classList.remove('active'));
    btn?.classList.add('active');
    if (tur) map.pm.enableDraw(CIZIM[tur].sekil, CIZIM[tur].ayar);
}

// ===== Sunucu hata govdesini coz + bildir =====
// ValidationProblemDetails ({errors}) / ProblemDetails ({title}) / JSON metin / duz metin
function sunucuHatasi(metin) {
    let mesaj;
    try {
        const p = JSON.parse(metin);
        mesaj = typeof p === 'string' ? p
            : p.errors ? Object.values(p.errors).flat().join('\n')
                : (p.title || metin);
    } catch {
        mesaj = metin;
    }
    bildirGoster(mesaj || 'Islem basarisiz.', 'hata');
}

// ===== Tek HTTP giris noktasi =====
// Sadece tasima isi: istegi at, hatayi bildir, govdeyi coz.
// Ne yapilacagina cagri yeri karar verir. Hata -> null ; govdesiz basarili yanit -> true.
async function istek(url, metot = 'GET', govde) {
    const r = await fetch(url, {
        method: metot,
        headers: govde ? { 'Content-Type': 'application/json' } : undefined,
        body: govde ? JSON.stringify(govde) : undefined
    });
    // Oturum dustu: govdeyi cozmeye calisma, giris sayfasina don.
    if (r.status === 401) { location.href = '/Hesap/Giris'; return null; }

    const metin = await r.text();
    if (!r.ok) { sunucuHatasi(metin); return null; }
    return metin ? JSON.parse(metin) : true;   // DELETE govdesiz 200 doner
}

// ===== POST: kaydet, haritaya ekle (SAYFA YENILENMEZ) =====
async function kaydet(tur, govde) {
    if (tur !== 'Proje') {
        if (!aktifProje) { bildirGoster('Once bir proje secin veya cizin.', 'hata'); return null; }
        govde.projeId = aktifProje.id;
    }
    const kayit = await istek(API[tur], 'POST', govde);
    if (!kayit) return null;

    if (tur !== 'Proje') ciz(tur, kayit);   // Proje'nin kendi cizimi projeYukle icinde
    map.closePopup();                       // acik form popup'ini kapat
    return kayit;
}

// ===== PUT: sadece oznitelikler; geometri/konum aynen korunur =====
async function guncelle(tur, eski, govde) {
    govde[GEO_ALAN[tur]] = eski[GEO_ALAN[tur]];
    govde.projeId = aktifProje.id;

    const yeni = await istek(`${API[tur]}/${eski.id}`, 'PUT', govde);
    if (!yeni) return null;

    map.closePopup();
    await yenile();                         // katmanlari sunucudaki guncel haliyle yeniden ciz
    bildirGoster('Kayit guncellendi.', 'basari');
    return yeni;
}

// ===== DELETE: sunucudan sil, katmani (ve bagli fiberleri) haritadan kaldir =====
async function sil(tur, id) {
    if (!await onayIste(`Bu veriyi (${tur}) silmek istediğinize emin misiniz?`)) return;

    // Sunucu reddederse haritaya dokunma.
    if (await istek(`${API[tur]}/${id}`, 'DELETE') === null) return;

    map.closePopup();
    await yenile();   // bagli fiberler ve degisen bos portlar sunucudan gelir
    bildirGoster('Kayit silindi.', 'basari');
}

// ===== Popup icerigi: ilgili bilgiler + Duzenle + Sil (id gosterilmez) =====
function popupIcerik(tur, kayit, katman) {
    const kutu = document.createElement('div');

    // Degerler kullanici girdisi: innerHTML degil textContent (XSS)
    for (const [alan, etiket] of Object.entries(BILGI[tur])) {
        const satir = document.createElement('div');
        const kalin = document.createElement('b');
        kalin.textContent = `${etiket}:`;
        satir.append(kalin, ` ${kayit[alan]}`);
        kutu.append(satir);
    }

    if (!duzenleyebilir) return kutu;          // goruntuleme yetkisi: sadece bilgi

    const butonlar = document.createElement('div');
    butonlar.className = 'popup-butonlar';

    // Duzenle sadece oznitelik formu olan tiplerde (Fiber'in formu yok)
    if (FORMLAR[tur]) {
        const duzenleBtn = document.createElement('button');
        duzenleBtn.type = 'button';
        duzenleBtn.dataset.duzenle = '';
        duzenleBtn.textContent = 'Düzenle';
        duzenleBtn.onclick = () => {
            const orta = katman.getBounds().getCenter();
            formPopupAc(tur, orta, {
                kayit,
                tipler: tur === 'Kabin' ? kabinTipleri([orta.lng, orta.lat]) : undefined
            });
        };
        butonlar.append(duzenleBtn);
    }

    const silBtn = document.createElement('button');
    silBtn.type = 'button';
    silBtn.dataset.sil = '';
    silBtn.textContent = 'Sil';
    silBtn.onclick = () => sil(tur, kayit.id);
    butonlar.append(silBtn);

    kutu.append(butonlar);
    return kutu;
}

// ===== Nesne popup'ini katmana bagla (cizim sirasinda gecici olarak kaldirilir) =====
function popupBagla(katman) {
    const { tur, kayit } = katman._veri;
    katman.bindPopup(() => popupIcerik(tur, kayit, katman));   // Leaflet popup'i kendi konumlandirir
}

// ===== Bir kaydi haritaya ciz =====
function ciz(tur, kayit) {
    const gj = wktToGj(kayit[GEO_ALAN[tur]]);
    // Nokta da poligon/cizgi de tek yoldan: L.geoJSON + pointToLayer (lng/lat cevrimi kutuphanede)
    const katman = L.geoJSON(gj, {
        // bubblingMouseEvents: Leaflet marker'i tiklamayi varsayilan olarak haritaya GECIRMEZ.
        // Geoman kose eklemeyi harita tiklamasindan aldigi icin bir nesnenin uzerine
        // fiber kosesi konulamazdi; bu yuzden acikca acildi (poligonlarda zaten acik).
        pointToLayer: (_ozellik, latlng) =>
            L.marker(latlng, { icon: IKON[tur], bubblingMouseEvents: true }),
        style: STIL[tur]
    });

    katman._veri = { tur, kayit };
    katman._gj = gj;                        // yerlesim kurallari bunun uzerinden bakiyor
    katman.eachLayer(l => {
        // Geoman snap listesi ic katmanlara bakar; _veri'yi ona da tasi (pm:snap -> layerInteractedWith)
        l._veri = katman._veri;
        // Fiberler snap hedefi degil: fiber ustune fiber yapismasin
        if (tur === 'Fiber') l.options.snapIgnore = true;
    });

    popupBagla(katman);
    katman.addTo(katmanlar);

    // Poligonun merkezine gorunmez yapisma hedefi: fiber ucu kenara degil
    // ortaya da yapisabilsin. Gorunmez ve tiklanamaz, sadece snap listesinde.
    if (tur === 'Santral' || tur === 'Konut') {
        const merkez = L.marker(katman.getBounds().getCenter(),
            { opacity: 0, interactive: false, keyboard: false });
        merkez._veri = katman._veri;
        merkezler.addLayer(merkez);
    }
    return katman;
}

// ===== Popup icinde form =====
// kayit verilirse duzenleme (PUT), verilmezse ekleme (POST). Iki akisin tek ortak yeri.
//   ekVeri         : ekleme sirasinda govdeye eklenecek geometri vb.
//   kayit          : duzenleme sirasindaki mevcut kayit
//   sonra          : basarili kayittan sonra calisacak geri cagirma
function formPopupAc(tur, latlng, { ekVeri, kayit, sonra, tipler } = {}) {
    const form = document.createElement('form');
    form.innerHTML = FORMLAR[tur];

    if (tipler)
        form.querySelectorAll('[name="kabinTipi"] option').forEach(opt => { if (!tipler.includes(opt.value)) opt.remove(); });


    // Duzenlemede formu mevcut degerlerle doldur (alan adlari kayit anahtarlariyla ayni)
    if (kayit) form.querySelectorAll('[name]').forEach(inp => {
        if (inp.name === 'veriDeger') inp.value = (kayit.kod || '').replace(KOD_ONEK[tur] || '', '');
        else if (kayit[inp.name] != null) inp.value = kayit[inp.name];
    });

    L.popup().setLatLng(latlng).setContent(form).openOn(map);

    form.querySelector('[data-kaydet]').onclick = async () => {
        if (!form.reportValidity()) return;                     // native HTML5 dogrulama
        const veri = Object.fromEntries(new FormData(form));    // { veriDeger: "...", derinlik: "1.5", ... }
        if (veri.veriDeger != null) {                            // sabit onek + kullanicinin girdigi deger
            veri.kod = (KOD_ONEK[tur] || '') + veri.veriDeger;
            delete veri.veriDeger;
        }
        const yeni = kayit
            ? await guncelle(tur, kayit, veri)
            : await kaydet(tur, Object.assign(veri, ekVeri));
        if (yeni) sonra?.(yeni);
    };
}

// ===== Bir projeyi aktif et: sinirini ciz, nesnelerini yukle, araclari ac =====
function projeYukle(proje) {
    aktifProje = proje;
    katmanlar.clearLayers();
    merkezler.clearLayers();
    if (projeKatmani) map.removeLayer(projeKatmani);

    projeGj = wktToGj(proje.geometri);
    projeKatmani = L.geoJSON(projeGj, {
        style: { color: '#051650', weight: 4, dashArray: '7.5', fillOpacity: 0.03 },
        interactive: false,
        snapIgnore: true                    // proje cizgisine yapisilmaz
    }).addTo(map);
    projeKatmani.eachLayer(l => { l.options.snapIgnore = true; });
    map.fitBounds(projeKatmani.getBounds());

    araclariAc(true);
    document.getElementById('proje-rapor-btn').disabled = false;
    document.getElementById('proje-kapat-btn').disabled = false;
    yukle();
    bildirGoster(`"${proje.ad}" projesi acik.`, 'basari');
}

function projeKapat() {
    aktifProje = null;
    katmanlar.clearLayers();
    merkezler.clearLayers();
    if (projeKatmani) map.removeLayer(projeKatmani);
    projeKatmani = null;
    projeGj = null;
    araclariAc(false);
    aracSec(null);
    document.getElementById('proje-sec').value = '';
    document.getElementById('proje-kapat-btn').disabled = true;
    document.getElementById('proje-rapor-btn').disabled = true;
    bildirGoster('Proje kapatildi.', 'bilgi');
}
document.getElementById('proje-kapat-btn').onclick = projeKapat;

// ===== Maliyet raporu (Grid.js tablo + native <dialog>) =====
const TL = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });

async function projeRapor() {
    if (!aktifProje) return;
    const rapor = await istek(`${API.Proje}/${aktifProje.id}/maliyet`);
    if (rapor) projeRaporGoster(rapor);
}
document.getElementById('proje-rapor-btn').onclick = projeRapor;

// rapor: MaliyetSonucu { kalemler[], iscilikGenelToplam, malzemeGenelToplam, genelToplam }
function projeRaporGoster(rapor) {
    const para = { formatter: TL.format };
    const dlg = document.createElement('dialog');
    dlg.className = 'rapor-dialog';
    const kapat = () => { dlg.close(); dlg.remove(); };
    dlg.addEventListener('cancel', kapat);
    dlg.addEventListener('close', kapat);

    const baslik = Object.assign(document.createElement('h3'), {
        textContent: `"${aktifProje.ad}" — Maliyet Raporu`
    });
    const tablo = document.createElement('div');
    const kapatBtn = Object.assign(document.createElement('button'), {
        textContent: 'Kapat', onclick: kapat
    });
    dlg.append(baslik, tablo, kapatBtn);

    const miktar = k =>
        (k.iscilikOlcusu === k.malzemeOlcusu && k.iscilikOlcusu !== 'adet')
            ? `${k.iscilikCarpani} ${k.iscilikOlcusu}`
            : `${k.adet} adet`;

    new gridjs.Grid({
        columns: ['Nesne', 'Miktar',
            { name: 'İşçilik', ...para }, { name: 'Malzeme', ...para }, { name: 'Toplam', ...para }],
        data: [
            ...rapor.kalemler.map(k => [k.nesneTuru, miktar(k), k.iscilikToplam, k.malzemeToplam, k.toplam]),
            ['GENEL', '', rapor.iscilikGenelToplam, rapor.malzemeGenelToplam, rapor.genelToplam]
        ]
    }).render(tablo);

    document.body.appendChild(dlg);
    dlg.showModal();
}

// ===== Proje secici (toolbar'daki dropdown) =====
async function projeSec() {
    const sec = document.getElementById('proje-sec');
    const liste = await istek(API.Proje) ?? [];
    // new Option: proje adi kullanici girdisi, innerHTML ile basilmaz (XSS)
    sec.replaceChildren(new Option(' Proje secin ', ''),
        ...liste.map(p => new Option(p.ad, p.id)));
    sec.onchange = () => {
        const secili = liste.find(p => p.id === sec.value);
        if (secili) projeYukle(secili);
    };
}
projeSec();

// ===== Aktif projenin kayitli nesnelerini yukle =====
const TURLER = ['Menhol', 'Kabin', 'Santral', 'Konut', 'Fiber'];

async function yukle() {
    if (!aktifProje) return;
    // Bes istek paralel; cizim sirasi TURLER dizisindeki sirayi korur.
    const listeler = await Promise.all(
        TURLER.map(tur => istek(`${API[tur]}?projeId=${aktifProje.id}`)));
    TURLER.forEach((tur, i) => listeler[i]?.forEach(kayit => ciz(tur, kayit)));
}

// ===== Fiber uc noktalari =====
// Geoman yapisma olaylari cizilen katmanda tetiklenir (haritada degil), bu yuzden
// pm:drawstart icinde workingLayer'a baglaniyoruz. pm:snap yukundeki
// layerInteractedWith, kosenin yapistigi katmani verir.
let sonYapisan = null;      // en son yapisilan katman (yapisma bozulunca null)
let fiberUclari = [];       // kose sirasina gore, o koseye yapisilan katman

map.on('pm:drawstart', (e) => {
    // Cizim boyunca nesne popup'lari kapali: bir nesnenin uzerine tiklamak
    // popup acmasin, kose koysun.
    katmanlar.eachLayer(g => g.unbindPopup());

    sonYapisan = null;
    fiberUclari = [];
    if (e.shape !== 'Line') return;
    e.workingLayer.on('pm:snap', (s) => { sonYapisan = s.layerInteractedWith; });
    e.workingLayer.on('pm:unsnap', () => { sonYapisan = null; });
    e.workingLayer.on('pm:vertexadded', () => { fiberUclari.push(sonYapisan); });
});

map.on('pm:drawend', () => katmanlar.eachLayer(popupBagla));

// ===== Toolbar =====
// Her buton sadece turunu soyler; sekil ve ayar CIZIM tablosundan gelir.
document.querySelectorAll('[data-tur]').forEach(b => {
    b.onclick = () => aracSec(b, b.dataset.tur);
});

// Proje cizme butonu proje secilmeden de acik; yetkisi olmayana yine de kapali.
if (!duzenleyebilir) document.querySelector('[data-tur="Proje"]').disabled = true;

// Esc -> aktif cizimi iptal et
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') aracSec(null);
});

// ===== Cizim bitince =====
// Geoman katmani haritaya KENDI ekler; biz sunucudan donen kayitla yeniden cizdigimiz
// icin gelen katmani kaldirmazsak nesne haritada iki kez gorunur.
map.on('pm:create', (e) => {
    const tur = aktifArac;
    aracSec(null);
    e.layer.remove();
    if (!tur) return;

    const gj = e.layer.toGeoJSON().geometry;
    const noktaMi = e.shape === 'Marker';
    const merkez = noktaMi ? e.layer.getLatLng() : e.layer.getBounds().getCenter();

    if (tur === 'Proje') {
        formPopupAc('Proje', merkez, {
            ekVeri: { geometri: gjToWkt(gj) },
            sonra: async (proje) => {
                await projeSec();
                document.getElementById('proje-sec').value = proje.id;
                projeYukle(proje);
            }
        });
        return;
    }

    // 1) Proje siniri: gercek poligon testi (artik cevreleyen dikdortgen degil).
    if (!icindeMi(gj, projeGj))
        return bildirGoster(`${tur} proje alanı dışına eklenemez.`, 'hata');

    // 2) Poligonlarin (Santral / Konut) uzerine nesne konulamaz.
    //    Kabin muaf: poligon ustune konabilir.
    //    Fiber muaf: ucunu bir konut ya da santral uzerinde bitirmek zorunda.
    if (tur !== 'Kabin' && tur !== 'Fiber') {
        const carpisan = katmanlar.getLayers().find(l =>
            ['Santral', 'Konut'].includes(l._veri.tur) &&
            (noktaMi ? noktaIcinde(gj.coordinates, halka(l._gj)) : cakisiyorMu(gj, l._gj)));

        if (carpisan)
            return bildirGoster(`${tur} bir ${carpisan._veri.tur} üzerine eklenemez.`, 'hata');
    }

    if (tur === 'Fiber') return fiberKaydet(gj);

    formPopupAc(tur, merkez, { ekVeri: { [GEO_ALAN[tur]]: gjToWkt(gj) }, tipler: tur == 'Kabin' ? kabinTipleri(gj.coordinates) : null });
});

// Fiber: uc koselerin yapistigi nesneler baslangic/bitis olur.
async function fiberKaydet(gj) {
    const bas = fiberUclari[0]?._veri ?? null;
    const bit = fiberUclari.at(-1)?._veri ?? null;

    // Uc tipi kurallari: bunlar once gelmeli, yoksa asagidaki u.tur null uzerinde patlar.
    if (!bas || !['Menhol', 'Kabin', 'Santral'].includes(bas.tur))
        return bildirGoster('Fiber baslangici bir menhol, kabin veya santral uzerinde olmali.', 'hata');
    if (!bit || bit.tur === 'Fiber')
        return bildirGoster('Fiber bitisi bir nesne veya konut olmali.', 'hata');

    // Bos port on kontrolu: sunucu da bakiyor, bu sadece anlik geri bildirim.
    const dolu = [bas, bit].find(u =>
        ['Kabin', 'Santral'].includes(u.tur) && u.kayit.bosPort <= 0);
    if (dolu) return bildirGoster(`${dolu.kayit.kod} üzerinde boş port kalmadı.`, 'hata');

    const yeni = await kaydet('Fiber', {
        baslangicId: bas.kayit.id,
        bitisId: bit.kayit.id,
        guzergah: gjToWkt(gj)
    });
    if (yeni) await yenile();   // iki ucun bos port sayisi degisti
}
async function yenile() {
    katmanlar.clearLayers();
    merkezler.clearLayers();
    await yukle();
}
