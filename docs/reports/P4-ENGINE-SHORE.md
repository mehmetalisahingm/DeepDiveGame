# #188/#156 — `boat-part-engine` kuru kıyıya taşındı

19 Ekim 2026, Utku (B). Taban `codex/p4-integration` @ `6816fba`, dal `p4/utku-engine-shore`. Mehmet'in #186 kararını uygular: su altındaki parça kuru kıyıya taşınır, **teknik kimlik aynı kalır** (`boat-part-engine`). Yeni konum ölçümle seçildi. Bu değişiklik S1 kızağını, tamir yuvalarını veya #190 etkileşimini uygulamaz; #188 kabulü veya P4.8 kapanışı da değildir.

## Sonuç

| | Önce | Sonra |
|---|---|---|
| Konum (x, y, z) | (−6.5, **7.0**, −8), `Beach_Wade` eğimi | **(−2.5, 8.4, −12.75)**, kuru plaj şeridi |
| Zone / su | Sea; `SwimVolume` içinde, yüzeyin 1,0 m altında | Coast; `SwimVolume` dışında, `WaterField` → Land |
| Kuru karadan E ile alınır mı | hayır (#188: 2,5 m içinde kuru duruş hücresi yok) | evet: 2,13 m'den göz ışını önce parçaya çarpar |
| Evden / meydandan görünür mü | hayır / hayır (#188) | evet, 19 px / 34 px (kadrajda) |

Sahne farkı tek satır: `BoatPart_Engine` transform'u. Sahne elle düzenlenmedi. `DiveTestAreaBeachSetup.EnginePosition` güncellendi ve sahne `DiveTestAreaBeachSetup.ApplyAndVerify` ile yeniden yazıldı (`P3_BEACH_SETUP_IDEMPOTENT`: ikinci koşu bayt bayt aynı).

## Tekrar üretim

Unity 6000.3.23f1 batch mode, `-executeMethod DeepDive.Editor.P4EngineShoreAudit.Run` ([kaynak](../../Assets/Editor/P4EngineShoreAudit.cs), [ham ölçüm](evidence/p4-engine-shore/engine-shore.json)). Yöntem #172 `P4RouteAudit` ve #188 `P48RepairAreaAudit` ile aynıdır:

- Gerçek `DiveTestArea` ek sahne olarak açılır. Üstüne `ContinuousTownLayout.Build`, `HomeSceneSetup.EnsureRig`, `CoastalHarborLink.BuildMarkers` ve dört `TownStorefront.Build` kurulur.
- #188'in S1 kızağı (3.6, 8.4, −12.6), yaw 90, 2,4 × 5 m, ölçüm süresince **katı kutu** olarak eklenir. Henüz sahnede yoktur; temkinli varsayımdır.
- Her adayda gerçek `BoatPart_Engine` o hücreye taşınır. Ölçümden sonra sahne kaydedilmeden kapanır.
- Kapsül `NetworkDiver.prefab`'tan okunur (yarıçap 0,35, boy 1,80, skin 0,08, step 0,30, slope 45). Yürüme 4 m/s, göz 1,55 m, FOV 60° (16:9 yatay 91,5°), toplama ışını 2,5 m.

Ölçüm `engineBefore` alanına taşımadan önceki konumu (−6.5, 7.0, −8; Sea; `SwimVolume` içinde) yazar.

## Aday ızgarası ve kurallar

0,25 m ızgara, x −14.5…14.5, z −20…−11: **4329 hücre**.

| Kural | Ayrıntı | Elenen |
|---|---|---|
| Kuru zemin | ilk çarpılan collider yürüme yüzeyi, y ≥ 8,01 | 314 |
| Kıyı | `SwimVolume` dışı ve zone = Coast (kasaba caddesi ve deniz dışarıda) | 2360 |
| Collider içinde değil | zeminin 0,45 m üstünde 0,3 m küre yalnız zemine değer (`NoFreePartIsBuried` kuralı); DiveExit dışı | 96 |
| S1 / çalışma / bırakma | kızağa ≥ 1,0 m, `repair-work-0…3`'e ≥ 1,06 m, `repair-drop-0`'a ≥ 1,31 m | 244 |
| Koridor (sert) | iskele (`Dock_Town`), `Shore_Ledge`/DiveExit sahanlığı, `Shore_Ramp`, `Beach_Wade`, wade ve sahanlık iniş bantları, kapı hattı (cadde → plaj), diğer iki parçanın S1'e taşıma hatları (hull → work-0/1, fuel-tank → work-3/drop), drop → iskele: hepsine ≥ 1,0 m | 926 |
| Diğer parçalar | hull ve fuel-tank'a ≥ 6 m | 361 |
| Duruş | 0,75–1,5 m'de kapsülü boş kuru hücre, göz ışını önce parçanın tetiğine çarpar | 0 |

**Kalan:** 28 aday. Parça bir trigger olduğu için yürümeyi kapatmaz. Bu yüzden #172 rota çizgileri yumuşak kuraldır: 1 m'den yakınsa −1 puan.

**Puan:**

- Kadrajda görünürlük: plaj girişi 3, meydan 3. Görüş hattı: kapı 2. Ev kapısı 1, wade üstü 0,5.
- Ek olarak + min(hull, fuel-tank uzaklığı)/4 ve + min(koridor payı, 3)/4.
- #188 bekleme bandının (kızağa < 4 m) içindeki her metre −0,5.

Puanı en yüksek ve birbirinden ≥ 1,5 m uzak adaylar gerçek CC ile yürütüldü. Her birine üç yürüyüş yapıldı:

- Ev spawn 0'dan: #172 a0 rotasıyla caddeye, oradan ızgara yolu.
- `Beach_TownGate`'ten.
- Wade üstünden (−6.5, −11.2), yani sudan çıkan oyuncunun yerinden.

## Aday tablosu

Görüşte "kadrajda" = görüş hattı açık ve bakış yönünde 16:9 kadrajın içinde. Plaj girişi: caddeden plaja çıkarken (0, 8.4, −17), kuzeye bakış. Meydan (0, 8.4, −29) ve ev kapısı (0, 8.4, −41.7) de kuzeye bakar. Kapı (0, 8.4, −13.5) yalnız görüş hattıdır, çünkü oyuncu orada döner.

| | **Seçilen A (−2.5, 8.4, −12.75)** | Alternatif B (−1, 8.4, −12.75) | Alternatif C (−4.75, 8.4, −14.5) |
|---|---|---|---|
| Zemin | `CoastWalk` bindirmesi + `Beach_Platform`, Coast, haritada | aynı | `Beach_Platform`, Coast, haritada |
| hull / fuel-tank uzaklığı | 9,5 / 15,0 m | 11,0 / 13,5 m | 7,5 / 17,4 m |
| S1 kızağına | 3,6 m (bekleme bandının dış ucu) | **2,1 m** (bekleme bandında) | 5,9 m |
| En yakın sert koridor | hull → work-0 taşıma hattı, 1,01 m | **kapı hattı, 1,00 m** (sınırda) | hull → work-1 hattı, 1,01 m |
| En yakın #172 hattı (yumuşak) | e (isteğe bağlı çevre yürüyüşü), 0,01 m | e, 0,16 m | b, 1,18 m |
| Duruş → parça (E) | (−1.25, −13.5) → 2,13 m, isabet | (0, −13.5) = kapı işareti → 1,99 m | (−3.25, −14.5) → 2,16 m |
| Plaj girişi | **kadrajda**, 4,9 m, −30°, 109 px | kadrajda, −13°, 121 px | görünür ama kadraj dışı (−62°) |
| Meydan | kadrajda, 16,4 m, 34 px | kadrajda, 34 px | kadrajda, 37 px |
| Kapı (görüş hattı) | görünür, 2,6 m | görünür, 1,2 m | görünür, 4,9 m (arkada, −102°) |
| Ev kapısı | kadrajda, 29,1 m, 19 px | kadrajda, 19 px | kadrajda, 20 px |
| Wade üstü (güneye bakış) | görünür, kadraj dışı (−69°) | görünür, kadraj dışı | kadrajda, 139 px |
| CC: ev spawn 0 → parça | 43,3 m · 10,8 s | 43,0 m · 10,7 s | 44,1 m · 11,0 s |
| CC: kapı → parça | 1,2 m · 0,3 s | 0 m (kapı duruş noktası) | 3,4 m · 0,8 s |
| CC: wade üstü → parça | 5,7 m · 1,4 s | 6,9 m · 1,7 s | 4,6 m · 1,2 s |
| Düşme / sıkışma / denize girme | yok | yok | yok |
| Puan | **10,43** | 10,05 | 8,63 |

**Neden A:**

- Bütün sert kuralları geçiyor.
- Caddeden plaja çıkan oyuncunun kadrajında, meydandan ve ev kapısından da kadrajda.
- Üç parça plajda batı → doğu sırayla dağılıyor: −12 / −2.5 / 12.5. Aralar 9,5 m ve 15 m.
- B daha ortada ama iki sorunu var: kapı koridorunun tam sınırında duruyor ve S1 bekleme bandının içinde, kızağa 2,1 m. Ayrıca alma noktası kapı işaretinin kendisi; dört kişilik bir ekip orada birbirinin yolunu keser.
- C bandın dışında ve kalabalık açısından en rahatı. Ama plaj girişinin kadrajında değil, hull'a 7,5 m ile dağılımı en dengesiz.

Puan farkı küçük (A 10,43, B 10,05). Seçim tasarım yargısı içeriyor ve tabloda açıkça görülüyor.

**Bilinen bedel (A):**

- Parça, #172 isteğe bağlı çevre yürüyüşü (e) çizgisinin üstünde. Hull'ın work-0'a taşıma hattına da 1,01 m.
- İkisi de trigger'dan geçer, yolu kapatmaz.
- S1 bekleme bandının dış ucunda (3,6 m), 4 m sınırının 0,4 m içinde.

## #156 eksik parça ipucu

- İpucu konumu **sahneden okunur, sabit değil**. `BoatRepairHintView` açılışta `FindObjectsByType<BoatPartAnchor>` ile anchor'ları toplar. Her satırı `BoatRepairGuide.Line(part, oyuncu, anchor.WorldPosition)` ile üretir ([BoatRepairHintView.cs:59-65](../../Assets/DeepDive/Composition/BoatRepairHintView.cs)). Taşınan anchor'ı kendiliğinden gösterir; kod değişikliği gerekmedi.
- Seçilen konumda ölçülen satırlar: kapıdan **"Motor: bati, 3 m"**, meydandan "Motor: kuzey, 16 m", wade üstünden "Motor: dogu, 4 m".
- Smoke sürücüsü (`IntegratedSmokeDriver.ProbeBoatParts`) parçayı `GameObject.Find("BoatPart_Engine")` ile bulur. `GoToBoatPart`, hedef `y < 7.9` ise su altı yaklaşımını seçer, değilse kuru yaklaşımı. Yeni konumda kuru yol seçilir.
- Aynı dosyanın 1054. satırındaki yorum ("the Engine, which sits on the wade slope itself") artık eskidi. Dosya Mehmet'in, dokunulmadı; kendisine not olarak iletilecek.

## Kayıt uyumu

- Kayıt parça **kimliği** tutar, konum tutmaz. `EconomySaveData.BoatPartIds` bir `List<string>`'tir; geri yüklemede `BoatRepairParts.IsPart` süzgecinden geçer. Talep/katkı (`TryContributeBoatPart`) yalnız kimlik ve kaynakla çalışır. Kimlik değişmediği için eski kayıtlar olduğu gibi açılır, göç gerekmez.
- **Eski 2/3 kayıt (engine eksik)**, yeni test `BoatPartReachabilityTests.ACampaignSavedWithoutTheEngineFindsItOnDryLandAtItsNewPlace` ile doğrulandı. Test şu adımları izler:
  1. `EconomySaveData`'da konum alanı olmadığını kontrol eder.
  2. hull + fuel-tank kaydını geri yükler: durum InProgress, eksik yalnız `boat-part-engine`.
  3. İpucu satırının kapıdan yeni anchor'ı gösterdiğini doğrular.
  4. Kapıdan kuru zeminde yürüyerek parçanın 1,5 m içine ulaşılabildiğini doğrular.
  5. Engine'i bulunmuş olarak ekler: Repaired. İkinci oyuncu aynı parçayı denerse `AlreadyProcessed` alır.
- Kayıt dosyasının kendisi (JSON şeması) değişmedi. Birden fazla açılış ve aynı anda iki oyuncu senaryosu `BoatQuestGuideTests` içinde zaten yeşil.

## Testler

Kural: **üç ücretsiz parça da kuru karada ve yürüyerek erişilebilir.**

| Test | Değişiklik | Kırmızı → yeşil |
|---|---|---|
| `BoatPartReachabilityTests.EveryFreePartLiesOnDryLand` | yeni: zemin y ≥ 8,01, parça zemine oturuyor, `SwimVolume` dışı, zone Coast | önce **kırmızı**: "boat-part-engine at (−6.50, 7.00, −8.00) lies on Beach_Wade below the water line"; sonra yeşil |
| `…FromTheBeachGateWithTheTownBuilt` / `…FromTheWadeTopWithTheTownBuilt` | eski "wade rafından (engine konumundan) yürü" testi yerine: kapıdan ve wade üstünden **yalnız kuru hücrelerle** yürüme | ikisi de önce kırmızı, sonra yeşil |
| `…ACampaignSavedWithoutTheEngineFindsItOnDryLandAtItsNewPlace` | yeni (yukarıda) | önce kırmızı, sonra yeşil |
| `DiveTestAreaBeachSceneTests.TheEngineSitsOnTheDrySandBetweenTheOtherTwoParts` | `TheEngineSitsOnTheWadeFace…` yerine: kumda, wade şeridinin dışında, hull ile fuel-tank arasında, ikisine ≥ 6 m | — |
| `DiveTestAreaBeachSceneTests.EveryBoatPartIsReachableWithoutSwimming` | sıkılaştırıldı: "Underwater değil" yerine **Land** | — |
| `BoatPartAnchorsSitAtTheDocumentedPositions`, `DiveTestAreaRouteSceneTests.TheP32BeachAndItsBoatPartsAreUnchanged` | engine sabiti (−2.5, 8.4, −12.75) | — |
| `P4EngineShoreBaselineTests` (5) | yeni: kanıttaki seçim = canlı anchor; seçilen ve iki alternatif kanıtlı ve ayrık; seçilen konuma üç yürüyüş gerçek CC ile yeniden sürülür | — |

**Kanıt dosyaları:**

- `P4RouteAudit` JSON'u engine konumunu ve görüş hatlarını içerdiği için yeniden üretildi. Ayrıntısı [P4-ROUTE-172](P4-ROUTE-172.md) notundadır: rotalar ve anchor'lar değişmedi; #185'ten gelen balık ve dükkân görsel sınırları da güncellendi. `P4RouteBaselineTests` yeşil.
- #188 kanıtı (`P48RepairAreaAudit`, PR #192) bu dalda yok, çünkü #192 henüz birleşmedi. **#192 birleşince bu dal rebase edilmeli ve `P48RepairAreaAudit.Run` yeniden koşulmalı.** `P48RepairAreaBaselineTests.RecordedPartsAndBerthMatchTheLiveScene` engine'in eski konumunu kayıtlı tuttuğu için o ana kadar kırmızı kalır. #188 raporundaki "engine → S0/S1" taşıma satırları (wade/rampa üzerinden yüzme) da yeni kuru yolla değişir.

EditMode: önce **1291/1291**, sonra **1299/1299** (+8).

## Yapılmayanlar

- Gerçek 1/2/4 süreç koşusu, Windows build, `-p46-coast-parts` smoke'u. Smoke'ta misafir engine'i Dive fazında alıyor; yeni konumda bu kara yolu koşulmadı.
- Sis, ışık, gece ve kontrast: görüş yalnız geometri. Collider'sız DevelopmentVisuals ve sağlanan kasaba sanatı görüşü kapatabilir, ölçülmedi.
- S1 kızağı, yuvalar, `repair-*` anchor'ları (#188 önerisi) ve #190 askıları: uygulanmadı. Kızak ölçümde geçici kutuydu.
- Parça görseli (0,6 m küp) ve gece işareti Mert'in sunum işidir. "Motor" HUD adı değişmedi (R01).
