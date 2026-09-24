# P3.3 Sandal runtime acceptance

Tarih: 2026-09-24  
Build: Unity 6000.3.23f1, Windows standalone, local host + local client  
Kod tabanı: `origin/codex/p3-integration` (`814e4a1`) üzerine bu teslimdeki runtime smoke düzeltmeleri  
Komut: `tools/Test-P1-Integrated.ps1 -Players 2 -Boat -Port 18882`

Ham kayıt: `Logs/P1-integrated-20260924-233113-783-2/host.json` ve `client1.json`.

## Sonuç

Host ve misafir raporları `passed: true`, `errors: []`.

Gerçek owner input/RPC akışıyla şu sıra geçti:

1. Üç parça E-pickup ile `0 → 1 → 2 → 3 → Repaired`; tekrar hull isteği reddedildi ve parçalar iki süreçte gizlendi.
2. İki oyuncu ayrı koltuklara bindi; host sabit `route-near-1` rotasını başlattı.
3. `Docked → Outbound → Anchored` fiziksel varışla gözlendi.
4. Her iki oyuncu demirde indi, su içinde tekrar bindi.
5. Demirde inen ilk sefer sorumlusunun yerine bağlı misafir sahipliği aldı ve dönüş RPC'sini gönderdi.
6. `Inbound → Docked` fiziksel dönüşü tamamlandı; iki oyuncu da dock'tan çıktı.
7. Session daha sonra normal `Dive → Return → Lobby` akışını tamamladı.

## Bu koşunun sınırı

Bu kayıt iki yerel Windows sürecinin kabulüdür. Dört süreç, ayrı bilgisayar/internet, gerçek oyuncu görsel kabulü ve canlı harita UI'sı P3.4 kapısında ayrıca bekler. `BoatMapPresenter` hesaplama katmanı vardır; oyuncu-facing harita ekranı henüz tamamlanmış sayılmaz.
