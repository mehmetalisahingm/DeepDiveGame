# Güncel durum ve görev takibi

Plan 1.9 — 14 Eylül 2026. **P3 kullanıcı kabulüyle kapalı; aktif faz P4.** P4 birleştirme koordinatörü plan gereği **Utku**.

## Mevcut sınır

- P0 kapalı.
- P1 kullanıcı kararıyla kapalı.
- P2 kullanıcı faz kabulüyle kapalı.
- **P3 KAPALI (kullanıcı kabulü).** Kapanış kaydı: [`P3-REPORT.md`](../reports/P3-REPORT.md).
- **P4 AÇIK.** İlk devir/polish işi: [#46 — su yüzeyi ve su dışı oksijen davranışı](https://github.com/mehmetalisahingm/DeepDiveGame/issues/46).
- P5–P6 kilitli.
- Unity sürümü: `6000.3.23f1`.

P3 kapanışında başka kişi review'ı kullanıcı kararıyla kapı olmaktan çıkarıldı; böyle bir onay varmış gibi kayıt tutulmadı. Ayrı bilgisayar/internet 20–30 dakikalık ekip testi yapılmış sayılmıyor.

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
| P3 | **KAPALI (kullanıcı kabulü)** | Mehmet | [`P3-REPORT.md`](../reports/P3-REPORT.md) |
| P4 | **AÇIK** | **Utku** | #46 ile ilk polish/devir işi açık |
| P5 | KİLİTLİ | Mert | Yok |
| P6 | KİLİTLİ | Mehmet | Yok |

## P3 kapanış özeti

Final özellik head'i `b2572e9bb48d35a1a75b53a172332df8f7af0f07` için GitHub Actions SUCCESS, EditMode **357/357 PASS** ve Windows build SUCCESS kaydedildi. Yerel runtime smoke: 1 oyuncu Event PASS, 4 oyuncu Event PASS, 2 oyuncu Hunt PASS, 2 oyuncu Record PASS. #45 `codex/p3-integration` dalına merge edildi ve #34/#35/#36 tamamlandı olarak kapatıldı.

Mehmet gerçek build'de biyolüminesans özel olayını gördü; güvenli dönüşün ve av gelirinin ortak bakiyeye yatmasının çalıştığını doğruladı ve P3'ü kabul etti.

## P4 başlangıç notu

P3 manuel kabulünde görünür bir su yüzeyi olmadığı ve oksijenin dalış boyunca sürekli azaldığı görüldü. Yukarı çıkış SafeReturn akışını doğru tetikliyor. Su yüzeyi, su altı/su dışı ayrımı ve oksijen davranışı P4/polish için #46'ya devredildi.

P4'ün ayrıntılı kapsamı için [`PHASES.md`](PHASES.md) esas alınır; P5 işi P4 kapanmadan alınmaz.

## CI

Unity CI `main`, `codex/p2-*`, `p2/*`, `codex/p3-*` ve `p3/*` push'larını kapsar. P4 dal adları açıldığında workflow kapsamı ayrıca doğrulanmalıdır; yapılmayan veya başarısız koşu PASS sayılmaz.
