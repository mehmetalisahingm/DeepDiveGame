# #170 — Tek kampanyada gerçek günlük ürün döngüsü

16 Ekim 2026. Başlangıç entegrasyonu `1e8ef00bf0c4433893cb517ec8b98c6dffea0f11` (PR #169 + görev planı #174). Bu teslim Mehmet'in günlük döngü kanıtıdır; P4.6 ekip/üç gün/ayrı bilgisayar kabulünü veya yeni sanat kabulünü üretmez.

## Bulunan ürün engeli

Sürekli dünyaya geçişten önce dünya sahnesinin her dalışta yeniden yüklenmesi, sahnedeki tek balığı da yeniden yaratıyordu. Artık dünya yüklenmeyince yakalanan balık kalıcı siliniyor, ikinci gerçek av yapılamıyordu. İlk balık 120 kazandırıyor, temel kamera 150 istiyor; kamera satın alma için ikinci av gerekiyor. Önceki ayrı `-Acceptance` alt sistem testi `SeedRecorderCamera` ile kamera/bakiye hazırlıyordu. O test gerçek klip yolunu sınar; sıfırdan kazanılmış kamera veya tam ürün ekonomisi kanıtı değildir.

Dar düzeltme: host sonraki benzersiz DiveId'de mevcut sahne balığını kendi başlangıç pozundan yeniden spawn eder. Aynı dalışta anında yenilenmez; oyuncu/world sahnesi yeniden yüklenmez. Yakalanan sahne balığının renderer/collider'ları despawn sırasında kapanır, tekrar spawn'da başlangıçtaki etkinlikleri geri gelir; av claim/health/motion yeniden başlar. Tür/habitat dağılımı veya yeni balık sistemi eklenmez, #155 açık kalır. Yaşam döngüsü değişikliği nedeniyle protokol `DeepDive-P4-2`; host ve guest aynı build'i kullanır. Utku'ya #155 üzerinden teşhis ve değişen world dosyaları iletildi; bu bildirim inceleme onayı değildir.

## Tekrar üretim

Dört kişi kontrolünde ikinci bir fiziksel engel bulundu: `TryBoardNearest`, mesafeyi yalnız boş koltuğa göre sınırlıyordu. Kuru iskelede arka koltuklara yaklaşık 2,3 m, öndekilere yaklaşık 4 m mesafe var; arka iki koltuk dolunca aynı binme noktası kalan iki oyuncuya yetmiyordu. Yakınlık herhangi bir authored koltuk noktasına göre, boş koltuk seçimi ayrıca host'ta yapılır. 2,75 m menzil büyütülmez, doluluk/faz/onarım kapıları kaldırılmaz. Regresyon aynı kuru noktadan dört ayrı koltuğu ve uzak kumsaldan ret sonucunu sınar. Dört kişilik pilotun ledge'den çapraz yürüyerek rampanın yanına takılan iskele yaklaşımı da kuru zemin üzerinden düzeltildi.

`ProductDaySmokeDriver` yalnız açık `-p46-day-role` development komutuyla etkinleşir. `tools/Test-P46-ProductDay.ps1` gerçek Windows süreçlerini başlatır; grafik açık host gerçek klip yakalar. Guest'ler ayrı süreçtir. Owner hareket, zıpkın/E/F/H, alışveriş/tekne/medya RPC yolları kullanılır. Başlangıç parası/avı/kamera/klip, saat, uyku sonucu, stok sağlayıcı veya seyahat teleport'u enjekte edilmez.

Sıra: evden kıyıya yürüyüş; üç ücretsiz parça; av ve fiziksel satış; ikinci dalışta av ve satış; kazanılan parayla kamera; üçüncü dalışta canlı balığın gerçek kaydı; güvenli dönüş; iskelede binme/yakın rota/inme/yeniden binme/liman; fiziksel eve/PC'ye dönüş ve oynatma/yayın; tekrarı ret; yatakta erken uyku; gün 2 ve tek yayın geliri. Aynı kampanya dosyası yeni host sürecinde tekrar açılır; kamera/av satışları/onarım/gün/yayın/gelir/klip dosyası ve tekrar yayın reddi kontrol edilir.

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P46-ProductDay.ps1 -Players 1 -Port 24777
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 24877
./tools/Test-P46-ProductDay.ps1 -Players 4 -Port 24977
```

## Doğrulama kaydı

Son ürün/test kaynağı `5cea97d9ad840cc69e5cffc0753662cc8e5c1d15`; taban entegrasyon `1e8ef00`'dır. Sonraki belge/kanıt commit'leri oyun kaynaklarını değiştirmez. Unity 6000.3.23f1, Windows x64 Mono build PASS; [1256/1256 EditMode PASS](evidence/p170-product/editmode.json). [Build kimliği](evidence/p170-product/build.json) EXE ile birlikte oyun DLL'leri ve sahne/asset dosyalarının SHA256 değerlerini içerir.

| Son build koşusu | Gerçek süreç sonucu | Yeni host süreci |
|---|---|---|
| `P46-product-20261016-202725-1` | [solo PASS](evidence/p170-product/solo-player0.json) | [reload PASS](evidence/p170-product/solo-reload.json) |
| `P46-product-20261016-202725-2` | [host](evidence/p170-product/two-player0.json) + [guest PASS](evidence/p170-product/two-player1.json) | [reload PASS](evidence/p170-product/two-reload.json) |
| `P46-product-20261016-202725-4` | [host](evidence/p170-product/four-player0.json), [guest 1](evidence/p170-product/four-player1.json), [guest 2](evidence/p170-product/four-player2.json), [guest 3 PASS](evidence/p170-product/four-player3.json) | [reload PASS](evidence/p170-product/four-reload.json) |

Yedi oyuncu sürecinin hepsinde worldLoads=1, üç benzersiz dalış, yeniden kullanılan balıkların üç ayrı network kimliği, 3/3 onarım, Docked→Outbound→Anchored→Inbound→Docked, haritada doğru oyuncu sayısı ve hareketli tekne, gün 2 ve erken uyku PASS. Host gerçek iki avı satar, kamerayı satın alır ve klibi üretir; guest'ler gerçek yaya/su/iskele/tekne/PC/yatak yollarını tamamlar, gün/klip/yayın/gelir aynalarını doğrular. Normal yolculukta en büyük karelik konum farkı tüm koşularda 0,160 m altında; koltuk/yatak/D07 yerleşimleri açık ayrı sınırdır. `[solo|two|four]-playerN-trace.txt` dosyaları gerçek logların P46/av satırlarını içerir; hata listeleri boş. Başarısız geliştirme pilotları bu sonuç tablosuna alınmadı.

Her kampanyada iki satış, üç parça, tek kapanış kimliği/tek gün özeti, tek yayın/tek settlement diskte doğrulandı. Yeni süreçte gün 2, bakiye 455, kamera, iki satış, onarım, iskelede boş sandal, klip hash/boyut ve tek yayın korundu; yeniden yayın reddedildi ve bakiye değişmedi. Kampanya ve reload sonrası dosyalar aynı kanıt klasöründe saklanır. 455 bakiye: balık 240 + günlük görev 80 + sponsor 150 + kanal 135 − kamera 150. Gün özetinin mevcut Income=240/Expenses=150 alanları balık/alışveriş ledger'ıdır; bütün gelir kalemlerinin UI'da toplam sunulduğu iddia edilmez.

Solo gerçek klip [payload](evidence/p170-product/solo.ddclip), [teknik veri](evidence/p170-product/solo-clip.json), [ilk kare](evidence/p170-product/solo-clip-frame-0.jpg), [30. kare](evidence/p170-product/solo-clip-frame-30.jpg): 320×180, 60 JPEG kare, yaklaşık 12,32 saniye. Kareler dosyadan aynen çıkarılıp incelendi; sahne/ekipman/balık görüntüsü var. PC'de dosya gerçekten decode edilip oynatıldı ve kare indeksi ≥3'e ilerledi; yeni host'ta aynı kontrol tekrar geçti. Aynı PC süreçleri ortak persistentDataPath kullandığından bu, ayrı bilgisayara medya aktarımı kanıtı değildir. Prototip görüntü yeni sanat/kalite kabulü değildir.

Ek iki süreçli legacy Hunt regresyonu `d135366` build'inde PASS: [host](evidence/p170-product/hunt-host.json), [guest](evidence/p170-product/hunt-guest.json). Bu eski alt sistem testi etiketli deniz başlangıç yerleşimini kullanır; evden başlayan ürün koşusu olarak sunulmaz. Son `5cea97d` build'inin 1/2/4 tam koşuları ayrıca gerçek avları tekrar sınadı.

Teslim PR #175'tedir. Bu rapor yazılırken GitHub CI çalışıyor; yerel PASS'tan CI sonucu türetilmez. Utku incelemesi, entegrasyona merge ve birleşmiş build'de tekrar kontrol henüz yapılmadı; #170 bu teslimle otomatik kapatılmaz.

## #141 kabul matrisi ve sınır

| #141 satırı | Bu teslimin kanıtı |
|---|---|
| Güvenli av/gerçek klip, fiziksel sefer ve eve dönüş | Son kaynakta 1/2/4 süreç PASS |
| Erken uyku, tek özet/tek yayın geliri | Son kaynakta 1/2/4 süreç PASS |
| Normal kapanıştan sonra yeni host, diskten devam/tekrar yayın reddi | Üç ayrı kampanya PASS |
| Ölüm/kayıp dalgıç | Son kaynakta bu birleşik koşuda çalıştırılmadı |
| 00:00 kapanışı | Son kaynakta bu birleşik koşuda çalıştırılmadı |
| Aktif dalış/sefer sırasında host kaybı | Son kaynakta bu birleşik koşuda çalıştırılmadı |
| Ayrı PC/internet dört insan ve üç ardışık gün | Bekliyor; aynı PC süreçleri yerine geçmez |

Eski birim veya alt sistem raporu yeni birleşik oturum PASS'ı gibi taşınmaz. Utku/Mert'in gerçek tamamı ve görsel değerlendirme #141'de açık kalır. Yeni şehir/rig/sanat, beş tür ve fiziksel tahta/keser/çivi tamiri bu teslimin kapsamı değildir.
