# P3 devam kaydı — 14 Eylül 2026

Kullanıcı isteğiyle mevcut çalışma P3 kapanış adayına kadar ilerletildi. **P3 henüz kapanmadı; P4 açılmadı.** Kapanış için gerçek oyunculu smoke ve ayrı bilgisayar/internet oynama kanıtı hâlâ gereklidir.

## Yapılan

- Utku'nun `94db4c3` Adım 5 kodu taban alındı. Collider ön koşulu gerçek Unity fiziğiyle **3/3 PASS**: raycast hedefi görüyor; CharacterController katman dışlamasıyla geçiyor; dışlama olmadan kontrol testi çarpışıyor. ProjectSettings/katman matrisi değiştirilmedi.
- `DiveTestAreaEventSetup` eklendi. Olay `(-9, 3.5, -2)` konumunda; 45 saniye sonra, 20 saniyelik pencere. URP ışık/parçacık placeholder'ı yalnız boş sunum alanlarına atanır; mevcut asset atamaları korunur. P2 setup script'i değiştirilmedi. Unity sahne hash'ini üretti.
- P3'ün temel ses işareti tamamlandı: `Bioluminescence` tanımı mevcut sualtı splash SFX'ini başlangıç cue'su olarak kullanır. Sahne özel bir `AudioSource` atamamışsa `SpecialEventPresenter` runtime'da yalnız bu eksik alan için 3D, playOnAwake kapalı bir kaynak oluşturur; ileride sahneye/Mert tarafından atanmış bir kaynak varsa onu ezmez.
- Son sahne üretimi byte-idempotent. Dört spawn'dan gerçek prefab kamera FOV/göz ofsetiyle raycast ve 4 saniyede en az Bronze testleri geçti. Mevcut balık subject testi artık FishActor taşıyan subject'i seçer.
- Mert'in **#44** dalı yerel entegrasyona alındı; **#43 alternatif uygulamadır, birlikte merge edilmemeli**. #44'ün varsayılan fiyat/Awake kaynaklı test hatası düzeltildi. `sea_bass` ve `event_bioluminescence` için açık kalite satırları var; tanımsız hedef ödenmez.
- Ödeme reddinde ledger sonucu ve aynı recordingId korunur; tekrar denemede yeniden para üretilmez. Otomatik test kayıtları kullanıcı kampanyasından ayrıdır; istemcilerin disk yazması engellenir. Diskte yalnız host loadout'u saklanır; oturumluk guest ID'leri başka oyuncuya ekipman aktaramaz.
- Protokol `DeepDive-P3-4`. Gerçek olay/ödeme/tüp/yükleme smoke senaryosu `-Event` ile eklendi.

## Doğrulama durumu

Ses bağlantısından **önceki** kapanış adayı head'inde:

- Tam EditMode: **356/356 PASS**, 0 başarısız (`Logs/P3-complete-final-tests.xml`).
- Setup: `Logs/P3-event-setup-final.log`, `P3_EVENT_SETUP_IDEMPOTENT`.
- Disk testleri gerçek dosyadan host tüpünü/para/ödeme kimliklerini geri yükledi, misafir ekipmanını taşımadı, bozuk primary için backup'ı denedi.
- Windows build: **PASS**, `Logs/P1-build.log`.

Başlangıç sesi için yapılan son kod/asset değişikliklerinden sonra bu sonuçlar otomatik olarak yeni head'e taşınmış sayılmaz. Yeni head için EditMode/build ve aşağıdaki gerçek oyunculu smoke tekrar çalıştırılmalıdır; yapılmadan PASS yazılmaz.

## P3 kapanışı için kalan gerçek kontroller

1. Yeni head'de Windows build ve EditMode doğrulamasını yenile.
2. Tek kişi ve dört yerel oyuncu için `./tools/Test-P1-Integrated.ps1 -Players 1 -Port 28777 -Event` ve `-Players 4 -Port 29777 -Event`. Senaryo gerçek olay penceresi, çekim, güvenli dönüş, kredi, host tüp satın alma ve disk yüklemeyi doğrular.
3. Av/çanta regresyonu: `./tools/Test-P1-Integrated.ps1 -Players 2 -Port 30777 -Hunt`; normal balık kaydı: `./tools/Test-P1-Integrated.ps1 -Players 2 -Port 31777 -Record`.
4. Gerçek build'de özel olayın ışık/parçacık ve başlangıç sesini gözle/kulakla kontrol et. Ardından aynı kampanyada **av + kayıt + satış + yükseltme + tekrar dalış** ve oyunu yeniden açınca yükleme davranışını doğrula.
5. Üç kişinin ayrı bilgisayar/internet üzerinden 20–30 dakikalık ortak oynama değerlendirmesini yap. Yerel süreç testi bunun yerine geçmez.
6. Bu kanıtlar geçince #45 ortak `codex/p3-integration` dalına alınır; #43/#44 alternatif PR'ları superseded olarak kapatılır, #34/#35/#36 kapanır, kısa P3 kapanış raporu yazılır ve ana dala geçiş yapılır.

Fiyat başlangıcı: sea_bass avı 120; iki kayıt hedefi için Bronze/Silver/Gold/Platinum 25/50/100/200; tüpler 100/250. Denge ve eğlence değerlendirmesi ekip testinde yapılacak.
