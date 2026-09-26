# P4.1-C ikinci teslim: ortak ev deposu (#90)

Tarih: 2026-09-26. Taban: `p4/mert-home-smoke` (PR #93) üstünde; #93 birleşince `codex/p4-integration`'a alınır.

## Karar (mevcut modelle uyumlu)
- Depo, **güvenli ve henüz ödenmemiş av**ı (`PendingTurnIn`, yalnız `Catch`) evde park eder; klipler P4.2 PC arşivinin işidir, depoya girmez.
- Bir eşya **tek yerde**: bekleyen/taşınan **veya** depoda. `TryStoreItem`/`TryRetrieveItem` tek atomik adım (bir listeden çıkar, diğerine ekle, kaydı yaz; yazma başarısızsa ikisi de geri alınır ve aynı requestId yeniden denenebilir). Depodaki eşya `_pending`'de olmadığından hiçbir NPC onu ödeyemez; satılmış bir av depoya girmez.
- Sahiplik mevcut bekleyen-eşya kuralıyla aynı: taşıyan oyuncu veya paylaşılan emanet. Geri alan oyuncu eşyanın taşıyıcısı olur; **taşıma kapasitesi** (`InventoryManager.CapacityGrams`) bu oyuncunun taşıdığı ödenmemiş av ağırlığına uygulanır (`InventoryFull`). 40 yuva (`StorageFull`).
- `(oyuncu, requestId, işlem)` tekrarı ilk cevabı döner; `DayLock` kapanışta depoyu kilitler (`DayClosing`, önbelleğe alınmaz).
- Kayıt: `EconomySaveData.StoredItems` (v3, hep paylaşılan; D06). Bozuk/elle düzenlenmiş dosya bir eşyayı iki yerde ya da ödenmiş olarak diriltemez.
- **Seam ayrımı:** Mehmet'in `HomeStorageInteraction.Bind(open)` / `TryOpen` (fiziksel açılış, #92) aynen kalır; eşya koy/al ayrı bir seam: **`HomeStorageItems`** (Core). Composition ikisini de host'ta bağlar. Koy/al isteği host'ta, oyuncunun gerçek konumunun depo hedefine yakınlığı doğrulanarak işlenir (`NotAtStorage`); istemci hangi eşyayı istediğini söyler, "yakınım" demez.
- İstemci: `EconomyPlayerSync` host'un yazdığı iki liste taşır (taşıdığım av / depodaki av); `HomeStorageView` (IMGUI) depoya yakınken bu listeleri gösterir ve `KOY`/`AL` isteği gönderir. Panel hiçbir eşya durumu tutmaz.

## Kanıt
- EditMode **691/691** (13 `HomeStorageTests` dahil).
- **Gerçek 2-süreç `tools/Test-P1-Integrated.ps1 -Players 2 -Storage` smoke: host PASS, client1 PASS.** Gerçek güvenli dönüşle sıraya giren 2'şer av (town smoke'undaki gibi envantere eklenip güvenli dönüş işaretlenir; gerçek dalış özeti bekleyen kaydı üretir) → eve dönüş → her süreç depoya yürür, bakar, `H` (Mehmet'in fiziksel açılışı kabul edilir) → panel açık → koy (taşınan 2→1, depoda 1) → aynı eşyayı tekrar koy (**reddedilir**, `InvalidTarget`) → geri al (taşınan 2, depoda 0) → ikisini de koy. Depodan uzakta gelen koy isteği **host tarafından reddedildi** (`NotAtStorage`, misafirde gözlendi). Host: yetki, kayıt dosyası (`StoredItems`) ve yeniden yükleme (`LoadNow`) aynı — `2×oyuncu` av depoda, bekleyen 0.
- Aynı build'de `-HomeSleep`, `-Day` (yeniden açılan host dahil) ve `-Trip` PASS.

## Doğrulanmadı
- Depodan alınan avın NPC'de satışı gerçek smoke'ta koşulmadı (satış EditMode'da: `RetrievingPutsItBackToBeSoldAndItPaysExactlyOnce`; NPC kıyıda, depo evde).
- Panelin görsel çizimi (IMGUI; headless build çizmez, açık/kapalı durumu sayısal doğrulandı), 4 süreç, ayrı bilgisayar.
- Yakınlık doğrulaması iki yerde ayrı (Mehmet'in `H` ışını, benim koy/al mesafe kontrolü); tek bir "depo açık" oturumu tutulmuyor — ortak bir açık-oturum modeli Mehmet'le konuşulacak bir tasarım kararı.
