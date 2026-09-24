# P3.4 birleşik kabul — checklist, test senaryosu, kayıt formatı ve görsel/ses audit'i

Durum: **hazırlık aşamasında**. #75 (Mehmet), #76 (Utku) ve #66'nın Composition/prefab entegrasyonu `codex/p3-integration`'da birleşti; iki yerel süreçli gerçek sandal seferi geçti ([kayıt](../reports/P3-BOAT-RUNTIME-ACCEPTANCE.md)). Tam P3.4 kapısı hâlâ dört süreç, ayrı bilgisayar/internet, oyuncu-facing harita UI'sı ve ortak görsel/ses kabulünü bekler. Bu doküman birleşik acceptance checklist'i, test senaryosu ve kayıt formatını korur; P3.3 kodunu bypass eden placeholder kullanılmadı.

## Ön koşul

Tam P3.4 koşumu, aşağıdaki entegrasyonlar `codex/p3-integration`'da birleştikten sonra başlar (iki süreçli sandal alt koşumu ayrıca geçti):
1. #75 merge — gerçek koltuk/binme-inme/sabit rota hareketi. **Birleşti; iki süreçli smoke geçti.**
2. #76 merge — gerçek iskele/demirleme/rota/world↔map verisi. **Birleşti; iki süreçli smoke geçti.**
3. #66'nın kalan Composition/prefab bağlantısı — **birleşti;** `BoatTripPlayerSync` NetworkDiver prefab'ında ve gerçek dock/seat/route sahne nesneleri mevcut. Oyuncu-facing harita UI'sı hâlâ açık teslimdir.

İki süreçli sefer adımları artık koşulabilir ve yukarıdaki kayıtla doğrulandı. Dört süreç, ayrı bilgisayar/internet, harita UI'sı ve tam görsel/ses kabulü hâlâ kapanış öncesi test edilmelidir.

## Birleşik uçtan uca senaryo (tam kapı açıldığında koşulacak)

Gerçek runtime, gerçek owner girdisi/RPC; enjekte edilmiş sonuç yok (bu oturum boyunca kurulan tüm smoke'ların standardı).

1. **Başlangıç.** İki (sonra dört) süreç bağlanır, roster/ready döngüsü, Lobby→Prep→Dive geçişi.
2. **Kasaba hazırlığı.** Prep fazında kasaba/NPC alanına erişim (mevcut sahne modelinde Prep de `DiveTestArea`'da geçiyor — bkz. `SessionNetworkAdapter.SceneForPhase`).
3. **Kıyıdan veya sandalla çıkış.** Kıyıdan yüzerek çıkış (zaten `-Town`/`-Record`/`-Event`/`-Hunt` ile doğrulanmış, PR #71) **ve** sandaldan `Outbound`'a çıkış (yeni, #75/#76 gerektirir).
4. **Av/kamera.** Balık avlama (`-Hunt`), kayıt/çekim (`-Record`/`-Event`), temel kamera satın alma.
5. **Dönüş.** Kıyıya veya iskeleye güvenli dönüş; sandal `Anchored→Inbound→Docked`.
6. **Satış.** Üç NPC hizmeti: ekipman dükkânı, balık alıcısı, kayıt alıcısı — gerçek E/F etkileşimiyle (zaten doğrulanmış, PR #64/#69).
7. **Sandal ilerlemesi/sefer.** Üç parçanın E-pickup ile bulunması (zaten doğrulanmış, PR #72) **ve** gerçek bir sefer döngüsü: bin → `Outbound` → `Anchored` (dalıp geri bin dahil) → `Inbound` → `Docked` (yeni, #75/#76 gerektirir).
8. **Save/reload.** Oyunu kapat, campaign dosyasını yeniden yükle; adım 9'daki kalıcılık maddeleri kontrol edilir.

## Kalıcılık kontrol listesi (kısmen zaten kanıtlı)

| Madde | Durum | Kanıt |
|---|---|---|
| Para, satılmış/ödenmiş kimlikler, ekipman, bekleyen teslimler | ✅ doğrulanmış | #62, `EconomyTests`/`EconomyPersistenceTests`/`EconomyDiskIntegrationTests` |
| `BoatRepairState` (tamamlanmış parça kimlikleri, 0→1→2→3→Repaired) | ✅ doğrulanmış | #62 save şema v2, gerçek 2-süreç `-Boat` smoke (#72) |
| `BoatTripState` restore edilmez; sandal iskelede/koltuklar boş açılır | ✅ doğrulanmış (gerçek `EconomyManager.ExportSaveData`/`TryRestore` ile) | `BoatTripPersistenceTests.cs` (#77), 3 test |
| Route unlock | ✅ karar verildi: **yeni alan yok** — v1'in tek rotasının kilidi zaten `BoatRepairState.Status==Repaired`, #62 ile kalıcı | `docs/plan/CONTRACTS.md` P3.3-C notu madde 3 |
| Gerçek sefer sırasında save/reload (host kopması dahil) | ❌ henüz koşulamaz | #75/#76 gerektirir |

## Duplicate/replay kilit senaryoları (tekrarında koşulacak)

Zaten doğrulanmış olanlar (PASS, gerçek smoke/test ile):
- Aynı sandal parçasının iki kez bulunması → `AlreadyProcessed` (PR #72, gerçek 2-süreç).
- Aynı av/kayıt kimliğinin iki kez satılması/teslimi → `SoldCaptureIds`/`PaidRecordingIds` (#62).
- Ekipman satın almanın iki kez işlenmesi → `AlreadyProcessed` (#62).
- Sefer başlatmanın ikinci kez tetiklenmesi (`Docked` dışı fazda) → `WrongPhase`; aynı `requestId`'nin tekrar gönderilmesi → idempotent replay (`BoatTripTests.cs`, EditMode, henüz gerçek ağ smoke'u değil).

Gerçek kapı açıldığında ayrıca koşulacak:
- İki oyuncunun AYNI koltuğa aynı anda binmeye çalışması (network race, EditMode'da değil gerçek RPC sırasıyla).
- Sefer sorumlusunun sefer sırasında kopması → devir + sonraki geri çağırmanın ikinci sandal üretmemesi.
- Host'un sefer sırasında kapanıp yeniden açılması → save'in `Docked`/boş koltuk döndürdüğünün gerçek reconnect ile doğrulanması.

## Kayıt formatı (P3 kapanış raporu için)

Her koşu için:
- **Ortam:** Unity sürümü, işletim sistemi, build türü (yerel Windows / ayrı bilgisayar / internet).
- **Commit/SHA:** hem test edilen kod hem de (varsa) build'in üretildiği tam commit.
- **Süreç sayısı:** solo / 2 süreç (host+rejoin) / 4 süreç (+ 5. reddedilen + geç katılan reddedilen).
- **Adım adım PASS/FAIL:** yukarıdaki uçtan uca senaryonun her adımı ayrı satır; "denenmedi" ile "FAIL" karıştırılmaz.
- **Bilinen açıklar:** o koşuda doğrulanmayan her şey açıkça listelenir (bu oturumun tüm PR'larında zaten uygulanan format — örn. PR #64, #69, #71, #72, #77).
- **Ek dosyalar:** `Logs/P1-integrated-*/*.json` ham rapor + varsa ekran görüntüsü/klip.

## Görsel/ses tutarlılık audit'i — ilk geçiş (2026-09-23/24)

`-Town -Capture` ile gerçek entegre build'den alınan ekran görüntüsü (`Logs/P1-integrated-20260923-235239-562-2/room.png`, commit `9ca6b07` — yani #77 birleşti, **#78 henüz birleşmedi**):

- **Bulgu:** Ekranın alt kısmında iki adet **magenta/pembe kapsül şekli** — Unity'nin "shader bulunamadı/derlenemedi" hata materyali rengi. Gerçek, somut bir görsel kusur; placeholder olarak "bilinip kabul edilen" bir şey değil.
- **Kök neden değerlendirmesi:** Yerel Mixamo FBX dosyaları diskte mevcut (`Assets/DeepDive/Player/Character/*.fbx`, 15 Eylül tarihli) ve `DiverPresentationCatalog.asset` commit edilmiş haliyle temiz (diff yok) — yani "yerel kurulum eksik" değil. Açık PR **#78** ("P3: ortak insan modeli, gerçek eller ve elde kamera", Mehmet, `codex/p3-playability-visual-repair`, henüz merge edilmedi) tam olarak bunu hedefliyor: "yerel eller ilkel geometri ve hatalı materyalle görünebiliyordu" — kendi açıklamasında bu tür bir sorunu tarif ediyor. **Bu benim üzerime yeniden üretmiyorum** (#79'un kendi kuralı: "Mehmet/Utku alanındaki prefab/world bağlantı hatalarını kendin ikinci kez uygulama"); #78 merge olunca aynı sahne yeniden yakalanıp doğrulanacak.
- **Denetlediğim ve sorun bulmadığım:** Proje URP kullanıyor ve `QualitySettings.asset`'te gerçek bir `customRenderPipeline` atanmış (guid `57211af7dbb94ea40a44550efda154d9`) — yani `TownServiceSetup`/`DiveTestAreaSetup`/`DiveTestAreaWaterSetup`'taki `Shader.Find("Universal Render Pipeline/Lit")` çağrıları doğru pipeline'ı hedefliyor, "yanlış shader" riski yok.
- **Kasaba/NPC/sandal sunumu (Mert-owned, V05/V06):** Kod incelemesiyle (henüz ikinci bir gerçek ekran görüntüsü almadım — bu ilk geçişin kapsamı sınırlı) NPC'ler ilkel kapsül+küre+küp (`TownServiceSetup`), sandal onarım durumu yalnız IMGUI metin (`EconomyPlayerSync`/`BoatTripPlayerSync`) — ikisi de daha önce PR'larda **bilinen, kabul edilmiş placeholder** olarak işaretlendi (PR #64, #69), "eksik referans" değil, "henüz gerçek sanat yok" durumu. #79'un ayırt etmek istediği kategori tam olarak bu: placeholder olduğu bilinen/kayıtlı şey blocker değildir; magenta materyal gibi **bilinmeyen/kayıtsız** bir kırıklık blocker'dır.

**Sonraki adım (bu audit'in devamı, ayrı küçük paket):** #78 merge olduktan sonra aynı `-Town`/`-Record`/`-Event` sahnelerini yeniden yakalayıp (a) magenta materyalin gittiğini doğrulamak, (b) kıyı/sualtı ışık-sis ayrımını (PR #78'in iddia ettiği) gerçek ekran görüntüsüyle teyit etmek, (c) kasaba/NPC ve sandal sahnelerinden de en az birer kare almak.

## Bu doküman ne DEĞİL

- P3 kapanış raporu değil (kapı kapalı, kapanamaz).
- #66'nın yerine geçmez; #66 kendi PR'ında (#77) ilerliyor.
- Mehmet/Utku'nun prefab/world sorunlarına bir düzeltme değil, yalnız kanıt ve yönlendirme.
