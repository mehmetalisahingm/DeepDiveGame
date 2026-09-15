# DeepDiveGame çalışma talimatları

Her görevden önce docs/plan/STATUS.md, PHASES.md içindeki mevcut faz, ilgili CONTRACTS.md bölümü ve WORKFLOW.md okunur. Ev/gün/medya/harita/tekne/boss işinde WORLD_SYSTEMS.md'nin ilgili bölümü de okunur.

## Ekip

- Mehmet (A): oyuncu, dalış, ekipman kullanımı ve temel oturum bağlantısı.
- Utku (B): sualtı, canlılar, avlanma ve çekim hedefini değerlendirme.
- Mert (C): kasaba, oturum ekranı, envanter, ekonomi, ilerleme ve kayıt.
- Kullanıcı atamasıyla Mert ayrıca görsel/ses üretimi, kaynak seçimi ve tutarlılıktan sorumludur. Mehmet takvim, devam/ayrılma planı, ürün hedefi, mevcut kapsamda tasarımın son kararı, bütçe/servis takibi ve birincil netcode incelemesini üstlenir. Mevcut geliştirme işleri ve faz koordinatörü dönüşümü korunur; netcode yedeği henüz seçilmedi.
- Herkes kendi sisteminin co-op davranışını ve testini de teslim eder. Bütün multiplayer işi Mehmet'e devredilemez.
- GitHub hesabı veya deneyim bilinmiyorsa uydurulmaz.

## Kapsam

- PLAN_HAZIR, P0 planının hazır olduğunu ve Unity/oyun uygulamasının başlamadığını belirtir.
- Sadece açık fazın verilen görevini uygula. Kullanıcının plan düzenleme isteği, oyun geliştirmesine veya sonraki faza geçmeye izin vermez.
- P0 hafiftir: ortak sürüm ve proje main'e gelir; Mehmet, Utku ve Mert güncel depoyu indirip aynı Unity sürümünde açar ve ortak örnek Windows build'ini çalıştırır. P0'da karşılıklı PR incelemesi veya herkesin deneme branch'i pushlaması kapanış şartı değildir. Branch koruması P1'de, CI P2'de ele alınır.
- docs/plan/P0_MEETING.md gündemi referanstır; P0 kapanışını bekletmez. Üretim, devam/ayrılma, hedef, bütçe, tasarım yetkisi ve netcode sorumluluğundaki açık kararlar ilgili özelliğe başlamadan ele alınır; gerçek kararları uydurma. Kayıt sahibi host olmadan aynı kampanyaya devam edilemeyeceği D06 sınırını koru.
- Gelecek faz sözleşmeleri taslak olabilir; ihtiyaç duyulan fazdan önce kesinleşir. Bütün geleceğin API'sini P0'da uygulama.
- İlk oynanabilir sürüm tek dalış bölgesidir. P2'de kontrol/av/oksijen-çanta testi, P3'te kamera ve tam döngü testi yapılmadan sonraki faza geçme; ikinci bölge ekleme.
- Plan 3.0 (15 Eylül): P3 insan modeli/yürüme-yüzme/eller/ekipman, kıyı/üç NPC/çantayla satış, ilk sandal görevi ve canlı tekne haritasını teslim eder. Kamera ayrı satın alınır; kıyı avı kamera veya onarım istemez. Ayrıntı GAMEPLAY_LOOP.md ve ASSET_PLAN.md içindedir; temel sanatı P4'e erteleme.
- P2/P3 testinden önce hareket, vuruş, nefes, UI ve sualtı ışık/sisini sağla. P3 sınırlı insan/ekipman animasyonu kapsam içidir; kapsamlı sinematik/karakter yaratma sistemi ekleme.
- P4 sırası: P4.1 ev/gün/keşif; P4.2 gerçek klip/PC/oyun içi kanal; P4.3 ekipman/üç tekne; P4.4 tür/boss; P4.5 günlük çeşitlilik/ev-kasaba/roller; P4.6 tam kabul. P4 açılmadan veya önceki ara teslim kabul edilmeden sıradakini uygulama. İzlenebilir klip şartını puan/thumbnail ile tamamlanmış sayma.
- Başlangıç sandalı üç erişilebilir parçayla onarılır; parçalar kıyıdan bulunabilir veya av geliriyle alınabilir. P4 motorlu ve araştırma teknesi satın alımını ekler; hepsi dört kişilik, denizde aynı anda bir aktif araçtır. Sabit rota korunur; serbest dümen/fizik ve ikinci ada kapsam dışıdır.
- Güvenli dönüş otomatik ödeme değildir. P3 NPC teslimi, P4.2 alternatif PC kanal yayını vardır; aynı kayıt iki ticari yoldan ödenmez. Video oyun kamerası/ortam sesidir; gerçek sosyal medya yükleme veya mikrofon kaydı yoktur. D06 host kampanyası sınırı korunur.
- Erken bitirirsen mevcut fazın testini/incelemesini veya kaydedilmiş destek görevini al.
- Kullanıcının açık yeni talimatı planı değiştirebilir; ilgili belgeleri tutarlı güncelle. Sessiz kapsam büyütme yapma.

## Sahiplik ve teslim

- Başka kişinin dosyasında veya ortak sahne/sözleşmede değişiklik gerekiyorsa etkilenen kişiyle koordinasyon kur.
- Unity .meta dosyalarını varlıklarıyla birlikte koru; sırları ve üretilen önbellekleri depoya ekleme.
- Test sağlayıcılarını teslimin gerçek oyun yolunda aktif bırakma.
- Test etmeden PASS veya başka kişi adına tamam kaydı oluşturma.
- Yerel/tek oyuncu testi, fazın istediği internet ve ayrı bilgisayar testinin yerine geçmez.
- Bir fazın yalnızca kendi görevin bitince tamamlandığını söyleme.

## Hafif faz kapısı

P0 istisnası: ortak temel main'e birleşir; üç kişi depoyu indirip projeyi aynı Unity sürümünde açtığını ve ortak Windows build'ini çalıştırdığını bildirince kısa kapanış kaydı yazılır. P1 ve sonrasında üç kişinin işleri birleşir, faz testleri geçer ve tek kısa kapanış kaydında commit/build, sonuçlar ve üçünün gerçek tamamı bulunur. Ayrı imza matrisi gerekmez. Faz main'e birleştirilip ilgili davranış doğrulandıktan sonra STATUS güncellenir ve sonraki faz açılır.

P2 ve P3'te kendi kapsamlarının oynanış değerlendirmesi de şarttır. P3'ün yeni insan/ekipman/kamera satın alma, kıyı/NPC/sandal/harita ve görsel kabulü geçmeden eski teknik kapanış adayını yeterli sayma. P3 main'e birleşip doğrulandıktan sonra P4 ayrıca açılır. P4.6'da balık/video/ev/gün/tekne/boss döngüsü art arda üç oyun günü ve gerçek ortak değerlendirmeyle kabul edilir. Eksik iş/test varsa ilerleme; başkasının tamamını üretme, mevcut kanıt yeterliyse gereksiz teyit isteme.

## Yetki

Planlama talebini motor kurulumu, ücretli servis/varlık satın alma veya oyunu yayınlama yetkisi sayma. P6 sonrası yayın ayrı karardır. Alt ajan kullanımı için açık kullanıcı veya uygulanabilir talimat gerekir.
