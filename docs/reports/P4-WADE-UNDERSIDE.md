# P4 World kontrolü: Beach_Wade / Shore_Ramp / Shore_Ledge altı, #185 balıkları, ev saksısı

21 Ekim 2026 · Utku (World) · dal `p4/utku-wade-underside`, taban `codex/p4-integration @ 6816fba` · Unity `6000.3.23f1`

Bu bir EditMode ölçümü ve dar düzeltmedir. 1/2/4 süreçli koşu, ayrı bilgisayar veya oyun testi değildir. Faz/issue kapanışı ya da başka biri adına kabul kaydı da değildir. Commit/push yapılmadı.

## 0) Devralınan durum (20 Ekim oturumu bağlantı kopunca yarıda kaldı)

- **Cep düzeltmesi yapılmamıştı.** Önceki rapor "sıkışma yok" diyordu ama bu yalnız `up+nearest-open-water` stratejisine dayanıyordu. Aynı ölçümde `up` 133/139, `up+toward-shore` 97/139 başlangıçta sıkışıyordu. `wade.json` ile `wade-before-fix.json` bu yüzden aynıydı.
- **Balık "düzeltmesi" oyun yoluna fizik ekliyordu.** `SwimVolumeBounds.Contains` her balık adımında `Physics.OverlapSphereNonAlloc(0,2 m)` çağırıp NetworkObject olmayan collider'ı su saymıyordu. `FishMotion`'da da engellenen adımda `repathTimer = 0` yapılıyordu. Audit sim'i aynı kuralı (`SwimVolumeBounds.ForBox`) kullandığı için "7 balık → 0" sonucu çıktı. `envelopeHits` değişmedi, çünkü zarf ölçümü geometrikti ve evler yerinden oynamamıştı. Bu, host'ta 15 balık × her FixedUpdate fizik sorgusu (ağ/performans riski) demekti ve sorunu evleri düzeltmeden gizliyordu. **Geri alındı:** `FishMotion.cs`, `SwimVolumeBounds.cs` ve `FishMotionTests.cs` tabandaki haline döndü.
- Eski audit zarfı elipsoit olarak örnekliyordu. `FishMotion.RandomOffset` ise |o| ≤ 1, |o.y| ≤ 0,5 kümesini (dilime kırpılmış küre) kullanır. Audit bu kümeye çevrildi.
- Saksı (5,3, 0, −5) yerel konumuna taşınmıştı. Bu korundu.

## Yöntem

`Assets/Editor/P4WadeUndersideAudit.cs` (`DeepDive.Editor.P4WadeUndersideAudit.Run`) P4RouteAudit ile aynı yolu izler: `DiveTestArea` eklemeli açılır, `ContinuousTownLayout.Build` ile `HomeSceneSetup.EnsureRig` kurulur, gerçek fizik sorguları ve gerçek bir `CharacterController` çalışır, sahne kaydedilmeden kapanır. NetworkObject collider'ları (balıklar, olay gövdesi) ölçüm boyunca kapalıdır.

- Dalgıç `NetworkDiver.prefab`: r 0,35, h 1,8, skin 0,08, swimSpeed 3. Yüzey y 8,0, deadband 0,10.
- Kaçış simülasyonu `NetworkPlayer.FixedUpdate`'in Underwater kuralıdır (`move × swimSpeed`, yerçekimi yok, akıntı 0). Ortam her adımda `WaterField.CreateTracker()` ile sınıflanır. Başarı ölçütü 10 s içinde Underwater'dan çıkmaktır.
- Başlangıçlar:
  - (a) Eski 139 nokta: product-day (−7,6, 3,3, −5,0) ile her plakanın altındaki 1 m ızgarada "alt yüze yaslı" ve "orta boşluk" noktaları. Dolgu collider'ları kapatılarak yeniden üretilir, böylece önce/sonra aynı noktalardan başlar.
  - (b) 324 kenar noktası: plaka izinin 0,5 ve 1,5 m dışında, üstü açık sudaki noktalar.
- Kanıt: [`evidence/p4-wade/wade-before-fix.json`](evidence/p4-wade/wade-before-fix.json) bu audit'in düzeltmeden önceki sahnede koşusudur. [`evidence/p4-wade/wade.json`](evidence/p4-wade/wade.json) düzeltme sonrasıdır.

## 1) Cep düzeltmesi: üç plakanın altı kapatıldı

Yeni setup: `Assets/DeepDive/World/Editor/DiveTestAreaShoreFillSetup.cs`, menü `DeepDive/P4/Dive test area: close the coast slab undersides`. Hiçbir şeyi silmez, eksik parçayı yaratır, var olanı yerinde ayarlar. Bayt-idempotenttir (`ApplyAndVerify` → `P4_SHOREFILL_SETUP_IDEMPOTENT`). NetworkObject yoktur. Malzeme `ShoreMaterial`'dır.

| Parça | Ne | Dünya ölçüsü |
|---|---|---|
| `Beach_Wade_Fill` | Wade'in kendi kutusu, aynı 25° dönüş ve iz; normal boyunca aşağı uzatılmış. Üst yüzü plakanın alt yüzüdür (0,02 m içeride) | 6 × 9,44 × 7,57, merkez (−6,5, 2,18, −9,72) |
| `Beach_Wade_Toe` | Fill'in eğik ayak kapağının açık bıraktığı kama | x −9,5…−3,5 · y −0,5…5,2 · z −6,56…−4,14 |
| `Shore_Ramp_Fill` | Aynısı, rampa için (Z ekseninde 25°) | 7,57 × 9,44 × 4, merkez (5,72, 2,18, −9) |
| `Shore_Ramp_Toe` | Rampa ayağındaki kama | x 0,14…2,56 · y −0,5…5,2 · z −11…−7 |
| `Shore_Ledge_Fill` | Ledge izi, deniz tabanından ledge alt yüzüne | x 7…11 · y −0,5…7,42 · z −11…−7 |

**Değişmeyenler:** Beach_Wade, Shore_Ramp ve Shore_Ledge'in transform'ları setup içinde karşılaştırılıp korunur (`P4_SHOREFILL_SLAB_MOVED` guard'ı). Bu yüzden kara yüzeyi, 25° eğim, ayak y 5,2, Beach_Platform/ledge/iskele birleşimleri, `WaterField`/`SwimVolume` (su–kara sınıfı) ve `SafeReturnZone` aynı kalır. Guard ve test, her plakanın üst yüzüne yukarıdan inen ışında ilk çarpılanın hâlâ plakanın kendisi olduğunu ve hiçbir parçanın plaka ayağının açık su kenarını geçmediğini doğrular. Platform, ledge ya da deniz tabanı altına taşan kısımlar onların içinde gizli kalır.

**Ara bulgu:** İlk denemede toe, plakanın *alt* ayak köşesinde (y 4,84) bitiyordu. Plakanın kendi eğik uç yüzü 0,17 m'lik bir çıkıntı bırakıyordu. `up+toward-shore` tutan dalgıç çıkıntıyla toe yüzünün iç köşesinde takıldı (972 denemede 51, ör. (−9,5, 0,5, −3,64) → (−9,5, 3,30, −3,92)). Toe yürünebilir ayak kenarına (y 5,2) kadar uzatıldı. Açık su yüzü artık eğimle ayak kenarında birleşen düz bir duvar.

**Oynanış etkisi (bilinçli):** Wade ve rampa ayağının altından yüzülemez. Derinden gelen dalgıç ayakta y 0…5,2 arasında bir duvara çarpar ve ayağın üstüne yükselir. Underwater iken yukarı girdi serbesttir, 6,2 sınırı da 1 m yukarıdadır. Ledge'in kuzey yüzü artık tabana kadar duvardır.

### Ölçüm, önce → sonra

| | Önce | Sonra |
|---|---|---|
| Kapsülün sığdığı alan: Beach_Wade / Shore_Ramp / Shore_Ledge | 39,0 / 25,3 / 15,0 m² | **0 / 0 / 0 m²** |
| Eski 139 başlangıçtan katı içinde kalan | 0 | **139** (product-day noktası dahil) |
| `up`: sıkışan / geçerli başlangıç | 133 / 463 | **0 / 324** |
| `up+nearest-open-water` | 0 / 463 | **0 / 324** |
| `up+toward-shore` | 148 / 463 (eski 139'dan 97'si) | **0 / 324** |
| En kötü kaçış süresi (up / open / shore) | 1,94 / 2,88 / 6,98 s | 1,94 / 2,74 / 4,06 s |

## 2) #185 balıkları

İlk satırın evleri (Fish_SeaBass_02…08) z −6'dan **z 2,5**'e taşındı. x, y (3,5), dönüş, isim, tür ve NGO kimliği aynı kaldı. Değişiklik, satırları yazan `CoastalFishPopulationSetup.cs`'te yapıldı ve setup yeniden çalıştırıldı. Sahne farkında yalnız bu 7 `m_LocalPosition` değişti, hiçbir `GlobalObjectIdHash` değişmedi. Neden z 2,5: zarf yatayda 6 m. Kapatılmış wade'in kuzey kenarı z −4,14, x −9,5…−3,5 aralığında, ve gövde payı 0,2 m isteniyor. Bu, batıdaki sütunlar için z ≥ 2,06 gerektirir. Seçilen değer ~0,6 m pay bırakır. İkinci satır (z 3, y 4,5) ve `Fish_SeaBass` (0, 4, 8) zaten temizdi, dokunulmadı.

| | Önce (z −6) | Sonra (z 2,5) |
|---|---|---|
| Zarfı sahneye değen balık | 7 (Beach_Wade, Beach_Platform, Shore_Ramp) | **0** |
| Zarfın sahneye en yakın noktası (ilk satır) | 0 m | **0,497 m** (sınırlayan: deniz tabanı) |
| 300 s gezinme sim'i, merkezi collider içinde (kare) | 5…330 / balık | **0** |
| Gövde teması (kare) | 21…413 / balık | **0** |

Sim'de oyundaki kural kullanılır: `FishMotion` + yalnız su kutusu (`SwimVolumeBounds`'un yaptığı). `FishMotion`/`SwimVolumeBounds`'a fizik eklenmedi.

**Öneri (uygulanmadı):** Kaçma (`FleeSpeed`) yalnız su kutusuyla sınırlı, yönü dalgıçtan uzağa ve mesafe sınırı yok. Wade'e yakın bir dalgıçtan kaçan balık bu yüzden kıyı dolgusuna girebilir. Bu ölçülmedi. Ağ/performans riski olmayan iki yol var: (a) kaçma hedefini gezinme kümesine (home ± WanderRadius) kırpmak; (b) sahne yüklenince host'ta bir kez statik engel ızgarası (ör. 0,5 m voxel) pişirip `IWaterBounds`'a vermek. Kare başına fizik sorgusu önerilmez. Karar balık davranışı sahibine (Utku/Mehmet) ait.

## 3) Ev saksısı

`SuppliedTownArt.Home`: `Plant` yerel (5,3, 0, 4,8) → (5,3, 0, −5), PC'nin arkasındaki köşe. Dünyada (5,3, 8,4, −53,0) olur, doğu yan koridoru (x 4,07…5,82 · z −50,83…−42,0) ile kesişmez, collider'ı da yoktur.

## Testler

`Assets/Tests/P4/Editor/P4WadeUndersideTests.cs`. Kırmızı koşu sahne değişmeden önce alındı (7 testten 5'i kırmızı). Yeşil koşu düzeltmeden sonra.

| Test | Önce | Sonra |
|---|---|---|
| `NoDiverFitsUnderACoastSlab` | kırmızı (39,0 / 25,31 / 15,0 m²) | yeşil |
| `DiverByACoastSlabSurfacesWithEveryEscapeInput` (3 strateji) | kırmızı (281/1389 sıkışık) | yeşil |
| `ShoreFillsStayUnderTheSlabsAndAreScenery` | kırmızı (dolgu yok) | yeşil |
| `CoastFishWanderEnvelopeStaysClearOfScenery` | kırmızı (02…08) | yeşil |
| `CoastFishNeverWanderIntoScenery` (3 tohum × 120 s) | kırmızı | yeşil |
| `CoastFishPopulationKeepsCountSpeciesAndIdentity` | yeşil | yeşil |
| `HomePlantStandsOutsideTheEastSideCorridorAndInsideTheRoom` | yeşil (saksı zaten taşınmıştı) | yeşil |

**Tüm EditMode: 1298/1298 geçti, 0 kırmızı.** Taban 1291 idi. Geri alınan `FishMotionTests` testi düştü, 7 yeni test eklendi. Rota/kıyı testleri bu koşunun içinde ve yeşil: `P4RouteBaselineTests`, `DiveTestAreaRouteSceneTests`, `DiveTestAreaBeachSceneTests`, `CoastalHarborLinkTests`, `ContinuousTownWalkTests`/`LayoutTests`, `TownSceneTests`. `P48RepairAreaBaselineTests` (#192) ve `P4EngineShore` (#194) bu dalda yok, koşulmadı. Bunlar birleşince yeniden koşulmalı.

## Koordinasyon / açık kalanlar

- `DiveTestArea.unity` ortak sahnedir (Mehmet onayı). `CoastalFishPopulationSetup.cs` Mehmet'in #185 dosyasıdır. `SuppliedTownArt.cs` Mert'in (Composition) alanıdır. Üçü de onaylanmadan birleştirilmemeli.
- `ProductDaySmokeDriver`'ın HoldForShore durağı (−7,6, 5,5, −5,0) hâlâ Beach_Wade plakasının içinde (yüzey 5,60, alt yüz 5,16). Değiştirilmedi, Mehmet'in sürücüsü. Product-day dalgıcı artık wade altına giremez, toe duvarına çarpar. Gerçek product-day/çok süreçli koşu yeniden yapılmalı. EditMode bunun yerine geçmez.
- **Kapsam dışı bulgu:** `Dock_Town` (x 8…10, y 8,0…8,4, z −7…−3,5) altında da kapsül sığıyor (önce 7,0 m², sonra 6,5 m²). Kaçış koşusu yapılmadı, dolgu eklenmedi. Ayrı karar gerekir.
- Ölçülmeyenler: ağ gecikmesi/1-2-4 süreç; akıntı (`CurrentDrift`); kamera eğimi; balık kaçma yolları; balık gövdesi için renderer yarı-boyutu kullanıldı, mesh kabuğu değil.
- Her batch koşusu 5 öksüz `.fbx.meta`'yı siliyor ve `DiverPresentationCatalog.asset`'i yeniden yazıyor. Her koşudan sonra `git checkout` ile geri alındı, değişikliklere dahil değil.
