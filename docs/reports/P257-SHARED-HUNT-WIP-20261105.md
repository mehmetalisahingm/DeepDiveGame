# #257 ortak av ve çekim: çalışma kaydı

Durum: **taslak, kabul değil**. Bir oyuncu avı topladığında sahneden kalkan hedef, başka oyuncunun açık kaydını iptal edebiliyordu. Bu değişiklik başarılı host toplama olayından önce aynı hedefin açık kayıtlarını normal değerlendirme ve gerçek klip yoluyla bitirmeyi dener. Kayıt/ödül koşulları değiştirilmez. Otomatik sonuç bildirimi nedeniyle protokol P4-8'dir; tüm katılımcıların build'i aynı olmalıdır.

## 5 Kasım devamı — iki süreçli gerçek klip

Genişletilmiş `P46-product-20261105-120253-316-2` koşusu da iki gerçek yerel GPU sürecinde PASS: misafir kayıt yaptı, host aynı 6,88 saniyelik klibi ev PC'sinde oynattı, klip sahibi misafir yayınladı ve ikinci yayın `PublicationAlreadyQueued` ile reddedildi. Her iki süreç aynı clip ID'yi raporladı. Kayıt 35 kare, 339.599 bayt; bu koşu ayrı PC veya gerçek internet testi değildir.

Windows build'de `P46-product-20261105-114407-599-2` koşusu iki yerel GPU sürecinde PASS. Host, misafirin çektiği balığı gerçek zıpkın girdisiyle vurdu ve topladı; host kayıt açıkken ölüm ve toplama alanlarını, misafir sunucunun başarılı otomatik durdurma bildirimini doğruladı. Her ikisi güvenli döndü; misafir klibi arşivlendi: 640×360, 46 kare, 9,17 saniye, 517.272 bayt. Test otomasyonunda avcının 22 m zıpkın menziline rağmen 3,5 m'ye yaklaşmaya çalışması ve anlık bildirimden sonra dönüşe devam etmemesi düzeltildi. Önceki koşuların zaman aşımı/iptal sonuçları PASS değildir.

Klip karelerinin görsel kontrolü kameracının uzak oyuncu modelinin içinden çekim nedeniyle görüntüyü kapattığını gösterdi. Host klip kamerası artık yalnız kendi kare render'ı sırasında kayıt sahibinin render bileşenlerini gizler ve önceki durumlarını `finally` ile geri yükler; diğer oyuncu görünür. Yeni klipte engel kalktı, ancak av ve avcı uzakta/küçük kaldı. Kadraj kalitesi son kabul değildir; misafir bilgisayarında dosya oynatma, kaynak kotası, ayrı PC ve #254 kimlik eşleştirmesi hâlâ açık.

Önceki PR başında CI `repair-rejoin` dört süreç koşusu 12,41 m beklenmedik konum sıçramasıyla FAIL; nedeni bu çalışmayla kanıtlanmadı ve yeni baş için sonuç değildir. #207/ilgili regresyon ve son CI sonucu ayrıca izlenmelidir.

Gerçek klip kareleri 320×180 JPEG kalite 58'den 640×360 kalite 65'e çıkarılmıştır; mevcut 5 fps ve 15/30/45 saniye sınırları korunur. Yukarıdaki tek klibin boyutu ölçüldü; uzun süreli performans ve toplam depolama kotası ayrıca açıktır.

## Kanıt ve eksikler

- Son kaynak Windows integrated build: PASS. Son kaynak Unity EditMode: **1505/1505 PASS**.
- Önceki kaynakla iki yerel GPU süreçli deneme `P46-product-20261105-101158-642-2` PASS: av toplandığında diğer oyuncunun kaydı gerçek klip olarak döndü (5,28 saniye, 27 kare, 71.742 bayt, 320×180). Bu önceki deneme vuruşun kayıt sırasında olduğunu ayrıca ölçmüyordu. İncelenen karelerde balık ve avcı çok küçük; görsel kabul değildir.
- Önceki kaynakla 640×360 ve yakın çekim denemesi `P46-product-20261105-103138-094-2` FAIL: iki otomatik oyuncu hareketli hedefi takip ederken adım 8 zaman aşımına uğradı. Sonraki denemenin ayrı kanıtı yukarıdadır.
- Misafir bilgisayarında dosya oynatma, ortak av için 1 ve 4 oyuncu, gerçek bağlantı kopması, ayrı bilgisayar ve insan görsel kabulü doğrulanmadı. #254 av kimliği/ağırlığı ile izlenme/abone ilişkisi ve #253 depolama kotası kapsam dışıdır. #257 ve P4.12/#164 açık kalır.
