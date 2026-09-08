# DeepDiveGame

1–4 oyunculu co-op sualtı keşif, avcılık ve ekipman geliştirme oyunu.

Ana döngü: kasabada hazırlan → dalışa git → keşfet, avla ve kaydet → oksijen bitmeden dön → avları ve kayıtları değerlendir → ekipman geliştir.

## Şu anki durum

- Bu dalda ortak Unity/URP projesi ve kamera/ışık/küp içeren P0 örneği hazır; oyun mekanikleri henüz yok.
- **P0 tamamlandı:** Unity temeli üç ayrı kurulumda açıldı ve Windows build'i çalıştı. [Kapanış kaydı](docs/reports/P0-REPORT.md).
- Aktif çalışma **P1 — Bağlan, görün, hareket et (AÇIK)**. P2–P6 kapalıdır.
- Başlangıç görev dağılımı: **Mehmet (A)** oyuncu/dalış, **Utku (B)** sualtı/canlılar, **Mert (C)** kasaba/ekonomi.
- Plan 1.6: P0 kapandı, P1 görevleri açıldı. Görsel/ses Mert'te; diğer ortak sorumluluklar Mehmet'te.

**Ortak sorumluluklar:** Mert görsel/ses üretimi, kaynak seçimi ve tutarlılığı yönetir. Mehmet takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesini üstlenir. Utku'nun ve Mert'in mevcut geliştirme işleri korunur; herkes kendi sisteminin entegrasyonunu ve co-op testini yapar.

**Projeyi aç:** Unity **6000.3.23f1**, URP **17.3.0**. Depo kökünü Unity Hub'a ekle; `Assets/P0/Scenes/P0Example.unity` sahnesini aç. [Kurulum ve Windows build rehberi](docs/SETUP.md). P0 kontrolü üç kişide başarılıdır.

**İndirme ve P0 testi:** [P0 main aktarımı](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8) · [Ekip için yayınlanmamış Windows test paketi](https://github.com/mehmetalisahingm/DeepDiveGame/releases). Kaynakları Git üzerinden, yalnızca çalıştırılacak build'i ZIP üzerinden paylaşın.

## Şimdi kim ne yapacak? — P1

| Kişi | Bu aşamadaki görevi | GitHub görev kaydı |
|---|---|---|
| **Mehmet (A)** | Oda/bağlantı, oyuncu oluşumu, yürüme/yüzme ve hareket senkronizasyonu | [P1-A: Mehmet](https://github.com/mehmetalisahingm/DeepDiveGame/issues/12) |
| **Utku (B)** | Hazırlık alanı, tek sualtı test bölgesi, çarpışmalar ve P1 birleştirme koordinasyonu | [P1-B: Utku](https://github.com/mehmetalisahingm/DeepDiveGame/issues/13) |
| **Mert (C)** | Oda/oyuncu listesi, hazır olma ekranı, oturum durumları ve basit branch koruması | [P1-C: Mert](https://github.com/mehmetalisahingm/DeepDiveGame/issues/14) |

**İlk iş:** Herkes `codex/p1-integration` dalından kendi küçük özellik dalını açar. Mehmet ve Mert `PlayerId`/`SessionState` bağlantısını netleştirir; Utku faz birleşmelerini koordine eder.

**Sonraki işler başlamadan önce:** [P0 toplantı gündemindeki](docs/plan/P0_MEETING.md) kaynak yöntemi, hedef, bütçe ve netcode yedeği gibi açık kararlar ilgili özellikten önce ele alınır. Bu gündem P0 kapanışını bekletmez.

**D06 sınırı:** Kayıt sahibi host yoksa aynı kampanyaya devam edilemez. Başka host ayrı kampanya açabilir; ilerleme otomatik taşınmaz. V1'e bulut kayıt veya host devri eklenmedi.

P1'de özellik PR'ları `codex/p1-integration` dalına gider. Solo ve gerçek internet üzerinden çok bilgisayarlı testler geçmeden P1 kapanmaz.

## Sonraki fazlarda kim ne yapacak?

Bu tablo özet; kesin teslimler ve kabul koşulları [faz planında](docs/plan/PHASES.md). P2–P6 şu anda kapalıdır.

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
