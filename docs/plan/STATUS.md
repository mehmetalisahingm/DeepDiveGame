# Güncel durum ve görev takibi

Plan 1.9 — 14 Eylül 2026. **P3 kullanıcı kabulüyle kapalı; aktif faz P4.** P4 birleştirme koordinatörü plan gereği **Utku**.

## Mevcut sınır

- P0 kapalı.
- P1 kullanıcı kararıyla kapalı.
- P2 kullanıcı faz kabulüyle kapalı.
- **P3 KAPALI (kullanıcı kabulü).** Kapanış kaydı: [`P3-REPORT.md`](../reports/P3-REPORT.md).
- **P4 AÇIK.** Görevler planın P4 tablosuna göre açıldı: #48 / #49 / #50.
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
| P4 | **AÇIK** | **Utku** | #48 / #49 / #50 |
| P5 | KİLİTLİ | Mert | Yok |
| P6 | KİLİTLİ | Mehmet | Yok |

## P4 aktif görevleri

| Görev | Sahip | Plan kapsamı | GitHub |
|---|---|---|---|
| P4-A | Mehmet | Bir palet ve bir çanta yükseltmesini uygular; avcı hasarını oyuncuya bağlar; mevcut ekipman ve etkileşim geri bildirimini tamamlar. | [#48](https://github.com/mehmetalisahingm/DeepDiveGame/issues/48) |
| P4-B | Utku | Aynı dalış bölgesinin çevre/ışık/sesini ve keşif noktalarını tamamlar; canlıları toplam beş türe çıkarır: üç yaygın, bir nadir, bir avcı. | [#49](https://github.com/mehmetalisahingm/DeepDiveGame/issues/49) |
| P4-C | Mert | Küçük kasabada üç hizmet noktasını aynı altyapıya bağlar; üç basit görev, jurnal ve tek bölgedeki ilerleme/kayıt verisini tamamlar. | [#50](https://github.com/mehmetalisahingm/DeepDiveGame/issues/50) |

P4 içerik sınırı: tek dalış bölgesi, küçük kasaba, toplam beş tür, mevcut tek özel olay, üç görev ve tüp/palet/çanta için birer yükseltme. Tek zıpkın ve kamera korunur. Aynı bölgenin sığ/derin kısımları olabilir; ikinci sahil veya ayrı dalış bölgesi açılmaz.

P4 bitiş koşulları:
- Beş tür, avcı ve olay 1 ve 4 oyuncuda doğru çalışıyor.
- Üç görev ve üç hizmet noktası mevcut döngüye bağlı; ödül tekrar verilmiyor.
- Ekipmanların açıklamaları ve gerçek etkileri aynı.
- Mevcut bölgede P3 döngüsü hâlâ çalışıyor; içerik ve kaynak/lisans listesi tamam.
- P5 ölçümü için referans bilgisayar/çözünürlük/kalite ve hedef kare süresi bu aşamada kaydedildi.

Yük desteği: Utku'nun çevre işi taşarsa Mehmet ses/etkileşim yerleşimini, Mert içerik verisi girişini devralır. Sahiplik devri kısa görev kaydına yazılır.

## P3 kapanış özeti

Final özellik head'i `b2572e9bb48d35a1a75b53a172332df8f7af0f07` için GitHub Actions SUCCESS, EditMode **357/357 PASS** ve Windows build SUCCESS kaydedildi. Yerel runtime smoke: 1 oyuncu Event PASS, 4 oyuncu Event PASS, 2 oyuncu Hunt PASS, 2 oyuncu Record PASS. #45 `codex/p3-integration` dalına merge edildi ve #34/#35/#36 tamamlandı olarak kapatıldı.

Mehmet gerçek build'de biyolüminesans özel olayını gördü; güvenli dönüşün ve av gelirinin ortak bakiyeye yatmasının çalıştığını doğruladı ve P3'ü kabul etti.

## CI

Unity CI `main`, `codex/p2-*`, `p2/*`, `codex/p3-*` ve `p3/*` push'larını kapsar. P4 dal adları açıldığında workflow kapsamı ayrıca doğrulanmalıdır; yapılmayan veya başarısız koşu PASS sayılmaz.
