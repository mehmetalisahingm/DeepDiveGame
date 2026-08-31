# P0 Windows görsel kontrolü

31 Ağustos 2026 — Mehmet'in bilgisayarında Codex ile gerçek Windows build penceresi kontrol edildi. Build: `P0-A-9596244`, kaynak: `9596244`. Unity 6000.3.23f1, URP 17.3.0, Windows x64/Mono, GTX 1650 / Direct3D 11.

**Sonuç: geçti.** Koyu mavi arka plan, düz zemin ve turkuaz küp görünüyor. Küp yüzlerinde ışık farkı ve zeminde gölge mevcut; pembe materyal, boş ekran veya görünür bozuk çizim yok. Alt+F4 ile normal kapanış ve test penceresinin kapanması da doğrulandı.

![Gerçek P0 Windows build penceresi](P0-A-9596244-windows.jpg)

Logda shader hatası veya exception bulunmadı. Direct3D frame timestamp uyarısında Unity CPU zaman ölçümüne geçtiğini bildiriyor; kontrolde görüntü sorunu görülmedi. Bu bir performans/FPS testi değildir.

Önceki Esc nedeniyle yarım kalan görsel kontrol bu kayıtla tamamlandı. Oyun kodu veya Windows ZIP değişmedi. ZIP içindeki BUILD-INFO, paketlenme anındaki eski kontrol durumunu içerir; güncel sonuç bu kayıttır.

Bu sonuç yalnızca Mehmet'in bilgisayarındaki örnek build içindir; Mehmet'in insan onayı, Utku/Mert testi veya temiz klon doğrulaması yerine geçmez. P0 açık, P1 kapalı; Utku incelemesi ve gerçek ekip kontrolleri/kararları bekleniyor.
