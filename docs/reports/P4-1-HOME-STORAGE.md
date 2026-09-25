# P4.1-C ikinci teslim: ortak ev deposu (#90)

Tarih: 2026-09-25. Taban: `p4/mert-home-day-state` (PR #91) üstüne yığılı; #91 birleşmeden bu dal PR olmaz.

## Karar (mevcut modelle uyumlu)
- Depo, **güvenli ve henüz ödenmemiş av**ı (`PendingTurnIn`, yalnız `Catch`) evde park eder; kayıtlar (klip) P4.2 PC arşivinin işidir, depoya girmez.
- Bir eşya **tek yerde**: bekleyen/taşınan **veya** depoda. `TryStoreItem`/`TryRetrieveItem` tek atomik adım (bir listeden çıkar, diğerine ekle, kaydı yaz; yazma başarısızsa ikisi de geri alınır ve aynı requestId yeniden denenebilir). Depodaki eşya `_pending`'de olmadığından hiçbir NPC onu ödeyemez; satılmış bir av depoya girmez.
- Sahiplik mevcut bekleyen-eşya kuralıyla aynı: taşıyan oyuncu veya paylaşılan emanet. Geri alan oyuncu eşyanın taşıyıcısı olur; **taşıma kapasitesi** (`InventoryManager.CapacityGrams`) bu oyuncunun taşıdığı ödenmemiş av ağırlığına uygulanır (`InventoryFull`). 40 yuva (`StorageFull`).
- `(oyuncu, requestId, işlem)` tekrarı ilk cevabı döner; `DayLock` kapanışta depoyu kilitler (`DayClosing`, önbelleğe alınmaz).
- Kayıt: `EconomySaveData.StoredItems` (v3, hep paylaşılan; D06). Bozuk/elle düzenlenmiş dosya bir eşyayı iki yerde ya da ödenmiş olarak diriltemez.
- Fiziksel depo etkileşimi Mehmet'in (#88): `HomeStorageInteraction.TryStore/TryRetrieve` (Core dikişi), `DayNetworkBinding` host'ta bağlar. HUD: `DEPO: n AV`.

## Kanıt
- EditMode **687/687** (13 yeni `HomeStorageTests`, gerçek `EconomySaveStore` ile diske yazma/yeniden yükleme, başarısız yazma, kurcalanmış dosya dahil).

## Doğrulanmadı
- Gerçek 2-süreç depo smoke'u yok: depoya girecek av gerçek bir güvenli dönüş gerektirir, fiziksel depo nesnesi/etkileşimi #88'e bağlı. Depo yalnız EditMode + gerçek dosya ile kanıtlı.
