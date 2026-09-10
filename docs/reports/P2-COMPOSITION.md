# P2 Composition — Mehmet, görev #26

10 Eylül 2026. Test edilen kaynak: `a07bd06d6d7492acefd46d651b40db7a156baa8e`.

- `SessionNetworkAdapter`, kendi nesnesindeki `InventoryManager` bileşenini kullanır/yoksa ekler; sahnede elle bağlama gerekmez.
- `DiveInventoryBinding`, yalnız bağlı host'un Dive fazında `DiveContext.Bind(IDiveContext)` ve `CatchClaim.Bind(ICatchClaimSink)` bağlantılarını kurar. Dönüş, bağlantı kaybı, disable ve destroy sırasında kendine ait bağları temizler. Eski bağ yeni oturumun bağını silemez.
- `TryClaim` aktif/bağlı dalgıcı, güncel diveId ve av verisini doğrular; gerçek `InventoryManager.TryAddCatch` sonucunu dönüştürür. Çanta kabul etmeden av tüketilmez.
- EditMode: **86/86 PASS**, bunun 12'si Composition kontrolüdür. Gerçek FishHealth/CaptureBuilder/CatchState/InventoryManager zincirinde tek ödül, tekrar istek, dolu çanta, eski dalış ve yetki kaybı doğrulandı.
- Windows build başarılı. Aynı bilgisayarda iki ayrı oyun süreciyle P1 bağlantı/dalış/hareket/yeniden katılma regresyonu geçti. İnternet veya balığa gerçek raycast/despawn testi sayılmaz.
- Yerel kanıtlar: `Logs/P2-composition-tests.xml`, `Logs/P1-build.log`, `Logs/P1-integrated-20260910-194250-617-2`.

Bağımlılık: Utku'nun `ebce95a`, `eb4a9e3`, `a542d2b` commitleri, içerikleri ve sahipliği korunarak güncel P2 tabanına cherry-pick edildi. Bu dal hem bu üç bağımlılığı hem Mehmet'in bağlantısını içerir; Mert ayrı balık PR'ı açacaksa aynı değişiklikler ikinci kez uygulanmamalı. Utku'nun sahne/sis/ışık ve CI işlerine dokunulmadı.

P2 açık, P3 kilitli. Balığın sahneye yerleşimi, envanterin istemci/UI senkronu, gerçek vur/öl/topla oynanışı ve P2 kabulü henüz tamamlanmadı. Bu PR üç kişi adına faz onayı vermez.
