# Güncel durum ve görev takibi

Plan 1.4 — 31 Ağustos 2026. Kullanıcı görsel/sesi Mert'e, diğer ortak sorumlulukları Mehmet'e atadı. Sade P0, P2/P3 oynama testleri ve tek bölgelik ilk sürüm korunur; atama işin veya toplantının tamamlanması değildir.

## Mevcut sınır

- Aktif faz: P0. Durum: ACIK. Mehmet'in ortak Unity proje/sahne/build hazırlığı yapıldı; oyun mekanikleri henüz uygulanmadı.
- `codex/p0-mehmet-foundation` dalında URP kamera/ışık/küp örneği ve Windows build komutu hazır. Windows x64/Mono build alındı; Mehmet'in bilgisayarında Codex ile [görsel kontrol geçti](../evidence/P0-A-9596244-visual-check.md). Diğer bilgisayarların testleri bekliyor; bu değişiklikler henüz faz dalına veya main'e birleşmedi.
- P1–P6 KILITLI. P0'ın açılması sonraki fazlara geçiş veya herhangi bir görevin tamamlanması değildir.
- Public depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame).
- MertKAYAR ve Utkuuzun14 write davetlerini kabul etti; ikisinin de gerçek push/pull testi bekleniyor. Branch koruması P1, CI P2 görevidir; kurulu oldukları iddia edilmez.
- Tamamlanmış oyun görevi veya ekip kapanış kaydı yok. Mehmet'in yerel build/dosya kontrolleri P0 kapanışı değildir.
- Ortak sorumlular atandı; P0 ekip toplantısı tamamlanmadı. Kaynak/üretim yöntemi, saatler, devam/ayrılma ayrıntıları, ürün hedefi, bütçe tutarı/paylaşımı, netcode yedeği ve D06 ekip farkındalığı hâlâ açık.

## Ekip

| Rol | Kişi | GitHub hesabı | Haftalık saat | Temel bilgisayar özellikleri |
|---|---|---|---|---|
| A | Mehmet | mehmetalisahingm | Henüz verilmedi | Yerelde doğrulandı: i5-12450H, 16 GB RAM, NVIDIA GTX 1650 / Intel UHD |
| B | Utku | Utkuuzun14 | Henüz verilmedi | Henüz verilmedi |
| C | Mert | MertKAYAR | Henüz verilmedi | Intel Core i5, RTX 4050, 16 GB RAM, Windows 11 |

İsimler kullanıcıdan alındı. Rol eşleştirmesi başlangıç atamasıdır. Mert kendi Unity/C#, ağ kodu ve görsel/ses deneyimini "deneyim yok" olarak bildirdi; bu uzmanlık iddiası değildir. Mert bilgileri toplar; repo erişimi için yönetici işlemlerini Mehmet yapar. Public depoya özel cihaz adı, seri numarası veya gereksiz kişisel bilgi yazılmaz.

## Ortam ve araçlar

| Konu | Durum / sorumlu |
|---|---|
| Unity/URP | Ortak proje depo kökünde; Unity 6000.3.23f1, URP 17.3.0, Visual Studio Editor 2.0.26 sabitlendi. Manifest ve paket kilidi kaynakta. Windows IL2CPP desteği kurulu; P0 örneği daha kısa derleme için Mono kullanıyor |
| Yerel kod araçları | Mehmet'te VS Code Unity 1.3.1, C# 2.140.9 ve C# Dev Kit 3.20.199 kuruldu. Unity Hub 3.17.2, Git 2.50.1 ve Git LFS 3.7.0 zaten kurulu; diğer bilgisayarlar doğrulanmadı |
| Git ve varlık düzeni | İlk .gitignore, Force Text ve Visible Meta Files ayarları Mehmet'in P0-A önkoşulu olarak önerildi; Utku incelemesi bekleniyor. Klasör/sahne sahipliği ve büyük varlıktan önce LFS P0-B'de |
| Repo ana dalı | main; ilk plan paylaşıldı |
| Ekip erişim testi | Mehmet kendi P0 branch'ini pushladı. Mert ve Utku'nun write erişimi aktif; kendi branch push/pull testleri bekliyor. Mert koordine eder |
| Örnek build | `P0-A-9596244`, kaynak `9596244`; [yayınlanmamış Windows ZIP](https://github.com/mehmetalisahingm/DeepDiveGame/releases), [kurulum/build rehberi](../SETUP.md). İki yerel derleme ve Mehmet'in bilgisayarında Codex görsel kontrolü başarılı; Utku/Mert'in aynı build'i çalıştırması bekliyor |
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
| P0-A | Mehmet | DEVAM | [Görev #1](https://github.com/mehmetalisahingm/DeepDiveGame/issues/1), [PR #4](https://github.com/mehmetalisahingm/DeepDiveGame/pull/4): proje, sürüm, sahne, Windows build, yerel görsel kontrol, branch push ve Mert'e ihtiyaç listesi hazır. Utku incelemesi, diğer bilgisayarların testleri, başkasının birleşmiş değişikliğini alma ve gerçek ortak kararlar bekliyor |
| P0-B | Utku | BEKLIYOR | [Görev #2](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2); write erişimi aktif ve görev Utku'ya atandı. Gerçek push/pull testi, dosya düzeni ve PR #4 incelemesi bekliyor |
| P0-C | Mert | DEVAM | [Görev #3 / ekip bilgileri](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3); write erişimi aktif. Mert kendi donanım ve deneyim bilgisini verdi; haftalık saatini boş bıraktı. Diğer ekip bilgileri, görsel/ses kaynak planı, karar kaydı, temiz klon, build ve gerçek push/pull testi bekliyor. Eksikler nedeniyle erken kapatılan görev yeniden açıldı |
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

P0-A için Mehmet asgari `.gitignore`, görünür `.meta` ve metin serileştirme ayarlarını önerdi; [P0-B koordinasyon kaydı](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2#issuecomment-5483065421) açıldı. Utku sahipliği/incelemesi korunur, P0-B tamamlandı sayılmaz. [Mehmet'in oyuncu görsel/ses ihtiyaçları](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3#issuecomment-5483112818) Mert'in kaydına eklendi. Gerçek kapasite bilinmediği için süre tahmini yapılmadı.

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

[Toplantı gündemi ve tek karar tablosu](P0_MEETING.md) T01–T07'nin kaynağıdır. Kullanıcı atamasıyla görsel/ses üretimi ve tutarlılık Mert'te; takvim/kapasite, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesi Mehmet'tedir. Mehmet kararları takip eder, Mert kayda destek olur. Netcode yedeği, bütçe/gelir paylaşımı, ticari hedef ve teslim tarihi belirlenmedi. Atamalar uzmanlık kanıtı veya üç kişinin tamamı değildir; oyun testi yapılmadı.

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
| 2026-08-31 | Plan 1.3: kullanıcı gündemi P0_MEETING'e eklendi; üretim, kapasite/ayrılma, hedef, bütçe, tasarım, insan netcode sorumluluğu ve host kayıt sınırı görünür oldu. Ekip kararları açık bırakıldı; faz ilerletilmedi |
| 2026-08-31 | Plan 1.4: kullanıcının ortak sorumlulukları üstlenme ve görsel/sesi Mert'e verme talimatı işlendi. Diğer geliştirme işleri, gerçek testler ve üç kişilik faz kapısı korundu; uzmanlık, harcama veya tamam kaydı üretilmedi |
| 2026-08-31 | Kullanıcının indirme/kurulum isteğiyle Mehmet'in bilgisayarına Unity 6000.3.23f1, Windows IL2CPP desteği ve VS Code Unity/C# eklentileri kuruldu. Unity sürümü, imzası ve Windows modül dosyaları doğrulandı; eski Unity korundu. Proje, lisansla proje açılışı ve build testi yapılmadı; P0 kapanmadı |
| 2026-08-31 | Kullanıcının verdiği Utkuuzun14 ve MertKAYAR hesaplarına write davetleri gönderildi ve GitHub API üzerinden doğrulandı; davet kabulü veya gerçek push/pull testi tamamlandı sayılmadı |
| 2026-08-31 | Mehmet'in P0-A isteğiyle ortak URP projesi, sabit sürümler, örnek sahne ve tekrarlanabilir Windows build hazırlandı. Yerel derleme ve .meta/önbellek kontrolleri yapıldı. Görsel kontrol Esc sonrası ekran aracında sürdürülemedi; yapılmış sayılmadı. Utku incelemesi, diğer bilgisayarlar ve gerçek ekip kararları açık; P0 kapanmadı |
| 2026-08-31 | Kullanıcının yeni görsel kontrol isteğinde ekran bağlantısı yenilendi. Gerçek P0 Windows penceresinde küp, zemin, ışık/gölge ve arka plan doğrulandı; ekran görüntüsü kanıtı kaydedildi. Önceki görsel test engeli kapandı. Diğer kişilerin testi/onayı üretilmedi; P0 açık kaldı |
| 2026-09-04 | MertKAYAR'ın depo write erişiminin aktif olduğu GitHub üzerinden doğrulandı; görev #3 Mert'e atandı. Gerçek branch push/pull ve temiz klon testleri henüz yapılmadı |
| 2026-09-04 | Utkuuzun14 hesabının depo write erişiminin aktif olduğu GitHub üzerinden doğrulandı; görev #2 Utku'ya atandı. Gerçek branch push/pull ve P0-B testleri henüz yapılmadı |
| 2026-09-04 | Mert kendi donanım ve deneyim bilgisini görev #3'e yazdı; haftalık saat boş kaldı ve diğer P0-C teslimleri için branch/commit/PR/test kanıtı oluşmadı. Erken kapatılan görev yeniden açıldı |
