# #157 — Kesintisiz dünya oturumu ve ev başlangıçları

15 Ekim 2026. Ürün kaynak commit'i `989b556af84788f4db7a0026548c1a23312a8f58`; hedef `codex/p4-integration`. #157'nin ikinci teknik teslimidir. P4.6 veya bütün #157 kapanışı değildir.

## Değişen davranış

Önceden `CompleteReturn`, `PrepArea` yükleyerek günlük fiziksel eve dönüşü atlıyordu. Dünya girişinde ve sabah yerleşiminde de ev anchor'ları yerine deniz lab spawn'ları tüketiliyordu. İlk oda/menü girişinden sonra artık Prep/Dive/Return/Lobby aynı dünyada kalır; faz değişikliği oyuncunun konumunu değiştirmez. Ev ve PC için yürümek gerekir.

Composition, NetworkSession'a instance spawn resolver bağlar. Dünya girişi, dünyadaki yeni bağlantı ve D07 sabah yerleşimi dört home anchor'ını kullanır. Eksik/çift anchor fallback yapmaz. Sabah oyuncu sırası yerine admission slot'u kullanılır; bütün hedefler çözülmeden kısmi yerleşim yapılmaz. İki kuzey başlangıcı yatak çıkışlarıyla kapsül çakışmaması için 0.75 m geriye alınmıştır. Utku'nun dünya sahnesi/ana geometri dosyası değiştirilmedi; bu iki yerleşim verisi ortak world incelemesine dahildir.

`DeepDive-P4-1` ağ token'ı bu konum/sahne sözleşmesini ayırır. Aynı oturumdaki bütün oyuncular yeni build'i kullanmalıdır. Offline oda girişinden dünyaya ilk yükleme, yatma pozu ve yeni gün sabah yerleşimi açık yerleşim sınırlarıdır; günlük kasaba–kıyı yolculuğunu atlayan bir ev menüsü eklenmedi.

## Doğrulama

| Kontrol | Sonuç | Kanıt |
|---|---|---|
| Tam EditMode, Unity 6000.3.23f1 | 1254/1254 PASS | `Logs/P46-all-tests.xml`, son kaynakta |
| Windows integrated build | PASS | `Logs/P1-build.log` |
| 1 süreç gerçek dünya turu | PASS, 179.9 m | [oyuncu](evidence/p157-world/world1-player0.json), [yeni süreçte kayıt](evidence/p157-world/world1-reload.json) |
| 2 süreç gerçek dünya turu | Host + guest PASS, 181.4 / 181.9 m | [host](evidence/p157-world/world2-player0.json), [guest](evidence/p157-world/world2-player1.json), [kayıt](evidence/p157-world/world2-reload.json) |
| 4 süreç gerçek dünya turu | Dördü PASS, 170.9–184.2 m | [P0](evidence/p157-world/world4-player0.json), [P1](evidence/p157-world/world4-player1.json), [P2](evidence/p157-world/world4-player2.json), [P3](evidence/p157-world/world4-player3.json), [kayıt](evidence/p157-world/world4-reload.json) |
| CoastParts regresyonu | Host + guest PASS, gövde Prep / motor Dive / yakıt Return, 0→1→2→3 | [host](evidence/p157-world/coast-host.json), [guest](evidence/p157-world/coast-client1.json) |
| Hunt regresyonu | Host + guest PASS, guest yeniden bağlandı | [host](evidence/p157-world/hunt-host.json), [guest](evidence/p157-world/hunt-client1.json) |
| Gerçek klip/PC/yayın/gelir/diskten açma | Host + guest + yeni host süreci PASS | [host](evidence/p157-world/media-host.json), [guest](evidence/p157-world/media-client1.json), [yeniden açma](evidence/p157-world/media-reload.json) |

Son dünya koşuları: `P46-world-20261015-230727-{1,2,4}`. Her oyuncuda `worldLoads=1`; gerçek input/RPC hareketi, F dükkân etkileşimi, kuru iskele noktası, yüzme, fiziksel kıyıya çıkış, PC mesafesi ve H/yatak isteği geçer. Gün 1→2, gerçek `EarlySleep` özeti ve sabah doğru ev slot'u doğrulanır. Dört süreçte en büyük seyahat örneği 0.269 m'nin altındadır; sahne yenilemesi/1.5 m üzeri seyahat sıçraması testi düşürür. Yeni test oyuncu transform'u yazmaz, teleport çağırmaz, ürün spawn sağlayıcısını değiştirmez, para/av/saat/uyku fixture'ı kurmaz.

Her koşudan sonra host'un aynı kampanya dosyası **yeni bir süreçte** açılır. Gün 2, önceki erken-uyku özeti ve dünyaya evde giriş korunur. Bu, ayrı bilgisayarlarda üç günlük kabul değildir.

CoastParts son koşusu `P1-integrated-20261015-230727-255-2-23577`. Av koşusu `P1-integrated-20261015-225456-272-2-21877`; medya koşusu `P1-integrated-20261015-230223-325-2-22677`. Av/medya, son test-pilot/örnekleme düzenlemelerinden önce aynı ürün kodunda geçmiştir. Gerçek klip 112050 byte; yeniden açmada dosya hash'i eşleşti, playable=true, bakiye 285→285, kanal geliri 135→135, gün 2→2 kaldı. Medya sahibi guest'tir; host adına yayın yapılmış sayılmaz.

Eski P1/P3 alt sistem testlerinin deniz başlangıcı artık açık fixture log'u taşır; onlar ürünün evden başlayan dünya turu kanıtı değildir. Eski PC/depo testleri de Lobby dönüşünde sahne taşıması beklemek yerine eve yürür. Eski CoastParts koşusunda guest'in gerçek yüzmesi faz-snapshot başlangıcında kaçmıştı; takip, 0.7 m gerçek sürekli yükselme şartını koruyarak fazlar arasında sürdürülür ve teleport örnekleri sayılmaz. İlk yeni turda son/solo H gün kapanışını aynı çağrıda tamamladığı için yatakta olunan kare kaçmıştı; gerçek erken-uyku özetiyle doğrulanır. Dört oyunculu pilotta ortak müşteri noktası ve iskele dışına taşan bekleme düzeni düzeltildi: dükkânda ayrı noktalar, 2 m genişliğindeki iskelede iki sıra ve arkadakilerin önce çıkışı. Ürün collision'ı kapatılmadı, başarısız gözlemler PASS sayılmadı.

Son build'in SHA-256 değerleri (exe tek başına paket değildir):

```text
DeepDive.Composition.dll 345b5cbdc484784b3e80e99675df48e8c51db14cdbfa078dd4509e1429440ae1
DeepDive.Network.dll     3e1473ceef5dee77f54521cce66accad26690ea494dd9cf867ec7f16c87beebf
DeepDive.P1.Lab.dll      7eeaaf76a5a9c82c166bb6d6031a00607681a54a32598b43949af19e8273a730
```

## Tekrar çalıştırma

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P46-ContinuousWorld.ps1 -Players 1 -Port 23177
./tools/Test-P46-ContinuousWorld.ps1 -Players 2 -Port 23277
./tools/Test-P46-ContinuousWorld.ps1 -Players 4 -Port 23477
./tools/Test-P1-Integrated.ps1 -Players 2 -Port 23577 -CoastParts
./tools/Test-P1-Integrated.ps1 -Players 2 -Port 21877 -Hunt
./tools/Test-P1-Integrated.ps1 -Players 2 -Port 22677 -Acceptance
```

## Açık kalan kabul

Yeni tur bir günlük teknik hareket/etkileşim/uyku turudur; bu turun içinde av, klip kaydı/yayın ve tekneye binme yapılmaz. Gerçek medya/av ayrı alt sistem koşularında doğrulanmıştır; bunlar tek bir tam ürün oturumuna birleştirilmiş veya üç güne yayılmış kabul sayılmaz. Yeni sanat/şehir ölçeği, beş tür, fiziksel taşıma/keser/çivi tamiri, görsel değerlendirme ve ayrı bilgisayar/internet dört oyuncu/üç gün kapıları açık kalır. #138/#140/#141 ve #153–#157'nin diğer kabul şartları kapanmış sayılmaz. Utku/Mert incelemesi, birleşmiş build ve gerçek ekip kabulü bu rapordan üretilmez. Önceden var olan beş FBX `.meta` silinmesi commit'e alınmadı.
