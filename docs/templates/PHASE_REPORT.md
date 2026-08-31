# Pn faz kapanış raporu — ŞABLON

Bu dosya kanıt değildir. Mevcut faz için `docs/reports/Pn-REPORT.md` konumuna kopyalanıp gerçek sonuçlarla doldurulur. Henüz gerçekleşmeyen teste PASS veya başkası adına onay yazılmaz.

## Kimlik

- Faz:
- Tarih:
- Entegratör:
- A / B / C gerçek kişiler:
- Plan sürümü:
- Faz dalı:
- İncelenen aday commit:
- Faz PR'ı:
- Birleşmiş ana dal commit'i:
- Teslim build kimliği / dosya özeti:
- Unity yaması / paket sürümleri:

## Kişi teslimleri

| Görev | Uygulayan | PR/commit | İnceleyen | Kabul kanıtı | Sonuç |
|---|---|---|---|---|---|
| Pn-A | | | | | CALISTIRILMADI |
| Pn-B | | | | | CALISTIRILMADI |
| Pn-C | | | | | CALISTIRILMADI |

## Kapsam

- Faz planındaki teslimlerle eşleşme:
- Ortak sözleşme değişiklikleri ve etkilenen kişiler:
- Destek için devredilen işler:
- Varsa açıkça onaylanmış plan değişikliği ve karar kaydı:
- Kapsam dışı değişiklik var mı:

## Test ortamı

- Bilgisayarlar, CPU/GPU/RAM ve işletim sistemi:
- Oyun süreçleri ve hangi bilgisayarda çalıştıkları:
- Yerel ağ / internet / Relay bölgesi:
- Çözünürlük ve kalite:
- Gecikme/paket kaybı ayarı ve ölçüm yöntemi (ilgiliyse):
- Kayıt başlangıç durumu:

## Kabul testleri

PHASES içindeki mevcut fazın her zorunlu maddesini ayrı satıra taşı. Zorunlu bir satırı kaldırma; yapılmadıysa CALISTIRILMADI yaz. Log, ekran kaydı veya adımların bulunduğu gerçek kanıt yolunu ekle.

| Test | Beklenen | Gerçekleşen | Commit/build | Kanıt | PASS / FAIL / CALISTIRILMADI |
|---|---|---|---|---|---|
| | | | | | CALISTIRILMADI |

## Önceki faz regresyonları

| Test | Neden tekrarlandı | Kanıt | Sonuç |
|---|---|---|---|
| | | | CALISTIRILMADI |

## Açık kusurlar

| Kayıt | Önem | Etki | Sorumlu | Kapanışı engeller mi | Kabul edenler |
|---|---|---|---|---|---|
| | | | | | |

## Gerçek ekip onayları

Üç kişi kendi onayını verir. Entegratörün kendi teslim imzası diğer iki kişinin PR incelemesi yerine geçmez. Testten sonra kod değiştiyse ilgili onay/test yenilenir.

| Rol | Gerçek kişi / hesap | İncelediği commit | Tarih | Karar / onay bağlantısı |
|---|---|---|---|---|
| A | | | | ONAY_YOK |
| B | | | | ONAY_YOK |
| C | | | | ONAY_YOK |

## Ana dal doğrulaması ve geçiş

- Faz PR'ı birleşti mi:
- Birleşmiş commit'te çalıştırılan build ve testler:
- Çakışma çözümü davranışı değiştirdi mi; hangi testler tekrarlandı:
- Kanıt:
- STATUS güncellemesi:
- Sonraki faz: KILITLI / ACIK
- Eksik kalan zorunlu koşul:

Bütün kapılar geçmeden sonraki faz ACIK yazılamaz.
