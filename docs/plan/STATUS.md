# GÃ¼ncel durum ve gÃ¶rev takibi

Plan 1.8 — 10 Eylül 2026. Aktif faz P2; koordinatör Mert.

## Mevcut sÄ±nÄ±r

- Aktif faz: P2. Durum: ACIK. P1 kullanıcı kararıyla kapandı; doğrulanmamış kontroller devredildi. [Kapanış ve açık kontroller](../reports/P1-REPORT.md). Ortak dal: `codex/p2-integration`.
- P0 KAPALI: Mehmet, Utku ve Mert ortak projeyi Unity `6000.3.23f1` ile aÃ§tÄ± ve Windows build'ini Ã§alÄ±ÅŸtÄ±rdÄ±. [KapanÄ±ÅŸ kaydÄ±](../reports/P0-REPORT.md).
- P3–P6 KILITLI. Yalnızca P2 görevleri alınır.
- Public depo: [mehmetalisahingm/DeepDiveGame](https://github.com/mehmetalisahingm/DeepDiveGame).
- MertKAYAR ve Utkuuzun14 write davetlerini kabul etti. Utku'nun PR #7 ve Mert'in PR #10 deÄŸiÅŸikliÄŸi main'e birleÅŸti; Ã¼Ã§ kiÅŸinin P0 kurulum/build testi geÃ§ti.
- P1 yerel testleri ve kullanıcı bildirimli internet bağlantısı tamamlandı; kalan kontroller kapanış kaydındadır.
- Ortak sorumlular atandÄ±. Kaynak/Ã¼retim yÃ¶ntemi, devam/ayrÄ±lma ayrÄ±ntÄ±larÄ±, Ã¼rÃ¼n hedefi, bÃ¼tÃ§e tutarÄ±/paylaÅŸÄ±mÄ±, netcode yedeÄŸi ve D06 ekip farkÄ±ndalÄ±ÄŸÄ± hÃ¢lÃ¢ aÃ§Ä±k; kullanÄ±cÄ± kararÄ±yla bunlar P0 kapanÄ±ÅŸÄ±nÄ± bekletmez ve ilgili iÅŸe baÅŸlamadan ele alÄ±nÄ±r.

## Ekip

| Rol | KiÅŸi | GitHub hesabÄ± |
|---|---|---|
| A | Mehmet | mehmetalisahingm |
| B | Utku | Utkuuzun14 |
| C | Mert | MertKAYAR |

Ä°simler kullanÄ±cÄ±dan alÄ±ndÄ±. Rol eÅŸleÅŸtirmesi baÅŸlangÄ±Ã§ atamasÄ±dÄ±r; uzmanlÄ±k iddiasÄ± deÄŸildir. Repo eriÅŸimi iÃ§in yÃ¶netici iÅŸlemlerini Mehmet yapar. Public depoya gereksiz kiÅŸisel bilgi yazÄ±lmaz.

## Ortam ve araÃ§lar

| Konu | Durum / sorumlu |
|---|---|
| Unity/URP | Ortak proje depo kÃ¶kÃ¼nde; Unity 6000.3.23f1, URP 17.3.0, Visual Studio Editor 2.0.26 sabitlendi. Manifest ve paket kilidi kaynakta. Windows IL2CPP desteÄŸi kurulu; P0 Ã¶rneÄŸi daha kÄ±sa derleme iÃ§in Mono kullanÄ±yor |
| Yerel kod araÃ§larÄ± | Mehmet'te VS Code Unity 1.3.1, C# 2.140.9 ve C# Dev Kit 3.20.199 kuruldu. Unity Hub 3.17.2, Git 2.50.1 ve Git LFS 3.7.0 zaten kurulu; diÄŸer bilgisayarlar doÄŸrulanmadÄ± |
| Git ve varlÄ±k dÃ¼zeni | Unity `.gitignore` ve `.gitattributes` kurallarÄ± Utku'nun [PR #7](https://github.com/mehmetalisahingm/DeepDiveGame/pull/7) deÄŸiÅŸikliÄŸiyle faz dalÄ±na birleÅŸti; LFS ve Smart Merge kontrolleri geÃ§ti. Projede Visible Meta Files ve Force Text kayÄ±tlÄ±; alan sahipliÄŸi CONTRACTS/WORKFLOW iÃ§inde belli. P0 incelemesi aranmaz |
| Repo ana dalÄ± | main; ilk plan paylaÅŸÄ±ldÄ± |
| Ekip eriÅŸim testi | ÃœÃ§ hesabÄ±n eriÅŸimi aktif; Mehmet, Utku ve Mert gÃ¼ncel ortak projeyi aldÄ± ve aÃ§tÄ± |
| Ã–rnek build | `P0-A-9596244`, kaynak `9596244`; [kurulum/build rehberi](../SETUP.md). Mehmet, Utku ve Mert'in Windows build kontrolleri baÅŸarÄ±lÄ± |
| Basit branch korumasÄ± | P1 Mert, yÃ¶netici iÅŸlemlerinde Mehmet; kurulmadÄ± |
| Otomatik build | P2 Utku, hesap/lisansta Mehmet; kurulmadÄ± |
| Performans ortamÄ± | P4 sonunda Ã¶lÃ§Ã¼mden Ã¶nce sabitlenecek; Ã¶neri 1080p/60 FPS |
| Manuel araÃ§ istisnasÄ± | HenÃ¼z yok; gerÃ§ek engel varsa neden/sorumlu/kontrol kÄ±sa kayda yazÄ±lÄ±r |

## Faz durumu

| Faz | Durum | BirleÅŸtirme koordinatÃ¶rÃ¼ | KapanÄ±ÅŸ kaydÄ± / main commit |
|---|---|---|---|
| P0 | KAPALI | Mehmet | [P0 kapanÄ±ÅŸ kaydÄ±](../reports/P0-REPORT.md); doÄŸrulanan temel `14e34f1` |
| P1 | KAPALI (kullanıcı kararı) | Utku | [Kapanış / devredilen kontroller](../reports/P1-REPORT.md) |
| P2 | ACIK | Mert | A/C kodu birleşti; 27 test ve yerel iki oyuncu regresyonu geçti. [Kayıt](../reports/P2-AC-MERGE.md); av/çanta entegrasyonu bekliyor |
| P3 | KILITLI | Mehmet | Yok |
| P4 | KILITLI | Utku | Yok |
| P5 | KILITLI | Mert | Yok |
| P6 | KILITLI | Mehmet | Yok |

## KiÅŸi gÃ¶revleri

AyrÄ±ntÄ±lar [PHASES.md](PHASES.md) iÃ§indedir. Gelecek fazÄ±n BEKLIYOR gÃ¶revi alÄ±namaz.

| GÃ¶rev | Sahip | Durum | KanÄ±t / kalan iÅŸ |
|---|---|---|---|
| P0-A | Mehmet | TAMAM | [GÃ¶rev #1](https://github.com/mehmetalisahingm/DeepDiveGame/issues/1), [PR #4](https://github.com/mehmetalisahingm/DeepDiveGame/pull/4), [PR #8](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8): ortak temel ve Windows build teslim edildi |
| P0-B | Utku | TAMAM | [GÃ¶rev #2](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2), [PR #7](https://github.com/mehmetalisahingm/DeepDiveGame/pull/7): Git dÃ¼zeni birleÅŸti; Unity aÃ§Ä±lÄ±ÅŸÄ± ve Windows build testi geÃ§ti |
| P0-C | Mert | TAMAM | [GÃ¶rev #3](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3), [PR #10](https://github.com/mehmetalisahingm/DeepDiveGame/pull/10): gÃ¼ncel main'i aldÄ±, Unity `6000.3.23f1` ile aÃ§tÄ±, gerÃ§ek Windows build aldÄ± ve Ã§alÄ±ÅŸtÄ±rdÄ±; gÃ¶rsel/ses kaynak planÄ± main'e birleÅŸti |
| P1-A | Mehmet | TESLIM | Kod birleşti; kullanıcı kararıyla faz kapandı. [Eksik kontroller](../reports/P1-REPORT.md) |
| P1-B | Utku | TESLIM | Kod birleşti; kullanıcı kararıyla faz kapandı. [Eksik kontroller](../reports/P1-REPORT.md) |
| P1-C | Mert | TESLIM | Kod birleşti; kullanıcı kararıyla faz kapandı. [Eksik kontroller](../reports/P1-REPORT.md) |
| P2-A | Mehmet | ENTEGRASYON_BEKLIYOR | PR #25 birleşti; [doğrulanan sonuçlar ve eksikler](../reports/P2-AC-MERGE.md) |
| P2-B | Utku | HAZIR | [Tek balık, avlanma, sis/ışık ve CI](https://github.com/mehmetalisahingm/DeepDiveGame/issues/21) |
| P2-C | Mert | ENTEGRASYON_BEKLIYOR | PR #24 birleşti; [doğrulanan sonuçlar ve eksikler](../reports/P2-AC-MERGE.md) |
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

## Alt iÅŸ ve destek devri

P0-A iÃ§in Mehmet asgari `.gitignore`, gÃ¶rÃ¼nÃ¼r `.meta` ve metin serileÅŸtirme ayarlarÄ±nÄ± Ã¶nerdi; [P0-B koordinasyon kaydÄ±](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2#issuecomment-5483065421) aÃ§Ä±ldÄ±. Utku'nun `.gitignore` ve `.gitattributes` teslimi PR #7 ile faz dalÄ±na birleÅŸti; diÄŸer P0-B iÅŸleri aÃ§Ä±k olduÄŸu iÃ§in gÃ¶rev tamamlandÄ± sayÄ±lmaz. [Mehmet'in oyuncu gÃ¶rsel/ses ihtiyaÃ§larÄ±](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3#issuecomment-5483112818) Mert'in kaydÄ±na eklendi.

## D01â€“D11 kÄ±sa karar kaydÄ±

KararlarÄ±n ayrÄ±ntÄ±sÄ± PHASES iÃ§indedir. Ä°simli plan yazÄ±lmasÄ±, ekip gÃ¶rÃ¼ÅŸmesinin yapÄ±ldÄ±ÄŸÄ± anlamÄ±na gelmez.

| Karar | Plan durumu | Ekip kaydÄ± |
|---|---|---|
| D01 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D02 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D03 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D04 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D05 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D06 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D07 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D08 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D09 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D10 | Tek bÃ¶lgelik ilk sÃ¼rÃ¼m plan kapsamÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |
| D11 | BaÅŸlangÄ±Ã§ varsayÄ±mÄ± | Ekip gÃ¶rÃ¼ÅŸmesi henÃ¼z yapÄ±lmadÄ± |

## P0 ekip kararlarÄ±

[ToplantÄ± gÃ¼ndemi ve tek karar tablosu](P0_MEETING.md) T01â€“T07'nin referansÄ±dÄ±r. KullanÄ±cÄ± atamasÄ±yla gÃ¶rsel/ses Ã¼retimi ve tutarlÄ±lÄ±k Mert'te; takvim, devam/ayrÄ±lma planÄ±, Ã¼rÃ¼n hedefi, tasarÄ±mda son karar, bÃ¼tÃ§e/servis takibi ve birincil netcode incelemesi Mehmet'tedir. AÃ§Ä±k kararlar ilgili Ã¶zelliÄŸe baÅŸlamadan ele alÄ±nÄ±r; P0 kapanÄ±ÅŸÄ±nÄ± bekletmez. Atamalar uzmanlÄ±k kanÄ±tÄ± veya harcama izni deÄŸildir.

## Faz kapanÄ±ÅŸÄ±

Tek kÄ±sa kayÄ±tta ÅŸu bilgiler yeterlidir:
- ÃœÃ§ kiÅŸinin iÅŸi birleÅŸti mi?
- O fazÄ±n kabul testleri hangi build/commit'te geÃ§ti; yapÄ±lmayan test var mÄ±?
- Mehmet, Utku ve Mert'in gerÃ§ek tamam mesajlarÄ± nerede?
- main'e birleÅŸmiÅŸ build kontrol edildi mi?
- P2/P3 ise temel his katmanÄ±yla oynanÄ±ÅŸ testinde devam mÄ±, dÃ¼zeltme mi kararÄ± verildi?

KayÄ±t yoksa tamamlandÄ± iÅŸaretlenmez. AyrÄ± imza matrisi yoktur; sonraki faz kendiliÄŸinden aÃ§Ä±lmaz.

## DeÄŸiÅŸiklik kaydÄ±

| Tarih | DeÄŸiÅŸiklik |
|---|---|
| 2026-08-31 | Plan 1.0 ve public repo baÅŸlangÄ±cÄ± oluÅŸturuldu |
| 2026-08-31 | KullanÄ±cÄ±nÄ±n verdiÄŸi isimlerle Mehmet=A, Utku=B, Mert=C baÅŸlangÄ±Ã§ daÄŸÄ±lÄ±mÄ± yapÄ±ldÄ± |
| 2026-08-31 | GÃ¶rÃ¼ÅŸÃ¼len sadeleÅŸtirme isimli gÃ¶rev planÄ±na iÅŸlendi: hafif P0, P1 koruma, P2 CI, P3 oynanÄ±ÅŸ kontrolÃ¼ ve tek bÃ¶lgelik ilk sÃ¼rÃ¼m; hiÃ§bir oyun fazÄ± tamamlanmadÄ± |
| 2026-08-31 | KullanÄ±cÄ± revizyonu onayladÄ±; P2 erken oynama testi ve P2/P3 temel his katmanÄ± eklendi. P0 gÃ¶revleri #1â€“#3 aÃ§Ä±ldÄ±; hiÃ§bir oyun testi tamamlandÄ± sayÄ±lmadÄ± |
| 2026-08-31 | Plan 1.3: kullanÄ±cÄ± gÃ¼ndemi P0_MEETING'e eklendi; Ã¼retim, devam/ayrÄ±lma, hedef, bÃ¼tÃ§e, tasarÄ±m, insan netcode sorumluluÄŸu ve host kayÄ±t sÄ±nÄ±rÄ± gÃ¶rÃ¼nÃ¼r oldu. Ekip kararlarÄ± aÃ§Ä±k bÄ±rakÄ±ldÄ±; faz ilerletilmedi |
| 2026-08-31 | Plan 1.4: kullanÄ±cÄ±nÄ±n ortak sorumluluklarÄ± Ã¼stlenme ve gÃ¶rsel/sesi Mert'e verme talimatÄ± iÅŸlendi. DiÄŸer geliÅŸtirme iÅŸleri, gerÃ§ek testler ve Ã¼Ã§ kiÅŸilik faz kapÄ±sÄ± korundu; uzmanlÄ±k, harcama veya tamam kaydÄ± Ã¼retilmedi |
| 2026-08-31 | KullanÄ±cÄ±nÄ±n indirme/kurulum isteÄŸiyle Mehmet'in bilgisayarÄ±na Unity 6000.3.23f1, Windows IL2CPP desteÄŸi ve VS Code Unity/C# eklentileri kuruldu. Unity sÃ¼rÃ¼mÃ¼, imzasÄ± ve Windows modÃ¼l dosyalarÄ± doÄŸrulandÄ±; eski Unity korundu. Proje, lisansla proje aÃ§Ä±lÄ±ÅŸÄ± ve build testi yapÄ±lmadÄ±; P0 kapanmadÄ± |
| 2026-08-31 | KullanÄ±cÄ±nÄ±n verdiÄŸi Utkuuzun14 ve MertKAYAR hesaplarÄ±na write davetleri gÃ¶nderildi ve GitHub API Ã¼zerinden doÄŸrulandÄ±; davet kabulÃ¼ veya gerÃ§ek push/pull testi tamamlandÄ± sayÄ±lmadÄ± |
| 2026-08-31 | Mehmet'in P0-A isteÄŸiyle ortak URP projesi, sabit sÃ¼rÃ¼mler, Ã¶rnek sahne ve tekrarlanabilir Windows build hazÄ±rlandÄ±. Yerel derleme ve .meta/Ã¶nbellek kontrolleri yapÄ±ldÄ±. GÃ¶rsel kontrol Esc sonrasÄ± ekran aracÄ±nda sÃ¼rdÃ¼rÃ¼lemedi; yapÄ±lmÄ±ÅŸ sayÄ±lmadÄ±. Utku incelemesi, diÄŸer bilgisayarlar ve gerÃ§ek ekip kararlarÄ± aÃ§Ä±k; P0 kapanmadÄ± |
| 2026-08-31 | KullanÄ±cÄ±nÄ±n yeni gÃ¶rsel kontrol isteÄŸinde ekran baÄŸlantÄ±sÄ± yenilendi. GerÃ§ek P0 Windows penceresinde kÃ¼p, zemin, Ä±ÅŸÄ±k/gÃ¶lge ve arka plan doÄŸrulandÄ±; ekran gÃ¶rÃ¼ntÃ¼sÃ¼ kanÄ±tÄ± kaydedildi. Ã–nceki gÃ¶rsel test engeli kapandÄ±. DiÄŸer kiÅŸilerin testi/onayÄ± Ã¼retilmedi; P0 aÃ§Ä±k kaldÄ± |
| 2026-09-04 | MertKAYAR'Ä±n depo write eriÅŸiminin aktif olduÄŸu GitHub Ã¼zerinden doÄŸrulandÄ±; gÃ¶rev #3 Mert'e atandÄ±. GerÃ§ek branch push/pull ve temiz klon testleri henÃ¼z yapÄ±lmadÄ± |
| 2026-09-04 | Utkuuzun14 hesabÄ±nÄ±n depo write eriÅŸiminin aktif olduÄŸu GitHub Ã¼zerinden doÄŸrulandÄ±; gÃ¶rev #2 Utku'ya atandÄ±. GerÃ§ek branch push/pull ve P0-B testleri henÃ¼z yapÄ±lmadÄ± |
| 2026-09-04 | Mert'in erken kapattÄ±ÄŸÄ± gÃ¶rev #3, P0-C teslimleri iÃ§in branch/commit/PR/test kanÄ±tÄ± oluÅŸmadÄ±ÄŸÄ±ndan yeniden aÃ§Ä±ldÄ± |
| 2026-09-04 | P0-C gÃ¶revi kullanÄ±cÄ± kararÄ±yla kiÅŸisel bilgi toplamadan yalnÄ±zca proje teslimlerine odaklanacak ÅŸekilde sadeleÅŸtirildi |
| 2026-09-06 | Utku'nun PR #7 deÄŸiÅŸikliÄŸi P0 faz dalÄ±na birleÅŸti. Unity `.gitignore`, LFS ve Smart Merge kurallarÄ± doÄŸrulandÄ±; Mehmet birleÅŸmiÅŸ deÄŸiÅŸikliÄŸi kendi branch'ine aldÄ±. Visible Meta Files, Force Text, sahiplik, PR #4 incelemesi ve ekip testleri aÃ§Ä±k olduÄŸundan P0-B ve P0 tamamlanmadÄ± |
| 2026-09-06 | KullanÄ±cÄ± kararÄ±yla P0 karÅŸÄ±lÄ±klÄ± inceleme ve ayrÄ± deneme branch'i ÅŸartlarÄ±ndan arÄ±ndÄ±rÄ±ldÄ±. Ortak temel main'e birleÅŸip Mehmet, Utku ve Mert gÃ¼ncel projeyi aynÄ± Unity sÃ¼rÃ¼mÃ¼nde aÃ§ar ve ortak Windows build'ini Ã§alÄ±ÅŸtÄ±rÄ±rsa P0 kapanacak |
| 2026-09-06 | Mehmet'in PR #4 deÄŸiÅŸikliÄŸi faz dalÄ±na birleÅŸti ve P0 main aktarÄ±mÄ± iÃ§in PR #8 aÃ§Ä±ldÄ±. Mevcut JPG kanÄ±tÄ± yeni LFS kuralÄ±na uygun pointer olarak dÃ¼zeltildi; P0 arkadaÅŸlarÄ±n indirme/aÃ§ma/build sonucu gelene kadar aÃ§Ä±k |
| 2026-09-06 | PR #8 main'e birleÅŸti (`169b245`). P0'Ä±n kalan tek kapÄ±sÄ± Utku ve Mert'in gÃ¼ncel main'i indirip Unity'de aÃ§masÄ± ve ortak Windows build'ini Ã§alÄ±ÅŸtÄ±rmasÄ±dÄ±r |
| 2026-09-07 | Mert gÃ¼ncel main `d46faff` Ã¼zerinde Unity aÃ§Ä±lÄ±ÅŸÄ± ve Windows build Ã§alÄ±ÅŸtÄ±rma sonucunu gÃ¶rev #3'e kaydetti. GÃ¶rsel/ses kaynak planÄ± PR #10 ile main'e birleÅŸti (`2f9c267`); P0 yalnÄ±zca Utku'nun sonucunu bekliyor |
| 2026-09-08 | Utku main `14e34f1` Ã¼zerinde Unity aÃ§Ä±lÄ±ÅŸÄ±, sahne ve Windows build Ã§alÄ±ÅŸtÄ±rma sonucunu gÃ¶rev #2'ye kaydetti. ÃœÃ§ kiÅŸinin P0 kanÄ±tÄ± tamamlandÄ±; P0 kapatÄ±ldÄ±, P1 aÃ§Ä±ldÄ± ve gÃ¶revler #12â€“#14 oluÅŸturuldu |
