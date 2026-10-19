# #188 — Ücretsiz parça erişimi ve dört kişilik tamir alanı ölçümü

18 Ekim 2026, Utku (B). Taban `codex/p4-integration` @ `6816fba` (#185 dahil). Bu rapor bir ölçüm ve **öneridir**. Oyun davranışı, Layout, sahne, prefab ve anchor değişmedi. #188 kabulü, #186 R01/R02 kararı veya P4.8 uygulaması değildir. Sözleşme referansı: PR #190 `docs/plan/P48_REPAIR_CONTRACT_PROPOSAL.md` (Mehmet, birleşmedi).

## Tekrar üretim

Unity 6000.3.23f1 batch mode, `-executeMethod DeepDive.Editor.P48RepairAreaAudit.Run` ([kaynak](../../Assets/Editor/P48RepairAreaAudit.cs)). Yöntem #172'deki `P4RouteAudit` ile aynıdır:

- Gerçek `DiveTestArea` ek sahne olarak açılır. Üstüne `ContinuousTownLayout.Build`, `HomeSceneSetup.EnsureRig`, `CoastalHarborLink.BuildMarkers` ve dört `TownStorefront.Build` kurulur.
- Ölçümden sonra sahne kaydedilmeden kapanır. Aday tekne kutuları ve işçi kapsülleri geçici ölçüm nesneleridir; kapanmadan önce silinir.

[Ham ölçüm (JSON)](evidence/p188-repair/current-repair-area.json). Kilit test: `P48RepairAreaBaselineTests` (5 test). Kanıt dosyasını okur, parça/yanaşma yerini canlı sahneyle karşılaştırır, kuru taşıma ve bırakma yollarını gerçek `CharacterController` ile yeniden yürütür.

**Ölçüm temeli**

- Kapsül `NetworkDiver.prefab`'tan okunur: yarıçap 0,35, boy 1,80, skin 0,08, step 0,30, merkez y 0,9. Dolgulu çap 0,86 m.
- Hızlar: yürüme 4 m/s, yüzme 3 m/s (palet 0). Göz 1,55 m, FOV 60°, toplama ışını 2,5 m.

**Duruş ve taşıma kuralları**

- **Duruş hücresi:** 0,25 m ızgarada kuru yürüme zemini (y ≥ 8,01, `SwimVolume` dışı). Merkezin ve ±0,2 m çevresinin altında zemin bulunur. Dolgulu kapsül hiçbir katı collider ile çakışmaz.
- **Taşıma:** Kara bacağı duruş ızgarasında genişlik öncelikli yol araması ve sadeleştirmeyle bulunur, ardından gerçek CC ile yürünür (dt 0,02, yer çekimi, her karede düşme kontrolü).
- **Su bacağı:** Su bacağında sahnenin `WaterField.CreateTracker()` izleyicisi (deadband dahil) her karede Land/Surface/Underwater kararını verir. Surface'te yukarı girdi kesilir (`FilterMoveInput` ile aynı).
- **Taşıma hızı:** Integration'da ve PR #190'da taşırken yavaşlama yok; süreler saf hareket süresidir.

**Kullanılan bantlar**

- Çalışma bandı: gövde kenarına 0,45–1,5 m.
- Bekleme bandı: 1,5–4 m.
- İşçi aralığı: ≥ 1,06 m (0,86 + 0,2 pay).
- Geçiş: ≥ 0,86 m.

## 1. Mevcut durum (ölçülen)

### Ücretsiz parçalar

| Teknik kimlik (R01 önerisi adı) | Konum (x, y, z) | Zone | Zemin | Su | Kuru karadan E ile alınır mı | Görsel |
|---|---|---|---|---|---|---|
| `boat-part-hull` (gövde yaması) | (−12, 8.4, −12.5) | Coast | `Beach_Platform` | kara, `SwimVolume` dışı | evet, parçanın üstünden | 0,6 m küp |
| `boat-part-engine` (oturak/destek) | (−6.5, **7.0**, −8) | **Sea** | `Beach_Wade` eğimi | **`SwimVolume` içinde, yüzeyin 1,0 m altında**; orada duran dalgıç `Surface` | **hayır**: 2,5 m içinde kuru duruş hücresi yok, yüzerek alınır | 0,6 m küp |
| `boat-part-fuel-tank` (kürek bağlantısı) | (12.5, 8.4, −12.5) | Coast | `Beach_Platform` | kara | evet | 0,6 m küp |

Sahnede yalnız üç `IBoatPartPickupTarget` vardır; üçü de `BoatPartAnchor`. Dekor nesnesi parça gibi işaretlenmiş değil, hiçbir parça collider içinde değil. PR #190 çalışırken askıya üç ek `IBoatPartPickupTarget` (`BoatRepairRackTarget`) ekler; bunlar sahnede değildir.

### Hasarlı sandal

- **Sahnede sandal yok.** `NetworkBoatController` çalışırken `route-near-1` wp0'a (+Rowboat ofseti 0 m) oturur. `BoatTripNetworkBinding` onarım bitene kadar gövdeyi gizler. PR #190 aynı pozda geçici bir kırık gövde çizer.
- **Yanaşma yeri:** (9, 8.0, −0.4), yaw 0 (kuzey). Kapladığı alan x 7.8…10.2, z −2.9…2.1; tamamı **suda** (`SwimVolume`, deniz tabanı y 0 → 8 m derinlik).
- Kıç ile iskele ucu (`Dock_Town` z −3,5) arasında **0,6 m su boşluğu** var.
- Dört koltuk (8.45/9.55, 8.35, −1.3/0.4) suyun üstünde. Biniş noktası `dock-town-1` (9, 8.4, −3.7), iskele ucunda.

### PR #190 geçici tamir alanının bu dünyadaki yeri

Yerel ofsetler yanaşma yerine uygulandığında:

| Öğe | Dünya konumu | Zemin | Biniş noktasına uzaklık |
|---|---|---|---|
| 3 yuva tetiği | (8.35 / 9.0 / 9.65, 8.75, −2.55) | **suyun üstü** (kıç içi) | 1,15–1,32 m |
| 3 askı (ortak malzeme) | (8.35, 8.65, −4.5 / −5.3 / −6.1) | `Dock_Town` kuru, `SwimVolume` dışı, çakışma yok | 1,03–2,49 m |
| 3 duruş noktası | (8.35 / 9.0 / 9.65, 8.4, −4.2 / −4.6 / −4.2) | `Dock_Town` | **0,82–0,90 m** |

Askılar güvenli bırakma şartlarını karşılıyor: kara üstünde, su dışında, collider içinde değil. Ancak askılar ve duruş noktaları 2 m genişliğindeki tek iskele koridorunun üstünde.

### Kuşbakışı (1 karakter = 1 m; ölçülen sınırlardan üretildi)

```
     x-15            0             15
   2 |~~~~~~~~~~~~~~~~~~~~~~~bbb~~~~~|   b  bugünkü yanaşma yeri (sandal suda, kuzeye bakar)
   0 |~~~~~~~~~~~~~~~~~~~~~~~bbb~~~~~|
  -2 |~~~~~~~~~~~~~~~~~~~~~~~bbb~~~~~|   kıç z -2.9 / iskele ucu z -3.5 → 0.6 m su
  -4 |~~~~~~~~~~~~~~~~~~~~~~~vKv~~~~~|   K dock-town-1 biniş; v S0'da sığan 2 işçi
  -6 |~~~~~~//////~~~~~~~~~~~===~~~~~|   = Dock_Town (2 m)   / wade (batı), rampa (doğu)
  -7 |~~~~~~//////~~~~//////:d==:~~~~|   d S0 için en yakın koridor dışı bırakma (7.5,-7.25)
  -8 |~~~~~~///2//~~~~//////:::::~~~~|   2 engine parçası (su altında, y 7.0)
  -9 |~~~~~~//////~~~~//////::X::~~~~|   X DiveExit   : Shore_Ledge
 -11 |~.....//////..../////w:::::...~|   w S1 işçi noktaları (4)
 -12 |~..1...........w.SSSSS.....3..~|   1 hull parçası, 3 fuel-tank parçası
 -13 |~...........,,,,,SSSSS........~|   S önerilen S1 kızak (karada, kıyıya paralel)
 -14 |~...........,,,,w,,..w.D......~|   D S1 güvenli bırakma (7.5,-14.5)
 -16 |~,,,,,,,,,,,,,,,,,,,,,,,,,,,,,~|   , CoastWalk / MainStreet (kasaba)
```

## 2. Parça → sandal taşıma yolları (gerçek CC)

Hedef: o alanın ilk çalışma noktası. Hiçbir yolda düşme veya sıkışma yok; hepsi hedefe ulaştı.

| Yol | S0 (bugünkü yanaşma, iskele ucu) | S1 (plaj doğu) | S2 (plaj batı) | S3 (cadde) |
|---|---|---|---|---|
| hull → | 26,4 m · 6,6 s | 12,5 m · 3,1 s | 6,9 m · 1,7 s | 16,6 m · 4,1 s |
| fuel-tank → | 10,1 m · 2,5 s | 16,3 m · 4,1 s | 18,4 m · 4,6 s | 14,5 m · 3,6 s |
| engine → wade üzerinden (0,74 s yüzme + yürüme) | 24,6 m · 6,3 s | 10,7 m · 2,8 s | 6,5 m · 1,8 s | 16,5 m · 4,3 s |
| engine → rampa üzerinden (4,2 s yüzme + yürüme) | 19,5 m · 5,9 s | 28,0 m · 8,0 s | 30,3 m · 8,6 s | 27,7 m · 7,9 s |

- Her engine yolu `surface > walk` sırasıyla tek su geçişi içerir.
- Engine'i yüzerek doğrudan sandalın suda kalan yanına taşımak 16,1 m · 5,4 s sürer. Ancak PR #190 yüzerken `Place` isteğini reddeder; bu yol onarım sayılmaz.
- Kara yolları: hull/fuel-tank parçaları tamamen kara, kamera veya onarılmış tekne istemiyor. Engine için kısa bir yüzme gerekiyor: wade su çizgisine 2,4 m, 0,74 s.

## 3. Dört kişilik çalışma alanı

Aday alanlar 2,4 × 5 m (Rowboat ölçüsü). S1–S3'te gövde ölçüm süresince katı kutu kabul edildi; PR #190 alanı collider'sız olduğu için bu temkinli bir varsayım.

**Alan özeti**

| | S0 bugünkü yanaşma | S1 plaj doğu (3.6, 8.4, −12.6), yaw 90 | S2 plaj batı (−2.2, 8.4, −12.6), yaw 90 | S3 cadde (5.5, 8.4, −19), yaw 90 |
|---|---|---|---|---|
| Gövdenin altı | %100 su | %100 kuru `Beach_Platform` | %100 kuru | `CoastWalk`/`MainStreet` (Town zone) |
| Çalışma hücresi (koridor dışı) | 21 (**0**) | 240 (155) | 252 (157) | 354 (354) |
| ≥ 1,06 m aralıklı işçi | **2** — (8.25, −3.75), (9.5, −3.75), iskele ucu | **4**, hepsi koridor dışı | 4, koridor dışı | 4, koridor dışı |
| Koridor dışı bekleme alanı (≤ 4 m) | **0 m²** | 34,8 m² | 35,0 m² | 74,3 m² |
| Güvenli bırakma (koridora ≥ 1,31 m, kuru, su dışı, çakışmasız) | (7.5, 8.4, −7.25), gövdeye 4,4 m, `Shore_Ledge` | (7.5, 8.4, −14.5), gövdeye 1,6 m | (1.75, 8.4, −14.25) | (2.25, 8.4, −16.5) |
| 8 m içinde kıyı balığı | 7 | 5 | 6 | 0 |

**Alan + işçiler varken koridorlar** (genişlik; uçlar arası yürünebilir yol)

| | S0 | S1 | S2 | S3 |
|---|---|---|---|---|
| İskele → `dock-town-1` (2,0 m) | **0,62 m; biniş ucu işçilerle kapalı** | 2,0 · 8,5 m | 2,0 | 2,0 |
| Rampa → DiveExit (3,95 m) | aynı | 3,65 | aynı | aynı |
| Wade → plaj (6,0 m) | aynı | aynı | 4,65 | aynı |
| Kapı (cadde → plaj) | aynı | 15,4 m geniş | **plaj ucu gövdenin altında** | 16,1 |
| Plaj şeridi (hull ↔ fuel-tank, 24,5 m) | aynı | düz çizgi kapalı, dolanma +2,2 m (26,7 m) | +3,8 m | +1,4 m |

**Okuma**

- **S0 (bugünkü poz)** dört kişiyi kaldırmıyor. Sandal suda olduğu için çalışma yalnız 2 m'lik iskele ucunda yapılabiliyor. Orada iki kişi sığıyor, iskeleyi 0,62 m'ye daraltıyor ve biniş noktasını kapatıyor. Koridor dışında bekleme ya da bırakma yeri yok; en yakını DiveExit sahanlığında, 4,4 m uzakta. PR #190'ın duruş noktaları da bu sonuçla tutarlı: üç kişi iskele ucunda, biniş noktasına 0,8–0,9 m.
- **S1** dört işçiyi koridor dışında, iskele/rampa/wade yollarına dokunmadan alıyor.
  - Bedel: plaj şeridinde 2,2 m dolanma ve kapıdan plaja inişin yanında bir gövde.
  - İskele, rampa ve wade aynı kalıyor. Meydandan ve iskeleden (9,7 m) kadrajda görünüyor.
- **S2** kapı hattının plaj ucunu gövdeyle kapatıyor ve wade yolunu 4,65 m'ye daraltıyor. **S3** kıyıda değil, kasaba caddesinde. İkisi de S1'den zayıf.

## 4. Görüş (göz 1,55 m, FOV 60°, yalnız geometri)

| Nereden (bakış) | hull | engine | fuel-tank | S0 yanaşma | S1 kızak | S1 bırakma |
|---|---|---|---|---|---|---|
| Ev kapısı (kuzey) | görünür, 31,6 m, 18 px | **hayır** (tezgâh), 16 px | görünür, 18 px | görünür, 42 m, 20 px | görünür, 29 m, 29 px | hayır (kayıt NPC gövdesi) |
| Meydan (kuzey) | görünür, 27 px | **hayır** (`MainStreet` kenarı; su altında) | görünür | görünür, 28 px | görünür, 16,8 m, 50 px | görünür, 46 px |
| Plaj ortası (kuzey) | görünür, kadraj dışı (±90°) | görünür, kadraj dışı (−55°) | kadraj dışı | kadrajda, 56 px | kadraj dışı (yanda) | kadraj dışı |
| Plaj ortası (doğu) | arkada | arkada | kadrajda, 45 px | kadraj dışı | kadrajda, 3,6 m, 224 px | kadrajda, 95 px |
| İskele (güney) | görünür, kadraj dışı | görünür, kadraj dışı | kadrajda, 63 px | arkada | kadrajda, 86 px | kadrajda, 74 px |

Engine evden ve meydandan görünmüyor: su altında ve bir engelin arkasında kalıyor (#156'daki "üçüncü parça bulunamadı" sorunu geometride de görülüyor). Onarım alanı karada (S1) olursa evden, meydandan ve iskeleden görünüyor. Bugünkü sandal ise oyuncu onarım bitene kadar hiç görmüyor; PR #190'ın geçici gövdesi suda, 42 m'de ≈ 20 px. Sis, ışık ve kontrast ölçülmedi (bkz. §6).

## 5. ÖNERİ — anchor kimlikleri ve yerleşim (uygulanmadı)

Tek koordinat sistemi geçerlidir: Unity dünyası, metre, +z kuzey. Kimlikler `WorldAnchorIds` düzeninde, kebab-case ve indekslidir; indeks `BoatRepairParts.All` sırasını izler (0 hull, 1 engine, 2 fuel-tank) ve PR #190'daki yuva indeksiyle aynıdır. Teknik parça kimlikleri değişmez. Anchor yalnız *yer*dir: edinme, rezervasyon ve tamamlanma `EconomyManager`/onarım otoritesinde kalır. İkinci onarım otoritesi veya sayaç yoktur (`BoatPartAnchor` kuralı).

| Önerilen kimlik | Ne | Önerilen değer | Dayanak |
|---|---|---|---|
| `repair-part-start-0` | gövde yaması başlangıcı | (−12, 8.4, −12.5), mevcut | ölçüldü: kuru, görünür |
| `repair-part-start-1` | oturak/destek başlangıcı | **karar:** (a) mevcut (−6.5, 7.0, −8), su altı, yüzerek alınır; (b) karaya taşınır, koordinat uygulama öncesi bu audit ile ölçülür | (a) erişilebilir ama evden/meydandan görünmüyor |
| `repair-part-start-2` | kürek bağlantısı başlangıcı | (12.5, 8.4, −12.5), mevcut | ölçüldü |
| `repair-boat-cradle` | tamir sırasında kırık sandalın durduğu kızak | (3.6, 8.4, −12.6), yaw 90, 2,4 × 5 m (S1) | ölçüldü: 4 işçi, koridor dışı |
| `repair-slot-0…2` | gövde yaması / oturak-destek / kürek bağlantısı yuvaları | **kızağa göre yerel** ofsetler (dünya kopyası yok); değerler R01 mesh'i ile Mert + Mehmet seçer | her yuva en az bir çalışma noktasından göz–yuva ≤ 2,5 m olmalı; seçilince audit'e eklenip ölçülür |
| `repair-work-0…3` | dört çalışma noktası | (0.5, 8.4, −11.5), (1.0, 8.4, −14.25), (6.25, 8.4, −14.25), (6.5, 8.4, −11.0) | ölçüldü; yuva ofsetleri seçilince yuvaya en yakın hücrelerle yeniden seçilir |
| `repair-drop-0` | güvenli bırakma/emanet (Q, guest kopması, Closing) | (7.5, 8.4, −14.5) | ölçüldü: kara, su dışı, çakışma yok, koridora ≥ 1,31 m, duruş ızgarasında; dört çalışma noktasından, kızaktan ve biniş noktasına CC sürüşü temiz (aşağıdaki tablo) |

**`repair-drop-0` CC sürüşleri (S1, gerçek CC, kızak katı kutu)**

Her çalışma noktası → bırakma yolunda diğer üç işçi kapsülü yerinde durur. Kızak → bırakma yolunda işçi yoktur; başlangıç kızak merkezine en yakın duruş hücresidir (gövde kenarına 0,45 m). Bırakma → biniş yolunda bırakmaya giden işçi 0 dışındaki üç işçi yerinde durur.

| Yol | Başlangıç (x, z) | Sürülen yol · süre | Düz çizgi | Sonuç |
|---|---|---|---|---|
| `repair-work-0` → drop | (0.5, −11.5) | 11,8 m · 2,96 s | 7,6 m | ulaştı; düşme/sıkışma/engellenen kare yok |
| `repair-work-1` → drop | (1.0, −14.25) | 6,9 m · 1,72 s | 6,5 m | ulaştı |
| `repair-work-2` → drop | (6.25, −14.25) | 1,3 m · 0,32 s | 1,3 m | ulaştı |
| `repair-work-3` → drop | (6.5, −11.0) | 3,6 m · 0,90 s | 3,6 m | ulaştı |
| S1 kızak kenarı → drop | (3.5, −14.25) | 4,0 m · 1,00 s | 4,0 m | ulaştı |
| drop → `dock-town-1` biniş | (7.5, −14.5) | 11,0 m · 2,76 s | 10,9 m | ulaştı, (9, −3.75)'te biter |

- Hepsi yalnız yürüme (`walk`), su geçişi yok, yüzme 0 m.
- `repair-work-0` yolu kızağın batı ucunu dolanır (düz çizgiye göre +4,2 m).
- Bırakma noktasından iskele binişine yol kuru kalır ve iskele koridorunu kullanır; S1 işçileri bu yolu kapatmaz.

**Sandalın tamir sırasında durduğu yer — öneri: karada, S1 kızağında.**

- Kırık `boat-1` kızakta görünür. Son katkı mevcut otoritede tamamlanınca aynı `boat-1`, bugünkü gibi `route-near-1` wp0'da yüzer hâle gelir; kızaktaki kırık görünüm kaybolur.
- Böylece rota, dört koltuk, biniş noktası ve tek aktif araç olduğu gibi kalır. Kızak bir sunum pozudur; araç pozu değildir.
- Kızak boşken ne olacağı (kızak kalır mı, izi silinir mi) ve kızaktan suya geçişin görünümü Mert + Mehmet kararıdır.
- Kızak ve yuvalar son katkıdan sonra collider değiştirse bile iskele/rampa/wade koridorlarına dokunmaz (§3).

**Alternatif — S0'da kalmak (ölçülen bedel):**

- En çok 2 çalışan sığar.
- İskele ucu ve biniş noktası tamir boyunca kapanır.
- Koridor dışında bekleme alanı yok; bırakma yeri 4,4 m uzakta, DiveExit sahanlığında.
- Gövde suda olduğu için kıyıdan bakınca sandal yok.

Bu seçenekte PR #190'ın askıları en azından kara ve su dışı şartını karşılar; ancak iskele koridorunun içindedir.

**#156 ile aynı hedef:**

- Görev satırı (`BoatRepairGuide.Line`) bugün `BoatPartAnchor.WorldPosition`'ı gösteriyor. Parça edinilmemişken hedef `repair-part-start-i` olmalı.
- Edinilmiş ama tamamlanmamışken: taşınıyorsa `repair-slot-i`, bırakıldıysa `repair-drop-0`.
- Böylece görev ipucu ve dünya ipucu her durumda aynı anchor'ı gösterir.
- Kızak/bırakma işareti sunumu Mert'indir. Dekor parçası `IBoatPartPickupTarget` almaz.

**Gece/gündüz:**

- Su altındaki engine meydandan zaten görünmüyor; gece daha da kaybolur. (b) seçeneği veya bir su üstü işaret gerekebilir.
- S1 kızağı 16,8 m'de ≈ 50 px (meydan); gece okunurluğu için ışık/işaret Mert'in sunum işi. Gerçek gece görüntüsü ölçülmedi.

## 6. Açık kararlar, kapılar ve yapılmayanlar

**Açık kararlar**

- **R01 (Mehmet + Mert + Utku):** Üç grup adı ve engine kimliğinin "oturak/destek" olarak gösterilmesi.
- **R01/R02 (Mehmet):** Yuva sayısının üç olması, kızak mı yoksa yanaşma yeri mi seçileceği (S1 / S0), `repair-part-start-1` için (a) mı (b) mi.
- **Mehmet:** PR #190'daki askı ve duruş ofsetleri bu anchor'lara bağlanacak mı?
- **Mert:** Kızak/yuva/bırakma görseli, gece ipucu, kızaktan suya geçiş sunumu.

**Uygulama kapısı:** #141 (P4.6 teknik temel) ve #159 (P4.7 görsel örnek kabulü). Kimlikler ancak bunlardan ve ekip onayından sonra `WorldAnchorIds`/sahneye yazılır. Sahne sırası: Utku anchor → Mehmet etkileşim → Mert sunum → ortak build.

**Yapılmayanlar**

- Gerçek 1/2/4 süreç koşusu, aynı parça yarışı, geç katılma, ağ gecikmesi, build. Issue #188 madde 7 açık kalır.
- Sis, ışık, kontrast ölçümü ve gerçek video.
- Akıntı, palet seviyeleri.
- Gerçek kalabalık ve animasyon.
- Layout, sahne, prefab ve anchor eklemesi.

EditMode: baseline 1291/1291 → sonra 1296/1296 (+5 `P48RepairAreaBaselineTests`).
