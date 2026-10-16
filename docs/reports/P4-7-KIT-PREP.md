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
İndirilen: resmî Drive klasöründen **FBX klasörü**, 71 dosya (10,8 MB sıkıştırılmış, ~30 MB açık), CC0 (paket sayfası). Oyun reposuna **eklenmedi**; ayrı bir boş Unity 6000.3.23f1 projesine (repo dışı) varsayılan ayarlarla içe aktarıldı. Ölçümler: [piratekit-measurements.tsv](evidence/p47/piratekit-measurements.tsv); kontak sayfaları (gri malzeme, kırmızı kutu = 1,8 m insan, ikisi de y=0 pivot düzleminde): [çevre](media/p47-piratekit-environment.png), [propler](media/p47-piratekit-props.png), [gemiler](media/p47-piratekit-ships.png).

**Eksik: `Atlas_Pirate.png`.** Kitin tek dokusu (9 KB düz renk paleti) Drive "kota aşıldı" hatası yüzünden inmedi; modeller dokusuz gri görünüyor, gerçek renkler henüz görülmedi. Atlas gelmeden malzeme/palet uyumu kararı verilemez.

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

## 6. Açık kararlar ve sonraki adımlar
- **Kullanıcı kararı:** stil yönü/palet onayı ve hangi kaynak ailesi (yukarıdaki öneri). R01/R02/R11 açık tasarım kararlarına dokunulmadı.
- Paket indirme onayı (dosya/kaynak/boyut) → içerik/ölçek denetimi → kit manifesti (kaynak, atıf, sürüm/tarih, dosya/.meta, ölçek/pivot, shader/malzeme, anchor, collider).
- Kapı geçilince: önce **bir** ev cephesi + **bir** ekipman dükkânı + kıyı + kırık sandal, Utku'nun yerleşimiyle, #151/#152 sırasına uyarak; gündüz/gece üç sabit kadraj önce/sonra, aynı build'de fiziksel yürüyüş ve kare süresi ölçümü.
- Bu hazırlık #154 veya #153'ün kapanışı değildir.
