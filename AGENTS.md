# DeepDiveGame çalışma talimatları

Her görevden önce docs/plan/STATUS.md, PHASES.md içindeki mevcut faz, ilgili CONTRACTS.md bölümü ve WORKFLOW.md okunur.

## Ekip

- Mehmet (A): oyuncu, dalış, ekipman kullanımı ve temel oturum bağlantısı.
- Utku (B): sualtı, canlılar, avlanma ve çekim hedefini değerlendirme.
- Mert (C): kasaba, oturum ekranı, envanter, ekonomi, ilerleme ve kayıt.
- Kullanıcı atamasıyla Mert ayrıca görsel/ses üretimi, kaynak seçimi ve tutarlılıktan sorumludur. Mehmet takvim/kapasite, devam/ayrılma planı, ürün hedefi, mevcut kapsamda tasarımın son kararı, bütçe/servis takibi ve birincil netcode incelemesini üstlenir. Mevcut geliştirme işleri ve faz koordinatörü dönüşümü korunur; netcode yedeği henüz seçilmedi.
- Herkes kendi sisteminin co-op davranışını ve testini de teslim eder. Bütün multiplayer işi Mehmet'e devredilemez.
- GitHub hesabı, deneyim, kapasite veya donanım bilinmiyorsa uydurulmaz.

## Kapsam

- PLAN_HAZIR, P0 planının hazır olduğunu ve Unity/oyun uygulamasının başlamadığını belirtir.
- Sadece açık fazın verilen görevini uygula. Kullanıcının plan düzenleme isteği, oyun geliştirmesine veya sonraki faza geçmeye izin vermez.
- P0 hafiftir: ortak sürüm, proje, dosya düzeni, ekip/erişim ve örnek build. Branch koruması P1'de, CI P2'de ele alınır.
- P0 toplantısında docs/plan/P0_MEETING.md gündemi de ele alınır. Üretim, kapasite/ayrılma, hedef, bütçe, tasarım yetkisi ve netcode sorumluluğu önerilerini ekip kararı veya uzmanlık kanıtı sayma; gerçek kararları uydurma. Kayıt sahibi host olmadan aynı kampanyaya devam edilemeyeceği D06 sınırını koru.
- Gelecek faz sözleşmeleri taslak olabilir; ihtiyaç duyulan fazdan önce kesinleşir. Bütün geleceğin API'sini P0'da uygulama.
- İlk oynanabilir sürüm tek dalış bölgesidir. P2'de kontrol/av/oksijen-çanta testi, P3'te kamera ve tam döngü testi yapılmadan sonraki faza geçme; ikinci bölge ekleme.
- P2/P3 testinden önce plandaki temel hareket, vuruş, nefes, UI ve sualtı ışık/sis geri bildirimini sağla. Nihai cila veya kapsamlı animasyon sistemi ekleme; his katmanını P4'e erteleme.
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

Üç kişinin işleri birleşir, faz testleri geçer ve tek kısa kapanış kaydında commit/build, sonuçlar ve üçünün gerçek tamamı bulunur. Ayrı imza matrisi gerekmez. Faz main'e birleştirilip ilgili davranış doğrulandıktan sonra STATUS güncellenir ve sonraki faz açılır.

P2 ve P3'te kendi kapsamlarının oynanış değerlendirmesi de şarttır. Bir kişinin işi eksik veya test başarısızsa fazı açma. Gerekli gerçek ekip onayını otomatik üretme; kanıtlar zaten yeterliyse gereksiz kullanıcı teyidi isteme.

## Yetki

Planlama talebini motor kurulumu, ücretli servis/varlık satın alma veya oyunu yayınlama yetkisi sayma. P6 sonrası yayın ayrı karardır. Alt ajan kullanımı için açık kullanıcı veya uygulanabilir talimat gerekir.
