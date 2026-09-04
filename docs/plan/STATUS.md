# Güncel durum ve görev takibi

Plan 1.4 — 31 Ağustos 2026. Kullanıcı görsel/sesi Mert'e, diğer ortak sorumlulukları Mehmet'e atadı. Sade P0, P2/P3 oynama testleri ve tek bölgelik ilk sürüm korunur; atama işin veya toplantının tamamlanması değildir.

## Mevcut sınır

- Aktif faz: P0. Durum: ACIK. Kullanıcının başlama isteğiyle GitHub görev kayıtları açıldı; oyun uygulaması henüz yapılmadı.
- Unity/oyun uygulaması başlamadı; çalışan oyun/build yok.
- P1–P6 KILITLI. P0'ın açılması sonraki fazlara geçiş veya herhangi bir görevin tamamlanması değildir.
- Public depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame).
- Utkuuzun14 ve MertKAYAR hesaplarına yazma (write) daveti gönderildi; kabul ve gerçek push/pull testi bekleniyor. Branch koruması P1, CI P2 görevidir; kurulu oldukları iddia edilmez.
- Tamamlanmış oyun görevi, faz testi veya ekip kapanış kaydı yok.
- Ortak sorumlular atandı; P0 ekip toplantısı tamamlanmadı. Kaynak/üretim yöntemi, devam/ayrılma ayrıntıları, ürün hedefi, bütçe tutarı/paylaşımı, netcode yedeği ve D06 ekip farkındalığı hâlâ açık.

## Ekip

| Rol | Kişi | GitHub hesabı |
|---|---|---|
| A | Mehmet | mehmetalisahingm |
| B | Utku | Utkuuzun14 |
| C | Mert | MertKAYAR |

İsimler kullanıcıdan alındı. Rol eşleştirmesi başlangıç atamasıdır; deneyimler bilinmediğinden uzmanlık iddiası değildir. Repo erişimi için yönetici işlemlerini Mehmet yapar. Public depoya gereksiz kişisel bilgi yazılmaz.

## Ortam ve araçlar

| Konu | Durum / sorumlu |
|---|---|
| Unity/URP | Mehmet'te Unity 6.3 LTS (6000.3.23f1) ve Windows Build Support (IL2CPP) kuruldu; çalıştırılan editör sürümü doğrulandı. Ortak proje/URP ve paket sürümlerinin sabitlenmesi henüz yapılmadı |
| Yerel kod araçları | Mehmet'te VS Code Unity 1.3.1, C# 2.140.9 ve C# Dev Kit 3.20.199 kuruldu. Unity Hub 3.17.2, Git 2.50.1 ve Git LFS 3.7.0 zaten kurulu; diğer bilgisayarlar doğrulanmadı |
| Git ve varlık düzeni | P0 Utku; büyük ikili varlıktan önce uygun LFS |
| Repo ana dalı | main; ilk plan paylaşıldı |
| Ekip erişim testi | Mehmet iki hesaba write daveti gönderdi; davet kabulü bekleniyor. Mert koordine eder; üç kişinin kendi branch'ine push/pull testi henüz yapılmadı |
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
| P0-A | Mehmet | BEKLIYOR | [Görev #1](https://github.com/mehmetalisahingm/DeepDiveGame/issues/1); uygulama/build ve ortak karar takibi bekliyor |
| P0-B | Utku | BEKLIYOR | [Görev #2](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2); hesap biliniyor, davet kabulü ve dosya düzeni bekliyor |
| P0-C | Mert | BEKLIYOR | [Görev #3](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3); görsel/ses kaynak planı, karar kaydı ve erişim testi bekliyor |
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

## P0 ekip kararları

[Toplantı gündemi ve tek karar tablosu](P0_MEETING.md) T01–T07'nin kaynağıdır. Kullanıcı atamasıyla görsel/ses üretimi ve tutarlılık Mert'te; takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesi Mehmet'tedir. Mehmet kararları takip eder, Mert kayda destek olur. Netcode yedeği, bütçe/gelir paylaşımı, ticari hedef ve teslim tarihi belirlenmedi. Atamalar uzmanlık kanıtı veya üç kişinin tamamı değildir; oyun testi yapılmadı.

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
| 2026-08-31 | Kullanıcı revizyonu onayladı; P2 erken oynama testi ve P2/P3 temel his katmanı eklendi. P0 görevleri #1–#3 açıldı; hiçbir oyun testi tamamlandı sayılmadı |
| 2026-08-31 | Plan 1.3: kullanıcı gündemi P0_MEETING'e eklendi; üretim, devam/ayrılma, hedef, bütçe, tasarım, insan netcode sorumluluğu ve host kayıt sınırı görünür oldu. Ekip kararları açık bırakıldı; faz ilerletilmedi |
| 2026-08-31 | Plan 1.4: kullanıcının ortak sorumlulukları üstlenme ve görsel/sesi Mert'e verme talimatı işlendi. Diğer geliştirme işleri, gerçek testler ve üç kişilik faz kapısı korundu; uzmanlık, harcama veya tamam kaydı üretilmedi |
| 2026-08-31 | Kullanıcının indirme/kurulum isteğiyle Mehmet'in bilgisayarına Unity 6000.3.23f1, Windows IL2CPP desteği ve VS Code Unity/C# eklentileri kuruldu. Unity sürümü, imzası ve Windows modül dosyaları doğrulandı; eski Unity korundu. Proje, lisansla proje açılışı ve build testi yapılmadı; P0 kapanmadı |
| 2026-08-31 | Kullanıcının verdiği Utkuuzun14 ve MertKAYAR hesaplarına write davetleri gönderildi ve GitHub API üzerinden doğrulandı; davet kabulü veya gerçek push/pull testi tamamlandı sayılmadı |
| 2026-09-04 | P0-C görevi kullanıcı kararıyla kişisel bilgi toplamadan yalnızca proje teslimlerine odaklanacak şekilde sadeleştirildi |
