# #255 — Kanal kuralları: 10 milyon hedefi için ölçüm ve öneri (5 Kasım 2026)

Durum: **ÖNERİ + ölçüm; uygulanmış denge değildir.** Sahibi Mert (kanal/ekonomi/save, #255); ortak sözleşme Utku (#254 doğrulanmış balık örneği/ağırlığı) ve Mehmet (#253 gerçek klip, depolama) ile koordine edilmeden `ClipManifest`, `PublicationSave` veya kayıt şeması değişmedi. Bütün sayılar çalışma değeridir; 10M'e ulaşma süresi sözü verilmez (`CHANNEL_GOAL_10M.md`).

## Ölçüm: mevcut kurallarla hedefe ulaşılamıyor

Mevcut `ChannelResultRules.Evaluate`: izlenme = `40·q²` (yeni konuda ×1,5) + takipçi/2; abone kazancı = izlenme/20; gelir = izlenme/4. `ChannelAuthority.MaxArchivedClips = 128`: arşiv doluyken yeni klip arşivlenmez (`RecordingMediaCaptureBinding` dosyayı siler), yani yayınlanamaz. Kampanya boyunca en fazla 128 klip yayınlanabilir.

`ChannelGoalTests.CurrentRulesCannotReachTheGoalWithinTheArchiveCap` bunu gerçek kural fonksiyonuyla simüle eder (EditMode; çıktı satırları `P411_CHANNEL_CEILING` ve `P411_CHANNEL_UNCAPPED_REACHED_AT`):

| Durum | Sonuç |
|---|---|
| 128 platin (q4) klip, ilk beş konu yeni | **30.252 abone, hedefin %0,30'u** |
| Arşiv sınırı olmasa, aynı koşul | 10.000.000'a **362. yayında** ulaşır |

Yani sınır, hedefi kampanya içinde ulaşılamaz kılıyor. Bu bir denge iddiası değil, mevcut davranışın ölçümü. Kural veya sınır değişince bu test gözden geçirilecek test.

## Bu PR'da ne var (kayıt şeması ve ödeme değişmedi)

- `ChannelGoal`: 10.000.000 hedefi ve altı kilometre taşı (100, 1.000, 10.000, 100.000, 1.000.000, 10.000.000), ilerleme oranı, sonraki kilometre taşı, "hedef tamam" durumu (oyun bitmez, kanal büyümeye devam eder). Girdi yalnız host'un takipçi sayısı; istemci aynısını yansıtır.
- Ev PC paneli (`HomePcView`): "KANAL" satırı artık hedef yüzdesini, sonraki kilometre taşını ve bir ilerleme çubuğunu gösterir. Çizelge doğrusal: başlangıç ilerlemesi küçük görünür, 10 milyonun dürüst ölçeği budur.
- `ChannelEvidenceRules` (**bağlı değil**): büyük gerçek av ve eşleşen çekim için izlenme çarpanı önerisi. Doğrulanmış ağırlık türün kendi aralığında %100–150; aynı oyuncunun aynı hedefinin doğrulanmış avıyla eşleşen çekim +%25; yalnız çekim (eşleşmeyen) bugünkü kural (%100); yalnız av kanala girmez, bugünkü gibi alıcıya satılır. Doğrulanmamış ağırlık (0) nötr: büyük görünen yakın çekim ağırlık sayılmaz. Bonus yoksa `ChannelResultRules.Evaluate` ile aynı sonucu verir (testle kanıtlı).

## Ortak sözleşme için açık sorular (kimin kararı)

1. **Arşiv sınırı (Mehmet #253, Mert):** yayını sonuçlanmış klibin medya dosyası silinip kayıt defteri (konu, yayın, abone) saklanabilir mi? Depolama sınırıyla birlikte karar verilmeli; kayıt şeması değişir.
2. **Büyüme eğrisi (Mert):** yalnız sınırı kaldırmak 362 platin yayın demek, yani yine çok uzun; çarpan/kilometre taşı bonusu gibi bir eğri gerekir. Hangi sürede ulaşıldığı çok günlük gerçek oyunla ölçülmeden söz verilmez.
3. **Doğrulanmış balık (Utku #254):** klip bildirimine host'un doğruladığı av kimliği ve ağırlık gerekir (`ClipManifest`/dünya bağlamı). Sırf tür kimliği ağırlık değildir.
4. **Eşleşmenin tüketimi (Mert, kayıt):** eşleşme bonusu bir avla ilk yayında harcanmalı; harcandığı bilgisi `PublicationSave`'e yeni alan olarak yazılacak. Eski kayıtlarda alan boş (0) = bonus yok, eski sonuçlar değişmez.
5. **Aynı klip/av/ödül tekrarı:** mevcut yayın kimliği (`PublicationId` klipten türetilir), tek ticari hak ve `SettleId` korunur; bu öneri onları gevşetmez.

## Denenmedi

- Gerçek iki oyuncu: av → gerçek kayıt → PC'de yayın → ertesi gün sonuç → reload (#255 kabulü); kural bağlanmadığı için ürün koşusu yok.
- Ev PC panelinin gerçek Windows build'indeki görüntüsü (yalnız derleme ve EditMode); ayrı PC/insan kabulü ve #164 açık.
