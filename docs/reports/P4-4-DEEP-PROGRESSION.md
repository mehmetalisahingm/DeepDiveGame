# P4.4-C: ansiklopedi → söylenti → iz → keşif → boss unlock progression ve kayıt (#123)

Tarih: 2026-10-06. Taban: #119 dalı (`p4/mehmet-final-route-fleet-integration`, `ad96af4`). Dal: `p4/mert-deep-progression-save`. Kurallar: `docs/plan/CONTRACTS.md` "Boss, görev ve araştırma" altındaki *Uygulama (#123)*.
**Başlama kapısı:** P4.3 final kabulü (#119 merge, #107/#108) gelmeden final merge/kapanış yok; bu PR hazırlık + bağımsız kanıt.

## Ne var
- **Tek kaynak** (`Progression/DeepProgressionAuthority`, saf C#, host): `Locked → Encyclopedia → Rumor → Trace → Discovery` (Discovery = boss açık). Sıra atlanamaz: söylenti olmadan iz, iz olmadan keşif, keşif olmadan boss açılmaz/tamamlanmaz (`OutOfOrder`).
- **Türetilen aşamalar** (ikinci keşif kaydı yok): ilk host-sayılmış tür gözlemi → ansiklopedi; keşfedilmiş ilk **Reef** hücresi → söylenti. Kanıt kimliği gözlem/hücre kimliğidir; replay ikinci adım üretmez.
- **Host-beslemeli aşamalar**: `DeepProgressionEvidence.TrySubmitTrace/TrySubmitDiscovery` (host seam'i, RPC yok: guest bir iz/keşif bildiremez), bağlı `IDeepProgressionWorld` (Utku'nun #122 kuralı) bağlamı doğrular; **validator bağlı değilken hiçbiri kabul edilmez** (`WorldUnavailable`). 3 farklı iz (`RequiredTraceCount`, çalışma değeri) iz aşamasını, geçerli arena keşfi boss'u açar. Aynı iz kimliği kim bildirirse bildirsin bir kez sayılır.
- **Mehmet için okuma modeli**: `BossProgression.IsAvailable("boss-deep-1")` yalnız tamamlanmış keşiften sonra true (bağlı değilken Locked); encounter bitince `TryCompleteBoss` ile **bir kez** kaydedilir. Geçici encounter durumu (aktif/can/içerideki oyuncular) **kalıcılaştırılmaz**.
- **Kayıt v7** (aynı kampanya dosyası, geç bağlanan otorite): sürümlü `DeepProgressionSaveData`. Eski dosya = kapalı varsayılan. Geri yükleme **aşama sayısına güvenmez**: her aşama kanıt kimlikleriyle desteklenmeli, desteklenmeyen kısım en yüksek tutarlı aşamaya düşer; bilinmeyen sürüm kapalı; sahte boss kimliği / açılmamış tamamlanma atılır. Yazma hatasında adım geri alınır, aynı istek tekrar denenebilir.
- **Ağ/UI**: aşama + iz sayısı her oyuncuya aynalanır (`EconomyPlayerSync`), ansiklopedide (J) tek satır; konum/iz/arena/boss kimliği taşımaz.

## Kanıt
- EditMode tam suite **1085/1085** (21 yeni `DeepProgressionTests`: sıra, türetme, fail-closed validator, replay/idempotency, yazma hatası, gerçek `EconomySaveStore` + dosya ile kaydet/aç, eski dosya, kurcalanmış kayıt, okuma seam'i, oyuncu metni).
- **Gerçek 2-süreç Windows smoke `Test-P1-Integrated.ps1 -Players 2 -Deep`** (+ gerçek ikinci host başlatması), PASS: kilitliyken iz/keşif/tamamlama reddedilir; host'un gerçek sayımı ansiklopediyi açar; host **gerçekten** resif rafına yüzer ve gerçek keşif söylentiyi açar; validator yokken iz reddedilir; yanlış bağlam reddedilir; 3 iz (biri guest adına) iz aşamasını açar, tekrar sayılmaz; yanlış arena reddedilir, doğrusu boss'u açar; tamamlama bir kez; dosya + `LoadNow` doğru. **Guest süreci aşamaları sırayla [0,1,2,3,4] gördü**, boss'un açık ve tamamlanmış olduğunu aynadan okudu. Reload: Discovery, 3 iz, 1 tamamlanma, tekrar iz/keşif/tamamlama `AlreadyProcessed`.
- Aynı build'de regresyon (host + client1 PASS): `-Deep`, `-Fleet`, `-Explore`, `-Day`+reload, `-Town`, `-Trip`, `-Record`, `-Acceptance`+reload, `-Media`+reload, `-Storage`, `-HomeSleep`, `-Boat`, `-Event`; `-Deep` iki kez PASS (toplam 14 koşu).

## Doğrulanmadı / açık
- **Etiketli fikstür**: iz/arena doğrulaması (Utku'nun #122 kuralı yok; smoke'ta `smoke-trace-*` / `smoke-arena`'yı kabul eden bir test validator'ü bağlanıyor) ve ansiklopedi adımı için tür gözlemi gerçek tür otoritesine `AcceptSighting` ile verildi (ürün binding'i aynı otorite; balık gerçekten avlanmadı). Resif keşfi **gerçek**: host'un gerçek yüzüşü. Gerçek derin dünya/iz noktaları/arena (#122) ve gerçek boss encounter'ı (#121) gelince aynı smoke fikstürsüz koşulmalı: bu #124 final acceptance'ın işi.
- Rumor kuralı yalnız "Reef hücresi keşfedildi"; P4.4 yeni derin canlıları gelince söylenti kaynağı (hangi tür/ipucu) Utku/Mehmet ile netleştirilmeli (şu an gerçek tür yalnız `sea_bass`).
- `RequiredTraceCount` = 3 çalışma değeri: Utku en az 3 farklı iz noktası tanımlamalı (kimlikler serbest). Boss kimliği `boss-deep-1` önerilen sabit; Mehmet farklı ister veya ek boss gerekirse tek yerden değişir (save-görünür).
- Boss encounter'ının kendisi (lifecycle, hasar, çoklu oyunculu durum) ve world spawn burada yok (#121/#122). Tamamlanma kaydı için `TryCompleteBoss` seam'i bir **öneri**: Mehmet onaylamalı/ihtiyacına uydurmalı.
- IMGUI satırının görsel kontrolü yapılmadı; 4 süreç ve ayrı bilgisayar denenmedi.
- Bu PR #119'a (ve onun içinde #120'nin tek commit'ine) yığılı.
