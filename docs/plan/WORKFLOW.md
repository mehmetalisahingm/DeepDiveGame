# Birlikte çalışma düzeni

Mehmet=A, Utku=B, Mert=C. Aynı fazda çalışılır; erken bitiren mevcut fazın incelemesine, testine veya devredilen alt işine destek olur.

## Nereden bakacağız?

- Kapsam ve teslim: [PHASES.md](PHASES.md).
- Güncel faz ve kişi görevi: [STATUS.md](STATUS.md).
- P0 ekip/üretim/bütçe/tasarım kararları: [toplantı gündemi ve kayıt](P0_MEETING.md); Mehmet takip eder, Mert kayıt desteği ve görsel/ses sorumluluğunu üstlenir.
- Sistem bağlantıları: [CONTRACTS.md](CONTRACTS.md).
- Kapanış: tek kısa [faz kaydı](../templates/PHASE_REPORT.md).
- Ortak kod: [GitHub deposu](https://github.com/mehmetalisahingm/DeepDiveGame); proje ZIP'leri birleştirilmez.

## Branch ve inceleme

- main, ortak doğrulanmış sürümdür; başlangıç plan commit'i oyun fazını tamamlamaz.
- Mevcut faz dalı örneği: codex/p0-integration.
- Küçük özellik dalları o faz dalından açılır: codex/p2-mehmet-oxygen, codex/p2-utku-fish, codex/p2-mert-inventory.
- Özellik PR'ı aynı faz dalına, faz kapanış PR'ı main'e gider.
- Ayrı kişi branch'lerinde haftalarca birikim yapılmaz; küçük birleşmeler yapılır.
- En az bir başka kişi değişikliği inceler: Mehmet'in işini Utku, Utku'nun işini Mert, Mert'in işini Mehmet.
- Birleştirmeyi P0/P3/P6'da Mehmet, P1/P4'te Utku, P2/P5'te Mert koordine eder. Koordinatör herkesin kodunu yazmaz.

Plan belgelerinin güncellenmesi oyun fazının kapanışı değildir. Kullanıcı talebine dayanan plan değişikliği ayrı dokümantasyon branch'inde hazırlanır; oyun testleri veya diğer kişilerin işi yapılmış sayılmaz.

## Günlük kısa kontrol

1. Aktif fazdaki kendi görevini ve bağımlılığını kontrol et.
2. Güncel faz dalını al; ortak sözleşme değişiyorsa etkilenen kişiyle anlaş.
3. Küçük işi yapıp test et; diğer sistem hazır değilse sözleşmeye uyan örnek veri kullan.
4. PR'a ne değiştiğini, hangi testin geçtiğini ve yapılmayan testi yaz.
5. Birleştirip build'i kontrol et. Engel varsa o gün görünür hâle getir.

P0 için build/erişim kontrolü yeterlidir. P1'den itibaren birleşen oyun davranışı en az iki oyuncuyla denenir; faz kapanışında o fazın 1/2/4 oyuncu kontrolleri ayrıca yapılır.

## Ortak dosyalar

Mehmet oyuncu/ekipman, Utku sualtı/canlı, Mert kasaba/ekonomi UI dosyalarının sahibidir. Herkesin ayrı test sahnesi olabilir ama proje ortaktır.

Mert tüm alanların görsel/ses kaynak seçimi, üretim planı, lisans/atıf listesi ve tutarlılık kontrolünden sorumludur. Her alan sahibi bu kaynakları kendi sistemine bağlar ve test eder; Mert'in sahipliği başkasının sahnesini koordinasyonsuz değiştirme izni değildir. Mehmet genel takvim/kapasite ve bütçe/servis takibini yürütür; faz birleştirme koordinatörlerinin dönüşümü korunur.

Ana sahne, input haritası, paket listesi, katman ve render ayarları birlikte koordine edilir. Aynı ana sahne/prefab üzerinde aynı anda çalışılmaz; ayrı parçalar birleştirilir. Dosya taşıma ve silmede .meta eşleşmesi korunur.

P0'da .gitignore, görünür .meta ve metin serileştirme kurulur. Smart Merge yardımcıdır; sonucu editörde kontrol edilir. Git LFS uygun büyük ikili varlıklar ilk kez eklenmeden önce kurulur. CI veya nihai grafik varlık seçimi P0'ı bekletmez; Mert kaynak/üretim yöntemini P0'da netleştirir. Dış varlık ilk kez eklenmeden Mert kaynak/lisans/atıf ve public repoda ham dosya paylaşım iznini kontrol eder; Utku Git/varlık düzenine eklenmesini inceler. İzin belirsizse varlık eklenmez.

## Araçları kademeli ekleme

| Zaman | Sorumlu | İş |
|---|---|---|
| P0 | Utku; erişimde Mehmet/Mert | İzlenecek dosyalar, ortak sürümler, herkesin kendi branch'ine pushlayabilmesi |
| P1 | Mert; yönetici işlemlerinde Mehmet | main/faz dallarında basit PR incelemesi ve force-push koruması |
| P2 | Utku; hesap/lisans işleminde Mehmet | Mevcut tekrarlanabilir build komutunu otomatik kontrole bağlama |

Koruma kurulana kadar da küçük PR ve bir başka kişinin incelemesi kuralı geçerlidir. Erişim/lisans engeli varsa neden, sorumlu ve kullanılan manuel kontrol yazılır; yapılmayan otomasyon PASS gösterilmez. İstisna tek kısa ekip kaydıyla kabul edilir; sahte başarılı kontrol veya gizli bypass yoktur.

## Fazı kapatma

1. Mehmet, Utku ve Mert'in zorunlu işleri faz dalında birleşir.
2. PHASES içindeki kabul koşulları ve ilgili eski davranışlar birlikte denenir.
3. Tek kısa kayda commit/build, sonuç, açık kusurlar ve üçünüzün gerçek tamam mesajı eklenir. Ayrı imza matrisi veya her özellik için üç inceleme gerekmez.
4. Faz PR'ı main'e birleşir; birleşmiş build açılır ve ilgili davranış kontrol edilir. Çakışma davranışı değiştirdiyse ilgili test yeniden yapılır.
5. STATUS güncellenir; ancak sonra sıradaki tek faz açılır.

P2'de temel his katmanıyla hareket/av/oksijen-çanta, P3'te kamera/tam döngü/tekrar dalma değerlendirmesi gerekir. Faz açıkken bir kişi gelecekteki faza ait kod, sahne veya içerik ekleyemez. Başkasının onayı veya testi uydurulamaz.

## İş yükü ve değişiklik

Haftalık saatler ve alt iş süreleriyle yükü kontrol edin. Erken bitiren, sahibinden devraldığı belirli alt işi yapabilir; hangi dosyayı değiştireceği kısa kayda yazılır. Eski fazdan gelen hata mevcut fazda bir düzeltme görevi olur ve ilgili regresyon tekrar denenir.

Yeni özellik, ikinci bölge, faz sırası değişikliği veya zorunlu kabulün kaldırılması sessizce yapılamaz. Kullanıcının açık kararından sonra ilgili plan ve durum belgeleri birlikte güncellenir. Rutin uygulama tercihleri ve faz içi yardım için yeni bir tören gerekmez.

Kullanıcı atamasıyla sistemler arası tasarım anlaşmazlığında, etkilenen kişilerin görüşü ve deneme sonucu alındıktan sonra mevcut kapsam içinde son karar Mehmet'tedir. Mert görsel/ses üretim tercihlerini yürütür. Mehmet'in son söz yetkisi başkasının tamamını veya testini üretmez; ücretli harcama, kapsam değişikliği ve faz geçişinin mevcut kuralları korunur.

Geçici yokluk ve kalıcı ayrılma kuralı T02'de kaydedilir. Üye ayrıldığında başkası onun adına tamam yazmaz; ekip/kapsam ve faz kapısı açıkça yeniden kararlaştırılmadan sonraki faza geçilmez. Kod, açık işler, kaynak listesi ve hesap erişimi devir ihtiyacı birlikte ele alınır.

## AI yardımcısına başlangıç mesajı

Rol adı ve görev kimliği size ait olanlarla değiştirilir:

> Ben Mehmet'im; görev kodum A. AGENTS.md, STATUS.md ve aktif fazdaki görevimi oku. Yalnızca bana atanmış mevcut faz işini CONTRACTS ve WORKFLOW sınırlarında yap. Başka faza geçme, başkasının dosyasını koordinasyonsuz değiştirme. Gerçek testleri, yapılmayan kontrolleri ve kalan engelleri açıkça yaz. Başkaları adına tamam veya onay kaydı oluşturma.
