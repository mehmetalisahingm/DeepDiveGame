# Medya regresyonu ve ortak av koşusu — 6 Kasım

Kapsam #253/#257 ve #266'da bildirilen test desteğidir; #253, #255, #257 veya P4.12 kabulü değildir.

`Test-P1-Integrated.ps1 -Players 2 -Media` birleşik `62b7b34` tabanında önce başarısızdı: host dört klip ve bir yayınla diğer oyuncuyu beklerken süre doldu; misafir arşiv kontrolüne ulaşmadı. `P45_HOME_RETURN` izleri, yeniden bağlanan misafirin batı rampasından kıyıya çıkmasına rağmen test yolunun ilk konumundaki doğu tarafını seçerek sandalın gövdesinde `(0.68, 8.48, -12.40)` takıldığını gösterdi. Mesaj alımı görülmesi tek başına içerik eşitliğini kanıtlamaz. Geçici ağ tanılaması kaldırıldı; oyun mesajlaşması veya protokolü değiştirilmedi.

Test sürücüsü, oyuncu batı rampasından kuru zemine çıktığında dönüş rotasının batı tarafından devam eder; hareket girdisi, çarpışma, arşiv ve yayın kontrolleri değiştirilmez. `P1-integrated-20261106-125641-760-2-25909` koşusunda aynı bilgisayardaki iki gerçek süreç PASS: misafir yeniden bağlandı, ikisi dört klibi ve birbirinin yayınını gördü; yinelenen yayın reddi, tek ödeme ve gerçek kayıt dosyasından host yeniden yüklemesi (dört klip, iki yayın) geçti. Bu eski medya regresyonu arşive test manifestleri gönderir; gerçek video çekimi kanıtı değildir.

`-SharedHunt` testinde klip üretmek için iki oyuncunun da grafik cihazı gerekir. Betik bu modda artık ayrıca `-Render` istemeden iki süreçte grafik açar; bu oyun içeriğine test sağlayıcısı eklemez.

`P46-product-20261106-125919-261-2`: `-Players 2 -SharedHunt -Mute` ile ayrıca `-Render` verilmeden iki Windows sürecinde Direct3D 11 ve gerçek kayıt PASS. Misafir çekti, host aynı 2.231 g balığı avladı; `FilmAndCatch` kanıtı oluştu. 57 karelik 538.988 bayt klip güvenli dönüş sonrası PC'de oynatıldı, sahibi yayınladı ve ikinci yayın reddedildi. Bu aynı bilgisayar testidir; gerçek internet/ayrı PC veya son görsel kabul değildir.

#266: kanal PR'ındaki klip tahliyesi `OnClipEvicted` ile dosyayı siliyor; mevcut `SubmitClip` yalnız `channel.Submit` döndükten sonra `SaveNow` çağırıyor. Kayıt yazımı başarısız olduğunda eski kampanya kaydı dosyası zaten silinmiş medyaya işaret edebileceğinden, disk hatası/yeniden açma ile atomik sıra doğrulanmadan bu PR birleştirilmedi.
