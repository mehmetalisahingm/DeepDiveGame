# P3.3-C harita UI + gerçek 2-süreç sefer/harita smoke (#66)

Tarih: 2026-09-24. Taban: `codex/p3-integration` @ `814e4a1` (PR #75/#82, #76/#81, #78, #80 dahil) + bu PR.

## Ne eklendi

- `BoatMapPresenter` (saf): dock + sandal + **bağlı oyuncular** + **sandala dönüş işareti**. Dönüş işareti yalnız `Anchored` fazında, sandalın onaylı konumunda ve yerel oyuncu **sandalda değilken** üretilir. Bölge dışı/onaysız konum çizilmez, kenara sıkıştırılmaz, enterpole edilmez.
- `BoatMapView` (`Composition`, IMGUI, `M` ile aç/kapa): yalnız girdileri toplar ve çizer. Faz ve sandal pozu Mehmet'in `BoatTripPlayerSync` aynasından, oyuncu konumları host-otoriter `NetworkPlayer` transformlarından, world→map Utku'nun `DiveRegionField.TryWorldToMap`'inden gelir; dönüşüm UI'da tekrarlanmaz.
- Save/load: yeni alan yok (bkz. CONTRACTS P3.3-C notu). Aktif sefer serialize edilmez; yeniden açılışta sandal iskelede, koltuklar boş.
- Smoke: `tools/Test-P1-Integrated.ps1 -Players 2 -Trip` (`-p3-trip`). Gerçek owner girdisi + RPC ile: 3 parça E-pickup → sandal onarılı → kıyıdan yürüyerek iskeleye → `B` ile bin (koltuğu host seçer) → tekrar `B` (koltuk/yolcu değişmemeli) → sefer sorumlusu `O` → `Outbound` → `Anchored` → sorumlu olmayan oyuncu `G` ile iner (dönüş işareti çıkmalı) ve tekrar biner → sorumlu `R` → `Inbound` → `Docked` → herkes iner. Her süreç **kendi** haritasını örnekler.

## Sonuç (yerel 2 süreç, Windows 11, Unity 6000.3.23f1, `Builds/P1-Integrated`)

| Koşu | host | client1 | not |
|---|---|---|---|
| `main` olduğu gibi | FAIL (adım 6 zaman aşımı) | FAIL (adım 5: tekrar binme `OutOfRange`) | aşağıdaki bulgu 1 |
| `NetworkBoatController`'da `Teleport` yaması (yerel, commit'lenmedi) | **PASS** | **PASS** | faz sırası `Docked,Outbound,Anchored,Inbound,Docked` iki süreçte de; 17 farklı canlı sandal konumu; iki süreç de 2 oyuncu ikonu; misafirde dönüş işareti + tekrar binme; host'ta gerçek `EconomySaveStore` SaveNow/LoadNow sonrası `Repaired` + `Docked` + boş koltuk |

`EditMode`: 637/637.

## Bulgular (Mehmet'in alanı — düzeltilmedi, kanıtla iletilir)

1. **Uzak oyuncunun koltuk/çıkış konumu CharacterController tarafından eziliyor.** `NetworkBoatController.KeepSeatedPlayersAttached` ve `ReleasePlayer` oyuncuyu `transform.SetPositionAndRotation` ile taşıyor; host'ta `CharacterController` her `FixedUpdate`'te `Move` çağırdığı için uzak misafirin transformu iskele konumuna geri dönüyor (host görünümü: sefer boyunca misafirin `z` değeri koltuk ile `-3.24` arasında gidip geliyor; `Anchored`'da inince misafir sandalın yanında değil **iskelede** kalıyor, bu yüzden tekrar binemiyor). Host'un kendi oyuncusu etkilenmiyor. `NetworkSession` zaten `NetworkPlayer.Teleport(Pose)` kullanıyor (controller'ı kapatıp `NetworkTransform.Teleport` çağırır). Denenen 2 satırlık değişiklik: iki `SetPositionAndRotation` çağrısı → `player.Teleport(new Pose(..., transform.rotation))`. Yama: bu raporun PR'ında yok; önerilen diff PR açıklamasında.
2. **İskele ucundan koltuğa mesafe sınırda.** Berth'teki sandalda `seat-0` iskele ucundaki oyuncudan ~2.3–2.53 m; `boardingRange = 2.5`. Güvertenin en ucuna kadar yürümeyen oyuncu `OutOfRange` alıyor (host'un ilk denemesi `z=-3.82`'de 2.53 m ile reddedildi). Hata değil ama kullanılabilirlik payı yok; aralığı ölçümü koltuk yerine gövde/iskele bölgesine göre yapmak veya menzili artırmak Mehmet'in kararı.

## Doğrulanmadı (açıkça)

- 4 süreç ve ayrı bilgisayar/internet koşusu.
- IMGUI haritanın ekran görüntüsü (headless render texture IMGUI'yi yakalamıyor); harita durumu `BoatMapView.LastIcons` üzerinden sayısal doğrulandı, çizim gözle doğrulanmadı.
- Aynı koltuğa iki oyuncunun aynı anda binme yarışı (bu koşuda iki oyuncu farklı koltuk aldı: host `seat-0`, misafir `seat-1`).
- Sefer sırasında host'un kapanıp yeniden açılması (gerçek yeniden başlatma). Kayıt sözleşmesi `BoatTripPersistenceTests` ile ve host'ta gerçek `EconomySaveStore` round-trip'i ile doğrulandı; process restart yapılmadı.
- Sefer sorumlusunun sefer sırasında kopması (yalnız EditMode).
