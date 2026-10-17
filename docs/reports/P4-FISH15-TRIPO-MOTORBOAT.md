# 15 kıyı balığı ve kullanıcı Tripo motorbotu

18 Ekim 2026. Kullanıcının açık yeni talebi iki dar içerik işini faz sırasından öne alır; P4.6/nihai görsel/ayrı bilgisayar kabulünü kapatmaz.

## Oyun değişikliği

- `DiveTestArea` artık **15 ayrı levrek** barındırır. Mevcut ilk balık korunur; 14 hayvan Unity Editor ile yazılır, ayrı NGO sahne hash'i alır. Hepsi mevcut host yüzme/kaçma/sağlık, zıpkın, av toplama, envanter, satış ve çekim yolundadır. Yeni tür veya test stok sağlayıcısı eklenmez. Yerleşim sığ su içindedir; bütün wander hacimleri sınırlar içinde kalır.
- Sonraki dalış mevcut `FishDiveLifecycleBinding` ile tüm nüfusu yeniler. Aynı dalışta toplanan balık hemen geri gelmez. Hayatta kalan gece balığının aynı karede despawn/spawn sırasında `Light` silme/yeniden ekleme hatası düzeltildi: ışık bileşeni saklanır, despawn'da kapatılır, host gece durumunu yeniden belirler.
- Kullanıcı motorbotu `boat-2` görünümünde: dokulu FBX, URP renk/normal materyali, ölçek/pivot uyarlaması. Fiziksel sınır 3,2 × 7 m; model yaklaşık **3,20 × 6,26 m**, yükseklik 2,29 m. Başlangıç sandalı ve araştırma teknesi ayrı kalır. Sabit dört koltuk, sahiplik, satın alma, save, binme/hareket/rota otoriteleri değişmez; görsel collider veya ikinci NetworkObject eklemez.
- Yeni sahne nesneleri nedeniyle ağ protokolü **DeepDive-P4-3**. Birlikte oynayan herkes aynı yeni build'i kullanmalıdır.
- İsteğe bağlı geliştirme testinde tekne genel çekimi ayrı `room-boat-overview.png` dosyasına yazılır; oyuncu kamerası çekimleri korunur, render sonrası kamera geri alınır. Normal oyunda test sürücüsü kurulmaz.

## Kaynak ve inceleme sınırı

Arşiv `luxury+motorboat+3d+model.zip`, SHA-256 `1A4451AD12BC7B317C602A5DA19E0F45D7099171B9150C3762ECCFA37A0ABA65`. FBX ve beş doku kullanıcı tarafından sağlandı. Kullanıcı kendi Tripo üretimi olduğunu ve public GitHub'a ekleme iznini açıkça bildirdi. Hesap planı/üretim tarihi arşivde yoktur; kayıt [CREDITS](../assets/CREDITS.md) içindedir. Kaynak doku setinin tamamı korunur; mevcut URP materyali renk ve normal dokusunu kullanır, metaliklik/pürüzlülük çalışma değeridir. Yeni servis çağrısı veya satın alma yoktur.

Dünya/canlı/sahne kısmı Utku, görsel kaynak/tutarlılık kısmı Mert incelemesi gerektirir. Bu teslim onların onayı veya faz kapanışı değildir. İlk sandalın fiziksel tahta/keser/çivi tamiri, beş tür, kasaba grafikleri ve genel sanat/performans kabulü bu işte tamamlandı sayılmaz.

Sahne blokları dosya kimliğiyle karşılaştırıldı: eski 310 bloğun yalnız kök listesine yeni balıklar eklendi; mevcut diğer bütün sahne nesnelerinin verisi aynı kaldı. Unity yeniden sıralaması diff'i büyütür, elle NGO hash'i uydurulmadı.

## Doğrulama

- Tam EditMode: **1273/1273 PASS**, `Logs/fish15-serialized-editmode.xml`. Tüm balıklarda ayrı ve sıfır olmayan NGO hash'i (ham kayıt dosyası dahil), av/çekim bileşenleri, host transform otoritesi ve su içi wander hacmi kontrol edilir. Yaşayan gece balığı üç anlık yeniden kullanımda tek ışığı güvenle korur. Motorbot prefabı, doku/URP, gövde değiştirme ve collider sınırları kontrol edilir.
- Windows build: ilk içerik build'i ve yaşam döngüsü düzeltmesini içeren build PASS.
- Gerçek iki süreçli av: **host + guest PASS**, `Logs/P1-integrated-20261018-000959-380-2-26577`. Her iki süreç 15 canlı balık görür; misafirin gerçek zıpkın/E girdisiyle bir av toplanır ve çantaya yansır; diğer 14 hayvan canlı kalır.
- Gerçek iki süreçli Tripo motorbot yolculuğu: **host + guest PASS**, `Logs/P1-integrated-20261018-000959-378-2-26677`; doğru gövde, gerçek binme/gidiş/demirleme/inme/yeniden binme/dönüş ve save kontrolü. Bu dar rota denemesi daha önce kazanılmış filo kampanyasının kopyasını kullanır; sıfır bakiyeden ürün kabulü olarak sunulmaz.
- İlk dört süreçli uzun koşu ikinci dalışa geçişte `FishActor.OnNetworkSpawn` gece ışığı hatasını yakaladı; PASS sayılmadı. Hata düzeltildi, regresyon testi eklendi; yeniden koşu aşağıda kayıtlıdır.

18 Ekim son uzun koşu: **4/4 gerçek süreç PASS**, `Logs/P46-product-20261018-001808-4`. Tek dünya yüklemesi, **üç ayrı dalışta 15'er canlı balık**, 45 farklı balık ağ yaşam kimliği ve ilk balığın üç ayrı yaşam kimliği her dört süreçte gözlendi. Gerçek av/satış, kamera alımı, gerçek klip/oynatma/yayın, dört kişilik sandal turu, uyku, gün 2 ve 135 kanal geliri doğrulandı. Yeni host sürecinde diskten açma/klip oynatma/ikinci yayın reddi PASS. Bu aynı PC'de dört süreçle bir günlük ürün döngüsüdür; dört insan/ayrı PC/üç oyun günü değildir. Son iki süreçli av tekrarı da `Logs/P1-integrated-20261018-001808-197-2-26977` altında host + guest PASS.

Yazar scriptinin tekrar çalıştırılması ayrıca kontrol edildi. İlk klonlamada Unity'nin bellekte düzelttiği NGO kimliklerinin diskte de kalması için ilk kayıttan sonra sahne yeniden açılıp NetworkObject değerleri tekrar kaydedilir. Ham YAML kimliklerini Unity'nin açılış onarımından önce kontrol eden ayrı test eklendi. Bu son düzeltme yalnız editör/yazılmış sahne kimlikleridir; aynı oyuncu sahne içeriği yeniden build edilerek karşılaştırılır.

Son kaynak `8e7e360`; uzun koşu kaynağı `3202dbf`. Yazar tekrar çalıştırıldığında sahne hash'i aynı kaldı. Son Windows rebuild PASS; hem `level0` hem `level1` byte hash'leri uzun koşuda kullanılan build ile **aynı** çıktı. Runtime managed DLL'leri de aynı kaldı. Böylece ham sahne yazım düzeltmesi sonrası test edilen oyun davranışının değişmediği ayrıca doğrulandı; 1273/1273 son EditMode bu kaynağı kapsar.

Ham süreç sonuçları ve görüntüler [kanıt klasöründe](evidence/p4-fish15-tripo-20261018/validation.json) saklanır. Oynanabilir yerel ZIP `Builds/DeepDive-15Balik-TripoMotorbot-Windows.zip`; ana kullanıcı checkout'unda bulunur. Bütün klasör çıkarılıp `DeepDiveGame-P1.exe` çalıştırılır; herkes aynı yeni protokolü kullanmalıdır. Dağıtım paketi okunabilirlik, EXE/veri/runtime içeriği ve dosya hash'iyle kontrol edildi.

![Tripo motorbot gerçek runtime genel çekimi](evidence/p4-fish15-tripo-20261018/tripo-motorboat.png)
![15 balıklı kıyıdan gerçek oyuncu kamerası](evidence/p4-fish15-tripo-20261018/coastal-fish.png)

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
