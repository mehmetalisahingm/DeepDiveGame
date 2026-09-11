# Güncel durum ve görev takibi

Plan 1.8 — 11 Eylül 2026. Aktif faz **P3**; birleştirme koordinatörü **Mehmet**.

## Mevcut sınır

- P0 kapalı.
- P1 kullanıcı kararıyla kapalı.
- P2 kullanıcı faz kabulüyle kapalı. Kapanış kaydı: [`P2-GAMEPLAY-VALIDATION.md`](../reports/P2-GAMEPLAY-VALIDATION.md).
- **P3 AÇIK.** Ortak dal: `codex/p3-integration`.
- P4–P6 kilitli. P3 kapanmadan bu fazların özellik işleri alınmaz.
- Unity sürümü: `6000.3.23f1`.
- P3 açılış kaydı: [`P3-KICKOFF.md`](../reports/P3-KICKOFF.md).

## Ekip

| Rol | Kişi | GitHub hesabı |
|---|---|---|
| A | Mehmet | `mehmetalisahingm` |
| B | Utku | `Utkuuzun14` |
| C | Mert | `MertKAYAR` |

## Faz durumu

| Faz | Durum | Birleştirme koordinatörü | Kayıt |
|---|---|---|---|
| P0 | KAPALI | Mehmet | [`P0-REPORT.md`](../reports/P0-REPORT.md) |
| P1 | KAPALI (kullanıcı kararı) | Utku | [`P1-REPORT.md`](../reports/P1-REPORT.md) |
| P2 | KAPALI (kullanıcı kabulü) | Mert | [`P2-GAMEPLAY-VALIDATION.md`](../reports/P2-GAMEPLAY-VALIDATION.md) |
| P3 | **AÇIK** | **Mehmet** | [`P3-KICKOFF.md`](../reports/P3-KICKOFF.md) |
| P4 | KİLİTLİ | Utku | Yok |
| P5 | KİLİTLİ | Mert | Yok |
| P6 | KİLİTLİ | Mehmet | Yok |

## Aktif P3 görevleri

| Görev | Sahip | Durum | GitHub |
|---|---|---|---|
| P3-A | Mehmet | AÇIK | [#34 — Kamera kaydı, kadraj ve tüp etkisi](https://github.com/mehmetalisahingm/DeepDiveGame/issues/34) |
| P3-B | Utku | AÇIK | [#35 — Kayıt kalitesi, hedef tanıma ve özel olay](https://github.com/mehmetalisahingm/DeepDiveGame/issues/35) |
| P3-C | Mert | AÇIK | [#36 — Satış, ortak para, dükkân ve kayıt](https://github.com/mehmetalisahingm/DeepDiveGame/issues/36) |

## P3 hedefi

Tek bölgede şu tam döngü çalışmalıdır:

**Hazırlan → avla → kaydet → dön → sat/değerlendir → tüp geliştir → tekrar dal.**

P3 kapanışında ayrıca şunlar doğrulanır:

- Aynı av/kayıt/istek iki kez para üretmez.
- Eşzamanlı alışveriş parayı eksiye düşürmez.
- Görüş dışındaki veya engel arkasındaki hedef geçerli kayıt üretmez.
- Tüp yükseltmesi doğru oyuncuya doğru kapasiteyi verir.
- Yeniden açılışta son tamamlanmış kayıt geri gelir; av/para/ekipman çoğalmaz.
- Solo ve dört oyuncuda tam döngü doğrulanır.
- P2 his katmanı korunur; kayıt, satış ve yükseltme sonuçları oyuncuya açıkça bildirilir.

Final sanat/cila P3 şartı değildir. Gerçek video, ikinci dalış bölgesi, bulut kayıt ve geniş içerik üretimi P3 kapsamı dışındadır.

## P2'den devreden smoke kontrolü

P2 kullanıcı kabulüyle kapatıldı; final head üzerinde ayrı kayıtlı dört oyunculu insan oturumu ve 10–15 dakikalık ortak his testi bulunmuyordu. P3'ün ilk ortak smoke/playtest'inde en az iki oyuncuyla temel av/dönüş akışı ve guest oyuncunun kendi çanta göstergesinin canlı güncellenmesi tekrar kontrol edilir.

## CI

Unity CI artık `main`, `codex/p2-*`, `p2/*`, `codex/p3-*` ve `p3/*` push'larını kapsar. EditMode testleri ve mevcut Windows build yöntemi çalıştırılır. Yapılmayan veya başarısız koşu PASS sayılmaz.

## Faz kapanış kuralı

Üç teslim P3 ortak dalında birleşir, kabul testleri ve kısa ortak oynama testi kayda alınır, ardından P3 main'e aktarılır. P4 kendiliğinden açılmaz; ayrıca faz açılış kararı verilir.
