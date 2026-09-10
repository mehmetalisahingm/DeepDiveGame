# DeepDiveGame

1–4 oyunculu co-op sualtı keşif, avcılık ve ekipman geliştirme oyunu.

Ana döngü: kasabada hazırlan → dalışa git → keşfet, avla ve kaydet → oksijen bitmeden dön → avları ve kayıtları değerlendir → ekipman geliştir.

## Şu anki durum

- Bu dalda üç kişinin P1 kodu ortak akışa bağlandı: oda/hazır ekranı, ağ oyuncuları, yürüme/yüzme ve hazırlık → dalış → dönüş. [Birlikte çalıştır](docs/P1_PLAY.md).
- **P0 tamamlandı:** Unity temeli üç ayrı kurulumda açıldı ve Windows build'i çalıştı. [Kapanış kaydı](docs/reports/P0-REPORT.md).
- Aktif çalışma **P2 — Avla, taşı, geri dön (AÇIK)**. P3–P6 kapalıdır.
- Başlangıç görev dağılımı: **Mehmet (A)** oyuncu/dalış, **Utku (B)** sualtı/canlılar, **Mert (C)** kasaba/ekonomi.
- Plan 1.8: P1 kullanıcı kararıyla kapandı; [doğrulanan sonuçlar ve devredilen kontroller](docs/reports/P1-REPORT.md). Görsel/ses Mert'te; diğer ortak sorumluluklar Mehmet'te.

**Ortak sorumluluklar:** Mert görsel/ses üretimi, kaynak seçimi ve tutarlılığı yönetir. Mehmet takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesini üstlenir. Utku'nun ve Mert'in mevcut geliştirme işleri korunur; herkes kendi sisteminin entegrasyonunu ve co-op testini yapar.

**Projeyi aç:** Unity **6000.3.23f1**, URP **17.3.0**. `codex/p2-integration` dalını al, depo kökünü Unity Hub'a ekle; `Assets/DeepDive/World/Scenes/PrepArea.unity` sahnesini aç. [P1 çalıştırma ve Windows build rehberi](docs/P1_PLAY.md). `main`, birleşmiş P1 temelini içerir.

**İndirme ve P0 testi:** [P0 main aktarımı](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8) · [Ekip için yayınlanmamış Windows test paketi](https://github.com/mehmetalisahingm/DeepDiveGame/releases). Kaynakları Git üzerinden, yalnızca çalıştırılacak build'i ZIP üzerinden paylaşın.

## Şimdi kim ne yapacak? — P2

| Kişi | Görev | GitHub |
|---|---|---|
| Mehmet | Oksijen, zıpkın, yüzme/nişan, vuruş ve nefes geri bildirimi | [#20](https://github.com/mehmetalisahingm/DeepDiveGame/issues/20) |
| Utku | Tek balık, avlanma, sualtı sis/ışık; av döngüsünden sonra CI | [#21](https://github.com/mehmetalisahingm/DeepDiveGame/issues/21) |
| Mert | Çanta, güvenli dönüş, dalış özeti/UI; P2 birleştirme ve oynama testi | [#22](https://github.com/mehmetalisahingm/DeepDiveGame/issues/22) |

**Şimdi:** `codex/p2-integration` dalını alın; önce av/vuruş/çanta bağlantısını birlikte netleştirin, sonra kendi özellik dalınızda görev alın. P2 kodu henüz başlamadı.

**Sonraki işler başlamadan önce:** [P0 toplantı gündemindeki](docs/plan/P0_MEETING.md) kaynak yöntemi, hedef, bütçe ve netcode yedeği gibi açık kararlar ilgili özellikten önce ele alınır. Bu gündem P0 kapanışını bekletmez.

**D06 sınırı:** Kayıt sahibi host yoksa aynı kampanyaya devam edilemez. Başka host ayrı kampanya açabilir; ilerleme otomatik taşınmaz. V1'e bulut kayıt veya host devri eklenmedi.

P2 özellik PR'ları `codex/p2-integration` dalına gider. P2 kabulü ve kısa oynama testi geçmeden P3 açılmaz.

## Sonraki fazlarda kim ne yapacak?

Bu tablo özet; kesin teslimler ve kabul koşulları [faz planında](docs/plan/PHASES.md). P2 açıktır; P3–P6 kapalıdır.

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
