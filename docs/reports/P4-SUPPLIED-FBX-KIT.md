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

Tam EditMode **1288/1288 PASS** (`Logs/supplied-kit-final-tests.xml`). Üç türden beşer hayvan, ayrı NGO kimlikleri, av/çekim bileşenleri, 12 modelin URP materyalleri/collider içermemesi, üç gerçek yüzme klibi ve mevcut yol/ev/servis sözleşmeleri doğrulandı. İlk denemede eski dar sahil sınırı testi büyük binaları reddetti; üretimde sürekli kasaba kurulumu öne alındı, dar fixture ayrı bırakıldı, tam test yeniden geçti.

Windows build PASS (`Logs/P1-build.log`). Gerçek iki süreçli av, sandal gidiş/dönüş, dükkân görseli ve ev→dükkân→iskele→su→PC→uyku/diskten açma kontrolleri bu build üzerinde çalıştırılır; sonuçlar aşağıda kaydedilir. Teknik test, nihai sanat veya ayrı PC/dört insan kabulü sayılmaz. Mert model/materyal/kasaba ve Utku balık/world incelemesi sahibidir; onların onayı üretilmez.

## Tekrar üretme

Unity batch method: `DeepDive.World.Editor.SuppliedKitImport.Apply`. Mevcut 15 sahne balığının kökleri/NGO kimlikleri korunur; yalnız görsel çocukları ve iki tür ataması değişir. `DeepDive/P4/Coast: author 15 huntable fish` yerleşimini yeniden kurmak gerekirse ardından kit method'u çalıştırılır.

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P1-Integrated.ps1 -Players 2 -Hunt -Capture
./tools/Test-P1-Integrated.ps1 -Players 2 -Trip -Capture
./tools/Test-P1-Integrated.ps1 -Players 2 -Shops -Capture
./tools/Test-P46-ContinuousWorld.ps1 -Players 2
```
