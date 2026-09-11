# P2-B dilim kaydı — balık DiveTestArea'ya + sualtı sis/ışığı

Bu bir faz kapanış kaydı değildir; P2-B görevinin sahne dilimine ait teslim ve test kaydıdır.

- Dilim / tarih: P2-B sahne işi — 11 Eylül 2026
- Sahip: Utku (B)
- Kaynak commit: `ba4355154ff8de0ff2b2df7b739b956d941eb100` (`p2/utku-fish`)
- Temiz teslim dalı: `p2/utku-scene-pass`, güncel `codex/p2-integration` tabanından
- Kaynak test ortamı: Unity 6000.3.23f1, URP 17.3.0, Windows 11

## Teslim

| İş | Sonuç |
|---|---|
| Balık sahnede: `Fish_SeaBass` (`NetworkObject`, `NetworkTransform`, `CapsuleCollider`, `CatchObject`, `FishActor`) | TAMAM |
| `SpeciesDefinition`: `sea_bass` / Levrek | TAMAM |
| Temel sualtı sisi/ışığı, skybox ve su yüzeyi | TAMAM |
| Kaynak sahne bütünlük testleri | Kaynak commit'te 7/7 PASS |
| Temiz dal ek korumaları | 10 sahne sözleşme testi tanımlı; yeni head üzerinde henüz çalıştırılmadı |

Yeni ana dosyalar:

```
Assets/DeepDive/World/Editor/DiveTestAreaSetup.cs
Assets/DeepDive/World/Editor/DeepDive.World.Editor.asmdef
Assets/DeepDive/World/Fish/Species/SeaBass.asset
Assets/DeepDive/World/Art/FishMaterial.mat
Assets/DeepDive/World/Art/UnderwaterSky.mat
Assets/DeepDive/World/Art/WaterSurfaceMaterial.mat
Assets/DeepDive/World/Tests/Editor/DiveTestAreaSceneTests.cs
```

Değişen ortak sahne/test dosyaları: `Assets/DeepDive/World/Scenes/DiveTestArea.unity` ve `Assets/DeepDive/World/Tests/Editor/DeepDive.World.Tests.asmdef`.

## Yöntem

`DiveTestAreaSetup.Apply` sahneyi Unity API'siyle kurup kaydeder. Bu seçim, sahneye gömülü `NetworkObject.GlobalObjectIdHash` değerinin Unity tarafından doğru üretilmesini korumak için yapıldı. Script idempotenttir; aynı isimli nesneyi günceller ve ikinci balık üretmez.

Balık bu dilimde scene-placed `NetworkObject` olarak tutulur. `NetworkSession`, NGO scene management'i kullandığından bu dilimde `NetworkPrefabs.asset` kaydı eklenmedi.

## Kaynak commit test kanıtı

Utku'nun `ba435515` kaynak commitinde raporlanan sonuçlar:

- EditMode tüm assembly'ler: **81/81 PASS**.
- `DeepDive.World.Tests`: **54/54 PASS**.
- Yeni `DiveTestAreaSceneTests`: **7/7 PASS**.
- Sahne kurulum logu: `P2_DIVEAREA_READY fish=Fish_SeaBass species=sea_bass home=(0,4,8) fogDensity=0.055`.
- Sahneye gömülü balığın `GlobalObjectIdHash` değeri: `3779525944`.

Bu sonuçlar kaynak `p2/utku-fish` çalışma ağacında alınmıştır. Sahne dilimi güncel `codex/p2-integration` tabanına temiz şekilde taşındıktan sonra Unity Test Runner/build bu yeni head üzerinde henüz çalıştırılmış sayılmaz; yeni doğrulama yapılmadan PASS iddiası genişletilmez.

## Temiz dalda eklenen kalite korumaları

Temiz port sonrasında `DiveTestAreaSceneTests` yalnız “balık var mı?” kontrolüyle bırakılmadı. Aşağıdaki sözleşmeler de testle kilitlendi:

- Fish `NetworkTransform` server-authority kalır; XYZ pozisyon ve XYZ rotasyon guest'lere senkronize edilir, statik scale senkronize edilmez.
- Dive sahnesinde 0..3 slotlarını kapsayan tam dört benzersiz spawn bulunur.
- Balığın başlangıç konumu, gerçek `NetworkDiver.prefab` içindeki `harpoonRange` okunarak bütün dört spawn için zıpkın menzili içindedir; test magic number kopyalamaz.
- `SeaBass.MaxHealth` ile gerçek prefab `harpoonDamage` birlikte üç vuruşluk P2 av ayarını korur; iki sahibin ayarları sessizce birbirinden kopamaz.
- `WaterSurface` görsel-only kalır ve collider eklenerek `DiveExit` yolunu kapatamaz.

Bu ek testler temiz branch üzerinde yazılmıştır fakat Unity Test Runner henüz bu yeni head için çalıştırılmamıştır; dolayısıyla tanımlı olmaları PASS kanıtı değildir.

## Composition düzeltmesi

Kaynak rapordaki “av zinciri Composition'da bağlı değil” tespiti eski `p2/utku-fish` tabanı için doğruydu fakat güncel ortak P2 dalı için geçerli değildir.

Güncel `codex/p2-integration` içinde:

- `DeepDive.Composition.asmdef`, `DeepDive.World` ve `DeepDive.Inventory` referanslarını içerir.
- `DiveInventoryBinding` üretim kodunda vardır.
- Aktif ve yetkili Dive sırasında `DiveContext.Bind(this)` ve `CatchClaim.Bind(this)` yapılır.
- Dive kapanınca / binding dispose olduğunda `Unbind` yapılır.
- `SessionNetworkAdapter`, `InventoryManager` ile bu binding'i oluşturur.
- `InventoryManager.TryAddCatch` sonucu `PlayerActionResult`a çevrilir.

Bu nedenle temiz `p2/utku-scene-pass` dalında `P2_FISH_DEAD_NO_DIVE` eski branch'teki bağlama eksikliğinden kaynaklanmamalıdır. Buna rağmen gerçek `harpoon -> death -> catch -> E -> inventory` zinciri runtime'da ayrıca doğrulanmalıdır; bu rapor onu PASS ilan etmez.

## Bilinen kusur

`CatchObject.Consume -> NetworkObject.Despawn(true)` scene-placed bir network nesnesini yok ettiği için NGO, in-scene network object hakkında uyarı logu üretebilir. Kaynak sahne dilimi bu davranışı bilinen log kirliliği olarak bırakmıştır. Davranışın host ve istemcilerde gerçek oyun sırasında doğru olduğu final P2 playtestinde ayrıca doğrulanacaktır.

## Ekip bağımlılıkları

1. `SeaBass.asset` için `maxHealth = 3`; mevcut `NetworkPlayer.harpoonDamage = 1` ile üç geçerli vuruş gerekir. Temiz dalda bu ilişki artık otomatik testle korunur.
2. Unity Smart Merge yerel makinelerde ayrıca yapılandırılmalıdır; `.gitattributes` tanımı tek başına yerel `unityyamlmerge` komutunu kurmaz.
3. `ProjectSettings/GraphicsSettings.asset` bu sahne dilimine dahil edilmemiştir.

## Henüz doğrulanmayanlar

Aşağıdakiler yeni temiz branch üzerinde yapılmadan P2 kapanış kanıtı sayılmaz:

- Unity Test Runner ve build doğrulaması.
- Gerçek host + istemci dalışı.
- Balığın runtime yüzme/kaçma davranışının sahnede gözle doğrulanması.
- `zıpkın -> ölüm -> catch -> E -> inventory` uçtan uca zinciri.
- Aynı catch'i iki oyuncunun aynı anda alma yarışı.
- Dolu çantada catch'in dünyada kalması.
- 1/2/4 oyuncu kabul turu ve 10–15 dakikalık ortak playtest.
