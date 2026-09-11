# P3 açılış kaydı — 11 Eylül 2026

Durum: **P3 AÇIK**.

P2 kullanıcı faz kabulüyle kapatıldı. Doğrulanan P2 oyun kodu commit'i `7ebdc4ae6ce4188758eddfb7bce9ea321031efa8`; EditMode ve Windows build GitHub Actions üzerinde PASS oldu. P2 kapanış kaydı `docs/reports/P2-GAMEPLAY-VALIDATION.md` içindedir.

## Ortak çalışma dalı

`codex/p3-integration`

P3 birleştirme koordinatörü: **Mehmet**.

Özellik dalları bu ortak daldan açılır ve P3 boyunca PR'lar bu dala hedeflenir. P4–P6 kilitli kalır.

## Görevler

- P3-A / Mehmet: #34 — kamera kaydı, kadraj geri bildirimi, kayıt başladı/bitti, tüp etkisi.
- P3-B / Utku: #35 — hedef/olay tanıma, görüş-mesafe-süre doğrulaması, kayıt kalitesi, özel olay.
- P3-C / Mert: #36 — av/görüntü geliri, ortak para, dükkân, tüp yükseltmesi, save/load.

## P3 çekirdek bitişi

Hazırlan → avla → kaydet → dön → sat/değerlendir → tüp geliştir → tekrar dal döngüsü tek bölgede çalışmalıdır.

Aynı av/kayıt/istek iki kez para üretmemeli; eşzamanlı alışveriş parayı eksiye düşürmemeli; görüş dışı veya engel arkasındaki hedef geçerli kayıt üretmemeli; tüp yükseltmesi doğru oyuncuya uygulanmalı; yeniden açılışta son tamamlanmış kayıt geri gelmeli ve av/para/ekipman çoğalmamalıdır. Solo ve dört oyuncuda tam döngü doğrulanır.

## P2'den devreden doğrulama

P2 kapanışı kullanıcı kabulüyle yapıldı; final head üzerinde ayrı kayıtlı 4 oyunculu insan oturumu ve 10–15 dakikalık ortak his testi yoktu. P3 ilk ortak smoke/playtest'inde en az iki oyuncuyla P2 av/dönüş akışı ve guest oyuncunun kendi çanta göstergesinin canlı güncellenmesi tekrar kontrol edilir.

## CI

Mevcut Unity test/build workflow'u P3 dallarını da kapsayacak şekilde güncellendi. `codex/p3-*` ve `p3/*` push'ları EditMode + mevcut Windows build doğrulamasını tetikler.

Final sanat/cila P3 kapanış şartı değildir. İkinci dalış bölgesi, gerçek video, bulut kayıt ve geniş içerik üretimi P3 kapsamı dışındadır.
