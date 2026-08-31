# Güncel durum ve görev takibi

Son güncelleme: 31 Ağustos 2026 — ilk plan ve kullanıcının istediği public depo başlangıcı.

## Mevcut sınır

- **Sıradaki/planlanan faz: P0**
- **Durum: PLAN_HAZIR**
- **Unity/oyun uygulaması başladı mı? Hayır. Repo paylaşımının başlangıç kurulumu yapıldı.**
- **Çalışan oyun/build var mı? Hayır.**
- **P1–P6: KILITLI.**
- **Uzak depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame), public.**
- **Ekip erişimleri, branch koruması ve CI: henüz kurulmadı/doğrulanmadı.**
- Tamamlanmış faz, onaylı oyun testi veya ekip kapanış imzası yok.

P0 oyun/proje uygulamasına ilişkin bir görev verildiğinde P0 ACIK yapılır. Kullanıcının açık isteğiyle public depo ve ilk plan paylaşımı hazırlandı; bu başlangıç adımı hiçbir oyun görevini veya P0 kapanışını tamamlanmış saymaz.

## Ekip ve ortam

| Rol | Gerçek kişi | GitHub hesabı | Haftalık kapasite | Bilgisayar/işletim sistemi |
|---|---|---|---|---|
| A | Atanmadı | Atanmadı | Ölçülmedi | Kaydedilmedi |
| B | Atanmadı | Atanmadı | Ölçülmedi | Kaydedilmedi |
| C | Atanmadı | Atanmadı | Ölçülmedi | Kaydedilmedi |

| Ayar | Durum |
|---|---|
| Unity | 6.3 LTS önerildi; kesin yama P0'da seçilecek |
| URP/NGO/Transport/MPS sürümleri | P0'da birlikte doğrulanıp kilitlenecek |
| Uzak repo ve ana dal | [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame), public; main |
| Unity servis projesi ve yetkileri | Oluşturulmadı/doğrulanmadı |
| Referans performans bilgisayarı | Seçilmedi |
| Referans çözünürlük/kalite/kare süresi | Öneri: 1080p, belirlenecek kalite, 60 FPS / 16,7 ms hedef; P0'da kesinleşecek |
| Otomatik build ve PR kontrolleri | Kurulmadı |
| Manuel kontrol istisnası | Yok; ihtiyaç oluşursa gerekçe ve üç gerçek onay kaydedilecek |

## Faz durumu

| Faz | Durum | Entegratör | Rapor / ana dal commit'i |
|---|---|---|---|
| P0 | PLAN_HAZIR | A | Yok |
| P1 | KILITLI | B | Yok |
| P2 | KILITLI | C | Yok |
| P3 | KILITLI | A | Yok |
| P4 | KILITLI | B | Yok |
| P5 | KILITLI | C | Yok |
| P6 | KILITLI | A | Yok |

## Ana iş paketleri

Paket ayrıntıları ve kabul koşulları [PHASES.md](PHASES.md) içindedir. Kapalı fazın BEKLIYOR görevi alınamaz. Her faz açılırken paketler küçük alt işlere bölünür; kişi adı, tahmin ve dosya sahipliği eklenir.

| Görev | Sahip | Durum | Tahmin / harcanan süre | PR / test kanıtı |
|---|---|---|---|---|
| P0-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P0-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P0-C | C | BEKLIYOR | Belirlenmedi | Uzak repo başlangıcı yapıldı; ekip erişimleri, CI ve kalan teslimler bekliyor |
| P1-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P1-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P1-C | C | BEKLIYOR | Belirlenmedi | Yok |
| P2-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P2-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P2-C | C | BEKLIYOR | Belirlenmedi | Yok |
| P3-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P3-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P3-C | C | BEKLIYOR | Belirlenmedi | Yok |
| P4-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P4-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P4-C | C | BEKLIYOR | Belirlenmedi | Yok |
| P5-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P5-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P5-C | C | BEKLIYOR | Belirlenmedi | Yok |
| P6-A | A | BEKLIYOR | Belirlenmedi | Yok |
| P6-B | B | BEKLIYOR | Belirlenmedi | Yok |
| P6-C | C | BEKLIYOR | Belirlenmedi | Yok |

## Alt işler ve destek devirleri

Henüz görev devri yapılmadı. İlk alt görevler P0 uygulaması açıldığında doldurulur.

| Alt görev | Üst paket | Uygulayan | Sistem sahibi / inceleyen | Dosya kapsamı | Tahmin | Durum |
|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — |

## Tasarım kararları

Ayrıntılar PHASES içindeki D01–D11 tablosundadır. Bunlar uygulama öncesi ortak çalışma varsayımlarıdır; üç kişinin bunları kabul ettiği henüz kaydedilmemiştir.

| Karar | Durum | Nihai karar / üç kişinin değerlendirme kaydı |
|---|---|---|
| D01 | ONERI | Yok |
| D02 | ONERI | Yok |
| D03 | ONERI | Yok |
| D04 | ONERI | Yok |
| D05 | ONERI | Yok |
| D06 | ONERI | Yok |
| D07 | ONERI | Yok |
| D08 | ONERI | Yok |
| D09 | ONERI | Yok |
| D10 | ONERI | Yok |
| D11 | ONERI | Yok |

## Engeller ve açık kararlar

- P0 uygulaması henüz başlatılmadı; üç kişinin gerçek kimliği ve çalışma kapasitesi kaydedilmedi.
- Kesin motor/paket uyumu ve internet üzerinden bağlantı henüz test edilmedi.
- İkinci/üçüncü dükkânın detayları kaynak konseptte yok; önerilen hizmet rolleri P0'da kesinleştirilecek.
- CONTRACTS sonundaki ayrıntılar P0 kapanmadan kesinleşmeli.

## Sonraki fazı açma kontrolü

- [ ] A paketi DOGRULANDI.
- [ ] B paketi DOGRULANDI.
- [ ] C paketi DOGRULANDI.
- [ ] Faz raporundaki zorunlu kabul ve regresyon testleri PASS.
- [ ] Üç gerçek kişinin aday commit'e bağlı kapanış onayı var.
- [ ] Faz PR'ı ana dala birleşti.
- [ ] Birleşmiş ana dal commit'i doğrulandı ve rapor bağlantısı eklendi.

Bu kutular yeni aktif faz için yeniden açılır; önceki kanıt faz raporunda korunur.

## Plan değişikliği kaydı

| Tarih | Değişiklik | Gerekçe / karar |
|---|---|---|
| 2026-08-31 | Plan 1.0 oluşturuldu | Kullanıcının üç kişinin aynı fazda kalması ve faz sonunda birleştirme isteği |
| 2026-08-31 | Public uzak depo ve main başlangıcı | Kullanıcının açık repo oluşturma/push talebi; faz kapsamı ve kapanış koşulları değiştirilmedi |
