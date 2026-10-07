// ===== Harita =====
const map = L.map('map').setView([39.9588, 32.8665], 15);
map.zoomControl.setPosition('bottomleft');

L.tileLayer('http://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}', {
    maxZoom: 20,
    subdomains: ['mt0', 'mt1', 'mt2', 'mt3'],
    attribution: '&copy; Google Maps'
}).addTo(map);

// ===== Yetki =====
// Kurallar sunucuda (Services/ProjeKurallari.cs); arayüz sadece sunucunun söylediğini uygular:
//   proje oluşturma            -> sayfa açılırken gelen data-proje-olusturabilir
//   projenin içindeki işlemler -> GET /api/projeler/{id} yanıtındaki izinler
// Bu sadece arayüzü kısıtlar, asıl engel sunucudadır.
const projeOlusturabilir = document.getElementById('map').dataset.projeOlusturabilir === 'true';
const nesneDuzenleyebilir = () => !!aktifProje?.izinler?.nesneDuzenleyebilir;

const katmanlar = L.layerGroup().addTo(map);   // aktif projenin nesneleri

// Poligonların (Santral, Konut) merkezindeki görünmez yapışma hedefleri: fiber ucu kenara olduğu gibi
// ortaya da yapışabilsin. Geoman yapışma listesini haritadaki katmanlardan kurduğu için haritaya eklenmek zorunda.
const merkezler = L.layerGroup().addTo(map);

// ===== Geoman (çizim) =====
// Yapışma her çizimde açık. Fiberler yapışma hedefi değil (ciz() içinde snapIgnore).
// Geoman'ın kendi araç çubuğu eklenmez; çizim modları toolbar'dan programla açılır.
const SNAP_TOLERANS = 20;   // piksel: köşe bu kadar yakınsa nesneye yapışır

map.pm.setLang('tr');
map.pm.setGlobalOptions({ snappable: true, snapDistance: SNAP_TOLERANS });

// ===== Yazdırma (leaflet.browser.print) =====
// Eklentinin kendi kontrolü eklenmez; toolbar'daki Yazdır butonu yazdırmayı programla başlatır (bkz. projeYazdir).
const yazici = L.browserPrint(map, { closePopupsOnPrint: true });

// ===== WKT <-> GeoJSON =====
const gjToWkt = (g) => wellknown.stringify(g);
const wktToGj = (w) => wellknown.parse(w);

// ===== Yerleşim kuralları =====
// Koordinatlar GeoJSON düzeninde: [lng, lat]. Sunucu karşılığı Services/GeometriDenetimi.cs;
// buradaki kontrol anlık geri bildirim içindir, son söz sunucudadır.

// Poligonun dış halkası ya da çizginin köşe dizisi
const halka = (gj) => gj.type === 'Polygon' ? gj.coordinates[0] : gj.coordinates;

// Ardışık köşe çiftleri (poligon halkası kapalı geldiği için bütün kenarları verir)
function* kenarlar(kose) {
    for (let i = 0; i < kose.length - 1; i++) yield [kose[i], kose[i + 1]];
}

// Nokta poligonun içinde mi (ışın atma)
function noktaIcinde([x, y], kose) {
    let icerde = false;
    for (let i = 0, j = kose.length - 1; i < kose.length; j = i++) {
        const [xi, yi] = kose[i], [xj, yj] = kose[j];
        if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) icerde = !icerde;
    }
    return icerde;
}

// Nokta herhangi bir santralin üzerinde mi (OLT kuralı). Parametre koordinat dizisi: [lng, lat].
function santralUstunde(koordinat) {
    return katmanlar.getLayers().some(l =>
        l._veri.tur === 'Santral' && noktaIcinde(koordinat, halka(l._gj)));
}

// Santral üzerindeki kabin sadece OLT olabilir, dışındakiler OLT olamaz
const kabinTipleri = (koordinat) => santralUstunde(koordinat) ? ['OLT'] : ['MDU', 'Splitter'];

// İki doğru parçası birbirini gerçekten kesiyor mu (uç uca değme kesişme sayılmaz)
function parcaKesisiyor(a, b, c, d) {
    const yon = (p, q, r) => Math.sign((q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0]));
    const d1 = yon(a, b, c), d2 = yon(a, b, d), d3 = yon(c, d, a), d4 = yon(c, d, b);
    return d1 !== d2 && d3 !== d4 && d1 !== 0 && d2 !== 0 && d3 !== 0 && d4 !== 0;
}

// Geometri (nokta, çizgi, poligon) sınır poligonunun tamamen içinde mi
function icindeMi(gj, sinirGj) {
    const sinir = halka(sinirGj);
    if (gj.type === 'Point') return noktaIcinde(gj.coordinates, sinir);

    const kose = halka(gj);
    if (!kose.every(k => noktaIcinde(k, sinir))) return false;   // her köşe içeride olmalı
    for (const [a, b] of kenarlar(kose))                         // hiçbir kenar sınırı kesmemeli
        for (const [c, d] of kenarlar(sinir))
            if (parcaKesisiyor(a, b, c, d)) return false;
    return true;
}

// İki poligonun iç alanları çakışıyor mu (sadece kenar teması çakışma sayılmaz)
function cakisiyorMu(aGj, bGj) {
    const a = halka(aGj), b = halka(bGj);
    if (a.some(k => noktaIcinde(k, b)) || b.some(k => noktaIcinde(k, a))) return true;
    for (const [p, q] of kenarlar(a))
        for (const [r, s] of kenarlar(b))
            if (parcaKesisiyor(p, q, r, s)) return true;
    return false;
}

// ===== Bildirim (Notiflix) =====
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

const BILDIRIM = { basari: Notiflix.Notify.success, hata: Notiflix.Notify.failure, bilgi: Notiflix.Notify.info };

// tip: 'bilgi' | 'basari' | 'hata'
function bildirGoster(mesaj, tip = 'bilgi') {
    (BILDIRIM[tip] || Notiflix.Notify.info)(mesaj);
}

// Onay kutusu. Onaylanırsa true, vazgeçilirse false döner.
function onayIste(mesaj, { onayMetni = 'Sil', vazgecMetni = 'Vazgeç', baslik = 'Onay' } = {}) {
    return new Promise(resolve => {
        Notiflix.Confirm.show(
            baslik, mesaj, onayMetni, vazgecMetni,
            () => resolve(true),
            () => resolve(false)
        );
    });
}

// Gerekçe soran pencere. Vazgeçilirse null döner.
function notIste(baslik) {
    return new Promise(resolve => Notiflix.Confirm.prompt(
        baslik, 'Gerekçe yazın:', '', 'Tamam', 'Vazgeç',
        cevap => resolve(cevap),
        () => resolve(null)));
}

// ===== Tip tabloları =====
const API = {
    Proje: '/api/projeler',
    Menhol: '/api/menholler', Kabin: '/api/kabinler', Santral: '/api/santraller',
    Konut: '/api/konutlar', Fiber: '/api/fiberler'
};

// Proje içindeki nesne türleri (çizim sırası bu sıradır)
const TURLER = ['Menhol', 'Kabin', 'Santral', 'Konut', 'Fiber'];

// Durum ve işlem adları -> ekranda görünen metin
const DURUM_ETIKET = { Planlama: 'Planlama', OnayBekliyor: 'Onay bekliyor', Onaylandi: 'Onaylandı' };
const ISLEM_ETIKET = {
    OnayaGonder: 'Onaya gönder', GeriCek: 'Geri çek', Onayla: 'Onayla', Reddet: 'Reddet',
    PlanlamayaAl: 'Planlamaya al', Olustur: 'Oluşturuldu', Degistir: 'Değişiklik', Sil: 'Silindi'
};

// Gerekçe sorulacak işlemler (sadece hangi pencerenin açılacağını belirler; kuralı sunucu uygular)
const NOT_GEREKEN = ['Reddet', 'PlanlamayaAl'];

const IKON = {
    Menhol: L.divIcon({ className: 'ikon ikon-menhol', iconSize: [16, 16] }),
    Kabin: L.divIcon({ className: 'ikon ikon-kabin', iconSize: [16, 16] }),
};

const STIL = {
    Santral: { color: '#9b59b6' },
    Konut: { color: '#2ecc71' },
    Fiber: { color: '#e74c3c', weight: 3 },
};

// Popup'ta gösterilecek alanlar ve etiketleri
const BILGI = {
    Menhol: { kod: 'Kod', derinlik: 'Derinlik (m)' },
    Kabin: { kod: 'Kod', kabinTipi: 'Tip', kabinKapasitesi: 'Kapasite', bosPort: 'Bos Port' },
    Santral: { kod: 'Kod', kapasite: 'Kapasite', bosPort: 'Bos Port' },
    Konut: { uavtKod: 'UAVT Kod', bbKsayi: 'BBK Sayisi' },
    Fiber: {},
};

// Kayıt gövdesindeki geometri alanının adı
const GEO_ALAN = { Menhol: 'konum', Kabin: 'konum', Santral: 'geometri', Konut: 'geometri', Fiber: 'guzergah' };

// Menhol, Kabin ve Santral kodunun değiştirilemeyen sabit öneki (formda ayrı gösterilir)
const KOD_ONEK = { Menhol: 'MNHL-', Kabin: 'KBN-', Santral: 'SNTR-' };

// Tür başına Geoman çizim şekli ve ayarı; toolbar butonu sadece türü söyler.
// Fiber'de finishOn:'snap': ikinci köşe bir nesneye yapıştığı anda çizim biter
// (ilk köşe yapışsa bile Geoman tek köşeli çizgiyi bitirmez).
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

// ===== Formlar (tür başına düz HTML) =====
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
        <label>Proje Adi<br><input name="projeAdi" maxlength="100" required></label><br>
        <button type="button" data-kaydet>Kaydet</button>`,
};

// ===== Durum =====
let aktifProje = null;     // GET /api/projeler/{id} yanıtı: { id, projeAdi, geometri, durum, redNotu, izinler, ... }
let projeKatmani = null;   // proje sınırını gösteren, tıklanamaz katman
let projeGj = null;        // aynı sınır, GeoJSON: içerik testleri bunun üzerinden
let aktifArac = null;      // o an çizilen tür: 'Proje' | 'Menhol' | 'Kabin' | 'Santral' | 'Konut' | 'Fiber'

// ===== Arayüz: butonlar ve durum şeridi aktif projeye göre ayarlanır =====
function arayuzuGuncelle() {
    // Nesne araçları: proje açık ve düzenleme izni varsa
    document.querySelectorAll('[data-tur]:not([data-tur="Proje"])')
        .forEach(b => b.disabled = !nesneDuzenleyebilir());
    // Rapor, yazdır, kapat: proje açıksa
    ['proje-rapor-btn', 'proje-yazdir-btn', 'proje-kapat-btn']
        .forEach(id => document.getElementById(id).disabled = !aktifProje);
    seritGuncelle();
}

// ===== Araç seçimi: önceki çizimi kapat, yenisini aç, vurguyu taşı =====
function aracSec(btn, tur = null) {
    map.pm.disableDraw();   // yarım kalan çizim varsa iptal
    aktifArac = tur;
    document.querySelectorAll('.toolbar-btn').forEach(b => b.classList.remove('active'));
    btn?.classList.add('active');
    if (tur) map.pm.enableDraw(CIZIM[tur].sekil, CIZIM[tur].ayar);
}

// ===== Sunucu hatası: gövdeyi çözüp bildirir =====
// ValidationProblemDetails ({errors}), ProblemDetails ({title}), JSON metin ya da düz metin gelebilir
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

// ===== Tek HTTP giriş noktası =====
// Sadece taşıma: isteği atar, hatayı bildirir, gövdeyi çözer; ne yapılacağına çağıran karar verir.
// Hata -> null, gövdesiz başarılı yanıt -> true.
//   sessiz: hata bildirimi gösterme (sonucu çağıran kendisi yorumlar)
async function istek(url, metot = 'GET', govde, { sessiz = false } = {}) {
    const r = await fetch(url, {
        method: metot,
        headers: govde ? { 'Content-Type': 'application/json' } : undefined,
        body: govde ? JSON.stringify(govde) : undefined
    });
    // Oturum düştü: giriş sayfasına dön
    if (r.status === 401) { location.href = '/Hesap/Giris'; return null; }

    const metin = await r.text();
    if (!r.ok) {
        if (!sessiz) sunucuHatasi(metin);
        // 409: proje başka bir oturumda kilitlendi (onaya gönderildi ya da onaylandı); ekranı sunucuyla eşitle
        if (r.status === 409 && aktifProje) aktifProjeyiTazele();
        return null;
    }
    return metin ? JSON.parse(metin) : true;   // DELETE gövdesiz 200 döner
}

// ===== POST: kaydet ve haritaya ekle (sayfa yenilenmez) =====
async function kaydet(tur, govde) {
    if (tur !== 'Proje') {
        if (!aktifProje) { bildirGoster('Once bir proje secin veya cizin.', 'hata'); return null; }
        govde.projeId = aktifProje.id;
    }
    const kayit = await istek(API[tur], 'POST', govde);
    if (!kayit) return null;

    if (tur !== 'Proje') ciz(tur, kayit);   // projenin kendi çizimi projeYukle içinde
    map.closePopup();
    return kayit;
}

// ===== PUT: sadece öznitelikler; geometri ve konum aynen korunur =====
async function guncelle(tur, eski, govde) {
    govde[GEO_ALAN[tur]] = eski[GEO_ALAN[tur]];
    govde.projeId = aktifProje.id;

    const yeni = await istek(`${API[tur]}/${eski.id}`, 'PUT', govde);
    if (!yeni) return null;

    map.closePopup();
    await yenile();   // katmanlar sunucudaki güncel haliyle yeniden çizilir
    bildirGoster('Kayit guncellendi.', 'basari');
    return yeni;
}

// ===== DELETE: sunucudan sil, haritayı yenile =====
async function sil(tur, id) {
    if (!await onayIste(`Bu veriyi (${tur}) silmek istediğinize emin misiniz?`)) return;

    // Sunucu reddederse haritaya dokunulmaz
    if (await istek(`${API[tur]}/${id}`, 'DELETE') === null) return;

    map.closePopup();
    await yenile();   // bağlı fiberler ve değişen boş portlar sunucudan gelir
    bildirGoster('Kayit silindi.', 'basari');
}

// ===== Popup içeriği: bilgiler, Düzenle ve Sil =====
function popupButonu(metin, oznitelik, onclick) {
    const b = Object.assign(document.createElement('button'), { type: 'button', textContent: metin, onclick });
    b.dataset[oznitelik] = '';   // css: .popup-butonlar [data-duzenle] ve [data-sil]
    return b;
}

function popupIcerik(tur, kayit, katman) {
    const kutu = document.createElement('div');

    // Değerler kullanıcı girdisi: innerHTML değil textContent (XSS)
    for (const [alan, etiket] of Object.entries(BILGI[tur])) {
        const satir = document.createElement('div');
        const kalin = document.createElement('b');
        kalin.textContent = `${etiket}:`;
        satir.append(kalin, ` ${kayit[alan]}`);
        kutu.append(satir);
    }

    if (!nesneDuzenleyebilir()) return kutu;   // kilitli proje ya da yetki yok: sadece bilgi

    const butonlar = document.createElement('div');
    butonlar.className = 'popup-butonlar';

    // Düzenle sadece öznitelik formu olan türlerde (fiberin formu yok)
    if (FORMLAR[tur])
        butonlar.append(popupButonu('Düzenle', 'duzenle', () => {
            const orta = katman.getBounds().getCenter();
            formPopupAc(tur, orta, {
                kayit,
                tipler: tur === 'Kabin' ? kabinTipleri([orta.lng, orta.lat]) : undefined
            });
        }));
    butonlar.append(popupButonu('Sil', 'sil', () => sil(tur, kayit.id)));

    kutu.append(butonlar);
    return kutu;
}

// Nesne popup'ını katmana bağlar (çizim sırasında geçici olarak kaldırılır)
function popupBagla(katman) {
    const { tur, kayit } = katman._veri;
    katman.bindPopup(() => popupIcerik(tur, kayit, katman));
}

// ===== Bir kaydı haritaya çiz =====
function ciz(tur, kayit) {
    const gj = wktToGj(kayit[GEO_ALAN[tur]]);
    // Nokta, çizgi ve poligon tek yoldan: L.geoJSON + pointToLayer (lng/lat çevrimi kütüphanede)
    const katman = L.geoJSON(gj, {
        // Leaflet marker tıklamasını varsayılan olarak haritaya geçirmez. Geoman köşeyi harita tıklamasından
        // aldığı için nesnenin üzerine fiber köşesi konamazdı; bu yüzden açıkça açıldı (poligonlarda zaten açık).
        pointToLayer: (_ozellik, latlng) =>
            L.marker(latlng, { icon: IKON[tur], bubblingMouseEvents: true }),
        style: STIL[tur]
    });

    katman._veri = { tur, kayit };
    katman._gj = gj;   // yerleşim kuralları bunun üzerinden bakar
    katman.eachLayer(l => {
        // Geoman'ın yapışma listesi iç katmanlara bakar; _veri ona da taşınır (pm:snap -> layerInteractedWith)
        l._veri = katman._veri;
        // Fiberler yapışma hedefi değil: fiber üstüne fiber yapışmasın
        if (tur === 'Fiber') l.options.snapIgnore = true;
    });

    popupBagla(katman);
    katman.addTo(katmanlar);

    // Poligonun merkezine görünmez yapışma hedefi (tıklanamaz, sadece yapışma listesinde)
    if (tur === 'Santral' || tur === 'Konut') {
        const merkez = L.marker(katman.getBounds().getCenter(),
            { opacity: 0, interactive: false, keyboard: false });
        merkez._veri = katman._veri;
        merkezler.addLayer(merkez);
    }
    return katman;
}

// ===== Popup içinde form =====
// kayit verilirse düzenleme (PUT), verilmezse ekleme (POST); iki akışın tek ortak yeri.
//   ekVeri : ekleme sırasında gövdeye eklenecek geometri vb.
//   kayit  : düzenlenen mevcut kayıt
//   sonra  : başarılı kayıttan sonra çalışacak geri çağırma
//   tipler : kabin tipi listesinde bırakılacak seçenekler
function formPopupAc(tur, latlng, { ekVeri, kayit, sonra, tipler } = {}) {
    const form = document.createElement('form');
    form.innerHTML = FORMLAR[tur];

    if (tipler)
        form.querySelectorAll('[name="kabinTipi"] option').forEach(opt => { if (!tipler.includes(opt.value)) opt.remove(); });

    // Düzenlemede form mevcut değerlerle doldurulur (alan adları kayıt anahtarlarıyla aynı)
    if (kayit) form.querySelectorAll('[name]').forEach(inp => {
        if (inp.name === 'veriDeger') inp.value = (kayit.kod || '').replace(KOD_ONEK[tur] || '', '');
        else if (kayit[inp.name] != null) inp.value = kayit[inp.name];
    });

    L.popup().setLatLng(latlng).setContent(form).openOn(map);

    const kaydetBtn = form.querySelector('[data-kaydet]');
    kaydetBtn.onclick = async () => {
        if (!form.reportValidity()) return;                    // HTML5 doğrulaması
        const veri = Object.fromEntries(new FormData(form));   // { veriDeger: "...", derinlik: "1.5", ... }
        if (veri.veriDeger != null) {                          // sabit önek + kullanıcının girdiği değer
            veri.kod = (KOD_ONEK[tur] || '') + veri.veriDeger;
            delete veri.veriDeger;
        }
        // İstek sürerken buton kapalı: çift tıklama aynı kaydı iki kez oluşturmasın
        kaydetBtn.disabled = true;
        try {
            const yeni = kayit
                ? await guncelle(tur, kayit, veri)
                : await kaydet(tur, Object.assign(veri, ekVeri));
            if (yeni) sonra?.(yeni);
        } finally {
            kaydetBtn.disabled = false;   // hata olduysa kullanıcı düzeltip tekrar deneyebilsin
        }
    };
}

// ===== Proje aç ve kapat =====
function nesneleriTemizle() {
    katmanlar.clearLayers();
    merkezler.clearLayers();
}

// Projeyi aktif eder: sınırını çizer, nesnelerini yükler, araçları açar
function projeYukle(proje) {
    aktifProje = proje;
    nesneleriTemizle();
    if (projeKatmani) map.removeLayer(projeKatmani);

    projeGj = wktToGj(proje.geometri);
    projeKatmani = L.geoJSON(projeGj, {
        style: { color: '#051650', weight: 4, dashArray: '7.5', fillOpacity: 0.03 },
        interactive: false,
        snapIgnore: true   // proje çizgisine yapışılmaz
    }).addTo(map);
    projeKatmani.eachLayer(l => { l.options.snapIgnore = true; });
    map.fitBounds(projeKatmani.getBounds());

    arayuzuGuncelle();
    yukle();
    bildirGoster(`"${proje.projeAdi}" projesi acik.`, 'basari');
}

// sessiz: başka bir akışın parçasıyken (proje silindi ya da görünmez oldu) ayrıca "kapatıldı" bildirimi çıkmasın
function projeKapat(sessiz = false) {
    aktifProje = null;
    nesneleriTemizle();
    if (projeKatmani) map.removeLayer(projeKatmani);
    projeKatmani = null;
    projeGj = null;
    aracSec(null);
    document.getElementById('proje-sec').value = '';
    arayuzuGuncelle();
    if (!sessiz) bildirGoster('Proje kapatildi.', 'bilgi');
}
// Ok fonksiyonu şart: onclick olay nesnesini ilk parametre olarak verir, o da sessiz = true sayılırdı
document.getElementById('proje-kapat-btn').onclick = () => projeKapat();

// Açık projeyi sunucuyla eşitler; durum başka bir oturumda değişmiş olabilir.
// Çağrıldığı yerler: durum işlemlerinden sonra, 409 alınınca, sekmeye geri dönülünce.
async function aktifProjeyiTazele() {
    if (!aktifProje) return;
    const { id, durum: eskiDurum } = aktifProje;
    const proje = await istek(`${API.Proje}/${id}`, 'GET', undefined, { sessiz: true });
    if (aktifProje?.id !== id) return;   // beklerken proje kapatıldı ya da başka proje açıldı

    if (!proje) {   // artık görünmüyor: örn. görüntüleyici açıkken proje Planlama'ya alındı
        projeKapat(true);
        bildirGoster('Bu proje artık görüntülenemiyor.', 'bilgi');
        return projeListesi();
    }

    aktifProje = proje;
    arayuzuGuncelle();
    if (proje.durum !== eskiDurum) {
        aracSec(null);       // yarım çizim varsa iptal: artık izni olmayabilir
        map.closePopup();    // açık popup eski izinlerle kurulmuştu
        projeListesi();      // listedeki durum etiketi de eskidi
        await yenile();      // durum değiştiyse içerik de değişmiş olabilir
    }
}

// Sekmeye geri dönülünce liste ve açık proje güncellenir
document.addEventListener('visibilitychange', async () => {
    if (document.visibilityState !== 'visible') return;
    await aktifProjeyiTazele();
    projeListesi();
});

// ===== Durum şeridi: rozet, izinli işlemler, geçmiş ve (yöneticiye) projeyi sil =====
function dugme(metin, onclick, sinif = '') {
    return Object.assign(document.createElement('button'), {
        type: 'button', className: `durum-btn ${sinif}`, textContent: metin, onclick
    });
}

function seritGuncelle() {
    const serit = document.getElementById('durum-seridi');
    serit.hidden = !aktifProje;
    if (!aktifProje) return serit.replaceChildren();

    const { durum, redNotu, izinler } = aktifProje;
    const rozet = Object.assign(document.createElement('span'), {
        className: `durum-rozet durum-${durum}`,
        textContent: DURUM_ETIKET[durum] ?? durum
    });
    if (durum === 'Planlama' && redNotu) {
        rozet.textContent += ' · reddedildi';
        rozet.title = `Red notu: ${redNotu}`;   // title düz metindir, XSS yok
    }

    // Butonlar sunucunun izin verdiği işlemlerden üretilir: JS hiçbir kuralı bilmez
    serit.replaceChildren(
        rozet,
        ...izinler.islemler.map(i => dugme(ISLEM_ETIKET[i] ?? i, () => islemYap(i), `islem-${i}`)),
        dugme('Geçmiş', projeGecmisi),
        ...(izinler.silebilir ? [dugme('Projeyi sil', projeSil, 'tehlike')] : []));
}

// Durum işlemi: gerekçe isteyen işlemde not penceresi, diğerlerinde evet/hayır onayı
async function islemYap(islem) {
    const etiket = ISLEM_ETIKET[islem] ?? islem;
    let not = null;

    if (NOT_GEREKEN.includes(islem)) {
        not = (await notIste(etiket))?.trim();
        if (!not) return bildirGoster('Gerekçe yazılmadığı için işlem yapılmadı.', 'bilgi');
    } else if (!await onayIste(`"${aktifProje.projeAdi}": ${etiket}?`, { onayMetni: etiket, baslik: etiket })) {
        return;
    }

    const sonuc = await istek(`${API.Proje}/${aktifProje.id}/islem`, 'POST', { islem, not });
    // Başarılı da olsa hatalı da olsa eşitlenir: hata sebebi durumun başka oturumda değişmesi olabilir
    await aktifProjeyiTazele();
    if (sonuc) bildirGoster(`${etiket}: tamamlandı.`, 'basari');
}

async function projeSil() {
    const ad = aktifProje.projeAdi;
    if (!await onayIste(`"${ad}" projesi ve içindeki bütün nesneler silinecek.`, { baslik: 'Projeyi sil' })) return;
    if (await istek(`${API.Proje}/${aktifProje.id}`, 'DELETE') === null) return;
    projeKapat(true);
    await projeListesi();
    bildirGoster(`"${ad}" silindi.`, 'basari');
}

// ===== Maliyet raporu, geçmiş ve yazdırma =====
const TL = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
const TARIH = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' });

async function projeRapor() {
    if (!aktifProje) return;
    const rapor = await istek(`${API.Proje}/${aktifProje.id}/maliyet`);
    if (rapor) projeRaporGoster(rapor);
}
document.getElementById('proje-rapor-btn').onclick = projeRapor;

function projeYazdir() {
    if (!aktifProje) return;
    yazici.print(L.BrowserPrint.Mode.Auto('A4', { margin: 10 }));
}
document.getElementById('proje-yazdir-btn').onclick = projeYazdir;

// Grid.js tablosu içeren yerel <dialog>; maliyet raporu ve geçmiş aynı kabuğu kullanır
function tabloDialogu(baslik, gridAyari) {
    const dlg = document.createElement('dialog');
    dlg.className = 'rapor-dialog';
    const kapat = () => { dlg.close(); dlg.remove(); };
    dlg.addEventListener('cancel', kapat);
    dlg.addEventListener('close', kapat);

    const tablo = document.createElement('div');
    dlg.append(
        Object.assign(document.createElement('h3'), { textContent: baslik }),
        tablo,
        Object.assign(document.createElement('button'), { textContent: 'Kapat', onclick: kapat }));

    new gridjs.Grid({ language: { noRecordsFound: 'Kayıt yok' }, ...gridAyari }).render(tablo);
    document.body.appendChild(dlg);
    dlg.showModal();
}

// rapor: { kaynak, onaylayanAdi, onayTarihi, kalemler[], iscilikGenelToplam, malzemeGenelToplam, genelToplam }
function projeRaporGoster(rapor) {
    const para = { formatter: TL.format };
    const miktar = k =>
        (k.iscilikOlcusu === k.malzemeOlcusu && k.iscilikOlcusu !== 'adet')
            ? `${k.iscilikCarpani} ${k.iscilikOlcusu}`
            : `${k.adet} adet`;

    // Onaylı projede rapor onay anındaki kopyadan gelir: birim fiyatlar sonradan değişse de aynı kalır
    const kaynak = rapor.kaynak === 'onay'
        ? ` (onay anı: ${rapor.onaylayanAdi}, ${TARIH.format(new Date(rapor.onayTarihi))})`
        : '';

    tabloDialogu(`"${aktifProje.projeAdi}" — Maliyet Raporu${kaynak}`, {
        columns: ['Nesne', 'Miktar',
            { name: 'İşçilik', ...para }, { name: 'Malzeme', ...para }, { name: 'Toplam', ...para }],
        data: [
            ...rapor.kalemler.map(k => [k.nesneTuru, miktar(k), k.iscilikToplam, k.malzemeToplam, k.toplam]),
            ['GENEL', '', rapor.iscilikGenelToplam, rapor.malzemeGenelToplam, rapor.genelToplam]
        ]
    });
}

// Tarihler sunucudan UTC ("...Z") gelir; Intl yerel saate çevirir
async function projeGecmisi() {
    const liste = await istek(`${API.Proje}/${aktifProje.id}/gecmis`);
    if (!liste) return;
    tabloDialogu(`"${aktifProje.projeAdi}" — Geçmiş`, {
        columns: ['Tarih', 'İşlem', 'Kullanıcı', 'Not'],
        data: liste.map(g => [TARIH.format(new Date(g.tarih)), ISLEM_ETIKET[g.islem] ?? g.islem, g.kullaniciAdi, g.not ?? '']),
        fixedHeader: true,
        height: '360px'
    });
}

// ===== Proje seçici (toolbar'daki açılır liste) =====
// Listeyi sunucudan yeniden kurar; açık proje seçili kalır. Onay bekleyenler sunucudan en üstte gelir.
async function projeListesi() {
    const sec = document.getElementById('proje-sec');
    const liste = await istek(API.Proje) ?? [];
    // Proje adı kullanıcı girdisi: new Option kullanılır, innerHTML ile basılmaz (XSS)
    sec.replaceChildren(
        new Option(liste.length ? ' Proje secin ' : ' Proje yok ', ''),
        ...liste.map(p => new Option(`${p.projeAdi} · ${DURUM_ETIKET[p.durum] ?? p.durum}`, p.id)));
    sec.value = aktifProje?.id ?? '';
}

// Proje her açılışta sunucudan taze okunur: sayfa açıldıktan sonra durumu değişmiş olabilir
async function projeAc(id) {
    const proje = await istek(`${API.Proje}/${id}`);
    if (!proje) return projeListesi();   // bu arada görünmez olmuş ya da silinmiş
    projeYukle(proje);
    if (proje.durum === 'Planlama' && proje.redNotu)
        Notiflix.Report.warning('Proje reddedildi', proje.redNotu, 'Tamam', { messageMaxLength: 500 });
}

document.getElementById('proje-sec').onchange = (e) => { if (e.target.value) projeAc(e.target.value); };
projeListesi();

// ===== Aktif projenin kayıtlı nesnelerini yükle =====
async function yukle() {
    if (!aktifProje) return;
    // Beş istek paralel; çizim sırası TURLER dizisindeki sırayı korur
    const listeler = await Promise.all(
        TURLER.map(tur => istek(`${API[tur]}?projeId=${aktifProje.id}`)));
    TURLER.forEach((tur, i) => listeler[i]?.forEach(kayit => ciz(tur, kayit)));
}

// Katmanları temizleyip sunucudaki güncel halleriyle yeniden yükler
async function yenile() {
    nesneleriTemizle();
    await yukle();
}

// ===== Fiber uç noktaları =====
// Geoman yapışma olayları çizilen katmanda tetiklenir (haritada değil); bu yüzden pm:drawstart içinde
// workingLayer'a bağlanılır. pm:snap yükündeki layerInteractedWith, köşenin yapıştığı katmanı verir.
let sonYapisan = null;   // en son yapışılan katman (yapışma bozulunca null)
let fiberUclari = [];    // köşe sırasına göre, o köşeye yapışılan katman

map.on('pm:drawstart', (e) => {
    // Çizim boyunca nesne popup'ları kapalı: nesnenin üzerine tıklamak popup açmasın, köşe koysun
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
// Her buton sadece türünü söyler; şekil ve ayar CIZIM tablosundan gelir.
document.querySelectorAll('[data-tur]').forEach(b => {
    b.onclick = () => aracSec(b, b.dataset.tur);
});

// Proje çizme butonu proje seçilmeden de açık; yetkisi olmayana kapalı
if (!projeOlusturabilir) document.querySelector('[data-tur="Proje"]').disabled = true;

// Esc: aktif çizimi iptal et
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') aracSec(null);
});

// ===== Çizim bitince =====
// Geoman katmanı haritaya kendi ekler; sunucudan dönen kayıtla yeniden çizdiğimiz için
// kaldırmazsak nesne haritada iki kez görünür.
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
                await projeAc(proje.id);   // POST yanıtı izinleri içermez; detay sunucudan okunur
                await projeListesi();
            }
        });
        return;
    }

    // 1) Proje sınırının içinde olmalı
    if (!icindeMi(gj, projeGj))
        return bildirGoster(`${tur} proje alanı dışına eklenemez.`, 'hata');

    // 2) Poligonların (Santral, Konut) üzerine nesne konamaz.
    //    Kabin muaf. Fiber de muaf: ucunu bir konut ya da santral üzerinde bitirmek zorunda.
    if (tur !== 'Kabin' && tur !== 'Fiber') {
        const carpisan = katmanlar.getLayers().find(l =>
            ['Santral', 'Konut'].includes(l._veri.tur) &&
            (noktaMi ? noktaIcinde(gj.coordinates, halka(l._gj)) : cakisiyorMu(gj, l._gj)));

        if (carpisan)
            return bildirGoster(`${tur} bir ${carpisan._veri.tur} üzerine eklenemez.`, 'hata');
    }

    if (tur === 'Fiber') return fiberKaydet(gj);

    formPopupAc(tur, merkez, {
        ekVeri: { [GEO_ALAN[tur]]: gjToWkt(gj) },
        tipler: tur === 'Kabin' ? kabinTipleri(gj.coordinates) : undefined
    });
});

// Fiber: uç köşelerin yapıştığı nesneler başlangıç ve bitiş olur
async function fiberKaydet(gj) {
    const bas = fiberUclari[0]?._veri ?? null;
    const bit = fiberUclari.at(-1)?._veri ?? null;

    // Uç türü kuralları önce gelmeli, yoksa aşağıdaki u.tur null üzerinde patlar
    if (!bas || !['Menhol', 'Kabin', 'Santral'].includes(bas.tur))
        return bildirGoster('Fiber baslangici bir menhol, kabin veya santral uzerinde olmali.', 'hata');
    if (!bit || bit.tur === 'Fiber')
        return bildirGoster('Fiber bitisi bir nesne veya konut olmali.', 'hata');
    if (bas.kayit.id === bit.kayit.id)
        return bildirGoster('Fiber başlangıcı ve bitişi aynı nesne olamaz.', 'hata');

    // Boş port ön kontrolü (anlık geri bildirim; sunucu da bakıyor)
    const dolu = [bas, bit].find(u =>
        ['Kabin', 'Santral'].includes(u.tur) && u.kayit.bosPort <= 0);
    if (dolu) return bildirGoster(`${dolu.kayit.kod} üzerinde boş port kalmadı.`, 'hata');

    const yeni = await kaydet('Fiber', {
        baslangicId: bas.kayit.id,
        bitisId: bit.kayit.id,
        guzergah: gjToWkt(gj)
    });
    if (yeni) await yenile();   // iki ucun boş port sayısı değişti
}
