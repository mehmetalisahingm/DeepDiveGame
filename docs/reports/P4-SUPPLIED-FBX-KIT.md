# Kullanıcının FBX kitinin oyun entegrasyonu — 18 Ekim 2026

Kullanıcı üç arşivden gereken modellerin hızlıca oyuna eklenmesini istedi. Bu açık talep, faz kabulünü kapatmadan yalnız sağlanan kitin görsel/içerik entegrasyonunu öne alır. Kaynaklar [CREDITS](../assets/CREDITS.md) içindedir; üç Quaternius paketinin resmî CC0 kaydı okundu. Ücretli servis/üretim/paket satın alınmadı. İç içe gelen tekrar balık ZIP'i ve kullanılmayan modeller oyuna taşınmadı.

## Görünen değişiklikler

- **15 balık, üç tür:** beş levrek, beş ton balığı, beş kırmızı mercan. Tuna/RedSnapper kendi sabit kimlikleri, av/çekim/satış ve ansiklopedi adlarıyla bağlandı. Levrek için CoralGrouper geçici görsel vekildir; biyolojik doğruluk veya beş tür teslimi iddiası yoktur. Üç modelin yüzme klipleri döngüde çalışır. Başlangıç sağlık/av fiyatı ve kalite ödül tarifesi önceki levrekle aynı tutuldu; aynı dalışta av yeniden doğmaz.
- **Başlangıç sandalı:** paket içindeki ahşap Boat modeli, mevcut dört koltuk/onarım/rota otoritesinin görselidir. Tripo motorbot ve araştırma teknesi ayrı kalır. Keser/tahta/çivi tamiri veya yeni tekne fiziği eklenmedi.
- **Kasaba:** dört hizmetin arkasına ShopBuilding, iki mevcut yan blok yerine TownBuilding görseli kondu. Eski tezgâh, tabela, NPC, müşteri yeri ve host servis denetimi korunur. Eski dar sahil test şeridinde büyük binalar kurulmaz; sürekli kasaba anchor'ları hazırlanır ve görseller bu parsellerde kurulur.
- **Ev:** dört yatak ve ortak depo modelleri değişti; kitaplık, kanepe ve bitki eklendi. Yatak/uyku, depo/PC, collider, ev spawn/çıkış anchor'ları aynı kalır. Mobilya dekoru yeni collider veya ikinci bir oyun durumu oluşturmaz.
- **Varlık aktarımı:** 12 kaynak FBX, 12 prefab, kaynak materyal renklerini koruyan URP materyalleri. Harici atlas dokuları arşivlerde yoktu; olmayan doku uydurulmadı. Görseller metreye/yerel pivota uyarlandı. Kaynak FBX'ler LFS'tedir.
- **Ağ:** yeni tür tanımları nedeniyle protokol `DeepDive-P4-4`; birlikte oynayanlar aynı build'i kullanmalıdır. Mevcut kampanya tür kimlikleri/saves korunur, şema göçü gerekmez.

## Doğrulama

Tam EditMode **1291/1291 PASS** (`Logs/supplied-kit-ready-tests.xml`). Üç türden beşer hayvan, ayrı NGO kimlikleri, av/çekim bileşenleri, 12 modelin URP materyalleri/collider içermemesi, üç gerçek yüzme klibi ve mevcut yol/ev/servis sözleşmeleri doğrulandı. İlk denemede eski dar sahil sınırı testi büyük binaları reddetti; üretimde sürekli kasaba kurulumu öne alındı, dar fixture ayrı bırakıldı, tam test yeniden geçti.

FBX eski iskelet ölçeği ayrıca gerçek bake edilmiş vertex ölçümüyle düzeltildi; yalnız renderer kutusuna göre normalize edilince hayvanlar çok küçük kalıyordu. FBX rig kökü korunur, ayrı metre parent'ı gerçek skinned vertex ölçümüne göre ayarlanır. Üç modelin başlangıç ve yarım klip anındaki gerçek mesh boyutu 0,7–1,6 m kontrolünden geçer; hedef uzunluk yaklaşık 1,05 m'dir. Aynı düzeltme skinned culling sınırlarını da gerçek mesh'e bağlar.

Windows build PASS (`Logs/P1-build.log`). Gerçek iki süreçli av, sandal gidiş/dönüş, dükkân görseli ve ev→dükkân→iskele→su→PC→uyku/diskten açma kontrolleri bu build üzerinde çalıştırılır; sonuçlar aşağıda kaydedilir. Teknik test, nihai sanat veya ayrı PC/dört insan kabulü sayılmaz. Mert model/materyal/kasaba ve Utku balık/world incelemesi sahibidir; onların onayı üretilmez.

İki süreçli kontroller: av `Logs/P1-integrated-20261018-004001-120-2-27577` host + guest PASS. Sandal `Logs/P1-integrated-20261018-003441-370-2-27077`, dükkân `Logs/P1-integrated-20261018-003441-423-2-27377` host + guest PASS. Sürekli dünya `Logs/P46-world-20261018-003441-2` iki süreçte tek yükleme, evden sahile gerçek yürüme/yüzme, PC/uyku ve yeni host'ta gün 2 açılışı PASS. Sonraki rig görsel ölçek düzeltmesi collider/anchor/rota değiştirmedi; gerçek av o düzeltmeden sonra tekrar geçti.

İlk son kamera denemesinde gerçek klip/arşiv/ödeme PASS olmasına rağmen host'un `recordingStarted` test alanı eksik kaldı: sırasız Unity nesne listesi, 15 hayvanlı sahnede iki süreçte farklı hedef seçiyordu. İsteğe bağlı smoke sürücüsü kararlı nesne adıyla aynı balığı seçer; normal oyunun raycast/çekim/ödeme yolu değiştirilmedi. Yakın balık görsel çekimi yalnız render sırasında kamera gözlem açısıdır, oyuncu veya hayvan taşınmaz. Kamera denemesi tekrar edilir.

Son kamera tekrarı `Logs/P1-integrated-20261018-004247-164-2-27677`: **host + guest PASS**; kayıt başlama/durma, gerçek 41 karelik klip, arşiv, güvenli dönüş ve ödeme doğrulandı. Son kaynak `26f1a18`; runtime oyun/rig kaynağı `0f6d052`. Ham sonuçlar ve gerçek Windows görüntüleri [kanıt klasöründe](evidence/p4-supplied-kit-20261018/validation.json). Kullanıcının ana checkout'unda `Builds/DeepDive-FBX-Kit-Windows.zip` hazırdır; ZIP'in bütün girdileri okunabildi, içindeki EXE build ile aynı hash'e sahiptir. Herkes aynı `DeepDive-P4-4` paketini kullanır.

![Kasaba ve dükkân modelleri](evidence/p4-supplied-kit-20261018/town.png)
![Ahşap başlangıç sandalı](evidence/p4-supplied-kit-20261018/rowboat.png)

## Tekrar üretme

Unity batch method: `DeepDive.World.Editor.SuppliedKitImport.Apply`. Mevcut 15 sahne balığının kökleri/NGO kimlikleri korunur; yalnız görsel çocukları ve iki tür ataması değişir. `DeepDive/P4/Coast: author 15 huntable fish` yerleşimini yeniden kurmak gerekirse ardından kit method'u çalıştırılır.

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P1-Integrated.ps1 -Players 2 -Hunt -Capture
./tools/Test-P1-Integrated.ps1 -Players 2 -Trip -Capture
./tools/Test-P1-Integrated.ps1 -Players 2 -Shops -Capture
./tools/Test-P46-ContinuousWorld.ps1 -Players 2
```
