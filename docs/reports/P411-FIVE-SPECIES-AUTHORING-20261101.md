# P4.11 / #218 / #155 — 15 mevcut ağ balığıyla beş türe geçiş hazırlığı

1 Kasım 2026. Bu PR sahnenin kendisini **henüz değiştirmez**; mevcut 15 in-scene NGO `NetworkObject` kimliğini koruyan, açıkça çalıştırılan bir Unity Editor authoring işlemi ekler.

## Hazırlanan süreç

- `Assets/DeepDive/World/Fish/P411FiveSpeciesRoster.cs` tam **15 balık = 5 tür × 3 birey** planını taşır. Kıyı: `sea_bass`, `tuna`; resif: `red_snapper`, `grouper`; derin: `lanternfish`.
- `Assets/DeepDive/World/Editor/SuppliedKitImport.cs` içindeki `BuildFiveSpeciesArt` üç ayrı ve gerçek animasyonlu kaynak için prefab üretir. **Humphead** geçici levrek silueti, mevcut **CoralGrouper** orfoz, **Anglerfish** fener balığı olarak kullanılır. Aynı renk kopyalarıyla beş tür taklit edilmez.
- `P411FiveSpeciesSceneSetup.Apply` varlıkları ve kimlikleri önceden doğrular; 15 eski sahne balığını aynı `NetworkObject` kimlikleriyle 3'er tür olarak yeniden yazar. Host AI, gerçek harpoon/kasa/satış ve respawn hatlarına dokunmaz; Unity'nin `GlobalObjectIdHash` kaydı yeniden save edilir.
- `PlayerActionView.FishResource` iki yeni türün gerçek kaynaklarını av/teslim kozmetiğine bağlayabilir; eski `sea_bass` görseli, yeni prefab üretilene kadar mevcut prefabda kalır.
- `P155FishHabitatTests` yalnızca tamamlanmış eski (5/5/5) veya yeni (3/3/3/3/3) dağılıma izin verir, yarım geçişi reddeder. İki ayrı salt-veri testi eklenmiştir.

## Çalıştırma ön koşulu

Quaternius **Animated Cute Fish Pack (Şubat 2020, CC0)** kaynağından `Humphead.fbx` ve `Anglerfish.fbx` dosyaları, var olan `CoralGrouper.fbx` yanına `Assets/DeepDive/Art/Supplied/Fish/` altına gerçek `.meta` dosyalarıyla eklenmelidir. Kaynak: https://quaternius.com/packs/cutefish.html . Bu iki dosya **GitHub dalına henüz eklenmedi**; Mert'in bilgisayarındaki indirme bu ortamda erişilebilir değildir. Eksikse `P411_FISH_SOURCE_MISSING` hatasıyla authoring başlamaz ve sahnede yarım değişiklik yapmaz.

Kaynaklar eklendikten sonra **Unity Editor > DeepDive > P4.11 > Author 5 animated fish species (15 NGO scene fish)** komutuyla işlem yapılabilir. Sonuç mutlaka `DiveTestArea.unity`, yeni prefabs, Unity .meta ve `docs/assets/CREDITS.md` değişiklikleriyle aynı PR'da commitlenmeli; ardından gerçek Windows build + 1/2/4 süreç, gece, habitat ve av → satış → kayıt → ansiklopedi testleri geçmelidir.

## Henüz teslim edilmeyenler

- FBX kaynakları, üç yeni prefab çıktısı, scene YAML üzerinde gerçek beş tür değişimi **mevcut PR'da yoktur**.
- Tuna sığ kıyıya geçince gerçek dalgıç yolu, balık kaçış hacmi ve NPC/kasa görüşü yeniden doğrulanmalıdır; konum dizisi ilk geometrik adaydır.
- Humphead levrek biyolojisiyle birebir aynı değildir; mevcut oyunun stylize sanat çizgisine göre görsel değerlendirme gerekir.
- Gün/gece ışığı, animasyon son kareleri ve av ölüm pozu; kayıt/ekonomi, 1/2/4 oyuncuda beş tür görünürlüğü ve performans ayrı doğrulanmalıdır.
- Nihai P4.12/#164 ve tarihsel #207 açık kalır.

Bu PR'ı ön hazırlık olarak merge etmek, beş tür oynanabilir demek değildir.
