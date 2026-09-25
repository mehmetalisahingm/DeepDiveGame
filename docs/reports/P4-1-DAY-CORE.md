# P4.1-C ilk teslim: ortak gün, uyku, 00:00 kapanışı, gün özeti, kayıt (#90)

Tarih: 2026-09-25. Taban: `codex/p4-integration` @ `38a0124`. Dal: `p4/mert-home-day-state`.

## Ne var
- `DayEngine` (`Assets/DeepDive/Day/`, saf C#, host-only): saat, yatak kapısı, 00:00, sabit kapanış sırası (kabul edilmiş eylemler → dalış sonuçları → özet → atomik yazma → sabah), tek özet, `closeId = close-day-N`.
- Sözleşmeler `Assets/DeepDive/Core/P4DayContracts.cs`: `CampaignDayState`, `DaySummary`, `IDayRoster`, `IDayCloseHooks`, `DayLock`, `HomeBedInteraction` (Mehmet'in fiziksel yatak katmanı #88 için dikiş), `DaySaveData`.
- `DayNetworkBinding` (Composition): aktif oyuncu = bağlı + pasif olmayan + oturum Lobby/yükleme değil; açık dalışı mevcut oturum geçişi ve envanter kayıp kuralıyla (D07) kapatır; gerçek satış/harcama/tekne ilerlemesi gün defterine akar; durum `EconomyPlayerSync` üzerinden herkese yansır (HUD: gün, saat, uyku n/m, 22:00/23:00 uyarısı, özet paneli).
- Kayıt: `EconomySaveData` v3 (`HasDay` + `Day`); gün, para ve tekne **aynı dosyada**. Kapanış yazması sırasında export zaten *ertesi günü + özeti + closeId'yi* adlar; yazma başarısızsa gün ilerlemez ve aynı closeId ile tekrar denenir. v1/v2 dosyalar "gün 1, 08:00" olarak yüklenir.
- `DayLock`: kapanış başlayınca ekipman/satış/tekne parçası/sefer başlatma `DayClosing` ile reddedilir (önbelleğe alınmaz).

## Kanıt
- EditMode **674/674** (36 yeni: `DayEngineTests`, `DayPersistenceTests` — gerçek `EconomySaveStore`, gerçek dosya, ikinci "boot").
- Gerçek 2-süreç smoke: `tools/Test-P1-Integrated.ps1 -Players 2 -Day` → host ve client1 `passed: true`; ardından **gerçek ikinci host başlatması** aynı kayıt dosyasında `host-reload passed: true`.
  - Gün 1: host yalnız saat *hızını* artırır (zamanı değil); açık dalış 00:00'da kapanır, `Midnight`, `close-day-1`, iki dalgıç geri dönemedi (özet `LostDivers=2`, iki süreçte de aynı), tek özet, diskte gün 2.
  - Aynı kapanışın tekrar yüklenmesi (`LoadNow`) günü ilerletmez; gün 1'in `closeId`'siyle gelen tekrar isteği gün 2'yi kapatmaz.
  - Gün 2: host tek başına yatakta → **kapanmaz** (bağlı misafir uyanık); misafirin yatağı da girince `EarlySleep`, `close-day-2`, gün 3. İki süreç de gün sırasını `1,2,3`, özet sırasını `1,2` gözlemler.
  - Yeniden açılış: gün 3, 2 özet, saat 08:00, aynı closeId'ler.
- Smoke'un bulduğu gerçek hata (düzeltildi): motorda "başka günün closeId'si" ayrımı yoktu; geçmiş güne ait tekrar isteği koşan günü kapatabiliyordu → `BeginClose(reason, requestedCloseId)`.

## Doğrulanmadı / sıradaki
- **Fiziksel yatak** Mehmet'in #88'i; smoke yatağa `HomeBedInteraction` dikişiyle girer (yakınlık doğrulaması yok). Sahne/yatak nesneleri yok.
- 4 süreç ve ayrı bilgisayar; 00:00'da tekne/dış demirlemede kalanın "güvenli değil" kuralı (tekne fiziksel durumu Mehmet/Utku); sabah yerleşimi (`DayNetworkBinding.MorningBegan` olayı Mehmet'in).
- Ortak depo, keşif haritası + ansiklopedi UI'ı, Utku'nun keşif hücresi/tür verisi (#89) — sonraki paketler.
