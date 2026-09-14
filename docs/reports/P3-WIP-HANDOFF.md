# P3 devam kaydı — 14 Eylül 2026

Kullanıcı isteğiyle mevcut çalışma ara teslim olarak pushlandı. **P3 kapanmadı; P4 açılmadı.**

## Yapılan

- Utku'nun `94db4c3` Adım 5 kodu taban alındı. Collider ön koşulu gerçek Unity fiziğiyle **3/3 PASS**: raycast hedefi görüyor; CharacterController katman dışlamasıyla geçiyor; dışlama olmadan kontrol testi çarpışıyor. ProjectSettings/katman matrisi değiştirilmedi.
- `DiveTestAreaEventSetup` eklendi. Olay `(-9, 3.5, -2)` konumunda; 45 saniye sonra, 20 saniyelik pencere. URP ışık/parçacık placeholder'ı yalnız boş sunum alanlarına atanır; ses boş, mevcut asset atamaları korunur. P2 setup script'i değiştirilmedi. Unity sahne hash'ini üretti.
- Son sahne üretimi byte-idempotent. Dört spawn'dan gerçek prefab kamera FOV/göz ofsetiyle raycast ve 4 saniyede en az Bronze testleri geçti. Mevcut balık subject testi artık FishActor taşıyan subject'i seçer.
- Mert'in **#44** dalı yerel entegrasyona alındı; **#43 alternatif uygulamadır, birlikte merge edilmemeli**. #44'ün varsayılan fiyat/Awake kaynaklı test hatası düzeltildi. `sea_bass` ve `event_bioluminescence` için açık kalite satırları var; tanımsız hedef ödenmez.
- Ödeme reddinde ledger sonucu ve aynı recordingId korunur; tekrar denemede yeniden para üretilmez. Otomatik test kayıtları kullanıcı kampanyasından ayrıdır; istemcilerin disk yazması engellenir. Diskte yalnız host loadout'u saklanır; oturumluk guest ID'leri başka oyuncuya ekipman aktaramaz.
- Protokol `DeepDive-P3-4`. Gerçek olay/ödeme/tüp/yükleme smoke senaryosu `-Event` ile eklendi, henüz çalıştırılmadı.

## Gerçek test sonucu

- Son tam EditMode: **356/356 PASS**, 0 başarısız. `Logs/P3-complete-final-tests.xml`.
- Son setup: `Logs/P3-event-setup-final.log`, `P3_EVENT_SETUP_IDEMPOTENT`.
- Önceki koşuların setup/eksik definition hataları giderildi; önceki başarısız koşular PASS sayılmadı.
- Disk testleri gerçek dosyadan host tüpünü/para/ödeme kimliklerini geri yükledi, misafir ekipmanını taşımadı, bozuk primary için backup'ı denedi.
- Son Windows build sonucu ve gerçek oyunculu smoke bu ara teslimde henüz doğrulanmış sayılmaz.

## Sıradaki işler

1. Windows build'i doğrula: `./tools/Build-P1.ps1 -Integrated`. **ConnectScenes veya P2 setup çalıştırma.**
2. Tek kişi ve dört yerel oyuncu için `./tools/Test-P1-Integrated.ps1 -Players 1 -Port 28777 -Event` ve `-Players 4 -Port 29777 -Event`. Bu senaryo yaklaşık 106 saniyedir: gerçek olay penceresi, çekim, güvenli dönüş, gerçek kredi, host tüp satın alma ve disk yükleme. Başarısızsa düzelt; PASS uydurma.
3. Av/çanta regresyonu: `./tools/Test-P1-Integrated.ps1 -Players 2 -Port 30777 -Hunt`; normal balık kaydı: `-Players 2 -Port 31777 -Record`.
4. Gerçek oyun yeniden açılışı, aynı oturumdaki sonraki dalış ve solo/dört oyunculu birleşik **av + kayıt + satış + yükseltme + tekrar dalış** döngüsü henüz tam doğrulanmadı. Testler ayrı dilimlerdir; tam döngü geçti denmemeli.
5. Özel olayın görselini gerçek build'de kontrol et; Mert'in başlangıç sesi hâlâ eksik. #43/#44 alternatiflerini tek teslimde netleştir; bu dalı güncel P3 ortak dalına PR ile al. Bu push ortak dala merge değildir.
6. Üç kişinin ayrı bilgisayar/internet ve 20–30 dakikalık ortak oynama değerlendirmesi olmadan P3'ü kapatma; insan onayı üretme.

Fiyat başlangıcı: sea_bass avı 120; iki kayıt hedefi için Bronze/Silver/Gold/Platinum 25/50/100/200; tüpler 100/250. Denge ve eğlence değerlendirmesi ekip testinde yapılacak.
