# #249 — Girilebilir ekipman mağazası: izole prefab (7 Kasım 2026)

Durum: **Üretim paketi (prefab + ölçümler); kabul değildir.** Sahneye yerleştirilmedi, servis/raf/kasa bağlanmadı. Utku'nun yol/zemin blokajı ve şehir yerleşimi, Mehmet'in raf seçimi, sepet ve kasa bağlaması bu prefabın dışında kalır (#249 sırası: Utku zemin → Mert bina kabuğu ve kapı → Mehmet raf/kasa). Hiçbir kutu işaretlenmedi, PASS yazılmadı.

## Ne teslim edildi

- `Assets/DeepDive/Art/Resources/CityKit/EquipmentStore.prefab` (`Resources.Load("CityKit/EquipmentStore")`): iki katlı bir binanın **zemin katında girilebilir ekipman mağazası**. Üst kat kapalı (içi yok, gereği değil), pencereli ve çatılı; tabela "EKIPMAN".
- Üretici: `Assets/Editor/P410EquipmentStoreBuilder.cs` (`DeepDive > P4.10 > Build equipment store prefab` veya `-executeMethod DeepDive.EditorTools.P410EquipmentStoreBuilder.Build`); önizleme: `P410EquipmentStorePreview.cs`. Prefab sahnede yok, her şey koddan yeniden üretilebilir.
- `EquipmentStoreLayout` (`Assets/DeepDive/Composition/`): yalnız veri taşıyan bileşen; ağ nesnesi ve davranış yok. Diğer sahiplere noktaları söyler.

## Ölçüler ve çerçeve

Başlangıç noktası: ön duvarın dış çizgisinin zemin ortası; sokak −Z, dükkân gövdesi +Z yönünde 10 m genişlik × 8 m derinlik; kapı X = 0'da ortalı. Kat yüksekliği 3,12 m (kitin 2 m modülleri).

| Konu | Değer | Utku'nun 4 Kasım önerisi |
|---|---|---|
| Kapı açıklığı | **2,6 m** geniş × 2,6 m yüksek, kapı kanadı yok | ≥ 1,8 m, rahat 2,6 m |
| Eşik | **0,00 m** (zemin sokakla aynı seviye, basamak yok) | ≤ 0,30 m |
| Raf/kasa/masa önü koridoru | ≥ 1,8 m serbest daire; tezgâh ile masa arası 2,65 m | ≥ 1,8 m |
| Müşteri noktası | tezgâhın 0,95 m önünde, masadan 1,7 m | — |

Kapı açıklığını modül ızgarasından bağımsız kurmak için ön duvarın iki yanı düz sıva modülleriyle (sarmalayıcıda x yönünde 0,925 ölçekli) doldurulur; açıklığın üstünde 0,52 m'lik duvar bandı, tomruk lento ve iki söve vardır. Pencereli modüller ön cephede iki yanda.

## Noktalar (Mehmet ve Utku için)

`EquipmentStoreLayout` alanları ve `Anchors` çocukları: `Entrance` (kapı önü), `Threshold` (eşik çizgisi), `CustomerPoint` (kasada müşteri, tezgâha bakar), `Register` (kasa, tezgâh üstünde), `ShopkeeperPoint` (tezgâh arkası), `ExitPoint` (çıkan oyuncunun sokak tarafı). `Displays` her satılan ekipman kimliği için bir görünür sergi verir; çocukları `Product_<kimlik>` kopyalarıdır:

| Kimlik | Görünen ürün | Adet |
|---|---|---|
| `camera-basic` | gri kamera (masa + iki raf) | 2 + 4 + 3 |
| `camera-advanced` | mavi kamera (masa) | 2 |
| `camera-professional` | siyah-altın kamera (masa) | 2 |
| `tube-1` | sarı tüp (üç raf) | 6 + 5 + 5 |
| `tube-2` | turuncu uzun tüp (üç raf) | 5 + 4 |
| `fins-1` | mavi palet (üç raf) | 5 + 4 |
| `bag-1` | kahverengi çanta | 4 + 3 + 4 |
| `harpoon-1` | duvar rafında üç zıpkın | 3 |

Ürünler **yer tutucu procedural modellerdir** (küp/silindir birleşimleri, URP Lit), katalogdaki kimliklere göre ayırt edilir; gerçek ekipman modelleri ayrı iştir. Mağaza ürünleri gerçek satışa veya envantere bağlanmamıştır.

Yerleşim: arka duvarda iki raf ünitesi (üstte zıpkın rafı), sol duvarda iki raf, ortada (kapının solunda) üç kamera kademesi için kaide ve masa, sağ duvarda tezgâh ve kasa, tezgâh arkasında sandıklar, kapı içinde paspas, tavanda dört yumuşak nokta ışığı (gölgesiz).

## Çarpışma

Prefabın kendi `Collision` çocuğunda basit kutular: zemin (üst yüzü y = 0), ön duvar iki parça ve lento, yan/arka duvarlar, tezgâh, masa, raf üniteleri, sandıklar. Görsel parçalarda collider yok. Sokak ve dış zemin Utku'nundur; prefab ona bağlı değildir.

## Ölçüm (EditMode, `P410EquipmentStoreTests`, 7/7 geçti)

- Dört gerçek boyutlu kapsül (yarıçap 0,3 / boy 1,8) kapı düzleminde yan yana serbest; açıklığın hemen dışında duvara çarpar.
- Eşikte zemin üst yüzü 0,00 m, içeriyle aynı seviye.
- Her rafın, masanın iki yanının ve müşteri noktasının önünde 1,8 m çapında serbest daire.
- Gerçek `CharacterController` (yarıçap 0,3, boy 1,8, adım 0,30 m) sokaktan kapıdan geçip tezgâh önündeki müşteri noktasına, arka raflara ve geri kapıya yürüdü; zeminden ayrılmadı. Düz sokak levhası testte eklenir, prefabın parçası değildir.
- Bütçe: 352 renderer, ~41,8 bin üçgen, 4 ışık (gölgesiz). Ağ nesnesi yok, tek betik `EquipmentStoreLayout`.

## Önizleme (`evidence/p410-equipment-store/`, **editör görüntüsü; Windows oyuncusu değil**)

[Gündüz sayfası](evidence/p410-equipment-store/sheet-day.jpg): sokaktan, kapı yakını, kapıdan iç, tezgâh, arka raflar, üstten plan. [Gece sayfası](evidence/p410-equipment-store/sheet-night.jpg): yalnız mağazanın kendi ışıklarıyla; kapıdan sıcak ışık sızıyor, ürünler ve tabela seçiliyor. Işık düz ortam ışığı + tek güneşle, oyun sahnesinin gerçek atmosferi değil.

## Kaynak

Duvar, zemin, çatı, pencere, sandık ve malzemeler Quaternius Medieval Village MegaKit (CC0, `CREDITS.md`); raflar, tezgâh, masa, kasa, tabela ve ürünler depoda üretilen procedural parçalar. Yeni indirme, ücretli kaynak veya dış model yok.

## Denenmedi / açık

- **Sahne yerleşimi, sokak, zemin ve dünya çarpışması (Utku); şehir konumu ve ölçek kararı (Mehmet, Mert);** #172'deki mağaza kapısı önerisi (−2, −112) bu prefabla uygulanmadı.
- **Raf seçimi, elde sepet, kasada ödeme/ret/iptal ve tek ödeme otoritesi (Mehmet, #242/#247)**; ürünler hiçbir servise bağlı değil, esnaf (NPC) modeli ve kasada konuşma yok (yalnız `ShopkeeperPoint`).
- Windows oyuncusu görüntüleri, 2/4 süreç sunumu, performans, gerçek atmosfer/sis altında gece okunurluğu, ses (kapı çanı, zemin adımı) ve kullanıcının görsel hedef kabulü; #164 ve #162 açık.
- Üst katın içi, kapı kanatları, dış tezgâh/tabela ayrıntıları, mağaza içi esnaf animasyonu.
- Ürünler yer tutucu; gerçek ekipman modelleri ve fiyat etiketleri ayrı iş.
