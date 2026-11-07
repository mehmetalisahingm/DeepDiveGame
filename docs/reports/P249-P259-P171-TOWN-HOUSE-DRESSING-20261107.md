# #249 / #259 / #171 / #162 — Gerçek mağaza, dolu ortak ev ve dekorlu kasaba

7 Kasım 2026 · Mert · Unity 6000.3.23f1 · dal `p4/mert-town-placement`

Oyuncu ilk başladığında boş küpler yerine içinde yaşanan bir ev, kapısından girilen bir ekipman mağazası ve döşenmiş, geceleri ışıklanan bir sokak görür. Bu belge neyin değiştiğini, neyin ölçüldüğünü ve neyin henüz kanıtlanmadığını yazar; "PASS" kararı verilmez (AGENTS.md: ara kabuller P4.12/#164'te).

## 1. Ne değişti

| Parça | Nerede | Not |
|---|---|---|
| **Girilebilir ekipman mağazası dünyada** | `ContinuousTownLayout.PlaceEquipmentStore`, `Resources/CityKit/EquipmentStore` | Ekipman servis noktasının (slot 0) çocuğu; Mehmet'in `EquipmentStoreShopBinding` bağı aynen çalışır. Kapı ana sokağa (+Z) bakar, 2,6 m açıklık, eşik zeminle aynı seviye. Eski tezgâh, batı arka bina bloğu ve çatısı kaldırıldı; batı çit mağazanın iki yanına bölündü; kasaba bölgesi mağazanın batı duvarını içine alacak kadar genişledi. |
| **Mağaza yerleşimi (10 × 8 m)** | `P410EquipmentStoreBuilder` | Tezgâh arka duvar boyunca, raflar iki yan duvarda ve arka duvarda, ortada ekran masası; her koridor ≥ 1,8 m (`CheckSphere` r 0,9). |
| **Dolu ortak ev** | `HomeDressing`, `DressingLighting`, `Resources/CityKit/SharedHouse`, `P4SharedHouseBuilder` | Dışta sıva kaplama, kiremit çatı, ön sundurma + "ORTAK EV" tabelası + fenerler, pencere kutuları; içte ahşap zemin, lambri, 6 perdeli pencere, kirişler, 3 sarkıt lamba, halı, flama, sahil takımı duvarı, can simidi. **Dört yatak dört ayrı renk** (mavi/yeşil/turuncu/kırmızı örtü + yastık). **PC köşesi**: masa üstü, monitör, klavye, "KANAL" panosu. Gece pencereler kararır, lambalar ve ekranlar yanar. |
| **Görünür gelişim (Home2)** | `DevelopmentVisual.BuildTownHome` | Önceden yalnızca eski `PrepArea` sahnesinde vardı; gerçek evin arka duvarında kanepenin üstünde iki raf + kupa/kitap/plaket. Alınmamışsa 4 askı çivisi. |
| **Mağaza geliştirmesi (TownShop)** | `DevelopmentVisual.BuildShop` | Eski konum (anchor − 1,5 m) artık mağazanın arka duvarının dışına düşerdi; ikinci raf mağaza içinde, sokak duvarının batısında. |
| **Dekorlu kasaba** | `TownDressing`, `Resources/CityKit/TownDressing`, `P4TownDressingBuilder` | Her kenar bloğuna ahşap çit (direk + korkuluk; blok boyutu/çarpışması aynı, yalnız rengi değişir), ana sokak + ev yolu + mağaza önü + tezgâh önleri parke, 9 sokak lambası (gece yanar), bayrak dizileri, palmiye, sandık/varil, bank, saksı. |
| **Tabelalar duvarın ardından görünmüyor** | `DepthTestedText`, `Resources/CityKit/DepthText.shader` | Yerleşik `TextMesh` fontu 3B sahnede her şeyin üstüne çiziliyordu ("EKIPMAN", "BALIK ALICI" binaların arkasından okunuyordu). Yalnız o TextMesh'in materyali derinlik testli bir kopyayla değiştirilir; font materyali ve IMGUI metni dokunulmaz. Mağaza, ev ve `TownStorefront` etiketleri kullanır. |
| **Smoke sürücüleri kapıdan yürür** | `TownStoreRoute` (+ 4 sürücü) | Düz çizgiyle mağaza duvarına çarpıyorlardı. Rota yardımcısı kapı → koridor → masa çevresi → kasa yolunu verir; mağazada aynı ürünün çok kopyası olduğu için sürücü kapıya en yakın kopyayı seçer. |

Hiçbir dekor parçası collider, Rigidbody, NetworkObject veya etkileşim noktası taşımaz; yatak/depo/PC hedefleri ve rota ölçümleri aynı kalır.

## 2. Başka sahiplerin dosyalarına dokunuş (haber verilecek)

- **Utku** — `ContinuousTownLayout.cs` (mağaza sabitleri, `TownMin.x`, batı arka blok/çatı kaldırıldı, batı çit ikiye bölündü, `PlaceEquipmentStore` + `TownDressing.Place` çağrıları), `ContinuousTownWalkTests.cs` (mağaza kapısı rotaları + yeni test), `ContinuousTownWorldContractTests.cs` (blok sırası testi dekor çocuğunu saymaz), `P4RouteAudit.cs` ve `p172-route/current-routes.json` (b ve e rotaları mağaza kapısından; tüm rotalar temiz). Zemin bloğu/şehir koordinatlarına dokunulmadı.
- **Mehmet** — `ProductDaySmokeDriver`, `ProductDayShopPresentation`, `IntegratedSmokeDriver`, `ContinuousWorldSmokeDriver`: yalnız yürüyüş yönlendirmesi ve "tek kopya" varsayımı (`SingleOrDefault` → en yakın kopya). `EquipmentStoreShopBinding`, `ShopShelfPresentation`, `NetworkPlayer` değişmedi. Not: `ShopShelfPresentation.Sign` hâlâ yerleşik font materyalini kullanır (mağazada çizilmez; eski primitif raflarda tabela duvar ardından görünür).

## 3. Ölçüm

Gerçek Windows oynatıcısı, `DiveTestArea` tek başına (`TownLookProbe`, `-town-look`), RTX 4050 Laptop / D3D11 / Ultra:

| | |
|---|---|
| Sahnedeki renderer | 1557 (kasaba + mağaza + ev + dekor) |
| Üst kameradan bütün kasaba, gündüz | ort. 1,51 ms · p95 1,81 ms · maks 2,44 ms (300 kare) |
| Işık sayısı | ev 5 (3 sarkıt + 2 fener), kasaba 10 lamba (yalnız gece açık) |

Görüntüler `docs/reports/evidence/p259-town-look/` (gündüz / alacakaranlık / gece, mağaza ve ev içi/dışı, geliştirme "built" durumları).

## 4. Testler

Tam EditMode: **1605 / 1605 geçti** (7 Kasım, bu dalın son hâli).

- `P259SharedHouseTests` (9): collider/gövde/ağ parçası yok, renderer ve ışık bütçesi, arsa içinde, **yürüme şeritlerinde hiçbir şey yok** (oda ortası, PC/depo durma yerleri, yatak araları, yan koridorlar, kapı aralığı), dört yatak dört renk ve yatağın üstünde, PC köşesi + tabelalar okunur yönde, gece/gündüz geçişi, rig tek sefer ve doğru yerde, geliştirme raf/rafı gerçek mağazada ve evde.
- `P4TownDressingTests` (7): collider yok, bütçe, **rota kanıtına göre hiçbir dekor rotaya 0,6 m'den yakın değil**, yürünür döşemede ve mağaza/ev içinde değil, lamba gece/gündüz, çit yalnız yeniden renklendirir (kenar blokları aynı boyut ve collider).
- `ContinuousTownWalkTests.SmokeDriverStoreRouteIsWalkableBothWays`: sürücü yardımcısının çizdiği her bacak (ev, sokak, meydan, plaj, kasa, masa önü, mağazanın doğusu, balık alıcı) iki yönde 0,25 m adımlarla gerçek kapsül yürüyüşünden geçer.
- `P410EquipmentStoreTests`, `HomeRigAnchorTests`, `ContinuousTownLayoutTests`, `ContinuousTownWorldContractTests`: dekor kökünün temizliği ve tek-etiket kuralı güncellendi.

Smoke (tek oyuncu, gerçek entegre Windows oynatıcısı, mağaza + ev + dekorlu kasaba):

| Koşu | Sonuç |
|---|---|
| `Test-P46-ContinuousWorld -Players 1` | geçti: 204,5 m yürüme (kapı/koridor rotası dahil), 9 adım, hata yok; gün 2 yeniden yükleme ev doğuşu ve önceki erken uyku özeti doğru |
| `Test-P46-ProductDay -Players 1` | geçti: gün 2, gelir 60; fiziksel kamera rafından seçim para harcamadan görüldü (`shelfSelectionSeen`, `shelfSelectionNoSpend`), kasada ödeme sonrası seçim temizlendi (`cashierSelectionCleared`), kamera alındı (`cameraBought`), 2 satış, yeniden yükleme/klip oynatma/çift yayın doğru; ilk koşuda mağazanın çoklu raf kopyası ve kapı rotası sürücüyü durdurmuştu, `TownStoreRoute` ile düzeltildi |

## 5. Bu belgenin kanıtlamadığı

- 2 ve 4 oyunculu süreç koşuları ve ayrı bilgisayar/internet testi yapılmadı (AGENTS.md: yerel test onların yerine geçmez).
- Mağazanın Mehmet'in raf seçimi/sepet/kasa akışı gerçek süreçte yalnız ProductDay sürücüsünün yoluyla (kamera rafından seç → kasada öde) denendi; çoklu oyunculu geç katılım (`Test-P410-ShopLateJoin`) bu dalda yeniden koşulmadı.
- Dekor yalnız Windows'ta bakıldı; ses/yan etkiler (#278) ve #217/#218/#189 oyun içi kabulleri ayrı iştir.
- Ev ve kasaba dekoru kutu/çit/primitif + Quaternius MegaKit + Shore kit karışımıdır; nihai sanat yönü değil, "boş değil" seviyesidir.

## 6. Varlık kaynağı ve lisans

| Varlık | Kaynak / lisans | Nerede |
|---|---|---|
| Medieval Village MegaKit (duvar, zemin, çatı, pencere, sandık modülleri) | Quaternius, **CC0 1.0** (`Assets/DeepDive/Art/MegaKit/LICENSE-Quaternius-MedievalVillage.txt`); ücretsiz sürüm, depoda zaten vardı | mağaza, ev dış kaplaması/çatısı, kasaba parkesi, sandıklar |
| Shore kit (palmiye, kaya, varil, kova, çapa, sandık) | Depodaki `ShoreKit` prefab'ları (Pirate Kit türevi, kaynak ve CC0 notu `docs/reports/P4-7-KIT-PREP.md`) | ev köşeleri, kasaba dekoru |
| Supplied kit (yatak, kanepe, kitaplık, bitki, masa lambası) | Depoda mevcut; bu işte değişmedi | ev içi |
| Pencere, perde, halı, lamba, bank, saksı, çit, bayrak, monitör, mağaza rafı/ürünleri | Bu işte üretilen küp/silindir/küre ilkelleri; harici varlık yok | `P4SharedHouseBuilder`, `P4TownDressingBuilder`, `P410EquipmentStoreBuilder` |
| `DepthText.shader` | Bu işte yazıldı (yerleşik `GUI/Text Shader`'ın derinlik testli eşi) | `CityKit/DepthText.shader` |
| Başarı kartı sesi (#278) | Kodla üretilen sinüs çanı; ses dosyası yok | `SuccessFeedbackRules.CreateChime` |

Konsept görseli kullanılmadı; bütün görüntüler gerçek Windows oynatıcısından.

## 7. Performans notu

`DiveTestArea` tek başına, RTX 4050 Laptop / D3D11 / Ultra: 1557 renderer, bütün kasaba üstten gündüz ort. 1,51 ms (p95 1,81 ms). Önceki (mağazasız/ev dekorsuz) ölçüm 1,23 ms idi; ev + kasaba dekoru yaklaşık 0,3 ms ekledi. Dekor draw-call sayısı kalem kalem ölçülmedi; düşük donanım ölçümü yapılmadı.

## 8. Hangi doğrulamalar yapıldı / yapılmadı

| Doğrulama | Durum |
|---|---|
| Tam EditMode | yapıldı, 1612/1612 |
| 1 oyunculu gerçek Windows smoke (`ContinuousWorld`, `ProductDay`) | yapıldı, ikisi geçti |
| Gündüz/alacakaranlık/gece görüntüleri, gerçek Windows oynatıcısı | yapıldı |
| İlk başarı kartı: ekran görüntüsü + gerçek ağ satışında günlük | yapıldı (`P278-SUCCESS-CARD-20261107.md`) |
| **Sesli kısa video** | **yapılmadı**: makinede ekran/ses kaydedici ve ffmpeg yok; çan yalnız üretim testiyle (uzunluk, tepe, kırpma) doğrulandı, kulakla değerlendirilmedi |
| 2 oyunculu gerçek av–çekim | yapılmadı |
| Teknik 1/2/4 süreç regresyonu | yalnız 1 süreç yapıldı; 2 ve 4 yapılmadı |
| Mağaza geç katılım (`Test-P410-ShopLateJoin`) | bu dalda yeniden koşulmadı |
| Ayrı PC / insan kabulü (#164) | yapılmadı |
| #255 10M dengelemesi | bu işin kapsamında değil, tamamlandı gösterilmiyor |
