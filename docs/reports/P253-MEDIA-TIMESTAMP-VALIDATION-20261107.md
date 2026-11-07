# Mehmet / P4 #253 — yeni kamera klipleri için zaman damgası bütünlüğü

Mevcut DDClip v1 decoder, artan olmayan kare zamanlarını reddediyordu; ancak `RecordingClipFile.Encode` aynı kareleri kaydedebiliyordu. Host daha sonra `ClipArchive` içine kabul edilen bir klibi oyuncu PC'sinde oynatamayabiliyordu. Ayrıca kayıt süresinden sonraki JPEG karenin klibe yazılması süre/kare tutarlılığını bozuyordu.

Düzeltme: V1 enkoder yeni oluşturulan kliplerde `0 ≤ frameTime ≤ duration+1ms` ve monoton artan (eşit zaman mümkün) kare zamanlarını zorunlu tutar. Geçersiz kare üretimi `ArgumentException` ile başarısız olur, `RecordingMediaCaptureBinding.TrySubmit` bu durumu güvenli şekilde sonlandırır; eksik/oynatılamaz klibin hash, arşiv veya kanal ödülü oluşturulmaz. Geriye dönük `TryDecode` yapısı **değişmedi**; eski dosyaların kabul ettiği aralık daraltılmadı. IO ve kilitli disk durumundaki mevcut tekrar denemeleri korunur.

Test: `RecordingClipFileTests.EncodeRefusesEmptyNonMonotonicOrOutOfDurationInput`: boş, azalan, süreyi aşan ve NaN kareler reddedilir; 0/0.2/0.4 s meşru 5 fps klip oynatılabilir. GitHub Unity EditMode ve 1/2/4 süreç CI kontrolü bekleniyor.

**Açık:** Gerçek Windows iki oyuncu ve üç kamera kademesinin 15/30/45 saniye, GPU belleği, disk MB, güvenli dönüş, misafir kameracı, host yeniden başlama ve ayrı PC #164 kanıtı bu yalın doğrulamadan çıkmaz. Bunlar #253/#257'de açık.
