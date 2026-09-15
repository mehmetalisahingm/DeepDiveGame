# Plan 3.1 — ürün ritmi, onboarding ve bitiş netleştirmeleri

15 Eylül 2026. Bu belge Plan 3.0'ı büyütmek için sınırsız yeni sistem eklemez; mevcut yaşayan dünya döngüsünün anlaşılır, dengeli ve bitirilebilir olmasını netleştirir. `PHASES.md`, `GAMEPLAY_LOOP.md`, `WORLD_SYSTEMS.md` ve `ASSET_PLAN.md` ana kapsamı korur. Buradaki sayılar ilk oynama testi hedefleridir; ölçüm yapılmadan başarı garantisi sayılmaz.

## 1. İlk 20–30 dakika: dünya içinden onboarding

İlk oturum uzun eğitim ekranlarıyla değil gerçek görev zinciriyle öğretir:

1. Ortak ev/kasaba başlangıcı ve hareket/etkileşim.
2. Balıkçı, ekipman dükkânı, iskele ve bozuk sandalın haritada görülmesi.
3. Kamera veya sandal zorunlu olmadan kıyıda ilk balığın avlanması.
4. Çantadaki balığın fiziksel olarak balıkçıya götürülüp satılması.
5. İlk kazançtan sonra oyuncuya iki açık hedef gösterilmesi: sandal parçasına katkı veya temel kamera satın alma.
6. İlk kamerayı alan oyuncunun kısa geçerli kayıt yapması ve P3 hattında görüntü alıcısına teslim etmesi.
7. Üç sandal parçasının tamamlanması, yakın rotaya ilk ortak yolculuk ve güvenli dönüş.

İpucu sistemi bağlama duyarlıdır; oyuncu adımı zaten yaptıysa tekrarlatmaz. Serbest keşif ve kıyı avı tutorial yüzünden kilitlenmez. İlk 30 dakikada oyuncu en az bir kez `avla → taşı → sat → geliştir` döngüsünü görmelidir.

## 2. Ekonomi ve progression temposu

Kesin fiyat yerine hedef tempo kullanılır. İlk dengeleme amacı:

- İlk temel kamera: normal kıyı avıyla **1 oyun günü içinde veya en geç 2. günün başında** erişilebilir.
- Sandal onarımı: bütün parçalar bulunarak ücretsiz tamamlanabilir; satın alma alternatifi yalnız hızlandırır.
- İlk anlamlı dalgıç yükseltmesi: ilk 1–2 günde alınabilir.
- Motorlu tekne: oyuncu sığ bölgeyi ve temel satış/kamera döngüsünü öğrendikten sonra, yaklaşık **3–5 başarılı oyun günü** hedefi.
- Araştırma teknesi ve derin rota: orta bölgeyi anlamlı süre oynadıktan sonra, yaklaşık **6–10 başarılı oyun günü** hedefi.
- Boss hazırlığı: son teknenin satın alınması tek başına boss'u açmaz; keşif izleri, uygun ekipman ve görev zinciri de gerekir.

Testte oyuncu aynı aktiviteyi zorunlu olarak uzun süre tekrar ediyorsa fiyatlar düşürülür veya gelir çeşitlenir. Tersi durumda yeni bölge/tekne çok hızlı açılıyorsa fiyat ve görev temposu ayarlanır. Ama ilerlemeyi yapay bekleme süresi, günlük enerji veya bakım borcuyla yavaşlatmayın.

## 3. Ortak para ve büyük harcama kuralı

Küçük kişisel/tahsisli ekipman alımları mevcut host doğrulamasıyla yapılır. Kampanyayı etkileyen büyük ortak harcamalar farklıdır:

- Motorlu tekne, araştırma teknesi ve ev/kasaba büyük yükseltmeleri `SharedMajorPurchase` sayılır.
- Solo oyunda oyuncu doğrudan onaylar.
- 2–4 bağlı etkin oyuncuda satın alma ekranı fiyatı, mevcut bakiyeyi ve açacağı sonucu herkese gösterir; host başlatır ve **en az bir başka etkin oyuncunun onayı** gerekir.
- Aynı isteğin tekrar gönderilmesi ikinci ödeme üretmez. Onay beklerken bakiye değişirse fiyat/bakiye yeniden doğrulanır.
- Oyuncu ekipmanları için takım oylaması zorunlu değildir; bu kural yalnız büyük ortak ilerleme düğümlerinde kullanılır.

Amaç arkadaşın bütün ortak parayı tek tıkla harcamasını önlemek, oyunu oylama ekranına çevirmemektir.

## 4. Oyuncu kimliği — tam karakter yaratma olmadan

Dört oyuncu uzaktan kolay ayırt edilir:

- Tek uyumlu insan rig'i korunur.
- En az dört belirgin wetsuit/renk varyantı ve küçük maske/aksesuar farkı vardır.
- Oyuncu görünen adı kısa mesafede okunabilir; su altında UI kalabalığı yaratmayacak mesafe kuralı kullanılır.
- Kamera, zıpkın, çanta ve tüp görünümü karakter rengini ezmez.
- Tam yüz editörü, beden slider'ı veya kozmetik mağaza sistemi ilk kapsamda yoktur.

## 5. NPC'ler hizmet makinesi gibi hissetmesin

Her ana NPC'nin adı/görsel kimliği ve küçük replik havuzu vardır. Büyük diyalog ağacı yoktur.

- Ekipmancı: yeni ürün, para yetersizliği ve derin rota hazırlığına tepki verir.
- Balıkçı: nadir/büyük av, ilk satış ve günlük siparişe tepki verir.
- Görüntü görevlisi: ilk kayıt, yüksek kalite, yeni tür ve özel olaya tepki verir.
- Tekne satıcısı: sandal onarımı, yeni rota ve büyük tekne satın alımına tepki verir.

Her NPC için başlangıçta yaklaşık 5–10 kısa durum repliği yeterlidir. İlerleme, hava/gece ve boss söylentisi bazı replikleri değiştirir. Serbest dolaşan karmaşık NPC AI ve dallanan sinematik konuşma sistemi yapılmaz.

## 6. İlerleme panosu: oyuncu şimdi ne için oynadığını bilmeli

Evdeki iş panosu veya PC ana ekranı tek bir `Sonraki Hedefler` alanı gösterir:

- aktif ana görev ve ilerleme (`Sandal parçaları 2/3`, `Boss izi 1/3`),
- sıradaki erişim hedefi (`Resif rotası`, `Derin rota`),
- önerilen yükseltme ve eksik kredi,
- aktif balık siparişi/sponsor,
- açılan/eksik ansiklopedi sayısı,
- kanalın bir sonraki görünür kilometre taşı.

Bu ekran zorunlu görev listesi değildir; keşif serbest kalır. Oyuncuya `neden para kazanıyorum?` sorusunun cevabını verir.

## 7. Günlük tekrar oynanabilirlik

P4.5'te var olan sipariş/sponsor sistemi sabit üç görevi ezberletmek yerine küçük varyasyonlar üretir. Yeni dev sistem kurulmaz; mevcut açılmış içerikten kombinasyon seçilir:

- tür veya habitat,
- gündüz/gece,
- sakin/rüzgârlı hava,
- sığ/resif/derin kesim,
- av ağırlığı/sayısı veya kayıt süresi/kalitesi gibi tek ana şart.

Görev üretici oyuncunun erişemediği bölgeyi, sahip olmadığı zorunlu ekipmanı veya henüz açılmamış boss hedefini seçmez. Aynı şablon arka arkaya gelmez. Ödül, normal aktivitenin yerini alacak kadar yüksek olmaz; yön ve çeşitlilik sağlar.

## 8. Boss oyunun ilk kampanya finalidir

Tek boss ilk sürümün ana hedefini tamamlar:

1. Kasaba söylentisi başlar.
2. Üç keşif izi bulunur.
3. Habitat/boss bölgesi haritada açılır.
4. Takım uygun tekne, oksijen ve ekipmanı hazırlar.
5. Boss karşılaşmasında öldürme/av veya araştırma-kayıt sonucu seçilebilir; iki yol da ana zinciri tamamlayabilir.
6. Güvenli dönüşten sonra ev/kasaba final özeti, trofe/ansiklopedi sonucu ve kanal/ekonomi sonucu gösterilir.

İlk kez tamamlandığında kısa bir `kampanya tamamlandı` sunumu/credit ekranı gelir. Ardından aynı kayıt **serbest oyunda devam eder**: keşif yüzdesi, kanal, günlük siparişler, koleksiyon ve geliştirmeler sürdürülebilir. Boss'u tekrar para çiftliğine çevirecek sınırsız ana ödül yoktur; tekrar karşılaşma varsa kozmetik/istatistiksel veya azaltılmış ödül sınırı uygulanır.

## 9. Ayarlar ve erişilebilirlik

En geç P5/P6 kapanışında aşağıdaki oyuncu ayarları gerçek build'de bulunur:

- mouse hassasiyeti ve ters Y,
- FOV ayarı,
- master/müzik/SFX/ambiyans sesleri,
- kamera sallantısı/head-bob ve motion blur azaltma/kapatma,
- altyazı ve önemli ses olayları için görsel uyarı seçeneği,
- UI ölçeği ve okunabilir metin,
- grafik kalite profili ve çözünürlük/tam ekran seçenekleri.

Ayar değişiklikleri kampanya ilerlemesini etkilemez. Rekabetçi avantaj yaratmayan konfor seçenekleri co-op'ta yereldir.

## 10. Müzik ve atmosfer kimliği

ASSET_PLAN'ın ses dili şu durum katmanlarını hedefler:

- kasaba/ev: sıcak ve güvenli,
- kumsal/açık deniz: hafif keşif,
- sualtı sığ: sakin ve meraklı,
- derin bölge/gece: seyrek, gerilimli,
- boss izi/karşılaşma: belirgin gerilim ve zirve,
- gün özeti/ertesi sabah: kısa kapanış ve yeniden başlama hissi.

Müzik keskin biçimde açılıp kapanmak yerine alan/tehlike durumuyla geçiş yapar. Lisanslı ticari şarkı varsayılmaz; kullanılan her parça CREDITS/lisans kontrolünden geçer.

## Kabul notları

Bu netleştirmeler mevcut fazlara dağıtılır; yeni P7 yaratmaz:

- **P3:** onboarding'in ilk kıyı/satış/kamera/sandal adımları, oyuncu kimliğinin temel görünümü ve NPC kısa geri bildirimi.
- **P4.1–P4.5:** progression panosu, büyük ortak satın alma onayı, günlük varyasyon, NPC ilerleme replikleri ve boss final/serbest oyun akışı.
- **P5:** ekonomi temposu, tekrarlanabilirlik, konfor ve performans ayarlarının dengelemesi/regresyonu.
- **P6:** ayarlar, final/credit, ses-müzik bütünlüğü ve temiz build kabulü.

Yeni ikinci ada, sınırsız görev üretimi, açık dünya araç fiziği, tam karakter yaratma, büyük diyalog ağacı veya canlı servis sistemi bu eklemelerle kapsama girmez. Plan 3.0'ın bitirilebilir sınırı korunur.
