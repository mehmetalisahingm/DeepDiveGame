# #172 — Mevcut yaya rotaları ölçümü ve örnek bölüm yerleşim önerisi

16 Ekim 2026, Utku (B). Taban `codex/p4-integration` @ `c7b2449`. Bu rapor ölçüm ve **öneridir**: oyun davranışı, Layout, sahne, prefab ve servis konumları değişmedi. P4.7 uygulaması veya #159/#172 kabulü değildir.

## Tekrar üretim

Unity 6000.3.23f1 batch mode, `DeepDive.Editor.P4RouteAudit.Run` ([kaynak](../../Assets/Editor/P4RouteAudit.cs)). Araç gerçek `DiveTestArea` sahnesini ek sahne olarak açar; `ContinuousTownLayout.Build`, `HomeSceneSetup.EnsureRig`, `CoastalHarborLink.BuildMarkers` ve dört `TownStorefront.Build` ile oyunun çalışırken kurduğu zemin/ev/tezgâh/işaretleri kurar, ölçer ve sahneyi kaydetmeden kapatır. Kapsül değerleri `NetworkDiver.prefab`'tan okunur. [Ham ölçüm (JSON)](evidence/p172-route/current-routes.json).

**19 Ekim yeniden üretim (#186 kararı, [P4-ENGINE-SHORE](P4-ENGINE-SHORE.md)):** `boat-part-engine` kuru kıyıya, (−2.5, 8.4, −12.75) noktasına taşındı. JSON aynı araçla `6816fba` + bu değişiklik üstünde yeniden üretildi. Engine satırı ve 4 engine görüş hattı değişti. Ayrıca #185'ten gelen 7 `Fish_SeaBass_09…15` sınırı (suda) ve dükkân görsel sınırları (20 görüş hattının hedef kutusu) güncellendi. Rotalar, darboğazlar, dünya anchor'ları ve yatak/spawn bulguları değişmedi. Aşağıdaki tablolar `c7b2449` ölçümüdür; dükkân görüş piksel değerleri JSON'da günceldir.

- Kapsül: yarıçap 0,35, boy 1,80, step 0,30, slope 45°, skin 0,08 → dolgulu çap 0,86 m. Kara hızı 4 m/s. Göz 1,55 m, dikey FOV 60° (16:9 yatay ≈ 91,5°).
- Rota yöntemi: `ContinuousTownWalkTests.Drive` ile aynı (gerçek `CharacterController.Move`, dt 0,02 s, yer çekimi, her karede zemin/düşme/deniz kontrolü). Süre = simülasyon kare sayısı × dt; dönüş/durma/UI yok.
- Tek koordinat sistemi: Unity dünyası, metre, y yukarı, **+z kuzey (deniz)**, +x doğu.

## 1. Sınırlar (ölçülen)

| Öğe | x | z | y (üst) | Not |
|---|---|---|---|---|
| Kara yürüme yüzeyi (Layout + sahne kıyısı) | −14,75…14,75 | −55,5…−3,5 | 8,4 | Ev arsası x ±7,5 |
| `Beach_Wade` / `Shore_Ramp` (25°) | −9,5…−3,5 / 0,14…7,17 | −11,17…−4,14 / −11…−7 | 8,4 → 4,84 | Kuru kısım ≈ 0,8 m (aşağıda) |
| `Shore_Ledge` / `Dock_Town` | 7…11 / 8…10 | −11…−7 / −7…−3,5 | 8,4 | İskele 2 m geniş |
| `SwimVolume` (tek su otoritesi) | −15…15 | −15…65 | −27…8,0 | |
| Görünen deniz düzlemi (`WaterSurface*`, collider yok) | −30…30 | −30…70 | 8,1 | **x ±15–30, z −30…−15 yüzülemez ama su gibi görünür** |
| `DiveRegionField` `region-near-1` | −15…15 | −15…65 | — | Kasaba/ev haritada değil |
| Zone Home / Town / Coast / Dock | ±5,82 / ±14,75 / ±15 / 7…11 | −53,83…−42 / −55,8…−14,75 / −14,75…−3,5 / −11…−3,5 | | |
| Kenar çitleri `Edge_*` (15) | dış kenarlar | | 8,4…9,6 | |
| `MainBuilding_West/East` | ±14,5…±21,5 | −32…−24 | 8,4…13,6 | Zemin dışında |
| Ev duvarları / yataklar / depo / PC | ±6 / x −4,07…4,07 / −4,57…−3,22 / 3,2…4,6 | −54…−42 / −45,58…−43,53 / −51,92…−51,17 / −51,9…−51,2 | | |
| Tezgâhlar (4) | −10/−3,3/3,4/10,1 ± 0,9 | −28,6…−28,1 | 9,4 | |

### Kuşbakışı (1 karakter = 1 m doğu-batı, 1 satır = 2 m kuzey-güney; ölçülen JSON'dan üretildi)

```
   z |x -22 ............................ 0 ............................ +22|
   0 |       ~~~~~~~~~~~~~~~~~~~~~~~~B~~~~~        |  B kırık sandal yanaşma yeri (9, -0.4)
  -4 |       ~~~~~~~~~~~~~~~~~~~~~~~.K~~~~~        |  K Dock_Town_Anchor (9, -3.7)
  -6 |       ~~~~~///////~~~~~~~~~~~..~~~~~        |  / wade (batı) ve rampa (doğu)
  -8 |       ~~~~~///2///~~~///////....~~~~        |  2 motor parçası (su altında)
 -10 |       ~~~~~///////~~~///////..X.~~~~        |  X DiveExit (9, -9)
 -12 |      ||..1.......................3.||       |  1 gövde, 3 yakıt tankı
 -14 |      ||..............G.............||       |  G Beach_TownGate
 -16 |      ||............................||       |
 -22 |       |............................|        |  ana cadde (22 m geniş)
 -24 |MMMMMMMM............................MMMMMMMM |  M MainBuilding (zemin dışında)
 -28 |MMMMMMMM...=c....=c=....=c=....=c...MMMMMMMM |  = tezgâh, c müşteri noktası
 -30 |MMMMMMMM..............Q.............MMMMMMMM |  Q town-square (0, -29)
 -36 |       |-----------......-----------|        |  meydanın güney çiti
 -40 |                 ||......||                  |  ev yaklaşımı (7,5 m)
 -42 |              |HH--...D..--HH-               |  D home-door; -- PlotNorth çitleri
 -44 |              |HHbbbbbbbbbbHH|               |  b yataklar
 -48 |              |HH...s..s...HH|               |  s home-spawn 0-3
 -52 |              |HH.T......P.HH|               |  T depo, P PC
 -56 |              |---------------               |
```

## 2. Anchor ve servis haritası (kimlikler aynen)

| Kimlik | Konum (x, y, z) | Zone | Haritada |
|---|---|---|---|
| `home-root`, `home-door` | (0, 8.4, −48), (0, 8.4, −42) | Home | hayır |
| `home-bed-0…3` | (−3.45 / −1.15 / 1.15 / 3.45, 8.7, −44.55) | Home | hayır |
| `home-storage`, `home-pc` | (−3.9, 9.2, −51.55), (3.9, 9.0, −51.55) | Home | hayır |
| `home-spawn-0…3` | (∓1.5, 8.4, −49.5), (∓1.5, 8.4, −48.25) | Home | hayır |
| `town-square` | (0, 8.4, −29) | Town | hayır |
| `town-service-slot-0…3` | (−10 / −3.3 / 3.4 / 10.1, 8.4, −29) | Town | hayır |
| Servis müşteri noktaları `shop-equipment`, `buyer-fish`, `buyer-recording`, `vendor-vehicle` | slot x, z −27.3 | Town | hayır |
| `Dock_Town_Anchor` = `dock-town-1` (biniş aynı nokta) | (9, 8.4, −3.7) | Dock | evet |
| `DiveExit` (tetik 3,5 × 2 × 3,5) | (9, 9.3, −9) | Dock | evet |
| `Beach_Wade` kuru üst / su çizgisi | (−6.5, 8.39, −10.97) / (−6.5, 8.01, −10.17) | Coast | evet |
| `Shore_Ramp` kuru üst / su çizgisi | (6.97, 8.39, −9) / (6.17, 8.01, −9) | Coast | evet |
| `Beach_TownGate` | (0, 8.4, −13.5) | Coast | evet |
| `boat-part-hull`, `boat-part-fuel-tank` | (−12, 8.4, −12.5), (12.5, 8.4, −12.5) | Coast | evet |
| `boat-part-engine` | (−2.5, 8.4, −12.75); 19 Eki'ye kadar (−6.5, **7.0**, −8), **Sea** | Coast | evet |
| Kırık sandal yanaşma yeri (`route-near-1` wp0 + Rowboat ofseti 0) | (9, 8.0, −0.4) | Sea | evet |
| Bed çıkış pozları `bed-0…3:exit` | (yatak x, 8.75, −47.52) | Home | hayır |
| Eski `PlayerSpawnPoint` `Spawn_0…3` | (±3, 1, ±3) | Sea | evet |

17 world anchor'ın 17'si bulundu. **Kırık sandal sahnede yoktur**: `NetworkBoatController` çalışırken yakın rota wp0'ına oturur ve `BoatTripNetworkBinding` onarım bitene kadar gizler. Oyuncu onarımdan önce iskelede "kırık sandal" görmez; yalnız üç parça vardır.

## 3. Rotalar (gerçek CharacterController, 4 m/s)

Hiçbir rotada düşme, sıkışma veya engellenen kare yok (blockedFrames 0).

| Rota | Mesafe (sürülen) | Süre | Sonuç | En dar genişlik (yer) |
|---|---|---|---|---|
| a0 doğrudan: spawn-0 → iskele | 58,6 m | 14,7 s | iskeleye vardı | 1,56 m — yatak-0 köşesi / PlotNorth çiti |
| a1 doğrudan: spawn-1 → iskele | 58,6 m | 14,7 s | iskeleye vardı | 1,56 m — yatak-3 / PlotNorth çiti |
| a2 doğrudan: spawn-2 → iskele | 57,4 m | 14,4 s | iskeleye vardı | 1,56 m |
| a3 doğrudan: spawn-3 → iskele | 57,4 m | 14,4 s | iskeleye vardı | 1,56 m |
| b alışveriş: ev → ekipman → iskele | 69,7 m | 17,5 s | vardı | 1,56 m; ev dışı 2,0 m (iskele) |
| c satış: iskele/DiveExit → balık → kayıt → ev PC → yatak-3 | 67,9 m | 17,0 s | vardı | 1,56 m; ev dışı 2,0 m (iskele) |
| d1 su girişi: ev → wade → su | 52,1 m (suya kadar) | 13,0 s | denize girdi | ev dışı 4,8 m (tezgâh arası) |
| d2 su çıkışı: rampa su çizgisi → DiveExit → ev | 56,4 m | 14,1 s | eve vardı | ev dışı 3,95 m (rampa) |
| e isteğe bağlı gezinti: kasaba çevresi + ev içi + iskele kolu | 155,0 m | 38,8 s | başlangıca döndü | ev dışı 2,0 m (iskele) |

Birleşik gün (kara kısmı): **b + c = 137,7 m ≈ 34,5 s**. Su içi yüzme, tekne yolculuğu ve tezgâh/PC/uyku etkileşim süresi dahil değil.

### Dar geçitler ve ev darboğazı

| Yer | Genişlik | Dolgulu kapsül (0,86) üstü pay |
|---|---|---|
| Yatak sırası, yan koridorlar (duvar–yatak-0 / yatak-3–duvar) | 1,748 m | 0,89 m |
| Yatak sırası, yataklar arası (3 boşluk) | 1,048 m | 0,19 m |
| Yatak sırası ile cephe çiti arası (rota üstünden, çapraz) | 1,557 m | 0,70 m |
| Açık cephe (kapı çizgisinin 0,2 m içi) | 11,648 m | — |
| Cephe dışında gerçek çıkış (`Edge_PlotNorth*` x ±3,75…±7,8 kapalı) | 7,5 m (`HouseApproach`) | — |
| İskele (`Dock_Town`) | 2,0 m | 1,14 m — sırayla geçiş, dört kişi yan yana değil |
| Spawn-2/3 → en yakın yatak çıkışı (bed-1/bed-2) | **0,807 m** | kapsül çapı 0,70'ten büyük, dolgulu 0,86'dan küçük (#173 bulgusu yeniden ölçüldü, aynı) |
| Spawn-0/1 → en yakın yatak çıkışı | 2,008 m | |

Dört spawn'ın hepsi aynı 7,5 m'lik yaklaşımdan çıkar; ev içindeki gerçek sıkışma yatak araları (1,05 m) ve 0,807 m spawn–yatak çıkışıdır. Dört sürecin aynı anda kapıdan geçişi EditMode'da ölçülmedi.

## 4. Görüş (göz 1,55 m, FOV 60°, yalnız geometri)

"Görünür" = gözden hedefin merkezine veya üstüne engelsiz ışın. "Kadrajda" = verilen bakış yönünde 16:9 kadraja giriyor. px = 1080p'de yaklaşık hedef yüksekliği.

| Nereden (bakış) | Hedef | Mesafe | Görünür | Kadrajda | px |
|---|---|---|---|---|---|
| Ev kapısı (kuzey) | 4 dükkân | 13,8–16,7 m | evet | evet (±14°…±37°) | 185–225 |
| Ev kapısı | İskele | 37,5 m | **hayır** — kayıt alıcısının NPC gövdesi | evet | ≈ 10 |
| Ev kapısı | Sandal yanaşma yeri (varsayılan 1,4 × 1 × 3,5 m kutu; sandal sahnede yok) | 42,3 m | evet | evet | ≈ 22 |
| Ev kapısı | Motor parçası | 34,3 m | **hayır** — balık tezgâhı | evet | 16 |
| Meydan (kuzey) | 4 dükkân | 3,4–10,1 m | evet | **hayır** (±79°…±86°, yanda) | 305–920 |
| Meydan | İskele / yanaşma yeri | 25,4 / 30,0 m | evet | evet | 15 / 31 |
| Meydan | Motor parçası | 22,0 m | **hayır** — `MainStreet` kenarı (parça su altında) | evet | 25 |
| Kıyı, plaj ortası (güney) | 4 dükkân | 16,2–18,8 m | evet | evet | 164–191 |
| Kıyı (güney) | Ev kapısı | 29,5 m | evet | evet | 95 |
| Kıyı (güney) | İskele / yanaşma yeri | 11,6 / 15,1 m | evet | hayır (arkada, −129°/−143°) | 32 / 62 |
| İskele (güney) | 4 dükkân / ev kapısı | 23,9–30,5 / 38,6 m | evet | evet | 101–129 / 73 |

Okuma: kasaba ile kıyı arasında görüş açık; boş zemin hiçbir şeyi saklamıyor. Evden iskele 37,5 m'de ≈ 10 px ve bir NPC gövdesi arkasında kalıyor; iskele/sandal için yüksek bir işaret yok. Sis, ışık ve kontrast ölçülmedi.

## 5. 60–90 s hipoteziyle kıyas (hipotez, sonuç değil)

60–90 s, #172 talimatındaki **hedef hipotezidir**; hangi tura uygulanacağı ve doğru olup olmadığı açık karardır.

| Ölçülen | Bugün | 60 s'ye oranı | 90 s'ye oranı |
|---|---|---|---|
| Tek yolculuk (a: ev → iskele) | 14,4–14,7 s | %24 | %16 |
| Alışveriş turu (b) | 17,5 s | %29 | %19 |
| Kara günlük birleşik (b + c) | 34,5 s | %58 | %38 |
| İsteğe bağlı gezinti (e) | 38,8 s | %65 | %43 |

4 m/s'de 60–90 s = 240–360 m yürüyüş. Hız kararıyla aynı süre için gereken uzunluk: 3 m/s → 180–270 m, 2 m/s → 120–180 m, 1,5 m/s → 90–135 m.

## 6. Örnek bölüm ÖNERİSİ (uygulama değil)

Amaç #159 kısa görsel örneği için tek, dolu bir yol: **ev → bir dükkân → kıyı → kırık sandal**. Yeni şehir, ada veya dalış bölgesi yok; aynı koordinat sistemi. Su tarafı (z ≥ −15: plaj, wade, rampa, sahanlık, iskele, `SwimVolume`) **değişmez**. Ev yerinde kalır (`HomeOrigin` ve ev anchor'ları aynı). Yeni zemin yalnız z < −15 ve x < −14 tarafında, su hacminin dışında.

Ölçeksiz şema; koordinatlar tablodadır.

```
   z    x: -30    -26    -22    -14        0        9
  -4                                               K≈B   iskele + yanaşma yeri (mevcut); [8] sandal görseli
 -12                   [6]~~~~~~|..1....G.....3..X|      plaj (mevcut); [6] korkuluk: görünen ama yüzülemez deniz
 -14                            |........ plaj ...|
 -17          (C) +=========+--o  <- kıyı bağlantısı, Edge_TownWest'te 4 m açıklık
                  |  [5]    |    \
 -19              |kıyı yolu|     \___ mevcut cadde/meydan
 -24        [4] S1 dükkân   |   MainBuilding_West (kalır veya [4]'ün arka cephesi olur)
 -27              c  <- müşteri noktası (slot 0 buraya taşınırsa)
 -31     (B) [3] köşe/ yan bağlantı ========> meydanın batı kenarı (-14, -31)  [7] ara sokak
 -39  (A) +--------- ana yol (6 m) -----------------+  <- Edge_ApproachWest'te açıklık
          [2] tabela/fener   [1] cepheler, çit       |
 -42                                                D  ev kapısı (mevcut)
 -48                                               ev (mevcut)
```

| Alan | Zemin önerisi (Utku) | Mert kitine ayrılan içerik (#171) | Neden |
|---|---|---|---|
| [1] Ana yol, kapı → batı | z −41,5…−36,5 (5 m, `Edge_PlotNorthWest` ile meydan güney çiti arasında), x −29…−3,75 | İki yanda alçak cephe/bahçe duvarı, saksı, 2 lamba, çit | Evden çıkışta tek yön; boş zemin değil |
| [2] Köşe (A) | (−26, −39) | Tabela direği + uzun fener/lamba (yön işareti) | Kapıdan 26 m'de görüş çizgisinin ucu |
| [3] Köşe (B) ve yan bağlantı [7] | (−26, −31) → meydan batı kenarı (−14, −31), 12 m × 3 m | Ara sokak: sandık, ip, duvar; meydanın mevcut dükkânlarına kestirme | Küçük döngü; tek çıkmaz yol olmaz |
| [4] S1 dükkân | Bina batı sokağın batısında (x −35…−29), müşteri noktası sokakta ≈ (−26, −27,3) | Dükkân binası/tabela/tezgâh kiti (ekipman) | Örnekteki tek dükkân |
| [5] Batı sokağı + kıyı bağlantısı | x −29…−23, z −41,5…−17 → (−14, −17) | Korkuluk, ağ, kasa, sahil lambası | Dükkândan denize yön; deniz görünür |
| [6] Korkuluk | Batı sokağı ve kıyı bağlantısının x < −15 dış kenarları | Ahşap korkuluk | Görünen deniz düzlemi x ±30'a kadar, `SwimVolume` ±15: arası düşüş boşluğu |
| [8] Kırık sandal | Yanaşma yeri (9, 8.0, −0.4) — konum değişmez | Onarılmamış sandal görseli (aynı yer, onarımda değişir) veya kıyıda ayrı enkaz | Bugün onarımdan önce iskelede hiçbir şey görünmüyor |

Öneri yolu (planlanan çizgi): ev içi 14,5 m + ana yol ve dükkân 40,7 m + kıyı yolu ve iskele 54,0 m = **≈ 109 m → 4 m/s'de ≈ 27 s**; yan bağlantı 12 m. Bu bölüm tek başına 4 m/s'de 60 s'ye ulaşmaz (2 m/s'de ≈ 55 s, 1,5 m/s'de ≈ 73 s). Uzunluğu 240 m'ye çıkarmak "kısa örnek" kapsamını aşar; hız veya kapsam kararı gerekir.

Görüş/yön bulma önerisi: kapıdan batıya bakınca [2] feneri yolun ucunda; (A)'dan kuzeye bakınca [4] tabelası 12 m'de; (C)'den kuzeydoğuya bakınca iskele ve sandal yeri ≈ 25–30 m'de, **iskelede yüksek bir direk/bayrak** (bugün 10 px). Ölçüm uygulamada aynı araçla tekrarlanır.

Uygulamada gerekecek (bu teslimde yapılmadı): yeni zemin/çit blokları, `Edge_ApproachWest` ve `Edge_TownWest`'te açıklık, `Zone_Town` genişlemesi (yoksa yeni yol `Unknown`), slot 0'ın taşınması (Mert ile), `ContinuousTownWalkTests`/`P4RouteBaselineTests` rotalarının yeniden üretilmesi.

## 7. Açık kararlar ve uygulama kapısı

- **Harita kapsamı:** Kasaba/ev `DiveRegionField` dışında (bütün ev/kasaba anchor'ları haritada değil). Örnek bölüm de dışarıda kalır. Bölge büyütülecek mi? (Mehmet karar, Mert harita UI.)
- **Hız:** 4 m/s kalırsa 60–90 s hipotezi kısa bölümle karşılanmaz. Hız Mehmet'in alanı.
- **Hipotezin kapsamı:** 60–90 s tek yolculuk mu, günlük kara turu mu?
- **Kırık sandal görünürlüğü:** yanaşma yerinde onarılmamış görsel mi, kıyıda ayrı enkaz mı? (Mehmet tekne, Mert görsel.)
- **Servis taşıma:** slot 0'ın yeni dükkâna taşınması Mert ile; kimlikler ve 4 servis sayısı değişmez.
- **Kapı:** Yerleşim uygulaması #141 teknik kabulü ve Mert #171 örnek kiti geldikten sonra başlar. Bu rapor kapıyı açmaz.

## 8. Yapılmayanlar

Gerçek 1/2/4 süreç turu, Windows build, gece/gündüz önce-sonra görüntüsü, sis/ışık okunurluğu, kare süresi, yüzme ayakları ve kullanıcı değerlendirmesi uygulama aşamasındadır. EditMode rota ölçümü ayrı bilgisayar veya internet testinin yerine geçmez.

## Otomatik tekrar kontrolü

`Assets/Tests/P4/Editor/P4RouteBaselineTests.cs` (11 test) rotaları JSON'dan okur, kopya tutmaz: kayıtlı rotalar temiz ve uzunlukları waypoint'lerle tutarlı; 17 world anchor ve spawn–yatak çıkışı (0,807 m) canlı yerleşimle aynı; dokuz rota gerçek `CharacterController` ile yeniden yürür (d2 rampa ayağı hariç, audit tam yürüdü). Yerleşim değişirse test kırılır: audit yeniden koşturulur ve bu rapor güncellenir. EditMode önce 1258/1258, sonra 1269/1269 PASS.
