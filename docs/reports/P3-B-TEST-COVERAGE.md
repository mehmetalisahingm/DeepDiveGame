# P3-B test envanteri — madde 4'ün mevcut karşılığı

Bu bir faz kapanış kaydı değildir. Mehmet'in P3-B görev listesindeki 4. maddenin
("test desteği: engel arkası çekim, tekrar ödül, güvenli dönmeyen kayıt sahibi")
bugün hangi testlerle zaten karşılandığını, hangi kısmının açık kaldığını gösterir.

- Dilim / tarih: P3-B özel olay dilimi, envanter — 14 Eylül 2026
- Sahip: Utku (B)
- Kaynak: `p3/utku-recording`, `codex/p3-integration` (`8c93138`) üzerine rebase edildi
- Ortam: Unity 6000.3.23f1, EditMode
- Son tam EditMode koşusu: **315/315 PASS**
  (`Unity.exe -runTests -batchmode -testPlatform EditMode`, 14 Eylül 2026). Rebase tabanı
  262 idi; özel olay dilimi 53 test ekledi (`SpecialEventWindowTests` 18,
  `SpecialEventScheduleTests` 12, `SpecialEventCycleTests` 16,
  `RecordingEventDefinitionTests` 12, `BioluminescenceEventAssetTests` 6,
  `RecordingSessionTests` 21→28).

  **Kapsam dışı:** `SpecialEventRunner` (NGO kabuğu) EditMode'da doğrulanmıyor — depoda
  NetworkManager başlatan veya `NetworkObject` spawn eden test altyapısı yok. Kabuğun
  doğrulaması Adım 6 sahne testinde ve uçtan uca koşuda olacak.

## Özet

| Mehmet'in senaryosu | Kural katmanı | Sahne katmanı | Uçtan uca (gerçek oyun) | Kredi |
|---|---|---|---|---|
| Engel arkası çekim | 4 test, geçiyor | 2 test, geçiyor | yok | yok |
| Tekrar ödül | 14 test, geçiyor | — | yok | yok |
| Güvenli dönmeyen kayıt sahibi | 14 test, geçiyor | — | kısmi | yok |

Üç senaryonun da **kural katmanı karşılığı yazılmış ve geçiyor**. Kalan iş, aşağıdaki
"Kapsanmayan" bölümündeki maddelerdir; bir kısmı başka kişilere bağlıdır.

"Kısmi" şu demek: `tools/Test-P1-Integrated.ps1 -Record` (Mehmet, `-p3-record 1`) dört
gerçek süreçle bir misafirin çekim isteğini ve yüzeye güvenli dönüşünü geçiriyor
(PASS: `Logs/P1-integrated-20260913-231037-669-4`). Bu, kayıt yolunun uçtan uca çalıştığını
gösterir; **boğulan kameraman → sıradaki güvenli kayıt** düşüşünü ayrıca doğrulamaz.

## 1. Engel arkası çekim

| Test | Yer | Ne kilitliyor |
|---|---|---|
| `OcclusionRejectsEvenAPerfectlyFramedSubject` | `RecordingFramingTests.cs:43` | Kadraj kusursuz olsa bile araya giren geometri örneği geçersiz kılar |
| `EveryRejectionScoresZeroSoNoTimeCanBeBankedOffIt` | `RecordingFramingTests.cs:111` | Reddedilen her örnek 0 puan; süreye dönüştürülebilecek bir artık kalmıyor |
| `FramesTheRulesRejectedBankNoTime` | `RecordingSessionTests.cs:92` | Engelli kareler çekim süresine eklenmiyor |
| `OnlyTheValidStretchOfALongTakeCounts` | `RecordingSessionTests.cs:106` | Uzun bir çekimin yalnız geçerli bölümü sayılıyor |
| `AnOccludedShotNeverCountsHoweverGoodTheFraming` | `DiveTestAreaRecordingSceneTests.cs:241` | Aynı kural gerçek DiveTestArea geometrisiyle |
| `TheOccluderMaskCoversTheArenaGeometryAndNothingMore` | `DiveTestAreaRecordingSceneTests.cs:184` | Maske arenayı engel sayıyor, dalgıcın kendi gövdesini veya hedefi saymıyor |

Çalışan kod: `RecordingSubject.IsOccluded` — host tarafında linecast, hedefin kendi
hiyerarşisindeki çarpışmaları atlar (yoksa her çekim reddedilirdi).

## 2. Tekrar ödül

| Test | Yer | Ne kilitliyor |
|---|---|---|
| `AReplayedStartReturnsTheEarlierResultWithoutRestartingTheTake` | `RecordingSessionTests.cs:212` | Tekrar edilen start ikinci çekim açmıyor |
| `AReplayedStopReturnsTheEarlierResultWithoutProducingASecondEvaluation` | `RecordingSessionTests.cs:226` | Tekrar edilen stop ikinci değerlendirme üretmiyor |
| `AReplayedStopRegistersNothingASecondTime` | `RecordingDirectorTests.cs:202` | Tekrar edilen stop'un boş take'i ledger'a girmiyor (IsPayable koruması) |
| `AQualityZeroTakeIsAcceptedButEarnsNothing` | `RecordingDirectorTests.cs:190` | Kalite 0 çekim kabul ediliyor ama ödül adayı olmuyor |
| `ARefusedStopIsNeverRegisteredEvenWithAPayableTake` | `RecordingDirectorTests.cs:236` | Reddedilmiş stop, elinde ödenebilir take olsa da kayıt açmıyor |
| `OnlyTheBestTakePerPlayerAndSubjectIsKept` | `RecordingLedgerTests.cs:40`, `RecordingDirectorTests.cs:251` | Aynı oyuncu/tür için çoklu çekim tek kayda iniyor |
| `SettlingTwiceDoesNotPayTwice` | `RecordingLedgerTests.cs:136`, `RecordingDirectorTests.cs:314` | Dalış iki kez kapatılırsa ikinci ödeme yok |
| `RegisteringAfterSettlementIsRefused` | `RecordingLedgerTests.cs:149` | Ödeme sonrası gelen kayıt kabul edilmiyor |
| `AStopAfterSettlementCannotReopenTheDive` | `RecordingDirectorTests.cs:347` | Kapanmış dalış geç gelen stop'la yeniden açılmıyor |
| `EveryPayoutGetsItsOwnRecordingId` | `RecordingLedgerTests.cs:233` | İki ödeme aynı recordingId'yi paylaşmıyor |
| `ATakeThatOutlivesItsDiveIsVoid` | `RecordingSessionTests.cs:252` | Önceki dalışta başlayan çekim sonrakinde bozuluyor |
| `ATakeStoppedAfterTheDiveEndedIsVoid` | `RecordingSessionTests.cs:266` | Dalış bittikten sonra kapatılan çekim geçersiz |
| `TakesStampedWithAnotherDiveAreNotPaid` | `RecordingLedgerTests.cs:185` | Yabancı diveId taşıyan çekim ödenmiyor |
| `ATakeStampedWithAnotherDiveNeverBecomesAClaim` | `RecordingDirectorTests.cs:219` | Aynı koruma Director kabuğunda |

Taşıma katmanında ayrıca Mehmet'in `RecordingRequestRulesTests` dosyası var
(`Assets/Tests/P3A/Editor/`, 9 test): requestId sırası ve yinelenen istek kapısı.

## 3. Güvenli dönmeyen kayıt sahibi

| Test | Yer | Ne kilitliyor |
|---|---|---|
| `SettlementPaysOncePerSubjectToTheBestDiverWhoSurfaced` | `RecordingLedgerTests.cs:76` | Tür başına tek ödeme, güvenli dönen en iyi kayda |
| `WhenTheBestCameramanDrownsTheNextBestSurvivorIsPaid` | `RecordingLedgerTests.cs:95` | En iyi kaydın sahibi dönemezse sıradaki güvenli kayıt |
| `ADrownedCameramanLosesTheSubjectToTheNextBestSafeRecording` | `RecordingDirectorTests.cs:293` | Aynı kural Director kabuğunda, `DiveSummary` ile |
| `IfNobodyWhoFilmedItSurfacedNobodyIsPaid` | `RecordingLedgerTests.cs:111` | Filme alanların hiçbiri dönmediyse ödeme yok |
| `ADiveWhereNobodySurfacedPaysNothing` | `RecordingLedgerTests.cs:125` | Kimsenin dönmediği dalış hiçbir şey ödemiyor |
| `NobodyIsPaidBeforeTheDiveSettles` | `RecordingDirectorTests.cs:272` | Stop anında kimseye para gitmiyor; ödeme güvenli dönüşten sonra |
| `TiesAreBrokenTheSameWayEveryTimeSoHostsAgree` | `RecordingLedgerTests.cs:247` | Beraberlik çözümü deterministik |
| `BetterMeansHigherTierThenBetterLookingThenLonger` | `RecordingLedgerTests.cs:259` | "Daha iyi kayıt"ın sırası sabit |
| `ARefusedClaimIsNotReportedAsPaidAndDoesNotFallToTheRunnerUp` | `RecordingLedgerTests.cs:199` | Ekonomi reddederse ödendi sayılmıyor, ikinciye de kaymıyor |
| `AnUnboundEconomyPaysNothingAndLeavesTheLedgerOpen` | `RecordingLedgerTests.cs:158` | Ekonomi bağlı değilken kayıtlar yakılmıyor |
| `WithoutABoundEconomyTheDiveStaysOpenForALaterSettlement` | `RecordingDirectorTests.cs:330` | Aynısı Director kabuğunda |
| `AnEconomyRefusalStillClosesTheSubjectForThisDive` | `RecordingDirectorTests.cs:385` | Red, aynı dalışta tekrar denemeye kapı bırakmıyor |
| `AMissingDiveIdPaysNothingAndLeavesTheLedgerOpen` | `RecordingLedgerTests.cs:172` | diveId'siz kapanış ödeme üretmiyor |
| `ThePaidResultCarriesEveryFieldMertNeeds` | `RecordingLedgerTests.cs:214` | `RecordingResult` alanları sözleşmeye uygun |

Composition seviyesinde Mehmet'in `RecordingDiveBindingTests` dosyası aynı zinciri
`SessionManager` + `InventoryManager` ile kuruyor (`SummaryPaysNextBestSafeRecorderOnce_NotOnStop`,
`MissingBackendRetainsOldSettlementWithoutPollutingNextDive`,
`NonHostCannotBindEvaluationViewsOrPayment` dahil). `7076906`, PR #41 ile
`codex/p3-integration`'a girdi.

## Kapsanmayan

1. **Kredi seviyesinde doğrulama yok.** Testler "ödenebilir `RecordingResult` üretildi"yi
   kanıtlıyor, "ortak kasaya kredi yazıldı"yı değil. Kayıt ödeme backend'i bağlı değil —
   `Assets/DeepDive/Economy/EconomyManager.cs:18`. Bağlanacağı yer hazır ve isimli:
   `RecordingWorldBinding.SetPaymentHandler(Func<RecordingResult, PlayerActionResult>)`
   (içeride `RecordingDiveBinding`'e deleg eder). **Mert'e bağlı.**
2. **Uçtan uca test üç senaryoyu ayrı ayrı zorlamıyor.** `tools/Test-P1-Integrated.ps1 -Record`
   kayıt yolunu gerçek oyunda geçiriyor ama engel arkası çekimi, tekrar ödül denemesini veya
   boğulan kameraman düşüşünü senaryo olarak kurmuyor. Bu üçünün oyun içi karşılığı henüz yok.
3. **Özel olaya özgü varyant yok.** Olay nesnesi henüz yok; olay tanımlandığında üç senaryonun
   olay hedefiyle tekrarı gerekir (özellikle "olay bitmişken/başlamamışken çekim").
4. **Olay henüz sahnede yok.** Kural katmanı ve tanım hazır — `SpecialEventWindow` +
   `SpecialEventSchedule`, `RecordingSession`'a `IRecordingWindow` ile bağlı, `Bioluminescence`
   tanım asset'i üretildi. Eksik olan NGO kabuğu, presenter ve sahne bağlantısı (Adım 4-6);
   o gelene kadar olay uçtan uca oynanamaz.

## Bu envanterin kanıtlamadığı şey

Bu not EditMode kural kapsamıdır. Gerçek oynanış, internet üzerinden çok oyunculu test veya
ekonomi entegrasyonu yerine geçmez. Madde 4 "bitti" demek için yukarıdaki 1. ve 2. maddeler
gerekir: 1 Mert'in ödeme API'sine bağlı, 2 bende.
