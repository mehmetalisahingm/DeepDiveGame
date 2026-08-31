# DeepDiveGame çalışma talimatları

## Önce mevcut fazı öğren

Her görevden önce şu dosyaları oku:

1. `docs/plan/STATUS.md`
2. `docs/plan/PHASES.md` içindeki mevcut faz
3. Değiştireceğin sistemin `docs/plan/CONTRACTS.md` kaydı
4. `docs/plan/WORKFLOW.md`

Henüz oyun projesi yoksa varmış gibi davranma. Bu planın ilk oluşturulması yalnızca belge çalışmasıdır; motor kurulumu veya oyun geliştirmesi yapılmış sayılmaz.

## Faz ve kapsam sınırı

- Sadece STATUS dosyasında çalışmaya açık olan fazdaki görevleri uygula.
- `PLAN_HAZIR` durumu, P0 planının hazır olduğunu ama P0 uygulamasının başlamadığını belirtir. Bir uygulama görevi verildiğinde P0 çalışmasına başlanabilir; gelecekteki fazlar açılmış sayılmaz.
- Bir sonraki fazın kodunu, sahnesini, içeriğini veya paketini "hazır olsun" diye ekleme.
- Gelecek fazlar için sözleşme tanımlamak serbesttir; bunları oyun davranışı olarak erken uygulamak yasaktır.
- Kullanılmayan genel altyapı, kapsam dışı sistem ve talep edilmemiş refactor ekleme.
- Aktif fazdaki görevin erken biterse mevcut fazı test et, incele veya görev devri yapılarak başka bir sahibine destek ol.
- Eski bir fazın hatası bulunursa mevcut fazda kayıtlı bir düzeltme görevi aç; hatayı saklama ve gerekli regresyonu çalıştır.
- Kullanıcının açık yeni talimatı bu belgeleri değiştirebilir. Böyle bir durumda ilgili plan, durum ve karar kayıtlarını tutarlı biçimde güncelle; sessizce yeni kapsam ekleme.

## Sahiplik ve ortak dosyalar

- A: oyuncu, dalış, ekipman kullanımı ve temel oturum bağlantısı.
- B: sualtı, canlılar, avlanma ve görüntü hedefinin değerlendirilmesi.
- C: kasaba, envanter, ekonomi, ilerleme ve kalıcı kayıt.
- Her kişi kendi sisteminin ağ senkronizasyonu, arayüzü ve testiyle birlikte teslim eder. Bütün multiplayer işi A'ya devredilemez.
- Başka kişinin sahip olduğu dosyada çalışma gerekiyorsa mevcut görev üzerinden koordinasyon kur ve sahiplik değişimini STATUS dosyasına kaydet.
- Ortak sözleşme, proje ayarı veya ana sahne değişikliğini tek taraflı yapma. Etkilenen kişilere incelet.
- Unity `.meta` dosyalarını ait oldukları varlıklarla birlikte taşı ve sürüm kontrolünde tut. Üretilen önbellekleri veya kimlik bilgilerini depoya ekleme.

## Teslim ve kanıt

- Her özellik ilgili Pn-A/B/C görev kimliğine bağlanmalı.
- Tek oyuncuda çalışması co-op teslimi değildir. Fazın istediği sayıda oyuncu ve ayrı süreç/bilgisayar koşullarını doğrula.
- Derlenmedi, çalıştırılmadı veya çevrimiçi test edilmediyse bunu açıkça yaz.
- Test etmeden PASS, imza olmadan ONAY veya başka kişi adına tamamlandı kaydı oluşturma.
- Uzak repo ve koruma kuralları kurulmadıysa uygulanıyormuş gibi bildirme.
- Fazın yalnızca bir kişinin işi bitince tamamlandığını ilan etme.

## Faz geçişi

Sonraki faz ancak şu koşullarla açılabilir:

1. A, B, C görevleri doğrulandı.
2. Geçerli fazın birleştirme dalında ortak kabul testleri geçti.
3. Faz raporunda üç gerçek katılımcının kapanış onayı var.
4. Fazın ana dala PR'ı birleşti; birleşmiş ana dal commit'i üzerinde gerekli kontroller geçti.
5. STATUS dosyası rapor ve commit bağlantısıyla güncellendi.

Fazı açmak için bu koşulları atlama. Kanıt yeterliyse ayrıca gereksiz kullanıcı teyidi isteme; gerekli ekip onayını otomatik üretemezsin.

## Bu depo için otomatik yetki olmayan işlemler

Planlama isteğini motor kurulumu, paket satın alma, bulut servis ücretini kabul etme veya oyunu yayınlama yetkisi sayma. Yayınlama P6 sonrasındaki ayrı karardır.

Alt ajan kullanımı kendiliğinden yetkili değildir; kullanıcının veya uygulanabilir bir talimatın açık isteği gerekir.
