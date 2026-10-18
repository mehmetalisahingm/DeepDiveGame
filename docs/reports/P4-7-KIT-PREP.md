# #171 P4.7-C: ev/dükkân/kıyı/sandal örnek kiti, hazırlık (taslak)

Tarih: 2026-10-16. Dal: `p4/mert-visual-inventory`. Bu belge yalnız **hazırlıktır** (#171 "Başlama sınırı"): envanter, ölçek, palet, parça listesi, kaynak adayı ve ses ihtiyacı. Yeni sanat oyun sahnesine **uygulanmadı**, hiçbir dış dosya indirilmedi veya repoya eklenmedi; uygulama #138/#140/#141 teknik temel kabulünden sonra başlar. Palet ve parça listesi kullanıcı/Utku/Mehmet onayını bekleyen taslaktır. Mevcut placeholder durumu: [P4-VISUAL-INVENTORY.md](P4-VISUAL-INVENTORY.md).

## 1. İnsan ölçeği (kitin her parçası bunlara göre)
Kaynak: repodaki ölçüm testleri (`P4PlayerScaleBaselineTests`, #178) ve mevcut dükkân yer tutucuları.
- Oyuncu kapsülü **1,8 m**, göz yüksekliği **~1,55 m** (kapsül boyunun %75–100'ü arasında), kapsül yarıçapı ~0,35 m.
- Mevcut yer tutucu dükkân: tezgâh 1,0 m, çatı ~2,7 m, tabela ~3,0 m, müşteri noktası tezgâhtardan 1,7 m önde (`TownStorefront`). Yeni kit bunları **değiştirebilir**; müşteri noktası ve `ServicePointAnchor` kimlikleri aynı kalır.
- Geçişler önerisi (Mehmet'in ölçüm sonucuyla doğrulanacak): kapı net genişlik ≥1,1 m / yükseklik ≥2,1 m, basamak ≤0,45 m (kapsül yükseltmesi), iskele tahtası ≥2 m (dört kişi geçişi #173).

## 2. Palet taslağı (tutarlı ahşap/taş/metal/su)
Mevcut düz renk malzemelerden (`Dev_*`, `Town_*`, `CoastalMaterials`) türetilmiş, doygunluğu azaltılmış bir kıyı paleti; hex değerleri **öneridir, onaylanmadı**.

| Rol | Önerilen | Kullanım |
|---|---|---|
| Ahşap açık / koyu | `#B98B55` / `#6B4A2B` | tezgâh, kapı, iskele tahtası / çerçeve, direk |
| Taş / duvar | `#C9C2B2` / `#8E8A80` | ev cephesi, zemin, temel |
| Kum / yol | `#D9BE85` | kıyı yolu, plaj (mevcut kum rengiyle uyumlu) |
| Metal / pas | `#8C969E` / `#9B5B3A` | tüp, cıvata, tabela çerçevesi / sandal pası |
| Su sığ / derin | `#5FA8B8` / `#1F5E70` | su malzemesi, kıyı köpüğü `#EAF4F2` |
| Çatı vurgusu (hizmet) | mavi `#3F6FB5`, yeşil `#3F9B5B`, turuncu `#D9822B`, mor `#8A56B8` | ekipman / balık / kayıt / tekne (mevcut hizmet renkleriyle aynı okuma) |
| Sıcak ışık (gece fener) | `#FFD08A` | fener, pencere |

## 3. Modüler parça listesi (örnek kit)
Her parça: ölçek 1 birim = 1 m, pivot taban merkezi, tek `Renderer` grubu + sadeleştirilmiş collider (yalnız yürünebilir/çarpışan yüzey; dekor collider'sız). Sahip: parça modeli/malzemesi Mert; yerleşim ve zemin/collider Utku; tutuş/kamera Mehmet.

| Paket | Parçalar | Bağlanacağı / not |
|---|---|---|
| Ev cephesi | duvar bloğu (2 m modül), köşe, kapı çerçevesi + kapı, pencere, çatı (iki eğim + köşe), basamak, sundurma/verandası, tabela panosu | `HomeSceneSetup` anchor'ları (`home-door`, yataklar, PC, depo); Utku #138 anchor'larını değiştirmez |
| Ekipman dükkânı | cephe (tezgâhlı), raf, tente, tabela, tüp/kamera sergi propları, müşteri noktası işareti | `shop-equipment` `ServicePointAnchor`; `TownStorefront` yer tutucusunun yerine (altında `Storefront` çocuğu bırakılırsa yer tutucu kendini çeker) |
| Kıyı | taş/kum yol parçaları, kenar taşı, kaya (3 boy), ahşap korkuluk/bağlama direği, iskele tahtası + kazık, şamandıra | Utku zemin/collider, su–kara rampası aynı koordinatta |
| Kırık sandal | gövde (kırık/onarılmış iki durum), üç montaj noktası (gövde/motor/yakıt tankı, mevcut `boat-part-*` kimlikleri), kürek, halat | #160 fiziksel tamir state'i Mehmet/ben; bu kit yalnız **görünüm**; örnek tahta/alet taşıma/tamir sistemi kazanmaz |
| Eller/nesne | mevcut kamera/zıpkın tutuşu korunur; örnek nesne için tutuş noktası ihtiyacı listesi | Mehmet |

## 4. Kaynak adayları ve lisans doğrulaması
Hiçbiri indirilmedi. Ücretli satın alma yoktur. Doğrulanan, sayfa düzeyindedir; **içeriğin amaca uygunluğu paket indirilip açılınca** denetlenir (sayfalar tek tek model listesi vermiyor).

| Aday | Doğrulanan | Doğrulanmayan |
|---|---|---|
| [Quaternius Pirate Kit](https://quaternius.com/packs/piratekit.html) | Lisans **CC0**, 71 model, dokulu, animasyonlu, FBX/OBJ/Blend/glTF, kişisel/ticari serbest | Hangi modeller var (gemi/iskele/sandal/bina?), ölçek/pivot |
| [Kenney Pirate Kit](https://kenney.nl/assets/pirate-kit) (v2.1) | Lisans **CC0** (sayfa); Kenney destek sayfası: tüm oyun varlıkları CC0, atıf zorunlu değil, ticari serbest | Dosya biçimleri ve model listesi sayfada yok |
| Kenney Fantasy Town Kit, Nature Kit | OpenGameArt aynası CC0 diyor (160+ / 330+ nesne, FBX/OBJ/glTF) | Kenney'in kendi sayfası bu oturumda okunmadı; ayna sayfasına güvenilmeden kenney.nl'den doğrulanmalı |
| Quaternius Simple Buildings / Buildings / Medieval Village (MegaKit) / Stylized Nature MegaKit / Ships Pack | Paket adları [resmî dizinde](https://quaternius.com/index.html) listeli | Paket başına lisans sayfada tek tek doğrulanmadı |
| Mevcut: Quaternius Ultimate Modular Characters | Lisans **CC0 1.0** (repoda `LICENSE-Quaternius.txt`), `CREDITS.md`'de kayıtlı | Aynı stil ailesi: yeni kit için tutarlılık avantajı |

Öneri (karar kullanıcıda): karakter zaten Quaternius olduğundan **Quaternius ailesinden** (Pirate Kit + bina/doğa paketleri) başlamak stil tutarlılığı verir; Kenney kitleri yedek/tamamlayıcı. Seçilen her paket indirme öncesi dosya adı/kaynak/boyutla onaylanır, sonra `docs/assets/CREDITS.md`'ye kaynak/lisans/tarih/dosya yoluyla işlenir ve `.meta` dosyalarıyla repoya alınır.

## 5. Ses ihtiyacı (seçim hazırlığı)
İhtiyaç: taş/ahşap/kum adımı, iskele tahtası, kapı/tezgâh, dalga ve kıyı ambiyansı, rüzgâr, gece cırcır/dalga, tekne gövdesi (cıvata/ahşap), mevcut `PickupConfirm`/`HitConfirmSplash` ile uyum. Mevcut kaynak: Mixkit (kayıtlı, atıf zorunlu değil). Ek aday kaynaklar (Kenney ses paketleri, Mixkit kategorileri) **henüz lisans/içerik açısından doğrulanmadı**. Bağlama listesi: yüzey adımı → Mehmet (oyuncu/zone), dalga/ambiyans → Utku (zone), UI/dükkân sesi → Mert.

## 5b. Quaternius Pirate Kit incelemesi (izole Unity projesi, 16 Ekim)
İndirilen: resmî Drive klasöründen **FBX klasörü**, 71 dosya (10,8 MB sıkıştırılmış, ~30 MB açık), CC0 (paket sayfası). Oyun reposuna **eklenmedi**; ayrı bir boş Unity 6000.3.23f1 projesine (repo dışı) varsayılan ayarlarla içe aktarıldı. Ölçümler: [piratekit-measurements.tsv](evidence/p47/piratekit-measurements.tsv); kontak sayfaları (kırmızı kutu = 1,8 m insan, ikisi de y=0 pivot düzleminde; ilk geçiş dokusuz gri): [çevre](media/p47-piratekit-environment.png), [propler](media/p47-piratekit-props.png), [gemiler](media/p47-piratekit-ships.png).

**Doku (atlas):** `Atlas_Pirate.png` Drive kotası yüzünden otomatik inmedi; kullanıcı elle indirdi (1024×1024 RGB PNG, 9014 bayt, geçerli). Atlas izole projede (Nokta filtre, mipmap kapalı) tüm modellere uygulandı: **dokulu görünüm** [çevre](media/p47-piratekit-tex-environment.png), [propler](media/p47-piratekit-tex-props.png), [gemiler](media/p47-piratekit-tex-ships.png). Atlas yaklaşık 66 düz renk karedir (beyaz zemin üstünde); öne çıkanlar: ahşap `#885E38` / `#C09A77` / `#57412C`, taş/gri `#83775E` `#A79DA4` `#6E7683`, altın `#D6A74E`, kırmızı `#8B1E1F`, açık mavi `#92B2D4`. Görünüm: sıcak, orta doygunlukta, cartoon-low-poly. Ahşap/taş tonları §2'deki palet önerisine (`#B98B55`/`#6B4A2B`, `#C9C2B2`/`#8E8A80`) yakın; **palet bu atlasa göre ince ayarlanabilir**, ama yeşil palmiye yaprağı ve kırmızı yelken gibi doygun renkler kitin kendi tarzı. Atlas dosyası repoya **eklenmedi** (içe aktarma ve `CREDITS.md` kaydı onay bekler).

Ölçülen (Unity birimi = 1 m, `useFileScale`, ölçek 1,0 — yeniden ölçekleme gerekmedi):
| Model | Boyut (x × y × z, m) | Pivot / not |
|---|---|---|
| `Environment_Dock` / `Dock_Broken` | 2,18 × 3,01 × 2,66 / 2,38 × 3,01 × 2,66 | Pivot kutunun ~2 m üstünde (altı −2,0, üstü +1,0): su çizgisinde duran kazıklı iskele modülü; kırık varyant da var |
| `Environment_Dock_Pole` | 0,37 × 3,01 × 0,38 | aynı pivot kuralı |
| `Environment_House1/2/3` | 13,5 × 6,8 × 4,6 / 3,8 × 9,4 × 3,7 / 8,5 × 10,6 × 3,8 | Pivot tabanda; **tam binalar** (12–15 bin köşe), modüler cephe parçası değil |
| `Ship_Small` / `Ship_Large` | 5,15 × 2,0 × 3,6 / 15,4 × 11,5 × 5,3 | `Ship_Small` pivot ≈ taban; tek mesh, **kırık varyantı yok** |
| `Environment_Rock_1..5` | 0,6–0,8 m | küçük kaya, kıyı dolgusu için uygun; `Cliff1..4` 5–8 m |
| `Environment_PalmTree_1..3` | 2,6–2,9 × 2,7–3,6 | küçük palmiye |
| `Environment_Sawmill` | 2,1 × 0,6 × 2,1 | kütük yığını (bina değil) |
| `Prop_Barrel/Bucket/Bucket_Fishes/Chest/Anchor…` | 0,7 / 0,7 / 0,9×1,2 / 1,4×0,9 / 0,8×0,95 | pivot tabanda, abartılı "sevimli" ölçek (ör. `Prop_Fish_Tuna` 2,1 m) |
| `Weapon_Axe` | 0,45 × 1,05 × 0,12 | keser mevcut (çivi/tahta yok) |

**İhtiyaçlarla uyum (dürüst değerlendirme):**
- **Kıyı:** iyi uyum: iskele modülü (kırık dahil), kazık, kaya, palmiye, uçurum, fıçı/kova/balık sandığı. Düşük poligon, tek dokulu paletli stil; mevcut Quaternius karakteriyle aynı aile.
- **Ev cephesi:** zayıf uyum: üç ev de büyük korsan-tema **tam bina**; mütevazı bir balıkçı evi veya kapı/pencere/çatı **modüler parçaları yok**. Cephe için başka paket (Simple Buildings / Medieval Village MegaKit; lisansları henüz doğrulanmadı) ya da bu evlerin arka plan olarak kullanımı gerekir.
- **Ekipman dükkânı:** tezgâh/raf/tabela yok; yalnız sergi propları (fıçı, kova, şişe, sandık, çapa) var. Tezgâhı/rafı biz yapmalıyız (palet uyumuyla).
- **Kırık sandal:** `Ship_Small` (5 m) dört kişilik ölçek için kabul edilebilir ama **kırık/parçalı değil**; üç montaj noktasını ve kırık görünümü ayırmak için kitin `Blends` klasöründeki kaynak dosya Blender'da düzenlenmeli (CC0 türev serbest) ya da `Dock_Broken` dili referans alınarak parça parça yapılmalı. `Blends` indirilmedi.
- **Ölçek sorunu:** modeller stilize/abartılı; 1,8 m insanla yan yana okunurluk iyi, ama ev/gemi ölçüleri oyuncu kapsülüne göre ayrıca sınanmalı (#173).

## 5c. İkinci tur: üç paket daha, izole inceleme (18 Ekim)
Kullanıcı kararıyla ev/dükkân/sandal için ikinci bir Quaternius paketi arandı. Üçü de repo **dışına** indirildi (kullanıcı onayıyla), §5b'deki izole Unity projesine **özgün malzemeleriyle** içe aktarıldı (ölçekleme yok). Repoya model/doku **eklenmedi**; yalnız ölçüm tablosu ve kontak sayfaları (kırmızı kutu = 1,8 m insan) eklendi. Lisans ve içerik denetimi:

| Paket | Kaynak / lisans | Boyut | Sonuç |
|---|---|---|---|
| Ultimate Textured Building Pack (Dec 2019) | [OpenGameArt](https://opengameart.org/content/lowpoly-buildings-pack), CC0; resmî `quaternius.com/packs/ultimatebuildings.html` 404 verdi, **resmî sayfayla doğrulanamadı** | 30 MB, 102 model | **Elendi**: modern şehir kiti (klima, boru, kumarhane tabelası, 4–6 katlı apartmanlar). Yalnız adından incelendi, Unity'de ölçülmedi. İleride gable çatılı 1–2 katlı gövde ve `Sign_Hardware` aday olabilir |
| [Medieval Village MegaKit, Standard](https://quaternius.com/packs/medievalvillagemegakit.html) | CC0 (paket içi `License_Standard.txt`; [OpenGameArt aynası](https://opengameart.org/node/174568) kullanıldı), ücretsiz katman | zip 100 MB, 176 model, 1024² PBR dokular ~37 MB | **Ev cephesi için en iyi aday** (aşağıda) |
| [LowPoly Ship Pack](https://opengameart.org/node/82831) | CC0 (`License.txt`) | 3,9 MB, 6 model | `Boat` sandal adayı; diğerleri uygun değil |

Pro/Source katmanları ücretli; **indirilmedi**.

**MegaKit ölçümleri** ([tablo](evidence/p47/medieval-megakit-measurements.tsv); kontak sayfaları: [duvar](media/p47-megakit-wall.png), [çatı](media/p47-megakit-roof.png), [kapı](media/p47-megakit-door.png), [pencere](media/p47-megakit-window.png), [prop](media/p47-megakit-prop.png)):
- Ölçek 1 birim = 1 m, yeniden ölçek gerekmedi; modül 2 m genişlik. Duvar 2,00 × 3,12 × 0,41 m (sıvalı/düzensiz tuğla; düz, kapılı, pencereli, kemerli varyantlar), kapı kanadı 1,12 × 2,10 m, zemin 2 × 2 m, dış merdiven 2 m genişlik/1,2 m yükseklik, kiremit çatı 4×4…8×14 boyları (ör. 6×6: 8,25 × 5,67 × 8,03 m), ahşap çatı 2×1 modülleri, baca, sandık (1,08 m), el arabası, ahşap çit.
- Parçalar modüler ve düşük poligon: ortalama ~670 üçgen, en ağırı `Roof_RoundTiles_8x12` 6356; duvar/zemin parçaları 30–200 üçgen. Pivotların çoğu tabanda; çatılar ve pencere kasaları kayık (ör. `Roof_RoundTiles_6x6` alt −0,78 m, `Window_Wide_Flat1` alt +0,94 m): modül bazında pivot düzeltmesi/prefab sarmalayıcı gerekir.
- **Dükkân için yok:** tabela, tezgâh, raf. Kapı, kepenk, sandık, el arabası, çit ve çatı var; tezgâh/raf/tabelayı kendimiz yapmalıyız.
- **Stil riski:** Pirate Kit düz renkli/yumuşak cartoon; MegaKit PBR dokulu (sıvadaki leke, kiremit, ahşap damarı), daha gerçekçi. Aynı sahnede yan yana durunca uyumsuzluk görülebilir: ya MegaKit dokuları düz renge/atlas paletine indirgenmeli ya da kıyı parçaları (iskele, kaya) bu stille uyumlandırılmalı. Palet kararı kullanıcıda.
- **Teknik not:** 8/176 modelin dokusu yok; saydam malzemeler (sarmaşık, metal çit, pencere camı) izole projede Standard gölgelendiriciyle beyaz/delik göründü; oyunda kesim/saydam malzeme kurulumu gerekir. Dokular 1024², normal haritaları Unity için ayrı klasörde. Oyundaki render hattıyla (URP/Built-in) malzeme uyumu henüz sınanmadı.

**Ship Pack ölçümleri** ([tablo](evidence/p47/ships-pack-measurements.tsv); [Boat](media/p47-ships-boat.png), [Lifeboat](media/p47-ships-lifeboat.png), [Viking](media/p47-ships-viking.png)):
- `Boat`: 0,36 × 0,16 × 0,67 m ham ölçü (300 köşe), koyu kahverengi küçük kürek tekne; dört kişilik için yaklaşık ×5 büyütme gerekir ve stil olarak "kırık sandal" için kabul edilebilir. Kırık/parçalı değil; üç montaj noktası için düzenleme gerekir.
- `Lifeboat`: kırmızı-beyaz modern şişme bot (uyumsuz). `Viking boat` (1,57 × 2,51 × 4,20 m) yelkenli uzun tekne: sandal için fazla; farklı bir teknenin (P4.3) adayı olabilir. `Sail ship`/`CruiseShip` çok büyük/modern.

**Özet öneri (karar kullanıcıda):** ev cephesi için MegaKit (dükkân için ek tezgâh/raf/tabela üretimi), kıyı için Pirate Kit, sandal için ya `Boat`'ı büyütüp parçalamak ya da Pirate `Ship_Small`'ı Blender'da bölmek. Stil birliği için palet/doku kararı (düz renk mi dokulu mu) önce verilmeli. Repoya içe aktarma ve `CREDITS.md` kaydı hâlâ uygulama kapısına ve onaya bağlı.

## 5d. Stil kararı (18 Ekim): MegaKit dokulu stil, palet güncellemesi
**Karar (kullanıcı, 18 Ekim):** ev cephesi ve dükkân Medieval Village MegaKit'in dokulu stilinde; kıyı Pirate Kit'ten, sandal §5e. Bu §2'deki hex önerilerini aşağıdaki **MegaKit ölçülmüş ortalama renkleriyle** değiştirir (dokuların alfa>200 piksellerinden 1024² örneklemeyle ortalama; gerçek oyun ışığında kayar, taslaktır, onay bekler):

| Rol | MegaKit dokusu | Ortalama renk | Not |
|---|---|---|---|
| Duvar sıva | `T_Plaster_BaseColor` | `#AE9E7F` | §2 "taş/duvar açık" `#C9C2B2`'den daha sıcak/koyu |
| Duvar düzensiz tuğla | `T_UnevenBrick_BaseColor` | `#877866` | temel/taban |
| Kırmızı tuğla | `T_RedBrick_BaseColor` | `#B77343` | baca/zemin vurgusu |
| Kiremit çatı | `T_RoundTiles_BaseColor` | `#B25330` | §2 hizmet renkleriyle (mavi/yeşil/turuncu/mor) çakışmasın diye hizmet vurgusu **tabelada/tentede**, çatıda değil |
| Ahşap | `T_WoodTrim_BaseColor` | `#8A6749` | §2 `#B98B55`/`#6B4A2B` aralığında; tezgâh/raf/tabela bu dokuyla |
| Taş kenar | `T_RockTrim_BaseColor` | `#90877B` | temel taşı, kıyı kenarı |

Pirate Kit kıyı parçaları (iskele ahşabı `#885E38`/`#C09A77`/`#57412C`, taş `#83775E`/`#A79DA4`) MegaKit ahşabıyla (`#8A6749`) sıcaklıkça yakın; **ana risk doku farkı** (Pirate düz renk, MegaKit leke/damar). Önerilen yaklaşım: kıyı parçalarını MegaKit ahşap/taş dokusuyla yeniden malzemelemek (UV atlas olduğundan doğrudan değil, malzeme değişimiyle deneyerek) ya da kıyıda yalnız MegaKit ahşap/taş parçalarını kullanmak; ikisi de #154 kapısında önce/sonra kadrajla sınanır. Mevcut ışık/URP gölgelendirici uyumu sınanmadı.

Dükkân üretimi (MegaKit'te yok): tezgâh, raf, tabela panosu, tente: ahşap dokuyla, 2 m modül ve 1,0 m tezgâh yüksekliğiyle (§1) kurulur.

## 5e. #189 için sanat hazırlığı (yalnız envanter, uygulama yok)
Kaynak: repodaki `Assets/DeepDive/Art/Supplied/Fish/Boat.fbx` ve `Resources/SuppliedKit/Rowboat.prefab` (#185 kullanıcı teslimi; Git LFS işaretçisi, `git lfs smudge` ile açıldı), izole projede içe aktarılıp ölçüldü. #189 R01/R02/R11 kararı Mert onayıyla [#186 yorumu](https://github.com/mehmetalisahingm/DeepDiveGame/issues/186#issuecomment-6250071136) altında ilerler; Utku/Mehmet kabulü ayrı.

**Mevcut tekne:** `Boat`: 4,67 × 2,01 × 9,01 m ham ölçü (376 köşe, 224 üçgen), **tek mesh, 2 alt mesh/2 malzeme, dokusuz** (koyu kahverengi düz malzeme), pivot tabana yakın (−0,03 m). Yani bugünkü gövdede ayrı tahta, oturak veya kürek yok; kontak sayfası: [sandal](media/p47-sandal-boat.png). Ölçüm: [tablo](evidence/p47/sandal-boat-measurements.tsv).

**Sonuç:** sekiz tamamlanma kombinasyonunu göstermek için mevcut meshin bölünmesi gerekmez; en düşük riskli yol **tek gövde + üç ayrı sunum parçası** (gövde yaması, oturak/destek, kürek bağlantısı) ve kırık görünüm için ayrı bir "hasar" katmanı:
- Sağlam gövde = mevcut `Boat` (kimlik/`boat-1` aynı kalır, #185 modeli yeniden kullanılır).
- Üç parça birbirinden bağımsız açılıp kapanan alt nesne: tamamlandıkça görünür/tamamlanmamışken boşluk veya kırık kenar görünür. Her parça kendi malzemesi ve kendi pivotuyla (yuva: Utku'nun #188 anchor'ı, kimlik `boat-part-hull|engine|fuel-tank`; görünür adlar R01: gövde yaması / oturak-destek / kürek bağlantısı).
- Kırık durumda gövdenin kendisi için iki yol (karar #189'da): (a) üç parçanın **eksik** hali delik/boşluk olarak gövde meshine bakılarak ayrı "hasar" meshiyle (kırık tahta kenarı) kapatılır; (b) Blender'da gövde parçalara bölünür. (a) daha ucuz ve R02'nin "tek sabit görünür parça" kararına uygun.
- Sekiz kombinasyon: bit maskesi 0..7 doğrudan görünür kombinasyona çevrilir (mevcut `BoatPartPresentation` kalıbı: `BoatPartsMask` bit → görünürlük). Test: sekiz maskenin her biri için beklenen görünür parça kümesi (EditMode) + gündüz/gece görüntüsü.
- **Yapılmayacak (R01):** motor/yakıt tankı görünümü sandala eklenmez; Tripo motorbot ayrı araç olarak kalır.

**Alet/ses/ölçek envanteri (taslak, henüz yapılmadı):** keser (Pirate Kit `Weapon_Axe` 0,45 × 1,05 × 0,12 m adayı, atlas dokusu; MegaKit ile stil uyumu sınanmalı), çivi/bağlantı parçası ve tahta için MegaKit `Prop_*`/ahşap duvar dokusuyla küçük parçalar (kaldırma, bırakma, yerleştirme, darbe sesi ayrı; ses kaynağı lisansı doğrulanmadan seçilmez). Taşınan tahta ölçeği: 1,8 m insana göre 1,5–2 m uzunluk, el tutuş noktası Mehmet #187'nin elinde. **Mehmet'in #190 prototipi** (geçici tahta/çivi/keser/ses) nihai sanat değildir ve bu işle değiştirilir.

## 6. Açık kararlar ve sonraki adımlar
- **Karar verildi (18 Ekim):** MegaKit dokulu stil (§5d). Palet hex değerleri taslak, Utku/Mehmet görüşü bekler. R01/R02/R11 için Mert onayı #186 yorumunda; Utku/Mehmet kabulü ayrı.
- Paket indirme onayı (dosya/kaynak/boyut) → içerik/ölçek denetimi → kit manifesti (kaynak, atıf, sürüm/tarih, dosya/.meta, ölçek/pivot, shader/malzeme, anchor, collider).
- Kapı geçilince: önce **bir** ev cephesi + **bir** ekipman dükkânı + kıyı + kırık sandal, Utku'nun yerleşimiyle, #151/#152 sırasına uyarak; gündüz/gece üç sabit kadraj önce/sonra, aynı build'de fiziksel yürüyüş ve kare süresi ölçümü.
- Bu hazırlık #154 veya #153'ün kapanışı değildir.
