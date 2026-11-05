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
7. `ChannelEvidenceRules` (**ödemeye bağlı değil**): doğrulanmış ağırlık %100–150, eşleşen çekim +%25, yalnız çekim kanala girmez; Utku (#254) doğrulanmış ağırlık ve av kimliğini klibe taşıyınca `ChannelResultRules.Evaluate`'in `viewsPercent` girdisine bağlanabilir.

## Ölçümler (EditMode, gerçek `ChannelAuthority` + gerçek `EconomyManager`/`EconomySaveStore`)

- **Gerçek kanal, canlı ekip** (günde 3 yayınlanabilir klip, kalite 3, her konu bir kez yeni): hedef **59. günde** aşıldı, 177 yayın, arşiv 128 klipte sabit, 49 kez medya atıldı, hiçbir klip/yayın reddedilmedi, bakiye ~65 bin kredi (fiyat ölçeğinde). Hedefe ulaşıldıktan sonra yeni yayın alındı ve kanal büyüdü. Test: `P411_CHANNEL_REAL`.
- **Yalnız kural** (arşivsiz): tek kaliteli yayınlarla hedefe platin 168, gümüş 192 yayında ulaşılıyor, yani anlık değil, ulaşılamaz da değil (`P411_CHANNEL_REACH`).
- **Oyun stili duyarlılığı** (yalnız kural modeli, ilk beş konu yeni): günde 3 klip kalite 3 için `FeedbackPercent` 50 → 129 gün, 120 → 59 gün, 200 → 38 gün; `FeedbackPercent` 120'de günde 2 klip kalite 2–3 → 92 gün, günde 1 klip kalite 2 → 192 gün, günde 4 klip kalite 3–4 → 43 gün. Bunlar süre sözü değil, ayar için duyarlılık; gerçek çok günlük oyunda ayarlanır.

## Açık kalanlar

1. **Utku #254:** klibe doğrulanmış av kimliği ve ağırlık; sonra `ChannelEvidenceRules` ödemeye bağlanır. Eşleşme bonusunun bir avla tek kez harcanması için `PublicationSave`'e yeni alan (eski kayıtta boş = bonus yok).
2. **Mehmet #253:** gerçek klip boyutu ve depolama kotası; medya atma politikasının gerçek dosya boyutlarıyla (disk kotası) doğrulanması. Şimdi sınır klip sayısıdır (128).
3. **Gerçek ürün koşuları:** iki gerçek oyuncu av → gerçek kayıt → PC'de yayın → ertesi gün → reload (smoke sonuçları PR'da), 4 süreç, ayrı PC/insan kabulü ve #164 açık.
4. **Denge:** hedefe ulaşma süresi ve gelir ölçeği çok günlük gerçek oyunla ayarlanır; bu belgedeki gün sayıları yalnız modeldir.
