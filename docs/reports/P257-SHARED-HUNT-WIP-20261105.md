# #257 ortak av ve çekim: çalışma kaydı

Durum: **taslak, kabul değil**. Bir oyuncu avı topladığında sahneden kalkan hedef, başka oyuncunun açık kaydını iptal edebiliyordu. Bu değişiklik başarılı host toplama olayından önce aynı hedefin açık kayıtlarını normal değerlendirme ve gerçek klip yoluyla bitirmeyi dener. Kayıt/ödül koşulları değiştirilmez. Otomatik sonuç bildirimi nedeniyle protokol P4-8'dir; tüm katılımcıların build'i aynı olmalıdır.

Gerçek klip kareleri 320×180 JPEG kalite 58'den 640×360 kalite 65'e çıkarılmıştır; mevcut 5 fps ve 15/30/45 saniye sınırları korunur. Dosya boyutu ve performans etkisi son kaynakta henüz ölçülmedi; toplam depolama kotası ayrıca açıktır.

## Kanıt ve eksikler

- Son kaynak Windows integrated build: PASS. Son kaynak Unity EditMode: **1505/1505 PASS**.
- Önceki kaynakla iki yerel GPU süreçli deneme `P46-product-20261105-101158-642-2` PASS: av toplandığında diğer oyuncunun kaydı gerçek klip olarak döndü (5,28 saniye, 27 kare, 71.742 bayt, 320×180). Bu önceki deneme vuruşun kayıt sırasında olduğunu ayrıca ölçmüyordu. İncelenen karelerde balık ve avcı çok küçük; görsel kabul değildir.
- Son kaynakla 640×360 ve yakın çekim denemesi `P46-product-20261105-103138-094-2` FAIL: iki otomatik oyuncu hareketli hedefi takip ederken adım 8 zaman aşımına uğradı. Son çözünürlük, vuruş, toplama ve klibin aynı çekimde olduğuna dair geçen gerçek oyun kanıtı yoktur. Test kanıtı, vuruş ve toplamanın aynı çekime ait olduğunu ayrıca daha sıkı bağlamalıdır.
- PC'de oynatma/yayınlama, 1 ve 4 oyuncu, gerçek bağlantı kopması, ayrı bilgisayar ve insan görsel kabulü doğrulanmadı. #254 av kimliği/ağırlığı ile izlenme/abone ilişkisi ve #253 depolama kotası kapsam dışıdır. #257 ve P4.12/#164 açık kalır.
