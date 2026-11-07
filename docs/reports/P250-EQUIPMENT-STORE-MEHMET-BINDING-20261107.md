# Mehmet / #250, #246 — gerçek mağazanın raf–sepet–kasa bağlanması

Bu dilim Mert'in birleşmiş #274 `Resources/CityKit/EquipmentStore` prefabını hiçbir sahneye otomatik yerleştirmeden, **var olan** `ServicePointAnchor` / `ShopShelfAnchor` / `SelectedShopItem` / `ShopCarryView` / `TownServiceHandler` sözleşmesine bağlar. Oyuna ikinci ekonomi, fiyat, RPC, kayıt veya sahte ödeme yazmaz.

## Bağlantı

- Utku mağaza prefabını gerçek `EquipmentShop` servis anchor'ının altına yerleştirdiğinde `TownStorefront.Build` bu prefabı bulur: yerine placeholder kabuk/raf oluşturmadan `EquipmentStoreShopBinding.Bind` çağırır. Runtime'da daha sonra child olarak oluşturulan mağazanın `EquipmentStoreLayout.Start` metodu da aynı bağlamayı yapar.
- Prefabın `Displays` alanlarındaki her `Product_` öğesine, mevcut ürün kimliği ve servis anchor'ı ile `ShopShelfAnchor` + yalnız tetikleyici (walk collider değil) eklenir. Eski grafikler/çeşitler kalır; yinelenen raf üretilmez.
- `Collision/Counter` üzerindeki **mevcut gerçek** collider, `EquipmentStoreCashierTarget` alır. Oyuncunun F ray'i kasa tezgâhına isabet edince mevcut `ServiceInteractionAuthority` çalışır; `ShopShelfPresentation` aynı ödemeyi ve iptali gösterir. `TownServiceBinding` artık yalnız yeni prefab varsa ödeme sırasında host tarafında **gerçek `CustomerPoint`** yakınlığını sınar.
- Yeni 10×8 m binada yürürken seçilen ürün artık eski 5 m sokak anchor menzili yüzünden düşmez; mağaza içi sınırı korunur. Sokakta uzaklaşınca silinir. Üst raftaki zıpkına bakarak seçimde feet Y farkı dar 2,5 m küreye takılmaz; yatay 2,5 m + en fazla 3,2 m dikey sınır kullanılır. Gerçek görüş ray'i, katı engel ve host yetkisi hâlâ zorunlu.

## Test ve sınırlar

`P250EquipmentStoreInteractionTests`: #274 gerçek prefabı üstünde seçilebilir trigger sayısı, yedi değil sekiz katalog kimliği, ağ/solid collider yokluğu, kasanın aynı servis noktası oluşu, uzak satın alma reddi, arka raf→kasa sepet geçerliliği ve yüksek zıpkına erişim. `ShopShelfTests`/CI geriye uyumluluk kontrolüdür. Bu PR hazırlanırken Unity/Windows süreçleri yerelde çalıştırılmadı.

**Bloke nihai maddeler:** Utku'nun gerçek şehir zemin/yol/kapı ve prefab world yerleşimi (#172/#153); Mert'in gerçek modelleri/aydınlatması/NPC'si (#249); gerçek Windows 2/4 süreçte kapı→raf→kasa→çıkış, host reload, ayrı PC görsel kabul (#250/#164). Bunlar olmadan #250/249/164 tamamlanmış sayılmaz.
