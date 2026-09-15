# DeepDiveGame

1–4 oyunculu co-op sualtı keşif, avcılık ve ekipman geliştirme oyunu.

Planlanan ana döngü: ortak evde uyan → harita/hava/görevlere bak → dükkândan hazırlan → yüzerek veya tekneyle çık → avla/keşfet/eldeki kamerayla klip çek → dön, çantadaki balığı balıkçıya sat → ev PC'sinde klipleri izle/yayınla → ekipman/tekne/ev geliştir → uyu/00:00 ve gün özeti → ertesi sabah kanal sonuçları/yeni hedefler.

**[TÜM PLAN — fazlar, görevler ve kabul koşulları](docs/plan/PHASES.md)** · [Ev/gün/video/tekne/boss ayrıntıları](docs/plan/WORLD_SYSTEMS.md)

## Şu anki durum

- **Plan 3.0 — 15 Eylül 2026. Aktif faz P3; P4–P6 kilitli.** Güncel kapsam ve gerçek kanıtın kaynağı [STATUS](docs/plan/STATUS.md).
- P0 kapalı; P1/P2 kullanıcı kabulüyle kapalı. Tarihsel test sınırları [P1](docs/reports/P1-REPORT.md) ve [P2](docs/reports/P2-GAMEPLAY-VALIDATION.md) kayıtlarında korunur.
- 14 Eylül özel olay/ekonomi/kayıt entegrasyonu ayrı çalışma dalı ara teslimidir; ortak dala merge/P3 kapanışı değildir. [Yapılan ve kalan teknik işler](docs/reports/P3-WIP-HANDOFF.md).
- İnsan/NPC/kumsal/sandal planına ayrı kamera alımı, canlı harita, ev/gün, gerçek klip/PC/oyun içi kanal, daha büyük tekneler ve boss **eklendi**; oyunda bitmiş veya test edilmiş sayılmaz.
- Görev dağılımı: **Mehmet (A)** oyuncu/dalış/ekipman, **Utku (B)** sualtı/canlılar/kıyı-rota, **Mert (C)** kasaba/NPC/ekonomi/kayıt.

**Ortak sorumluluklar:** Mert görsel/ses üretimi, kaynak seçimi ve tutarlılığı yönetir. Mehmet takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesini üstlenir. Utku'nun ve Mert'in mevcut geliştirme işleri korunur; herkes kendi sisteminin entegrasyonunu ve co-op testini yapar.

**Projeyi aç:** Unity **6000.3.23f1**, URP **17.3.0**. Aktif ortak çalışma dalı `codex/p3-integration`; kendi değişikliklerini koruyarak bu dalı al, depo kökünü Unity Hub'a ekle. Temel [çalıştırma rehberi](docs/P1_PLAY.md) ve P3'e özgü [devam kaydı](docs/reports/P3-WIP-HANDOFF.md) birlikte kullanılır. Ortak dal ile ayrı çalışma dalının build/protokolü karıştırılmaz.

**İndirme ve P0 testi:** [P0 main aktarımı](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8) · [Ekip için yayınlanmamış Windows test paketi](https://github.com/mehmetalisahingm/DeepDiveGame/releases). Kaynakları Git üzerinden, yalnızca çalıştırılacak build'i ZIP üzerinden paylaşın.

## Hangi özellik ne zaman gelecek?

| Teslim | Oyuncuya görünen sonuç |
|---|---|
| **P3.0** | Mevcut teknik entegrasyonun doğrulanması; yeni kapsamın kapanışı değildir |
| **P3.1** | İnsan karakter; yürüme, su üstü/sualtı yüzme, dalma/çıkma; elde kamera/zıpkın ve arkadaşının gördüğü ekipman |
| **P3.2** | Kumsal/üç NPC/çantayla satış; ilk sandal görevi; kamera ayrı satın alınır, önce kıyıda avlanmak serbesttir |
| **P3.3** | Üç parçayla sandal onarımı, dört koltuk ve yakın gidiş/dönüş; haritada canlı sandal/oyuncu/iskele |
| **P3.4** | İnsan, ekipman, kasaba, kıyı, sualtı ve sandalın gerçek build'de ortak oyun/görsel kabulü |
| **P4.1** | Ortak ev/dört yatak/depo; ortak saat, herkes uyuyunca veya 00:00'da gün sonu/özet; keşif haritası ve ansiklopedi temeli |
| **P4.2** | Gerçek çekim klibini sonradan izleme, ev PC'sinden oyun içi kanala yayın, ertesi gün izlenme/takipçi/gelir |
| **P4.3** | Üç kamera kademesi, ekipman gelişimi; motorlu ve araştırma teknesini ayrı satın alma, yeni rotalar |
| **P4.4** | Sığ/resif/derin keşif, beş normal tür + bir gerçek boss, ansiklopedi/iz/trofe ve araştırma/av seçimi |
| **P4.5–P4.6** | Sipariş/sponsor, gece/hava/akıntı, ev-kasaba gelişimi ve hafif roller; tüm dünya/sanat ve üç oyun günü kabulü |

Ara teslimler takvim sözü değildir. Ayrıntı: **[oyuncunun tam yolculuğu](docs/plan/GAMEPLAY_LOOP.md)** · **[fazlar ve kişi görevleri](docs/plan/PHASES.md)** · **[görsel, animasyon ve ses planı](docs/plan/ASSET_PLAN.md)**.

**Sonraki işler başlamadan önce:** [P0 toplantı gündemindeki](docs/plan/P0_MEETING.md) kaynak yöntemi, hedef, bütçe ve netcode yedeği gibi açık kararlar ilgili özellikten önce ele alınır. Bu gündem P0 kapanışını bekletmez.

**D06 sınırı:** Kayıt sahibi host yoksa aynı kampanyaya devam edilemez. Başka host ayrı kampanya açabilir; ilerleme otomatik taşınmaz. V1'e bulut kayıt veya host devri eklenmedi.

P3 özellik PR'ları `codex/p3-integration` dalına gider. Yeni kapsamın teknik, görsel ve ortak oynama kabulü olmadan P3 kapanmaz; P4 ayrıca açılır. Plan düzenleme oyun geliştirme yetkisi değildir.

## Sonraki fazlarda kim ne yapacak?

Bu tablo özet; kesin teslimler ve kabul koşulları [faz planında](docs/plan/PHASES.md). P3 açıktır; P4–P6 kilitlidir.

| Faz | Mehmet | Utku | Mert |
|---|---|---|---|
| P3 | İnsan/eller/ekipman, kamera tahsisi, sandal hareketi/koltuk/konum | Kıyı/su, balık/olay, yakın rota/harita koordinatı | Sanat setleri, ilk görev/kamera alımı, NPC/çanta/ödeme, onarım/harita/kayıt |
| P4 | Ev/uyku etkileşimi, gerçek klip/aktarım/oynatma, ekipman/üç tekne, boss hasarı | Keşif/tür/boss, rotalar, gece/hava/akıntı ve çevre | Ev/gün/PC/kanal, tekne sahipliği, görev/sponsor/ansiklopedi/kayıt ve sanat |
| P5 | Ağ/insan/medya/tekne hataları ve performans | Boss/keşif/hava/çevre hataları ve performans | Gün/kanal/kayıt/ekonomi/UI hataları ve denge |
| P6 | Temiz teslim build'i ve sürüm kaydı | Bağımsız paket testi, oyuncu rehberi ve atıflar | Kayıt/çevrimiçi kabulü ve bilinen sorunlar |

Temel insan/ekipman/NPC/kıyı/sandal sanatı **P3 şartıdır**. P4 ev/kanal/keşif/tekne/boss döngüsünü ve sanatı tamamlar; P5 yeni özellik üretmez. Bir deniz bölgesi, üç tekne kademesi ve aynı anda tek aktif tekne vardır. Yayın oyun içi kanaldadır; gerçek sosyal medya, serbest dümen/deniz fiziği ve ikinci ada kapsam dışıdır.

## Çalışmaya başlama sırası

1. [Güncel durum ve görevler](docs/plan/STATUS.md): Yalnızca açık fazdaki görev alınır.
2. [Faz planı](docs/plan/PHASES.md): Kapsam, kişi başına teslim ve kabul koşulları okunur.
3. [Ortak sözleşmeler](docs/plan/CONTRACTS.md): Sistemler bu bağlantılara göre geliştirilir.
4. [Birlikte çalışma kuralları](docs/plan/WORKFLOW.md): Branch, inceleme, birleştirme ve bekleme kuralları uygulanır.
5. [Kısa faz kapanış kaydı](docs/templates/PHASE_REPORT.md): Test sonucu ve üçünüzün tamam mesajı tek yerde tutulur; ayrı imza matrisi yoktur.

AI yardımcıları ayrıca [AGENTS.md](AGENTS.md) kurallarını izler.

## Depoyu alma

Ortak public depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame).

```sh
git clone https://github.com/mehmetalisahingm/DeepDiveGame.git
cd DeepDiveGame
```

Herkes depoyu okuyup klonlayabilir. Aynı depoya branch göndererek çalışacak ekip üyelerinin ayrıca collaborator erişimi olması gerekir. Başlangıçta ana dal `main` kullanılır; uygulama işleri aktif fazın çalışma düzenine göre ayrı dallarda yapılır.

## Önerilen teknik temel

- Unity 6.3 LTS, C#, URP.
- Netcode for GameObjects, Unity Transport, Multiplayer Services SDK ve Relay.
- İlk hedef Windows PC; birinci şahıs oynanış.
- Tek depo, kısa özellik branch'leri, her faz için bir birleştirme dalı.
- Kesin editör yaması ve paket sürümleri P0'da doğrulanıp sabitlenir.

## Temel kural

**Mehmet, Utku ve Mert aynı fazda çalışır. Erken bitiren sonraki faza geçmez; mevcut fazın testine, incelemesine veya geciken görevine destek olur.**

CI ve faz kanıtlarının güncel kaydı [STATUS](docs/plan/STATUS.md)'tadır. Yapılmayan test veya ekip onayı tamamlanmış sayılmaz. Public depoya sır, erişim anahtarı veya özel ekip bilgisi eklenmez.
