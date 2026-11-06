# #255 — Kanal kuralları: 10 milyon hedefi, arşiv ve büyüme (5 Kasım 2026)

Durum: **Uygulandı (çalışma değerleriyle); denge iddiası değildir.** Sahibi Mert (kanal/ekonomi/save, #255). Mehmet'in 5 Kasım kararı: yayınlanmış ve sonucu kesinleşmiş klibin medya dosyası depolama gerektiğinde silinebilir; yayın geçmişi, konu, yayın kimliği, abone, izlenme ve gelir kalıcı korunur; medya sınırı kanalı engellemez; 10 milyon abone gerçekten ulaşılabilir olmalı; büyüme eğrisi ve ekonomi yeniden dengelenir; hedefe ulaşmak oyunu bitirmez; eski kayıt uyumu, tekrar yayın koruması ve yeniden yükleme test edilir. Bütün sayılar çalışma değeridir; hedefe ulaşma süresi sözü verilmez (`CHANNEL_GOAL_10M.md`).

## Önceki ölçüm (değişiklikten önce)

`ChannelResultRules`: izlenme = `40·q²` (yeni konuda ×1,5) + takipçi/2; abone = izlenme/20; gelir = izlenme/4. `MaxArchivedClips = 128` ve dolu arşivde yeni klip arşivlenmeden siliniyordu: en iyi durumda 128 platin klip **30.252 abone, hedefin %0,30'u**; sınırsız olsa bile 362 yayın gerekirdi. Hedef kampanya içinde ulaşılamazdı.

## Ne değişti

1. **Arşiv kanalı engellemez** (`ChannelAuthority`). Arşiv doluyken yeni klip gelince en eski klibin **medyası** atılır: önce yayını sonuçlanmış (settled) klip, yoksa hiçbir zaman yayınlanamayacak (ticari olmayan, yayınlanmamış) klip. Henüz yayınlanabilecek ya da yayınlanıp sonuçlanmamış klip asla atılmaz (böyle bir klipten başka yer yoksa eskisi gibi `ArchiveFull`). Atılan klibin dosyası host'ta silinir (`OnClipEvicted` → `RecordingMediaCaptureBinding.DeleteClipFile`).
2. **Yayın geçmişi kalıcı.** Atılan klibin `PublicationSave` kaydı (kimlik, konu, başlık, izlenme, abone, gelir, settle kimliği) kalır; kayıt dosyasına yazılır ve yeniden yüklenince geri gelir. Kaydın klibi yoksa yalnız o klibin kendi `SettleId`'sini taşıyan sonuçlanmış kayıt geçmiş sayılır; uydurma kayıt hâlâ atılır. Atılmış klip yeniden arşivlenemez (`AlreadyArchived`) ve yeniden yayınlanamaz; ödeme bir kez, `SettleId` ile korunur.
3. **İstemciye giden mirror** (`ExportMirror`): bütün klipler ve yayınları, medyası atılmış kayıtlardan yalnız en yeni 12'si; abone sayısı tam. Kayıt dosyası (`ExportMedia`) tüm geçmişi tutar. Böylece uzun kampanyada ağ mesajı sınırsız büyümez.
4. **Büyüme eğrisi** (`ChannelResultRules`, çalışma değeri): takipçilerin yeni yayınlara katkısı `FeedbackPercent = 120` (önceden 50). Takipçi yokken sonuç eskisiyle aynı (ilk klipler aynı kazanır). Abone aynı oranla: izlenme/20.
5. **Gelir ekonomiye sığar:** gelir = (klibin kendi izlenmesi + en fazla `IncomeFeedbackCapViews = 1200` takipçi kaynaklı izlenme) / 4. Büyük kanal izlenmeyi milyonlara taşır ama bir yayın en fazla birkaç yüz kredi öder; en pahalı satın alma birkaç bin kredi (araştırma teknesi 2.400). Eskiden gelir izlenmeyle sınırsız büyüyecekti.
6. **Hedef modeli** (`ChannelGoal`) ve ev PC panelinde ilerleme çubuğu: 10.000.000 ve kilometre taşları 100 … 10.000.000; hedefe ulaşınca "hedef tamam", kanal büyümeye devam eder.
7. **Av–çekim kanıtı bağlandı (#254 ile):** Utku'nun `RecordingResult.Specimen` kanıtı (`FilmOnly`/`FilmAndCatch`, `SpecimenId`, ana makinede doğrulanmış `WeightGrams`, `CaptureId`) `ClipManifest` ve `ClipSave` ile taşınır (varsayılanlı alanlar, eski kayıtlar `None` yüklenir; yarım kayıt `None` olur). Gün sonu sonucunda yalnız **aynı hayvanın çekimi ve avı birlikte** (`FilmAndCatch`) `ChannelEvidenceRules` ile izlenmeyi artırır: türün kendi ağırlık aralığında %100–150 (boyut) × %125 (eşleşme); yalnız çekim (`FilmOnly`), kanıtsız veya eski klip bugünkü kural (%100). Bonus **hayvan başına bir kez** ödenir (`PublicationSave.SpecimenId` ve `EvidencePercent`, yeniden yüklemede korunur). Tür başına tek sonuç kuralı ve ödeme sayısı değişmedi; balık başına sonuç önerilmedi (ödeme sayısını artırırdı).

## Ölçümler (EditMode, gerçek `ChannelAuthority` + gerçek `EconomyManager`/`EconomySaveStore`)

- **Gerçek kanal, canlı ekip** (günde 3 yayınlanabilir klip, kalite 3, her konu bir kez yeni): hedef **59. günde** aşıldı, 177 yayın, arşiv 128 klipte sabit, 49 kez medya atıldı, hiçbir klip/yayın reddedilmedi, bakiye ~65 bin kredi (fiyat ölçeğinde). Hedefe ulaşıldıktan sonra yeni yayın alındı ve kanal büyüdü. Test: `P411_CHANNEL_REAL`.
- **Yalnız kural** (arşivsiz): tek kaliteli yayınlarla hedefe platin 168, gümüş 192 yayında ulaşılıyor, yani anlık değil, ulaşılamaz da değil (`P411_CHANNEL_REACH`).
- **Oyun stili duyarlılığı** (yalnız kural modeli, ilk beş konu yeni): günde 3 klip kalite 3 için `FeedbackPercent` 50 → 129 gün, 120 → 59 gün, 200 → 38 gün; `FeedbackPercent` 120'de günde 2 klip kalite 2–3 → 92 gün, günde 1 klip kalite 2 → 192 gün, günde 4 klip kalite 3–4 → 43 gün. Bunlar süre sözü değil, ayar için duyarlılık; gerçek çok günlük oyunda ayarlanır.

## Kanıt köprüsü doğrulaması

- EditMode: kanıt klip kaydından geçer ve eski kayıt `None` yüklenir; `FilmAndCatch` en büyük hayvan %187, en küçük %125, `FilmOnly` ve kanıtsız %100; aynı hayvan başka klipte ve yeniden yüklemeden sonra ikinci bonus almaz; gün sonu tekrar çalıştırmak bir daha ödemez (`ChannelEvidenceWiringTests`).
- Gerçek iki Windows süreci (`Test-P46-ProductDay.ps1 -Players 2 -SharedHunt -Mute`): misafir çekti, host aynı balığı avladı; Utku'nun kanıtı `kind=FilmAndCatch grams=2115`, klip `P4_MEDIA_CLIP archived ... specimen=FilmAndCatch:2115g` ile arşivlendi. İlk koşuda klip `None:0g` ile arşivlenmişti: `RecordingMediaCaptureBinding` dünya bağlamını eklemek için manifesti yeniden kurarken kanıtı düşürüyordu; düzeltildi. Gün sonu ödemesi bu smoke'ta koşmaz, ödeme ucu EditMode'da kanıtlıdır.
- Medya dosyası silme sırası: tahliye edilen klibin dosyası yalnız onu artık listelemeyen kayıt **başarıyla yazıldıktan sonra** silinir (`PendingMediaDeletes`); başarısız yazımda dosya yerinde kalır (Mehmet'in #269 raporundaki atomik sıra bulgusu).

## Açık kalanlar

1. **Utku #254:** `FilmAndCatch` gerçek süreçte kanıtlandı (aşağıda); 4 süreç, iki kamera ve küçük/büyük balık karşılaştırması gerçek süreçte hâlâ denenmedi. Balığın ağırlığına göre görünür ölçek farkı ayrı iş.
2. **Mehmet #253:** gerçek klip boyutu ve depolama kotası; medya atma politikasının gerçek dosya boyutlarıyla (disk kotası) doğrulanması. Şimdi sınır klip sayısıdır (128).
3. **Gerçek ürün koşuları:** iki gerçek oyuncu av → gerçek kayıt → PC'de yayın → ertesi gün → reload (smoke sonuçları PR'da), 4 süreç, ayrı PC/insan kabulü ve #164 açık.
4. **Denge:** hedefe ulaşma süresi ve gelir ölçeği çok günlük gerçek oyunla ayarlanır; bu belgedeki gün sayıları yalnız modeldir.
