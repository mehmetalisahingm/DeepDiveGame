# #253 Kamera süre sınırı — 4 Kasım 2026

Bu dar teslim #252'nin tüm 10 milyon abone döngüsü değildir. Gerçek balık örneği/ağırlık–av–çekim bağlantısı #254, kanal/izlenme/abone/ekonomi #255 açık kalır. Kullanıcının hedefinin planı `docs/plan/CHANNEL_GOAL_10M.md` içindedir.

Temel/ileri/pro çalışma limitleri 15/30/45 saniyedir. Host, açılışta doğruladığı kademe ile sürenin dolmasını uygular, normal kayıt değerlendiricisi ve klip yoluyla otomatik durdurur; HUD geçen/azami süreyi gösterir. Gönderilen kareler ve medya süresi sınıra bağlıdır. Arşivin mevcut 128 klip / dosya üst sınırı değişmedi; toplam disk kotası veya gelişmiş silme politikası bu teslimde eklenmedi.

## Kanıtlar

- Derlenen kaynak: `codex/p4-camera-duration-253` üzerinde henüz commit edilmemiş oyun ve test kodu, baz `d1e3cb5`. Windows build Composition DLL SHA256 `441f010cd68456ba7fafd776c796f6439a84de6825262b0bfe73bbb0768bde35`.
- Tam Unity EditMode **1490/1490 PASS**; ayrı odaklı ekipman testi **12/12 PASS**. Kademelerin 15/30/45 değerleri test edildi.
- Windows build ve aynı PC'de iki GPU süreci: `tools/Test-P46-ProductDay.ps1 -Players 2 -Port 25863 -CameraAutoLimit -Render -Mute` PASS. Host normal oyunda kamerayı satın aldı; temel kamera otomatik durdu (`cameraAutomaticLimit=true`), gerçek dosya 14,9915 sn, 74 kare, 322.861 bayt. Üç dalış/av, klip, PC'de oynatma ve yayın, sefer ve uyku; yeni host sürecinde yeniden oynatma ve çift yayın reddi PASS.
- [Host](evidence/p253-camera-20261104/player0.json), [misafir](evidence/p253-camera-20261104/player1.json), [yeniden açma](evidence/p253-camera-20261104/reload.json), [gerçek kayıt göstergesi](evidence/p253-camera-20261104/player0.json.camera-rec.png). Aynı PC süreçleridir; insan, ayrı PC veya internet kabulü değildir.

İleri/pro kademe sürelerinin gerçek oyunla ve misafir kameracının sunucu süre bitimiyle ayrıca denenmesi, art arda kayıt ve toplam disk kullanım senaryoları henüz yapılmadı; #253 açıktır. Uygulama tam üretim teslimi veya #164 kabulü sayılmaz.

## Son protokol (P4-7) Windows tekrarı

Kaynak aynı kod ve `NetworkSession.Protocol` P4-7 sürümüdür; eski client ile yeni host karıştırılmaz. Son Windows `DeepDive.Composition.dll` SHA256 `3701fac629a66b7bef02adc44803ed046444f81bee7cec43eedc9c3493a7c25e`. `tools/Test-P46-ProductDay.ps1 -Players 2 -Port 25864 -CameraAutoLimit -Render -Mute`: her iki süreç üç dalışlı ürün günü ve fresh-host reload PASS. Otomatik duran gerçek temel kamera klibi **15 s, 73 kare, 391.967 bayt**; oynatma, yayın ve duplicate reddi PASS. [Host](evidence/p253-camera-final-20261104/player0.json), [misafir](evidence/p253-camera-final-20261104/player1.json), [reload](evidence/p253-camera-final-20261104/reload.json), [REC görüntüsü](evidence/p253-camera-final-20261104/player0.json.camera-rec.png). 1490/1490 EditMode, protokol sabiti P4-7'ye yükseltilmeden hemen önceki aynı oyun kuralları kaynağında geçti; bu sonuç yeni sürümün yeniden koşulmuş tam testi diye sunulmaz.

## Misafir kameracı — gerçek iki süreç

`tools/Test-P46-ProductDay.ps1 -Players 2 -Port 25870 -GuestCameraLimit -Render -Mute`: host ve misafir iki gerçek Windows GPU süreciyle PASS. Kampanya sıfırdan başladı, ortak parayı gerçek av/satış sağladı; önce host sonra misafir kendi temel kamerasını kasadan satın aldı. Misafir balığı kaydetti, 15 saniyede sunucu otomatik durdurdu, iki oyuncu güvenli döndü ve sunucu arşivinde sahibi misafir olan 15 saniyelik gerçek klip doğrulandı: 74 kare, 363.085 bayt. [Host raporu](evidence/p253-guest-camera-20261104/player0.json), [misafir raporu](evidence/p253-guest-camera-20261104/player1.json), [misafirin gerçek REC görüntüsü](evidence/p253-guest-camera-20261104/player1.json.camera-rec.png). Misafir otomatik bitişi ve satın alması raporda ayrı gösterilir; bu kısa test klibi yayınlama veya fresh-host reload iddiası taşımaz. Bunlar önceki temel host tam gün koşusunda ayrıca doğrulanmıştır.

Önceki iki deneme PASS değildir: ilkinde oyun ağ bağlantısından önce 20 saniyelik başlatma sınırı doldu; ikincisinde host kasada kalıp misafirin aynı test müşteri noktasına erişimini engelledi, koşu durduruldu. Başlatma süresi 60 saniyeye çıkarıldı; test, hostu kendi girdisiyle kasadan uzak yürüttü. Oyun collider/eşik/konum kuralları gevşetilmedi. İleri/pro kameraların gerçek oyun, toplam disk kotası ve #253'ün diğer kabul maddeleri hâlâ açıktır.
