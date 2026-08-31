# Aynı fazda çalışma ve birleştirme düzeni

Bu kurallar üç kişilik ekip içindir. Otomatik araçlar kapsam dışı oyun davranışını her zaman anlayamaz; branch koruması kadar sahiplik, inceleme ve test gerekir.

## Tek gerçek durum kaydı

- Kapsam ve kabul koşulları: `PHASES.md`.
- Aktif faz, görev sahibi, engel ve kanıt: `STATUS.md`.
- Sistem bağlantıları: `CONTRACTS.md`.
- Gerçek faz kapanış kanıtı: `docs/reports/Pn-REPORT.md`.
- Çalışan kodun kaynağı: ortak Git deposu; ZIP, sohbet eki veya Drive klasörüyle proje birleştirme yapılmaz.

## Faz ve görev durumları

Faz: `KILITLI → ACIK → ENTEGRASYON → DOGRULAMA → TAMAMLANDI`.

İlk durum istisnası: P0 için `PLAN_HAZIR`; uygulama başladığında ACIK olur. Aynı anda en fazla bir faz ACIK/ENTEGRASYON/DOGRULAMA olabilir.

Görev: `BEKLIYOR → CALISILIYOR → INCELEME → BIRLESTI → DOGRULANDI`.

- "Kod bitti" INCELEME anlamına gelir; ortak oyunda çalıştığı doğrulanmadan DOGRULANDI olmaz.
- BIRLESTI, mevcut faz dalına birleştiğini belirtir; ana dal teslimi değildir.
- Engeller ayrıca yazılır; bekleyen kişi gelecekteki faza geçmez.
- Bir kapı testi bozulursa faz uygun önceki aktif duruma geri alınır, test düzeltilir. Başarısız sonuç üzerinden faz açılmaz.

## Git modeli

İlk incelemede yerel depo commitsiz `master` dalındaydı. Kullanıcının açık paylaşım isteği üzerine `main` başlangıç dalı ve [public uzak depo](https://github.com/mehmetalisahingm/DeepDiveGame) oluşturuldu. Ekip erişimleri, koruma kuralları ve build altyapısı P0'da tamamlanır.

- Ana dal: `main` — yalnızca tamamlanmış, doğrulanmış fazlar.
- Faz dalı: `codex/p0-integration`, sonra `codex/p1-integration` vb.
- Özellik dalı örneği: `codex/p2-a-oxygen`, `codex/p2-b-fish`, `codex/p2-c-inventory`.
- Dal kişinin adına aylarca açık kalmaz; tek küçük işi taşır ve birleşince kapatılır.
- Her özellik dalı mevcut faz dalından açılır. Özellik PR'ının hedefi aynı fazın integration dalıdır.
- Faz sonunda integration → main PR'ı açılır.

İlk commitsiz depo için P0 başlangıç istisnası: plan ve minimum izlenen iskelet ana dala başlangıç commit'i olarak alınabilir; P0 tamamlandı etiketi konmaz. Ardından P0 çalışması normal faz dalından yürür.

## Günlük döngü

1. STATUS'tan aktif fazı, görevini, sahip olduğun dosyaları ve bağımlılıkları kontrol et.
2. Güncel faz dalını al; ortak sözleşmeyi değiştirmeden önce etkilenen kişiye bildir.
3. Küçük işi uygula; bağımlı sistem bitmediyse sözleşmeye uyan test sağlayıcısını kullan.
4. Kendi modülünü test et; PR'a görev kimliği, test sonucu ve değişen bağlantıları yaz.
5. Diğer kişi incelesin. Varsayılan dönüşüm A'nın PR'ını B, B'ninkini C, C'ninkini A inceler.
6. Faz dalına küçük birleşmeler yap; gün sonunda faz build'inin durumu ve engelleri kaydet.
7. Oyun özellikleri olan fazlarda en az iki oyuncuyla kısa ortak deneme yap; fazın tam 1/2/4 oyuncu matrisi kapanışta ayrıca çalışır.

Yeni geliştirme, o kişinin inceleme/test işlerini tamamlamasının yerine geçmez. Faz sonuna kadar hiç birleştirmeden beklemek yasaktır; büyük çakışmalar küçük günlük birleşmelerle azaltılır.

## Erken bitiren ve geciken kişi

- Erken bitiren: kendi teslimini belgeleyip peer review ve ortak test alır.
- Ardından geciken kişinin belirli bir alt görevini, aynı dosyada çakışma yaratmadan devralır. Devreden/devralan ve dosya kapsamı STATUS'a yazılır.
- Ana sorumlu ilgili sistemin sözleşmesini ve kabulünü takip etmeyi sürdürür.
- Gecikme görünür olduğunda aynı çalışma gününde engel yazılır; sorun faz kapanışına saklanmaz.
- Yetişmediği için zorunlu test veya özellik silinmez. İş bölümü/takvim revize edilir; kapsam azaltma gerekiyorsa açık değişiklik kaydı ve ürün sahibi kararı gerekir.
- Eşitlik aynı sayıda commit değil, benzer toplam emek ve ortak teslim sorumluluğudur.

## Sahne ve varlık çakışmalarını önleme

- A oyuncu/ekipman prefablarını, B sualtı/canlı prefablarını, C kasaba/ekonomi UI'ını düzenler.
- Her kişi kendi küçük test sahnesine sahiptir; bunlar aynı ortak projededir.
- Ana başlangıç sahnesi, ortak sahne listesi, katmanlar, input haritası, paket listesi ve render ayarı ortak dosyalardır; değişiklikler sıraya alınır.
- Ortak ana prefab/sahne üzerinde eşzamanlı düzenleme yapılmaz; parçalar prefab/ayrı sahneler olarak birleştirilir.
- Unity'de görünür `.meta` ve metin serileştirme ayarları P0'da doğrulanır; Smart Merge yardımcıdır, çakışmayı otomatik olarak doğru çözdüğü varsayılmaz.
- Model, ses ve büyük kaynak dokuları gibi uygun ikili dosyalar için Git LFS kullanılır; metin C# dosyaları LFS'ye alınmaz.
- Birleştirilemeyen varlıklarda dosya kilidi veya yazılı tek düzenleyici kuralı uygulanır.
- Dosya taşıma/silmede `.meta` eşleşmesi korunur. Çakışma çözümü sonrasında sahne ve referanslar editörde kontrol edilir.

## Koruma ve otomatik kontroller

P0'da gerçek uzak repo üzerinde uygulanacak düzen:

- main'e doğrudan push ve force push kapatılır.
- Özellik PR'ı için en az bir başka kişinin incelemesi ve proje build kontrolü gerekir.
- Fazı main'e taşıyan PR'ın yazarı entegratördür; diğer iki kişinin incelemesi ve faz raporunda üçünün onayı gerekir.
- İmza, gerçek kişi/hesap, tarih ve incelenen commit ile ilişkilendirilir; boş kutu veya şablon metni imza sayılmaz.
- Paket/editör sürümü, kayıp referanslar ve mevcut anlamlı testler kontrol edilir. CI başlatıldı demek CI geçti demek değildir.
- Ortak sözleşme veya proje ayarı değişikliği etkilenen kişilere ayrıca inceletilir.
- Faz etiketi veya PR başlığı tek başına kapsam uygunluğunu kanıtlamaz; inceleyen değişikliklerin açık faza ait olduğunu kontrol eder.

GitHub planı veya Unity build lisansı bir korumayı engellerse üç kişinin onayıyla uygulanabilir manuel kontrol tanımlanır ve STATUS'ta açıkça belirtilir. Gizli bypass, sahte başarılı kontrol veya yapıldığı iddia edilen koruma olmaz.

## Faz kapanış kapısı

1. A/B/C görevleri DOGRULANDI durumunda; alt görevlerde açık zorunlu iş yok.
2. Entegratör bütün parçaların mevcut faz dalında olduğunu ve build'in çalıştığını doğrular.
3. PHASES dosyasındaki faz kabul listesi ve ilgili eski faz regresyonları çalıştırılır.
4. [Faz raporu şablonu](../templates/PHASE_REPORT.md) gerçek sonuçlarla doldurulur.
5. Üç kişi aday commit üzerinde kapanış onayı verir; kod değişirse etkilenen test/onay yenilenir.
6. Faz PR'ı main'e birleşir. Birleşmiş commit'te build ve gerekli ortak testler tekrar doğrulanır. Çakışma çözümü oyun davranışını değiştirmişse ilgili tam kabul testleri yeniden çalıştırılır.
7. Rapor, main commit'i ve doğrulama kanıtı STATUS'a eklenir; faz TAMAMLANDI yapılır.
8. Ancak bundan sonra sıradaki tek faz ACIK yapılır, kişi paketleri tahminlenir ve yeni integration dalı oluşturulur.

Üç kişinin onayı olmadan faz açılmaz. Yalnızca kullanıcının açıkça değiştirdiği çalışma kuralı bu düzeni değiştirebilir; asistan başka kişiler adına onay veremez.

## Geçmiş fazdaki hata

Yeni fazdayken önceki bir özelliğin bozulması keşfedilirse aktif fazda `Pn-FIX-01` gibi bir görev kaydedilir. Hatanın ilgili eski kabul testi tekrar çalıştırılır. Geçmiş rapor sessizce yeniden yazılmaz; ek düzeltme ve kanıt bağlantısı verilir. Aktif fazı ilerletmek için hatayı sonraki faza gizlice atmak yasaktır.

## Plan değişikliği

- İşin teknik çözümünü iyileştirmek veya faz içindeki yükü dağıtmak, kapsam ve sözleşme korunuyorsa rutin ekip kararıdır.
- Yeni özellik, yeni bölge/tür, faz sırası değişimi, kabul testinin kaldırılması veya görsel hedef büyümesi bir kapsam değişikliğidir.
- Kapsam değişikliğine önce amaç, maliyet, etkilenen fazlar, ertelenecek işler ve karar sahibi yazılır. Kullanıcının açık onayı alınmadan uygulanmaz.
- Gerçek onay sonrası PHASES, CONTRACTS ve STATUS birlikte güncellenir. Değişiklik geriye dönük başarı kanıtı oluşturmaz.

## Test sonucu dili

`PASS`: ilgili test belirtilen build/commit ve ortamda gerçekten geçti.

`FAIL`: çalıştırıldı ve beklenen sonuç oluşmadı.

`CALISTIRILMADI`: kanıt yok. PASS yerine kullanılamaz.

`UYGULANMAZ`: yalnızca test bu faza ait değilse; zorunlu kabulü atlamak için kullanılamaz.

Yerel çoklu editör/süreç testleri yararlıdır; fazın istediği internet ve ayrı bilgisayar doğrulamasının yerine geçmez.

## AI yardımcısına verilecek görev başlangıcı

Her kişi aynı deponun güncel planını kullanır. Aşağıdaki mesajdaki rol ve görev kimliği gerçekten kendisine atanmış olanlarla değiştirilir:

> Ben bu projede A rolündeyim. Önce AGENTS.md, docs/plan/STATUS.md ve aktif fazın planını oku. Yalnızca bana atanmış açık faz görevini, CONTRACTS.md ve WORKFLOW.md sınırları içinde uygula. Gelecek faza geçme; başka kişinin görevini veya ortak sözleşmeyi koordinasyonsuz değiştirme. Teslimde değişen dosyaları, çalıştırılan testleri, çalıştırılmayan kontrolleri ve kalan engelleri yaz. Fazı diğer kişiler adına tamamlandı veya onaylandı işaretleme.
