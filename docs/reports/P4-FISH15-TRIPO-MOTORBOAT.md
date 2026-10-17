# 15 kıyı balığı ve kullanıcı Tripo motorbotu

18 Ekim 2026. Kullanıcının açık yeni talebi iki dar içerik işini faz sırasından öne alır; P4.6/nihai görsel/ayrı bilgisayar kabulünü kapatmaz.

## Oyun değişikliği

- `DiveTestArea` artık **15 ayrı levrek** barındırır. Mevcut ilk balık korunur; 14 hayvan Unity Editor ile yazılır, ayrı NGO sahne hash'i alır. Hepsi mevcut host yüzme/kaçma/sağlık, zıpkın, av toplama, envanter, satış ve çekim yolundadır. Yeni tür veya test stok sağlayıcısı eklenmez. Yerleşim sığ su içindedir; bütün wander hacimleri sınırlar içinde kalır.
- Sonraki dalış mevcut `FishDiveLifecycleBinding` ile tüm nüfusu yeniler. Aynı dalışta toplanan balık hemen geri gelmez. Hayatta kalan gece balığının aynı karede despawn/spawn sırasında `Light` silme/yeniden ekleme hatası düzeltildi: ışık bileşeni saklanır, despawn'da kapatılır, host gece durumunu yeniden belirler.
- Kullanıcı motorbotu `boat-2` görünümünde: dokulu FBX, URP renk/normal materyali, ölçek/pivot uyarlaması. Fiziksel sınır 3,2 × 7 m; model yaklaşık **3,20 × 6,26 m**, yükseklik 2,29 m. Başlangıç sandalı ve araştırma teknesi ayrı kalır. Sabit dört koltuk, sahiplik, satın alma, save, binme/hareket/rota otoriteleri değişmez; görsel collider veya ikinci NetworkObject eklemez.
- Yeni sahne nesneleri nedeniyle ağ protokolü **DeepDive-P4-3**. Birlikte oynayan herkes aynı yeni build'i kullanmalıdır.
- İsteğe bağlı geliştirme testinde tekne genel çekimi ayrı `room-boat-overview.png` dosyasına yazılır; oyuncu kamerası çekimleri korunur, render sonrası kamera geri alınır. Normal oyunda test sürücüsü kurulmaz.

## Kaynak ve inceleme sınırı

Arşiv `luxury+motorboat+3d+model.zip`, SHA-256 `1A445AD12BC7B317C602A5DA19E0F45D7099171B9150C3762ECCFA37A0ABA65`. FBX ve beş doku kullanıcı tarafından sağlandı. Kullanıcı kendi Tripo üretimi olduğunu ve public GitHub'a ekleme iznini açıkça bildirdi. Hesap planı/üretim tarihi arşivde yoktur; kayıt [CREDITS](../assets/CREDITS.md) içindedir. Kaynak doku setinin tamamı korunur; mevcut URP materyali renk ve normal dokusunu kullanır, metaliklik/pürüzlülük çalışma değeridir. Yeni servis çağrısı veya satın alma yoktur.

Dünya/canlı/sahne kısmı Utku, görsel kaynak/tutarlılık kısmı Mert incelemesi gerektirir. Bu teslim onların onayı veya faz kapanışı değildir. İlk sandalın fiziksel tahta/keser/çivi tamiri, beş tür, kasaba grafikleri ve genel sanat/performans kabulü bu işte tamamlandı sayılmaz.

Sahne blokları dosya kimliğiyle karşılaştırıldı: eski 310 bloğun yalnız kök listesine yeni balıklar eklendi; mevcut diğer bütün sahne nesnelerinin verisi aynı kaldı. Unity yeniden sıralaması diff'i büyütür, elle NGO hash'i uydurulmadı.

## Doğrulama

- Tam EditMode: **1272/1272 PASS**, `Logs/fish15-final-editmode.xml`. Tüm balıklarda ayrı ve sıfır olmayan NGO hash'i, av/çekim bileşenleri, host transform otoritesi ve su içi wander hacmi kontrol edilir. Yaşayan gece balığı üç anlık yeniden kullanımda tek ışığı güvenle korur. Motorbot prefabı, doku/URP, gövde değiştirme ve collider sınırları kontrol edilir.
- Windows build: ilk içerik build'i PASS; yaşam döngüsü düzeltmesini içeren son build ve uzun koşu sonucu aşağıda güncellenir.
- Gerçek iki süreçli av: **host + guest PASS**, `Logs/P1-integrated-20261018-000959-380-2-26577`. Her iki süreç 15 canlı balık görür; misafirin gerçek zıpkın/E girdisiyle bir av toplanır ve çantaya yansır; diğer 14 hayvan canlı kalır.
- Gerçek iki süreçli Tripo motorbot yolculuğu: **host + guest PASS**, `Logs/P1-integrated-20261018-000959-378-2-26677`; doğru gövde, gerçek binme/gidiş/demirleme/inme/yeniden binme/dönüş ve save kontrolü. Bu dar rota denemesi daha önce kazanılmış filo kampanyasının kopyasını kullanır; sıfır bakiyeden ürün kabulü olarak sunulmaz.
- İlk dört süreçli uzun koşu ikinci dalışa geçişte `FishActor.OnNetworkSpawn` gece ışığı hatasını yakaladı; PASS sayılmadı. Hata düzeltildi, regresyon testi eklendi. Son koşu ve diskten açma sonucu bekleniyor.

Görüntüler gerçek Windows build'inden; genel tekne çekimi yalnız gözlem kamerası açısıdır. Yapay görsel veya hedef tasarım resmi değildir.

## Yeniden çalıştırma

```powershell
# Birlikte oynayan herkes aynı build/protokolü alır.
./tools/Build-P1.ps1 -Integrated
./tools/Test-P1-Integrated.ps1 -Players 2 -Hunt -Capture
# Tekne sahibi bir kampanyanın KOPYASI ile:
./tools/Test-P1-Integrated.ps1 -Players 2 -Trip -TripVehicle boat-2 -TripRoute route-near-1 -CampaignSeed '<campaign.json>' -Capture
./tools/Test-P46-ProductDay.ps1 -Players 4
```

Unity menüsü: `DeepDive/P4/Coast: author 15 huntable fish`; kullanıcı kaynak seti için `DeepDive/P4/Import supplied Tripo motorboat`. İlk menü aynı isim/kimlikleri koruyarak tekrar çalışır. Ana kullanıcı checkout'undaki ilgisiz materyal/render değişiklikleri ve eski silinmiş FBX `.meta` dosyaları bu teslim dışında bırakılır.
