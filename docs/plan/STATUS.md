# Güncel durum ve görev takibi

Plan 1.2 — 31 Ağustos 2026. Kullanıcının onayladığı isimli dağılım, sade P0, P2/P3 oynama testleri ve tek bölgelik ilk sürüm.

## Mevcut sınır

- Aktif faz: P0. Durum: ACIK. Kullanıcının başlama isteğiyle GitHub görev kayıtları açıldı; oyun uygulaması henüz yapılmadı.
- Unity/oyun uygulaması başlamadı; çalışan oyun/build yok.
- P1–P6 KILITLI. P0'ın açılması sonraki fazlara geçiş veya herhangi bir görevin tamamlanması değildir.
- Public depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame).
- Ekip erişimleri henüz doğrulanmadı. Branch koruması P1, CI P2 görevidir; kurulu oldukları iddia edilmez.
- Tamamlanmış oyun görevi, faz testi veya ekip kapanış kaydı yok.

## Ekip

| Rol | Kişi | GitHub hesabı | Haftalık saat | Temel bilgisayar özellikleri |
|---|---|---|---|---|
| A | Mehmet | mehmetalisahingm | Henüz verilmedi | Henüz verilmedi |
| B | Utku | Henüz verilmedi | Henüz verilmedi | Henüz verilmedi |
| C | Mert | Henüz verilmedi | Henüz verilmedi | Henüz verilmedi |

İsimler kullanıcıdan alındı. Rol eşleştirmesi başlangıç atamasıdır; deneyimler bilinmediğinden uzmanlık iddiası değildir. Mert bilgileri toplar; repo erişimi için yönetici işlemlerini Mehmet yapar. Public depoya özel cihaz adı, seri numarası veya gereksiz kişisel bilgi yazılmaz.

## Ortam ve araçlar

| Konu | Durum / sorumlu |
|---|---|
| Unity/URP | 6.3 LTS önerildi; kesin yama ve gereken paketler P0'da Mehmet tarafından doğrulanacak |
| Git ve varlık düzeni | P0 Utku; büyük ikili varlıktan önce uygun LFS |
| Repo ana dalı | main; ilk plan paylaşıldı |
| Ekip erişim testi | P0 Mert koordine eder, Mehmet yetki verir; üç kişi kendi branch'ine pushlayacak |
| Örnek build | P0 Mehmet üretir; üç kişi kendi bilgisayarında çalıştırır |
| Basit branch koruması | P1 Mert, yönetici işlemlerinde Mehmet; kurulmadı |
| Otomatik build | P2 Utku, hesap/lisansta Mehmet; kurulmadı |
| Performans ortamı | P4 sonunda ölçümden önce sabitlenecek; öneri 1080p/60 FPS |
| Manuel araç istisnası | Henüz yok; gerçek engel varsa neden/sorumlu/kontrol kısa kayda yazılır |

## Faz durumu

| Faz | Durum | Birleştirme koordinatörü | Kapanış kaydı / main commit |
|---|---|---|---|
| P0 | ACIK | Mehmet | GitHub görevleri açıldı; kapanış yok |
| P1 | KILITLI | Utku | Yok |
| P2 | KILITLI | Mert | Yok |
| P3 | KILITLI | Mehmet | Yok |
| P4 | KILITLI | Utku | Yok |
| P5 | KILITLI | Mert | Yok |
| P6 | KILITLI | Mehmet | Yok |

## Kişi görevleri

Ayrıntılar [PHASES.md](PHASES.md) içindedir. Gelecek fazın BEKLIYOR görevi alınamaz.

| Görev | Sahip | Durum | Kanıt / kalan iş |
|---|---|---|---|
| P0-A | Mehmet | BEKLIYOR | [Görev #1](https://github.com/mehmetalisahingm/DeepDiveGame/issues/1); uygulama/build bekliyor |
| P0-B | Utku | BEKLIYOR | [Görev #2](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2); hesap ve dosya düzeni bekliyor |
| P0-C | Mert | BEKLIYOR | [Görev #3 / ekip bilgileri](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3); saat/donanım ve ekip erişim testi bekliyor |
| P1-A | Mehmet | BEKLIYOR | Yok |
| P1-B | Utku | BEKLIYOR | Yok |
| P1-C | Mert | BEKLIYOR | Yok |
| P2-A | Mehmet | BEKLIYOR | Yok |
| P2-B | Utku | BEKLIYOR | Yok |
| P2-C | Mert | BEKLIYOR | Yok |
| P3-A | Mehmet | BEKLIYOR | Yok |
| P3-B | Utku | BEKLIYOR | Yok |
| P3-C | Mert | BEKLIYOR | Yok |
| P4-A | Mehmet | BEKLIYOR | Yok |
| P4-B | Utku | BEKLIYOR | Yok |
| P4-C | Mert | BEKLIYOR | Yok |
| P5-A | Mehmet | BEKLIYOR | Yok |
| P5-B | Utku | BEKLIYOR | Yok |
| P5-C | Mert | BEKLIYOR | Yok |
| P6-A | Mehmet | BEKLIYOR | Yok |
| P6-B | Utku | BEKLIYOR | Yok |
| P6-C | Mert | BEKLIYOR | Yok |

## Alt iş ve destek devri

Henüz yok. Bir iş devredilirse görev, uygulayan, sistem sahibi ve değişecek dosyalar kısa bir satırla kaydedilir. Her faz açılırken gerçek kapasiteye göre alt iş süreleri tahminlenir.

## D01–D11 kısa karar kaydı

Kararların ayrıntısı PHASES içindedir. İsimli plan yazılması, ekip görüşmesinin yapıldığı anlamına gelmez.

| Karar | Plan durumu | Ekip kaydı |
|---|---|---|
| D01 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D02 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D03 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D04 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D05 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D06 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D07 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D08 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D09 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |
| D10 | Tek bölgelik ilk sürüm plan kapsamı | Ekip görüşmesi henüz yapılmadı |
| D11 | Başlangıç varsayımı | Ekip görüşmesi henüz yapılmadı |

## Faz kapanışı

Tek kısa kayıtta şu bilgiler yeterlidir:
- Üç kişinin işi birleşti mi?
- O fazın kabul testleri hangi build/commit'te geçti; yapılmayan test var mı?
- Mehmet, Utku ve Mert'in gerçek tamam mesajları nerede?
- main'e birleşmiş build kontrol edildi mi?
- P2/P3 ise temel his katmanıyla oynanış testinde devam mı, düzeltme mi kararı verildi?

Kayıt yoksa tamamlandı işaretlenmez. Ayrı imza matrisi yoktur; sonraki faz kendiliğinden açılmaz.

## Değişiklik kaydı

| Tarih | Değişiklik |
|---|---|
| 2026-08-31 | Plan 1.0 ve public repo başlangıcı oluşturuldu |
| 2026-08-31 | Kullanıcının verdiği isimlerle Mehmet=A, Utku=B, Mert=C başlangıç dağılımı yapıldı |
| 2026-08-31 | Görüşülen sadeleştirme isimli görev planına işlendi: hafif P0, P1 koruma, P2 CI, P3 oynanış kontrolü ve tek bölgelik ilk sürüm; hiçbir oyun fazı tamamlanmadı |
| 2026-08-31 | Kullanıcı revizyonu onayladı; P2 erken oynama testi ve P2/P3 temel his katmanı eklendi. P0 görevleri #1–#3 açıldı; hesap/saat bilgileri uydurulmadı, hiçbir oyun testi tamamlandı sayılmadı |
