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

## 6. Açık kararlar ve sonraki adımlar
- **Kullanıcı kararı:** stil yönü/palet onayı ve hangi kaynak ailesi (yukarıdaki öneri). R01/R02/R11 açık tasarım kararlarına dokunulmadı.
- Paket indirme onayı (dosya/kaynak/boyut) → içerik/ölçek denetimi → kit manifesti (kaynak, atıf, sürüm/tarih, dosya/.meta, ölçek/pivot, shader/malzeme, anchor, collider).
- Kapı geçilince: önce **bir** ev cephesi + **bir** ekipman dükkânı + kıyı + kırık sandal, Utku'nun yerleşimiyle, #151/#152 sırasına uyarak; gündüz/gece üç sabit kadraj önce/sonra, aynı build'de fiziksel yürüyüş ve kare süresi ölçümü.
- Bu hazırlık #154 veya #153'ün kapanışı değildir.
