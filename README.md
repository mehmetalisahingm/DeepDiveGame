# DeepDiveGame

1–4 oyunculu co-op sualtı keşif, avcılık ve ekipman geliştirme oyunu.

Ana döngü: kasabada hazırlan → dalışa git → keşfet, avla ve kaydet → oksijen bitmeden dön → avları ve kayıtları değerlendir → ekipman geliştir.

## Şu anki durum

- Bu dalda ortak Unity/URP projesi ve kamera/ışık/küp içeren P0 örneği hazır; oyun mekanikleri henüz yok.
- Aktif çalışma **P0 — Ortak temel (ACIK)**. Mehmet Windows build aldı; Unity temeli ve Utku'nun Git düzeni faz dalında birleşti. P0 main aktarımı PR #8 üzerinden yapılıyor; Utku ve Mert'in indirme/açma/build sonucu bekleniyor.
- P1–P6 kapalıdır. Bir fazın üç kişi tarafından tamamlanıp birleştirilmesi ve doğrulanması bitmeden sonraki faz başlamaz.
- Başlangıç görev dağılımı: **Mehmet (A)** oyuncu/dalış, **Utku (B)** sualtı/canlılar, **Mert (C)** kasaba/ekonomi.
- Plan 1.5: P0 ortak projeyi indirip açma ve örnek build'i çalıştırma düzeyine sadeleştirildi. Görsel/ses Mert'te; diğer ortak sorumluluklar Mehmet'te.

**Ortak sorumluluklar:** Mert görsel/ses üretimi, kaynak seçimi ve tutarlılığı yönetir. Mehmet takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesini üstlenir. Utku'nun ve Mert'in mevcut geliştirme işleri korunur; herkes kendi sisteminin entegrasyonunu ve co-op testini yapar.

**Projeyi aç:** Unity **6000.3.23f1**, URP **17.3.0**. Depo kökünü Unity Hub'a ekle; `Assets/P0/Scenes/P0Example.unity` sahnesini aç. [Kısa kurulum ve tek komutla Windows build](docs/SETUP.md). Build ve Mehmet'in bilgisayarında Codex ile [görsel kontrol](docs/evidence/P0-A-9596244-visual-check.md) başarılı; diğer iki bilgisayarın testi bekliyor.

**İndirme ve P0 testi:** [P0 main aktarımı](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8) · [Ekip için yayınlanmamış Windows test paketi](https://github.com/mehmetalisahingm/DeepDiveGame/releases). Kaynakları Git üzerinden, yalnızca çalıştırılacak build'i ZIP üzerinden paylaşın.

## Şimdi kim ne yapacak? — P0

| Kişi | Bu aşamadaki görevi | GitHub görev kaydı |
|---|---|---|
| **Mehmet (A)** | Ortak Unity temelini main'e alır; üç kişinin sonuçlarından P0 kapanışını yapar | [P0-A: Mehmet](https://github.com/mehmetalisahingm/DeepDiveGame/issues/1) |
| **Utku (B)** | Güncel main'i indirir, Unity `6000.3.23f1` ile açar ve ortak build'i çalıştırır | [P0-B: Utku](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2) |
| **Mert (C)** | Güncel main'i temiz klonlar, Unity `6000.3.23f1` ile açar ve ortak build'i çalıştırır | [P0-C: Mert](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3) |

**İlk iş:** Mert'in (**MertKAYAR**) ve Utku'nun (**Utkuuzun14**) write erişimi aktif. PR #8 main'e birleşince ikisi de güncel depoyu indirip projeyi ve build'i açacak.

**Sonraki işler başlamadan önce:** [P0 toplantı gündemindeki](docs/plan/P0_MEETING.md) kaynak yöntemi, hedef, bütçe ve netcode yedeği gibi açık kararlar ilgili özellikten önce ele alınır. Bu gündem P0 kapanışını bekletmez.

**D06 sınırı:** Kayıt sahibi host yoksa aynı kampanyaya devam edilemez. Başka host ayrı kampanya açabilir; ilerleme otomatik taşınmaz. V1'e bulut kayıt veya host devri eklenmedi.

P0'da karşılıklı PR incelemesi veya herkesin deneme branch'i pushlaması gerekmiyor. Üç kişi aynı sürümle projeyi açıp ortak build'i çalıştırdığını kendi görev kaydına yazınca P0 kapanır. P0 bitmeden P1'e geçilmez.

## Sonraki fazlarda kim ne yapacak?

Bu tablo özet; kesin teslimler ve kabul koşulları [faz planında](docs/plan/PHASES.md). P1–P6 şu anda kapalıdır.

| Faz | Mehmet | Utku | Mert |
|---|---|---|---|
| P1 | Oda/bağlantı, oyuncu, yürüme/yüzme senkronizasyonu | Tek sualtı test alanı, giriş/çıkış, çarpışmalar | Oda/hazır ekranı, dalış geçişi, temel branch koruması |
| P2 | Oksijen, zıpkın, yüzme/nişan ayarı, vuruş ve nefes geri bildirimi | Bir balık, vurulma tepkisi, temel sis/ışık; av oynanabilir olduktan sonra CI | Çanta/kapasite, av toplama/dönüş, UI geri bildirimi, kısa oynama testi |
| P3 | Kamera/kayıt geri bildirimi, tüp etkisi, kayıt sonrası ekipman testi | Çekim hedefi/kalitesi, özel olay ve temel işaretleri, tekrar ödül testi | Satış, görüntü geliri, para, yükseltme, kayıt ve işlem geri bildirimi |
| P4 | Palet/çanta yükseltmesi, avcı hasarı, ekipman iyileştirmeleri | Aynı bölgeyi ve toplam beş canlı türünü tamamlamak | Üç hizmet noktası, üç görev, jurnal/ilerleme |
| P5 | Ağ/oyuncu/ekipman hataları ve performans | AI/çarpışma/grafik hataları ve performans | Kayıt/ekonomi/UI hataları ve denge |
| P6 | Temiz teslim build'i ve sürüm kaydı | Bağımsız paket testi, oyuncu rehberi ve atıflar | Kayıt/çevrimiçi kabulü ve bilinen sorunlar |

P2'de kontrol/av ve oksijen-çanta kararlarını, P3'te kamera/gelir/tekrar dalma isteğini değerlendirirsiniz. Basit ses, vuruş tepkisi, sis/ışık ve UI işaretleri bu testlerden **önce** yapılır; nihai cila P4/P5'te tamamlanır.

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

Belgelerdeki kurallar henüz GitHub tarafından teknik olarak uygulanmıyor. Public uzak depo oluşturuldu; ekip erişimleri P0'da, basit branch koruması P1'de, otomatik build P2'de ele alınacak. Plan dosyalarının paylaşılması P0'ın tamamlandığı anlamına gelmez. Public depoya sır, erişim anahtarı veya özel ekip bilgisi eklenmez.
