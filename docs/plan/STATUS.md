# Güncel durum ve görev takibi

Plan **3.0 — 15 Eylül 2026**. Aktif faz **P3**; birleştirme koordinatörü **Mehmet**.

## 15 Eylül plan değişikliği

Kullanıcının yeni isteğiyle Plan 3.0 hazırlandı. P3 kıyı/insan/NPC/sandal temeli korunarak ilk sandal görevi, ayrı kamera satın alma ve canlı tekne haritası netleştirildi. P4'e ortak ev/yatak/gün/00:00/özet; gerçek klip/PC/oyun içi kanal; motorlu ve araştırma teknesi satın alma; keşif/ansiklopedi/boss; sponsor/sipariş/hava/roller ve kasaba gelişimi eklendi.

**Yeni işler PLANLANDI; uygulanmış/test edilmiş veya ekipçe tamamlanmış sayılmıyor.** Bu değişiklik oyun geliştirme başlangıcı ya da P4 açılışı değildir. [Ne zaman ne gelecek?](GAMEPLAY_LOOP.md#hangi-özellik-ne-zaman-gelecek) · [Yeni faz teslimleri](PHASES.md) · [Görsel/animasyon/ses planı](ASSET_PLAN.md).

Ev/gün, gerçek video, kanal, büyük tekne ve boss ayrıntısı: [WORLD_SYSTEMS](WORLD_SYSTEMS.md). P0–P6 adları ve koordinatör dönüşümü korunur; P4 kendi içinde sıralı alt teslimlerle ilerler. Gerçek sosyal medyaya yayın bu planın parçası değildir.

## Mevcut sınır

14 Eylül ara teslim kaydı: özel olay Adım 6 ve #44 ekonomi entegrasyonu ayrı çalışma dalında. Raporda ses bağlantısından **önceki** head için **356/356 PASS** ve Windows build başarısı var; son ses değişikliği sonrasında yeniden doğrulama bekliyor. Ortak dala merge veya P3 kapanışı değildir. [P3-WIP-HANDOFF.md](../reports/P3-WIP-HANDOFF.md) tarihsel yapılan/kalan listesidir; yeni kapsam için tek başına kapanış listesi değildir. Bu plan düzenlemesinde oyun testleri çalıştırılmadı.

- P0 kapalı.
- P1 kullanıcı kararıyla kapalı.
- P2 kullanıcı faz kabulüyle kapalı. Kapanış kaydı: [`P2-GAMEPLAY-VALIDATION.md`](../reports/P2-GAMEPLAY-VALIDATION.md).
- **P3 AÇIK.** Ortak dal: `codex/p3-integration`.
- P4–P6 kilitli. P3 kapanmadan bu fazların özellik işleri alınmaz.
- Unity sürümü: `6000.3.23f1`.
- P3 açılış kaydı: [`P3-KICKOFF.md`](../reports/P3-KICKOFF.md).

## Ekip

| Rol | Kişi | GitHub hesabı |
|---|---|---|
| A | Mehmet | `mehmetalisahingm` |
| B | Utku | `Utkuuzun14` |
| C | Mert | `MertKAYAR` |

## Faz durumu

| Faz | Durum | Birleştirme koordinatörü | Kayıt |
|---|---|---|---|
| P0 | KAPALI | Mehmet | [`P0-REPORT.md`](../reports/P0-REPORT.md) |
| P1 | KAPALI (kullanıcı kararı) | Utku | [`P1-REPORT.md`](../reports/P1-REPORT.md) |
| P2 | KAPALI (kullanıcı kabulü) | Mert | [`P2-GAMEPLAY-VALIDATION.md`](../reports/P2-GAMEPLAY-VALIDATION.md) |
| P3 | **AÇIK** | **Mehmet** | [`P3-KICKOFF.md`](../reports/P3-KICKOFF.md) |
| P4 | KİLİTLİ | Utku | Yok |
| P5 | KİLİTLİ | Mert | Yok |
| P6 | KİLİTLİ | Mehmet | Yok |

## Mevcut P3 teknik işlerinin kaydı

Aşağıdaki tablo önceki ortak dal kaydını gösterir. Ayrı dalın 14 Eylül entegrasyonu üstteki rapordadır; uzak GitHub durumu bu plan düzenlemesinde yeniden doğrulanmadı. Yeni alt görevler aşağıda ayrıca listelenir.

| Görev | Sahip | Durum | GitHub |
|---|---|---|---|
| P3-A | Mehmet | Kamera/kadraj/tüp (#38) ve World bağlantısı (#41) birleşti; ödeme bağlantısı ve diskten yükleme sonrası tüp testi P3-C API'lerini bekliyor | [#34 — Kamera kaydı, kadraj ve tüp etkisi](https://github.com/mehmetalisahingm/DeepDiveGame/issues/34) |
| P3-B | Utku | Çekim değerlendirmesi ve sahne bağlantısı birleşti (#40); özel olay açık | [#35 — Kayıt kalitesi, hedef tanıma ve özel olay](https://github.com/mehmetalisahingm/DeepDiveGame/issues/35) |
| P3-C | Mert | Ekonomi çekirdeği (#37) ve para/alışveriş sonucu göstergesi (#39) entegre; kayıt ödemesi/fiyatlar, gerçek alışveriş isteği/UI ve save/load açık | [#36 — Satış, ortak para, dükkân ve kayıt](https://github.com/mehmetalisahingm/DeepDiveGame/issues/36) |

## Revize P3 teslim sırası

| İş | Sahiplik | Durum / bitiş kanıtı |
|---|---|---|
| P3.0 Mevcut olay/ödeme/save entegrasyonu | Mehmet koordinasyon; herkes kendi modülü | Ayrı dal ara teslimi var; son head build/test ve gerçek ortak akış yeniden doğrulanacak. Bu iş yeni kapsamı kapatmaz |
| P3.1 İnsan, yürüme/yüzme, elde kamera/zıpkın | Mehmet entegrasyon; Mert insan/animasyon/ekipman seti; Utku su geçişi | **PLANLANDI** — iki oyuncu birbirini yürürken/yüzerken/çekim yaparken görecek |
| P3.2 Kumsal, NPC, ilk görev ve ayrı kamera | Mert kasaba/NPC/ekonomi/kayıt; Utku kıyı; Mehmet etkileşim | **PLANLANDI** — çantayla satış, ayrı kamera alımı, ilk sandal görevi, parça bulma veya satın alma |
| P3.3 Sandal, harita ve gidiş/dönüş | Mehmet hareket/koltuk/konum; Utku rota/koordinat; Mert onarım/harita UI/kayıt | **PLANLANDI** — üç parça, dört yolcu, yakın rota ve haritada canlı sandal/oyuncu/iskele |
| P3.4 Teknik, görsel ve ortak oynama kabulü | Üç kişi; koordinasyon Mehmet, sanat tutarlılığı Mert | **BEKLİYOR** — yeni kapsam gerçek build'de ve ayrı bilgisayar/internette doğrulanacak |

Yeni işler yerel plan görevleridir; yeni GitHub issue/PR açıldığı veya diğer kişilere mesaj gönderildiği anlamına gelmez. Eski #34/#35/#36 yalnız ilk teknik kapsamı izler; yeni alt teslimler uygulamaya alınırken aynı fazın görev kaydına eklenir. Görev kapanışı faz kapanışı değildir.

## P4 için planlanan yeni teslimler — kilitli

| Sıra | Oynanabilir sonuç | Durum |
|---|---|---|
| P4.1 | Ortak ev/dört yatak/depo, ortak saat/uyku/00:00/özet; keşif haritası ve ansiklopedi temeli | PLANLANDI / KİLİTLİ |
| P4.2 | Gerçek klip yakalama ve sonradan izleme; ev PC'si, ortak oyun içi kanal, ertesi gün izlenme/takipçi/gelir | PLANLANDI / KİLİTLİ; teknik medya denemesi gerekli |
| P4.3 | Üç kamera kademesi; sandal→motorlu→araştırma teknesi satın alma; tek aktif araç ve yeni rotalar | PLANLANDI / KİLİTLİ |
| P4.4 | Üç derinlik kesimi, beş normal tür + bir gerçek boss, ansiklopedi/iz/görev/trofe | PLANLANDI / KİLİTLİ |
| P4.5 | Sipariş/sponsor, gece/hava/akıntı, iki ev seviyesi/üç kasaba iyileştirmesi, hafif roller | PLANLANDI / KİLİTLİ |
| P4.6 | Bütün dünya/sanat ve art arda üç oyun gününün ortak kabulü | BEKLİYOR / KİLİTLİ |

P4 açılınca her ara teslimin kabulü sıradakinin başlangıç koşuludur. P5 hata/denge/performans ve P6 teslim olarak kalır. Yeni sayısal klip/depolama, derinlik ve saat ayarları taslaktır; başarı veya tarih taahhüdü değildir.

### Sıradaki somut işler

- Uygulama görevi alındığında P3.0'ın son kodu/testi [14 Eylül devrine](../reports/P3-WIP-HANDOFF.md) göre doğrulanır; #43/#44 alternatifleri birlikte uygulanmaz. Raporun önceki kapanış adayı yeni insan/NPC/sandal işlerini içermiyor.
- İlk görünür teslim P3.1: Mert insan rig'i, minimum hareket klipleri ve kamera/zıpkın setini sağlar; Mehmet kendi prefabına, Utku su geçişine bağlar. P3.2 ve P3.3 için ilgili sözleşmeler alan sahipleriyle netleştirilir.
- P3.2'de dalış sonu otomatik ödeme, **güvenli teslim adayı** üretimine çevrilir; gerçek av/kayıt geliri NPC etkileşiminde alınır. Bekleyen eşya/emanet/onarım ve kayıt şeması ayrıca uygulanır; mevcut save kodu bunları tamamlamış sayılmaz.
- Test edenler aynı commit/build/protokolü kullanır. 14 Eylül ayrı dal raporu `DeepDive-P3-4`, önceki [P3-A entegrasyon kaydı](../reports/P3-A-INTEGRATION.md) `DeepDive-P3-3` içerir; raporlardan biri gelişigüzel güncel build yerine seçilmez.

## P3 hedefi

Tek bölgede şu tam döngü çalışmalıdır:

**NPC'de hazırlan → kumsala yürü → yüz veya onarılmış sandalla git → avla/eldeki kamerayla kaydet → kıyıya/iskeleye dön → çantayla balık alıcısına ve kayıt değerlendirme NPC'sine git → ekipmanı geliştir → tekrar dal.**

P3 kapanışında ayrıca şunlar doğrulanır:

- Aynı av/kayıt/istek iki kez para üretmez.
- Eşzamanlı alışveriş parayı eksiye düşürmez.
- Görüş dışındaki veya engel arkasındaki hedef geçerli kayıt üretmez.
- Tüp yükseltmesi doğru oyuncuya doğru kapasiteyi verir.
- Yeniden açılışta son tamamlanmış kayıt geri gelir; av/para/ekipman çoğalmaz.
- İnsan yürüme/yüzmesi, elde kamera ve uzaktaki ekipman görünümü gerçek oyuncu prefabında çalışır.
- Üç NPC hizmeti, fiziksel kıyı giriş/çıkışı ve satış bekleyen çanta/kayıt akışı vardır; dönüşte otomatik ödeme yoktur.
- Sıfır parayla kıyı döngüsü açılır; sandal onarımı ve gidiş/dönüş solo/dört oyuncuda yapılır, yüklemede onarım korunur.
- İlk görev sandal onarımıdır; önce kıyı avı yapılabilir. Kamera ayrı alınır; harita sandalı canlı konumuyla gösterir.
- Solo, iki oyunculu gözlem ve en az iki bilgisayardaki dört süreçte tam döngü doğrulanır.
- P2 his katmanı korunur; yeni [P3 görsel kabulü](ASSET_PLAN.md#p3-görsel-kabulü) ve üç kişinin ortak oynama değerlendirmesi tamamlanır.

Temel insan/animasyon/ekipman ve kıyı/NPC/sandal sanatı P3 şartıdır. Ev/gün/PC/klip/kanal, büyük tekneler ve boss P4'te tamamlanır. Önceki genel video/büyük tekne dışlaması kaldırıldı; ikinci dalış bölgesi, serbest tekne simülasyonu, gerçek sosyal medya yükleme ve bulut kampanya kapsam dışıdır.

## P2'den devreden smoke kontrolü

P2 kullanıcı kabulüyle kapatıldı; final head üzerinde ayrı kayıtlı dört oyunculu insan oturumu ve 10–15 dakikalık ortak his testi bulunmuyordu. P3'ün ilk ortak smoke/playtest'inde en az iki oyuncuyla temel av/dönüş akışı ve guest oyuncunun kendi çanta göstergesinin canlı güncellenmesi tekrar kontrol edilir.

## CI

Unity CI artık `main`, `codex/p2-*`, `p2/*`, `codex/p3-*` ve `p3/*` push'larını kapsar. EditMode testleri ve mevcut Windows build yöntemi çalıştırılır. Yapılmayan veya başarısız koşu PASS sayılmaz.

## Faz kapanış kuralı

Üç teslim P3 ortak dalında birleşir, kabul testleri ve kısa ortak oynama testi kayda alınır, ardından P3 main'e aktarılır. P4 kendiliğinden açılmaz; ayrıca faz açılış kararı verilir.
