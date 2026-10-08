# Güncel durum ve görev takibi

Plan **3.0 — 15 Eylül 2026**. Son durum güncellemesi: **8 Ekim 2026**.

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

**Durum: TAMAMLANDI / entegrasyonda.** PR #129, kabul edilen head `60e6306`, merge `515f5b6`. Merge sonrası kaynak ağacı aynı.

Gerçek iki süreçli filo satın alma/gerçek Reef/aktif seçim/restore; motorlu-resif ve araştırma-derin rota için binme → gidiş → demirleme → iniş/yeniden binme → dönüş → boş dock geçti. İki süreçte doğru gövde ve harita doğrulandı. #107/#108 kapalı; #109 önceden tamamlanmıştı. Eski PR #127, #129 tarafından karşılandı.

## P4.4 — Derin keşif ve boss

**Durum: TAMAMLANDI / entegrasyonda.** #124 final kabulü PR #129 ile geçti; #121/#122/#123 kapalı.

Gerçek iki süreçli akış: ansiklopedi → gerçek Reef → üç fiziksel iz → arena → iki ayrı oyuncunun zıpkın vuruşu → kuru güvenli dönüş → tek kalıcı boss completion → ikinci host açılışında restore. Aktif transient karşılaşma geri yüklenmiyor.

CI `37699987867` ve `37699982093`: Unity/EditMode + Windows build, P4.3 filo/rota ve P4.4 boss/restore kontrolleri SUCCESS. [Kapanış kanıtı](../reports/P4-3-P4-4-ACCEPTANCE.md).

Bu kanıt iki gerçek yerel/CI sürecidir; ayrı bilgisayar/internet, nihai sanat ve P4.6 üç günlük ortak kabul yerine geçmez. Başkaları adına manuel oynanış onayı üretilmedi. Kullanıcı iki ara teslimin kapanışını ve sonraki görevlerin açılışını istedi.

## P4.5 — Yaşayan dünya

**Durum: AKTİF.** P4.3/P4.4 kabul kapıları kapandı; uygulama görevleri açıldı.

| Sahip | GitHub | Teslim |
|---|---|---|
| Mehmet | [#130](https://github.com/mehmetalisahingm/DeepDiveGame/issues/130) | Hafif rol etkileri, akıntı/hareket bağlantısı, ekip uyarıları ve pingler |
| Utku | [#131](https://github.com/mehmetalisahingm/DeepDiveGame/issues/131) | Sakin/rüzgarlı hava, gece davranışı, bir akıntı alanı, görev world uygunluğu |
| Mert | [#132](https://github.com/mehmetalisahingm/DeepDiveGame/issues/132) | Üç sipariş/üç sponsor, iki ev seviyesi/üç kasaba iyileştirmesi, rol UI/save ve dar denge turu |

Koordinatör Utku; hedef `codex/p4-integration`. Herkes kendi gerçek co-op davranışını ve görsel bağlantısını teslim eder. P4.6, P5 ve P6 kapalıdır.

## Sıradaki ortak kapı

P4.5 birleşik kabulü: erişilebilir günlük hedefler → doğrulanmış av/çekim → tek ödül → görünür ev/kasaba ilerlemesi → rol/hava/akıntı → save/load/replay. Bu kabul sonrası P4.6 üç ardışık oyun günü ve gerçek ortak görsel/oynanış değerlendirmesine geçilir.
