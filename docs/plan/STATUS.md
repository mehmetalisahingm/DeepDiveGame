# Güncel durum ve görev takibi

Plan **3.0 — 15 Eylül 2026**. Son durum güncellemesi: **1 Ekim 2026**.

Aktif faz: **P4**. P0–P3 kapalıdır. P4 kendi içinde sıralı ilerler: **P4.1 → P4.2 → P4.3 → P4.4 → P4.5 → P4.6**. Bir ara teslim birleşik kabul kapısını geçmeden sıradaki özellik işi tamamlanmış sayılmaz.

Unity sürümü: `6000.3.23f1`.

## Ekip

| Rol | Kişi | GitHub hesabı | Ana alan |
|---|---|---|---|
| A | Mehmet | `mehmetalisahingm` | Oyuncu, ağ, ekipman kullanımı, capture ve araç hareket bağlantıları |
| B | Utku | `Utkuuzun14` | World, sualtı, canlılar, rotalar, keşif ve kamera hedef/world doğrulaması |
| C | Mert | `MertKAYAR` | Ev/kasaba, ekonomi, UI, save/load, kanal ve progression |

P4 birleştirme koordinatörü: **Utku**. Herkes kendi alanının kodunu, entegrasyon seam'ini ve gerçek co-op kanıtını birlikte teslim eder.

## Faz durumu

| Faz | Durum | Not |
|---|---|---|
| P0 | KAPALI | Ortak Unity temeli |
| P1 | KAPALI | Ağ oturumu / temel co-op |
| P2 | KAPALI | Oksijen, av, çanta ve güvenli dönüş |
| P3 | KAPALI | İnsan/kıyı/NPC/sandal/harita/kayıt-ekonomi temel döngüsü |
| P4 | **AÇIK** | Ev, keşif, medya, ileri ekipman/tekne, boss ve yaşayan dünya |
| P5 | KİLİTLİ | Hata, denge, performans ve sertleştirme |
| P6 | KİLİTLİ | Final teslim |

## P4.1 — Ev, gün ve keşif

**Durum: TAMAMLANDI / entegrasyonda.**

Teslim edilen ana parçalar:
- ortak ev, dört yatak, fiziksel uyku etkileşimi ve sabah normalize akışı,
- ortak saat, 00:00 kapanışı, erken uyku kapısı ve tek günlük özet,
- ortak depo ve kalıcı depolama,
- gün/gece world modeli,
- keşif hücreleri, derinlik bandı ve tür gözlem otoriteleri,
- keşif haritası/sis, ansiklopedi ve save/load,
- gerçek oyuncu/world binding'i, player/boat/ping harita bağlantısı.

Önemli birleşen PR'lar: `#91`, `#92`, `#93`, `#94`, `#95`, `#96`, `#97`, `#98`, `#99`.

## P4.2 — Çek, izle ve yayınla

**Durum: TAMAMLANDI. Üç ana teslim merge edildi ve birleşik acceptance kapısı #112 / #106 ile PASS olarak kapandı.**

### A — Mehmet · gerçek clip capture / playback
- PR `#104` merge edildi.
- Gerçek oyuncu kamerasından host-authoritative frame capture.
- Deterministik `recordingId -> clipId`.
- Yerel timestamp'li clip container, SHA-256 ve boyut manifesti.
- Güvenli dönüş sonrası `ClipArchive` kabulü.
- `ClipPlayback` seam'i.
- PR kanıtı: **812/812 EditMode PASS**, Windows build PASS ve gerçek 2-process koşuda oynatılabilir clip üretimi.

### B — Utku · recording world context
- PR `#105` merge edildi.
- `RecordingWorldContext`: subject kind, region, cell, depth band ve first-recording bilgisi.
- Gizli world/fish pozisyonu medya verisine sızdırılmaz.
- Mevcut species observation / exploration otoriteleri tüketilir; ikinci world authority yoktur.
- PR kanıtı: **856/856 EditMode PASS**.

### C — Mert · ev PC / kanal / medya save
- PR `#103` merge edildi.
- Ev PC arşivi, playback bağlantısı, başlık/yayın akışı ve ortak kanal.
- Aynı clip için duplicate yayın engeli.
- NPC veya kanal için tek ticari hak.
- Gün kapanışında izlenme/gelir settlement ve kalıcı medya kaydı.
- Gerçek 2-process `-Media` smoke host + client PASS; save/load sonrası tekrar ödeme/yayın yok.

### P4.2 kapanış kapısı

**KAPANDI.** Acceptance harness/raporu PR `#112` ile integration'a alındı; issue `#106` completed olarak kapatıldı.

Birleşik ürün akışında aynı güncel integration build'i üzerinde şu zincir doğrulandı:

1. Gerçek oyuncu kamerası ile kayıt başlatılır ve gerçek clip oluşur.
2. Recording world context clip manifestine aynı authoritative akışta bağlanır.
3. Güvenli dönüşten sonra clip ev PC arşivinde görünür.
4. Clip gerçekten playback edilebilir.
5. Geçerli clip kanala publish edilir; duplicate publish ikinci sonuç üretmez.
6. Gün kapanır; ertesi gün izlenme/gelir yalnız bir kez işlenir.
7. Oyun yeniden açıldığında clip, yayın, hak ve gelir state'i çoğalmadan geri gelir.
8. `-Explore`, `-Storage`, `-Day`, `-HomeSleep`, `-Trip` regresyonları korunur.
9. En az iki gerçek süreçte fixture'sız birleşik medya smoke PASS olur.

P4.2 acceptance tamamlandığı için **P4.3 aktif geliştirmeye açıldı**.

## P4.3 — Kamera ve büyük tekneler

**Durum: AKTİF.**

Alan dağılımı:

| Sahip | Kapsam |
|---|---|
| Mehmet | İki üst kamera modelinin oyuncu/presentation etkisi; palet/çanta/zıpkın yükseltmelerinin player bağlantısı; sandal/motorlu/araştırma teknesi için koltuk, host-authoritative hareket ve harita konum seam'i |
| Utku | Kamera menzil/düşük ışık doğrulaması; yakın/resif/derin demirleme rotaları; route/world geometry ve çevre okunurluğu |
| Mert | Liman tekne satıcısı; sandal → motorlu → araştırma teknesi satın alma/sahiplik/aktif araç seçimi; katalog/tahsis/emanet/save ve tek aktif araç progression'ı |

P4.3 kabulünde:
- üç kamera kademesi somut yetenek farkı üretmeli,
- motorlu ve araştırma teknesi ayrı gövde/model olmalı; eski sandal yalnız ölçeklenmiş gibi olmamalı,
- tüm araçlar dört kişilik olmalı,
- aynı anda yalnız bir aktif araç denizde olmalı,
- satın alma/save/reload duplicate tekne veya para üretmemeli,
- sabit rotalı seyahat korunmalı; serbest dümen/fizik simülasyonu eklenmemeli,
- gerçek en az iki süreçli araç satın alma → seçim → binme → rota → dönüş smoke'u kaydedilmeli.

Mehmet'in P4.3-A foundation'ı PR `#111` ile merge edildi. Utku `#108` ve Mert `#109` kapsamları ile final cross-owner binding/smoke tamamlanmadan P4.3 kapanmaz.

## Sonraki P4 teslimleri

| Ara teslim | Durum |
|---|---|
| P4.4 — Derin keşif ve boss | KİLİTLİ |
| P4.5 — Sponsor/sipariş, hava/gece/akıntı, ev-kasaba gelişimi | KİLİTLİ |
| P4.6 — Tam dünya ve üç ardışık gün kabulü | KİLİTLİ |

## Bilinen ayrı takip

- `#86`: eski P3.4 oyun testlerinden bağımsız GitHub Actions / workflow follow-up. Oyun feature ilerlemesinin otoritesi değildir; CI katmanı ayrıca yeşile döndürülmelidir.

## Şu an sıradaki tek ortak kapı

**P4.3 birleşik entegrasyon ve araç acceptance:**

`kamera tier/world doğrulaması + tekne sahipliği/aktif seçim + Mehmet'in araç network seam'i -> gerçek 2-process board -> outbound -> anchor -> disembark/reboard -> inbound -> dock`

Bu zincir fixture'sız ve aynı integration build'inde geçince P4.3 kapanır ve P4.4 açılabilir.
