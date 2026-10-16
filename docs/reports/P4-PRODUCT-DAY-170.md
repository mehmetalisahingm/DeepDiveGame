# #170 — Tek kampanyada gerçek günlük ürün döngüsü

16 Ekim 2026. Başlangıç entegrasyonu `1e8ef00bf0c4433893cb517ec8b98c6dffea0f11` (PR #169 + görev planı #174). Bu teslim Mehmet'in günlük döngü kanıtıdır; P4.6 ekip/üç gün/ayrı bilgisayar kabulünü veya yeni sanat kabulünü üretmez.

## Bulunan ürün engeli

Sürekli dünyaya geçişten önce dünya sahnesinin her dalışta yeniden yüklenmesi, sahnedeki tek balığı da yeniden yaratıyordu. Artık dünya yüklenmeyince yakalanan balık kalıcı siliniyor, ikinci gerçek av yapılamıyordu. İlk balık 120 kazandırıyor, temel kamera 150 istiyor; kamera satın alma için ikinci av gerekiyor. Önceki ayrı `-Acceptance` alt sistem testi `SeedRecorderCamera` ile kamera/bakiye hazırlıyordu. O test gerçek klip yolunu sınar; sıfırdan kazanılmış kamera veya tam ürün ekonomisi kanıtı değildir.

Dar düzeltme: host sonraki benzersiz DiveId'de mevcut sahne balığını kendi başlangıç pozundan yeniden spawn eder. Aynı dalışta anında yenilenmez; oyuncu/world sahnesi yeniden yüklenmez. Yakalanan sahne balığının renderer/collider'ları despawn sırasında kapanır, tekrar spawn'da başlangıçtaki etkinlikleri geri gelir; av claim/health/motion yeniden başlar. Tür/habitat dağılımı veya yeni balık sistemi eklenmez, #155 açık kalır. Yaşam döngüsü değişikliği nedeniyle protokol `DeepDive-P4-2`; host ve guest aynı build'i kullanır. Utku'ya #155 üzerinden teşhis ve değişen world dosyaları iletildi; bu bildirim inceleme onayı değildir.

## Tekrar üretim

`ProductDaySmokeDriver` yalnız açık `-p46-day-role` development komutuyla etkinleşir. `tools/Test-P46-ProductDay.ps1` gerçek Windows süreçlerini başlatır; grafik açık host gerçek klip yakalar. Guest'ler ayrı süreçtir. Owner hareket, zıpkın/E/F/H, alışveriş/tekne/medya RPC yolları kullanılır. Başlangıç parası/avı/kamera/klip, saat, uyku sonucu, stok sağlayıcı veya seyahat teleport'u enjekte edilmez.

Sıra: evden kıyıya yürüyüş; üç ücretsiz parça; av ve fiziksel satış; ikinci dalışta av ve satış; kazanılan parayla kamera; üçüncü dalışta canlı balığın gerçek kaydı; güvenli dönüş; iskelede binme/yakın rota/inme/yeniden binme/liman; fiziksel eve/PC'ye dönüş ve oynatma/yayın; tekrarı ret; yatakta erken uyku; gün 2 ve tek yayın geliri. Aynı kampanya dosyası yeni host sürecinde tekrar açılır; kamera/av satışları/onarım/gün/yayın/gelir/klip dosyası ve tekrar yayın reddi kontrol edilir.

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P46-ProductDay.ps1 -Players 1 -Port 24777
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 24877
./tools/Test-P46-ProductDay.ps1 -Players 4 -Port 24977
```

## Doğrulama kaydı

EditMode 1255/1255 PASS (`Logs/P170-tests.xml`). Windows build PASS. Gerçek birleşik koşuların sonuçları ve build kimliği tamamlandığında aşağıya kaydedilir; pilot sürücünün başarısız denemeleri kabul sayılmaz.

## #141 kabul matrisi ve sınır

Bu koşu erken uyku, güvenli dönüş ve normal host kapanışı sonrası diskten açmayı hedefler. Aynı yeni kaynakta ölüm/kayıp dalgıç, 00:00 kapanışı ve aktif dalış/sefer sırasında host kaybı bu koşunun parçası değildir; eski birim veya alt sistem raporunu yeni birleşik oturum PASS'ı gibi taşımaz. Ayrı bilgisayar/internet dört insan ve üç ardışık gün, Utku/Mert'in gerçek tamamı ve görsel değerlendirme #141'de açık kalır. Yeni şehir/rig/sanat, beş tür ve fiziksel tahta/keser/çivi tamiri bu teslimin kapsamı değildir.
