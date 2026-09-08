# Güncel durum ve görev takibi

Plan 1.6 — 8 Eylül 2026. P0 üç ayrı kurulumda tamamlandı; aktif faz P1'dir. Görsel/ses Mert'te, diğer ortak sorumluluklar Mehmet'tedir.

## Mevcut sınır

- Aktif faz: P1. Durum: ACIK. Mehmet'in bağlantı/oyuncu uygulaması [PR #16](https://github.com/mehmetalisahingm/DeepDiveGame/pull/16) içinde yerel testlerden geçti; henüz faz dalına birleşmedi. Utku/Mert teslimleri ve ayrı bilgisayarlarda internet kabulü bekliyor.
- P0 KAPALI: Mehmet, Utku ve Mert ortak projeyi Unity `6000.3.23f1` ile açtı ve Windows build'ini çalıştırdı. [Kapanış kaydı](../reports/P0-REPORT.md).
- P2–P6 KILITLI. P1'in açılması görevlerin tamamlandığı anlamına gelmez.
- Public depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame).
- MertKAYAR ve Utkuuzun14 write davetlerini kabul etti. Utku'nun PR #7 ve Mert'in PR #10 değişikliği main'e birleşti; üç kişinin P0 kurulum/build testi geçti.
- P0 kapanış kaydı var; henüz oynanış veya multiplayer kabulü yok. P1 testleri ayrıca yapılacak.
- Ortak sorumlular atandı. Kaynak/üretim yöntemi, devam/ayrılma ayrıntıları, ürün hedefi, bütçe tutarı/paylaşımı, netcode yedeği ve D06 ekip farkındalığı hâlâ açık; kullanıcı kararıyla bunlar P0 kapanışını bekletmez ve ilgili işe başlamadan ele alınır.

## Ekip

| Rol | Kişi | GitHub hesabı |
|---|---|---|
| A | Mehmet | mehmetalisahingm |
| B | Utku | Utkuuzun14 |
| C | Mert | MertKAYAR |

İsimler kullanıcıdan alındı. Rol eşleştirmesi başlangıç atamasıdır; uzmanlık iddiası değildir. Repo erişimi için yönetici işlemlerini Mehmet yapar. Public depoya gereksiz kişisel bilgi yazılmaz.

## Ortam ve araçlar

| Konu | Durum / sorumlu |
|---|---|
| Unity/URP | Ortak proje depo kökünde; Unity 6000.3.23f1, URP 17.3.0, Visual Studio Editor 2.0.26 sabitlendi. Manifest ve paket kilidi kaynakta. Windows IL2CPP desteği kurulu; P0 örneği daha kısa derleme için Mono kullanıyor |
| Yerel kod araçları | Mehmet'te VS Code Unity 1.3.1, C# 2.140.9 ve C# Dev Kit 3.20.199 kuruldu. Unity Hub 3.17.2, Git 2.50.1 ve Git LFS 3.7.0 zaten kurulu; diğer bilgisayarlar doğrulanmadı |
| Git ve varlık düzeni | Unity `.gitignore` ve `.gitattributes` kuralları Utku'nun [PR #7](https://github.com/mehmetalisahingm/DeepDiveGame/pull/7) değişikliğiyle faz dalına birleşti; LFS ve Smart Merge kontrolleri geçti. Projede Visible Meta Files ve Force Text kayıtlı; alan sahipliği CONTRACTS/WORKFLOW içinde belli. P0 incelemesi aranmaz |
| Repo ana dalı | main; ilk plan paylaşıldı |
| Ekip erişim testi | Üç hesabın erişimi aktif; Mehmet, Utku ve Mert güncel ortak projeyi aldı ve açtı |
| Örnek build | `P0-A-9596244`, kaynak `9596244`; [kurulum/build rehberi](../SETUP.md). Mehmet, Utku ve Mert'in Windows build kontrolleri başarılı |
| Basit branch koruması | P1 Mert, yönetici işlemlerinde Mehmet; kurulmadı |
| Otomatik build | P2 Utku, hesap/lisansta Mehmet; kurulmadı |
| Performans ortamı | P4 sonunda ölçümden önce sabitlenecek; öneri 1080p/60 FPS |
| Manuel araç istisnası | Henüz yok; gerçek engel varsa neden/sorumlu/kontrol kısa kayda yazılır |

## Faz durumu

| Faz | Durum | Birleştirme koordinatörü | Kapanış kaydı / main commit |
|---|---|---|---|
| P0 | KAPALI | Mehmet | [P0 kapanış kaydı](../reports/P0-REPORT.md); doğrulanan temel `14e34f1` |
| P1 | ACIK | Utku | [Görevler #12–#14](https://github.com/mehmetalisahingm/DeepDiveGame/issues?q=is%3Aissue+is%3Aopen+P1) açıldı; kapanış yok |
| P2 | KILITLI | Mert | Yok |
| P3 | KILITLI | Mehmet | Yok |
| P4 | KILITLI | Utku | Yok |
| P5 | KILITLI | Mert | Yok |
| P6 | KILITLI | Mehmet | Yok |

## Kişi görevleri

Ayrıntılar [PHASES.md](PHASES.md) içindedir. Gelecek fazın BEKLIYOR görevi alınamaz.

| Görev | Sahip | Durum | Kanıt / kalan iş |
|---|---|---|---|
| P0-A | Mehmet | TAMAM | [Görev #1](https://github.com/mehmetalisahingm/DeepDiveGame/issues/1), [PR #4](https://github.com/mehmetalisahingm/DeepDiveGame/pull/4), [PR #8](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8): ortak temel ve Windows build teslim edildi |
| P0-B | Utku | TAMAM | [Görev #2](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2), [PR #7](https://github.com/mehmetalisahingm/DeepDiveGame/pull/7): Git düzeni birleşti; Unity açılışı ve Windows build testi geçti |
| P0-C | Mert | TAMAM | [Görev #3](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3), [PR #10](https://github.com/mehmetalisahingm/DeepDiveGame/pull/10): güncel main'i aldı, Unity `6000.3.23f1` ile açtı, gerçek Windows build aldı ve çalıştırdı; görsel/ses kaynak planı main'e birleşti |
| P1-A | Mehmet | DEVAM | [PR #16](https://github.com/mehmetalisahingm/DeepDiveGame/pull/16): oda/katıl/ayrıl, oyuncu oluşumu, host yetkili yürüme/yüzme ve ağ sahne yükleme yazıldı. Windows build, 7 test ve yerel 1/4 oyunculu kontroller geçti. Utku incelemesi, Mert/Utku entegrasyonu ve internet testi bekliyor. [Çalıştırma](../P1_NETWORK.md) |
| P1-B | Utku | DEVAM | [Görev #13](https://github.com/mehmetalisahingm/DeepDiveGame/issues/13): hazırlık alanı, tek sualtı test alanı, çarpışmalar ve P1 birleştirme koordinasyonu |
| P1-C | Mert | DEVAM | [Görev #14](https://github.com/mehmetalisahingm/DeepDiveGame/issues/14): oda/hazır ekranı, `SessionState` geçişleri ve basit branch koruması |
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

P0-A için Mehmet asgari `.gitignore`, görünür `.meta` ve metin serileştirme ayarlarını önerdi; [P0-B koordinasyon kaydı](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2#issuecomment-5483065421) açıldı. Utku'nun `.gitignore` ve `.gitattributes` teslimi PR #7 ile faz dalına birleşti; diğer P0-B işleri açık olduğu için görev tamamlandı sayılmaz. [Mehmet'in oyuncu görsel/ses ihtiyaçları](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3#issuecomment-5483112818) Mert'in kaydına eklendi.

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

[Toplantı gündemi ve tek karar tablosu](P0_MEETING.md) T01–T07'nin referansıdır. Kullanıcı atamasıyla görsel/ses üretimi ve tutarlılık Mert'te; takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesi Mehmet'tedir. Açık kararlar ilgili özelliğe başlamadan ele alınır; P0 kapanışını bekletmez. Atamalar uzmanlık kanıtı veya harcama izni değildir.

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
| 2026-08-31 | Mehmet'in P0-A isteğiyle ortak URP projesi, sabit sürümler, örnek sahne ve tekrarlanabilir Windows build hazırlandı. Yerel derleme ve .meta/önbellek kontrolleri yapıldı. Görsel kontrol Esc sonrası ekran aracında sürdürülemedi; yapılmış sayılmadı. Utku incelemesi, diğer bilgisayarlar ve gerçek ekip kararları açık; P0 kapanmadı |
| 2026-08-31 | Kullanıcının yeni görsel kontrol isteğinde ekran bağlantısı yenilendi. Gerçek P0 Windows penceresinde küp, zemin, ışık/gölge ve arka plan doğrulandı; ekran görüntüsü kanıtı kaydedildi. Önceki görsel test engeli kapandı. Diğer kişilerin testi/onayı üretilmedi; P0 açık kaldı |
| 2026-09-04 | MertKAYAR'ın depo write erişiminin aktif olduğu GitHub üzerinden doğrulandı; görev #3 Mert'e atandı. Gerçek branch push/pull ve temiz klon testleri henüz yapılmadı |
| 2026-09-04 | Utkuuzun14 hesabının depo write erişiminin aktif olduğu GitHub üzerinden doğrulandı; görev #2 Utku'ya atandı. Gerçek branch push/pull ve P0-B testleri henüz yapılmadı |
| 2026-09-04 | Mert'in erken kapattığı görev #3, P0-C teslimleri için branch/commit/PR/test kanıtı oluşmadığından yeniden açıldı |
| 2026-09-04 | P0-C görevi kullanıcı kararıyla kişisel bilgi toplamadan yalnızca proje teslimlerine odaklanacak şekilde sadeleştirildi |
| 2026-09-06 | Utku'nun PR #7 değişikliği P0 faz dalına birleşti. Unity `.gitignore`, LFS ve Smart Merge kuralları doğrulandı; Mehmet birleşmiş değişikliği kendi branch'ine aldı. Visible Meta Files, Force Text, sahiplik, PR #4 incelemesi ve ekip testleri açık olduğundan P0-B ve P0 tamamlanmadı |
| 2026-09-06 | Kullanıcı kararıyla P0 karşılıklı inceleme ve ayrı deneme branch'i şartlarından arındırıldı. Ortak temel main'e birleşip Mehmet, Utku ve Mert güncel projeyi aynı Unity sürümünde açar ve ortak Windows build'ini çalıştırırsa P0 kapanacak |
| 2026-09-06 | Mehmet'in PR #4 değişikliği faz dalına birleşti ve P0 main aktarımı için PR #8 açıldı. Mevcut JPG kanıtı yeni LFS kuralına uygun pointer olarak düzeltildi; P0 arkadaşların indirme/açma/build sonucu gelene kadar açık |
| 2026-09-06 | PR #8 main'e birleşti (`169b245`). P0'ın kalan tek kapısı Utku ve Mert'in güncel main'i indirip Unity'de açması ve ortak Windows build'ini çalıştırmasıdır |
| 2026-09-07 | Mert güncel main `d46faff` üzerinde Unity açılışı ve Windows build çalıştırma sonucunu görev #3'e kaydetti. Görsel/ses kaynak planı PR #10 ile main'e birleşti (`2f9c267`); P0 yalnızca Utku'nun sonucunu bekliyor |
| 2026-09-08 | Utku main `14e34f1` üzerinde Unity açılışı, sahne ve Windows build çalıştırma sonucunu görev #2'ye kaydetti. Üç kişinin P0 kanıtı tamamlandı; P0 kapatıldı, P1 açıldı ve görevler #12–#14 oluşturuldu |
