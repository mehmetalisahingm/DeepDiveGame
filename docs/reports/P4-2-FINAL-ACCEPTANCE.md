# P4.2 final acceptance: fikstürsüz gerçek klip → PC → kanal → gün → yeniden açılış (#106)

Tarih: 2026-10-01. Taban: `codex/p4-integration` @ `e3e2040` (+ #110'daki `-Record` kararlılık düzeltmesi). Dal: `p4/p42-final-acceptance`.

## Ne eklendi
`tools/Test-P1-Integrated.ps1 -Players 2 -Acceptance` (sürücü: `IntegratedSmokeDriver`, `-p4-acceptance` / `-p4-acceptance-reload`). **Hiçbir klip, manifest, dünya bağlamı veya gün kapanışı fikstürü yoktur.**
- Gerçek `-Record` yakalaması klibi üretir (host gerçek grafik cihazıyla; `-nographics` capture'ı kapatır). Kaydeden oyuncu klibi NPC'ye **satmaz**.
- Ev PC'sinde, her iki süreçte: klip gerçek mi (`clipId == ClipIdForRecording`, hazır, güvenli dönmüş, kalite 1-4, süre/boyut > 0, 64 haneli hash), dünya bağlamı dolu mu (tür=Species, region/cell/depth band), PC paneli açık mı, oynatılabilir mi.
- Sahibi: PC'den uzakta → `NotAtPc`; PC'de yayın → kabul; aynı klip tekrar → `PublicationAlreadyQueued`.
- Sahibi olmayan süreç: aynı klibi yayınlamak → `NotOwner`.
- Tek ticari hak (host): yayın öncesi gerçek kayıt NPC kuyruğunda (1), yayın sonrası kuyrukta 0 ve kanala ayrılmış; aynı kaydı NPC'ye tekrar sokma `DuplicateRequest`; NPC'den para gelmedi.
- Gün: iki oyuncu da yatağa girer, **gerçek uyku kapısı** günü kapatır (gün 1→2). Yayın sonucu tek kez ödenir (bakiye farkı = yayın geliri), aynı gün tekrar settle 0, dosyada clip/yayın/settleId/hak/bakiye/gün doğru, `LoadNow` sonrası çoğalma yok. Sonuç (izlenme/gelir/takipçi) iki sürecin aynasında görünür.
- Gerçek ikinci host başlatması aynı kampanya dosyasında: klip, yayın, sonuç, takipçi, hak, bakiye, gün, keşif gözlem sayısı ilk koşunun raporuyla **aynı**; tekrar yayın `PublicationAlreadyQueued`, NPC'ye tekrar giriş `DuplicateRequest`, settle 0 ödeme, klip yeniden oynatılabilir.
- Betik ayrıca diskteki `.ddclip` dosyasının gerçek bayt boyutunu ve SHA-256'sını manifestle karşılaştırır, host logunda Mehmet'in `P4_MEDIA_PRODUCT_OK` satırını arar.

## Kanıt (aynı Windows build'i)
- `-Acceptance` 3/3 PASS (host, client1, host-reload; klip 96-100 KB, dosya boyutu ve hash manifestle eşleşti).
- Regresyon: `-Explore`, `-Storage`, `-Day` (+ host yeniden açma), `-HomeSleep`, `-Trip`: hepsi host + client1 PASS.
- EditMode tam suite **856/856**.
- `-Record` (#110) ve `Test-P4-MediaProduct.ps1` önceki koşularda PASS.

## Doğrulanmadı
- Kök neden ters yön (NPC'de ödenmiş kayıt kanala yayınlanamaz, `RightsConsumed`) bu smoke'ta değil, gerçek `EconomyManager` ile `ChannelTests` EditMode'unda kanıtlı.
- "Yarım/başarısız capture kalıcı medya bırakmaz" ve world-context replay (ikinci keşif/ansiklopedi sonucu üretmez) bu smoke'ta yalnız dolaylı: keşif gözlem sayısı reload'da değişmedi (2→2). Asıl kanıt Mehmet/Utku'nun EditMode testleri.
- Dört süreç, ayrı bilgisayar, IMGUI görsel kontrolü; gün kapanışı **uyku** ile (gece yarısı yolu `-Day`'de kanıtlı).
- Üç kişilik kapıdan yalnız benim zincirim; Mehmet ve Utku kendi maddelerini işaretler.
