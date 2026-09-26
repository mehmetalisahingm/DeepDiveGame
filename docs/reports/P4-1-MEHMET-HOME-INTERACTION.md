# P4.1-A — Mehmet ev/uyku/ping entegrasyonu (#88)

## Kapsam

- `PrepArea` ortak ev odasına dört kararlı yatak (`bed-0..bed-3`) ve fiziksel ortak depo hedefi eklenir.
- Oyuncu `H` ile baktığı yatak/depo nesnesine fiziksel olarak etkileşir. İstemci hedef/bed id seçmez; host kendi `NetworkPlayer` görüşü, mesafesi ve gerçek collider üzerinden hedefi çözer.
- Yatak isteği yalnız Mert'in `HomeBedInteraction -> DayEngine` seam'ine gider. İkinci gün/saat/uyku otoritesi yoktur.
- Uyuyan oyuncunun kimliği/bed id'si named-message mirror ile tüm istemcilere taşınır. Aynı yatak/oyuncu kuralları ve replay idempotency `DayEngine` tarafından korunur.
- Son oyuncunun yatağa girmesi aynı çağrı içinde günü kapatabildiği için `Morning` sonucu sonrası stale sleeping state yeniden yazılmaz.
- Gün kapanışı kalıcı yazmada beklerse oyuncu input/recording durumu dondurulur; sabah tüm oyuncu transient presentation/vitals durumu normalize edilir.
- `DayNetworkBinding.MorningBegan` geldiğinde `BoatTripManager.ResetToDocked()` kullanılır; yeni tekne otoritesi kurulmaz. Gerekirse host `PrepArea` sahnesini yükler ve oyuncuları dört ayrı home spawn noktasına taşır.
- `P` host-doğrulamalı harita ping'idir. Host aim ray'ini çözer; Utku'nun `DiveRegionField` dönüşümü hedefi kabul etmiyorsa nokta clamp/guess edilmez.
- `P4MapPositionFeed`, Mert'in P4 haritası için yalnız server-authoritative replicated `NetworkPlayer` konumlarını, host-published `BoatTripPlayerSync` tekne pozunu ve Utku'nun world->map dönüşümünü sunar.
- Depo fiziksel doğrulaması Mehmet katmanındadır; gerçek ortak depo state'i Mert'in henüz bağlayacağı `HomeStorageInteraction` seam'indedir. Seam bağlı değilken işlem açıkça `StorageUnavailable` döner.

## Kontroller

- `H`: yatak/depo fiziksel etkileşimi, yataktayken tekrar `H`: kalk.
- `P`: harita ping'i (8 saniye).
- `M`: mevcut P3 haritasını aç/kapat; ping overlay aynı harita koordinatını kullanır.

## Otomatik regresyon

`HomeInteractionContractTests` şu sözleşmeleri kilitler:

- dört yatak kimliği kararlı ve benzersiz,
- depo otoritesi bağlı değilken fiziksel katman kendi state'ini üretmez,
- bağlandığında player/request bilgisi gerçek depo sahibine aynen iletilir.

## Henüz gerçek runtime kanıtı olmayanlar

Bu branch uzaktan kodlandı; Unity EditMode ve gerçek 2-process smoke sonucu henüz çalıştırılmış kabul edilmez. Merge kararı öncesi branch üzerinde en az:

```powershell
.\tools\Build-P1.ps1 -Tests
.\tools\Test-P1-Integrated.ps1 -Players 2 -Port 18782
```

ve manuel olarak iki oyuncunun farklı yataklara girip çıkması, son uyuyanla günün tek kez kapanması, sabah stale sleep/seated state kalmaması ve `P` ping'inin iki istemcide aynı yerde görünmesi doğrulanmalıdır.
