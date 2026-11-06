# P4 / Mehmet — late-join medya arşivi snapshot düzeltmesi

Kaynak: #253, #257 ve #255'in raporladığı mevcut 2-oyuncu medya testinde, host klipleri kaydetmesine rağmen misafir `mediaArchiveSeen=0` görebiliyordu. Bu belirti için somut bir yarışı kapatıyoruz; bütün vakaların tek kök nedeni olduğu **kanıtlanmadı**.

## Kök neden adayı ve değişiklik

`MediaNetworkBinding` başlangıçta hostun ilk medya aynasını yalnız `OnClientConnected` callback'inde ve revizyon değiştiğinde gönderiyordu. Misafir istemci, tam bağlantı olayından sonra `MirrorMessage` handler'ını kaydederse o ilk mesajı kaçırabiliyor; gün boyu yeni yayın/klip değişikliği olmadan hiç arşiv gelmiyordu.

İstemci, mesaj alıcısını kaydettikten ve bağlantı kurduktan sonra `DeepDive.P4.Channel.Request.v1` ile küçük bir snapshot isteği yollar. Host yalnız bağlı istemciye ve mevcut host kampanyası bağlandıktan sonra güncel `channel.ExportMirror()` sonucu gönderir. Henüz cevap yoksa 2 saniye aralıkla tekrar istenir; gerçek mirror gelince istek durur. Eski revizyon paketi yeni arşivi silmez. Bağlantı durduğunda istek takvimi ve mevcut mirror sıfırlanır.

Bu protokol **tek yönlü host kaynaklı görüntüleme** içindir: istemci arşiv, gelir, kayıt veya ödeme verisi göndermez/değiştiremez. Mevcut `PublishMessage`, hak, imza ve ekonomi kuralları korunur.

## Test ve açık kabul

`P253MediaMirrorRecoveryTests`: sekiz istemci başlatma/yeniden deneme durumu; geçersiz zaman ve beş revizyon/tekrar durumu. GitHub CI ve gerçek iki süreçli `Test-P1-Integrated.ps1 -Players 2 -Media` koşusu ile ayrıca doğrulanmalıdır. Yerel Unity veya Windows çok süreç testi bu PR hazırlanırken çalıştırılmadı. PR teknik olarak birleşse bile kayıt/yayın, 4 oyuncu ve ayrı bilgisayar #164 son kabulü kapanmaz.
