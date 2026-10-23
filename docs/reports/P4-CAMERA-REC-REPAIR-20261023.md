# Kamera REC/vizör ve Mehmet #187 oturum doğrulaması — 23 Ekim 2026

Kullanıcının kamera sunumunu şimdi geliştirme talebi ve #187 dar uygulama istisnası kapsamında. P4.6/#141, P4.7/#159 veya nihai sanat/ekip kabulü kapanmaz.

## Oyuncunun gördüğü değişiklik

- Sol kayıt paneli: host kaydı kabul edince kırmızı ışık + REC ve ilerleyen HH:MM:SS.
- Kabul edilmiş durdurmadan sonra gri ışık, STBY ve 00:00:00.
- Açık vizör köşeleri; gerçek kayıt rayıyla aynı ekran merkezinde odak işareti; mevcut kamera modeli ve R/C kontrol ipuçları.
- Reddedilen Start REC açmaz. Reddedilen Stop aktif süreyi sıfırlamaz. Eski sonuç yeni kayıt durumunu değiştiremez.
- Owner kamerasına bağlı gerçek Canvas, eldeki modelin önünde çizilir. Ayrı arşiv capture kamerası UI katmanı 5'i dışlar; HUD klibe basılmaz. Network hedef ID'si kullanıcıya gösterilmez.
- Bu teslim arayüz/sunum geliştirmesidir. Mevcut klip kodlaması 320×180 / 5 FPS prototip olarak kalır; 720p/24 FPS başarısı iddia edilmez.

## Doğrulama

Runtime sürücü/ana UI kaynağı `515b54a7fbde1e10adf9aa4b433f5dc6b11c073c`. Son kaynak `86161d1`, yalnız Canvas'ı eldeki modelin önüne alır. Ham raporlar ve her koşunun kaynağı [manifest](evidence/camera-repair-20261023/manifest.json) içindedir.

- Windows integrated build PASS.
- EditMode: 1346/1346; üç yeni host-cevap/tally regresyonu dahil.
- Dört gerçek Windows süreci, `-RepairOnly -RepairInterruptions`: PASS. İki guest aynı ücretsiz gövde malzemesini ister; host yalnız bir taşıyıcı verir ve diğer istek reddedilir. Taşıyıcı bir darbeden sonra gerçekten NGO oturumundan ayrılır. Malzeme acquired kalır, rezervasyon çözülür. Host mevcut faz geçişleriyle Lobby'ye döner; guest yeni network ID ile normal Lobby kabulünden katılır. Başka oyuncu malzemeyi geri alır ve tamamlar.
- Aynı koşuda ikinci malzeme yarım bırakılırken host saati hızlandırılır. `DayManager.Update -> DayEngine.Tick -> Midnight` gerçek kapanış/save/sabah kancalarını çalıştırır; doğrudan malzeme/katkı veya gün değeri yazılmaz. Yarım katkı bitmiş görünmez; malzeme korunur. Son dosya v9, üç tamamlanmış parça, sıfır kullanılmamış malzeme, gün 2, tek özet/close ID.
- Son UI kaynağında solo tam ürün günü ve yeni host reload: PASS. REC/STBY görüntüleri bu koşudan alındı. SHA-256 doğrulanmış gerçek klipten çıkarılan ilk karede HUD olmadığı ayrıca görüntüyle kontrol edildi (`archive-first-frame.jpg`).
- İki süreçli tam ürün günü: üç gerçek dalış, iki balık satışı, kamera satın alma, gerçek klip, tekne gidiş-dönüş, ev PC playback/yayın/uyku ve yeni host'ta reload/duplicate yayın reddi PASS. Kayıt göstergesinin süre ilerlemesi ve Stop'ta sıfırlanması da host raporunda doğrulanır.

## Görsel kanıt

Aşağıdakiler gerçek owner kamerasının ve aynı üretim Canvas'ının URP render çıktısıdır; sonradan birleştirilmiş arayüz çizimi değildir. IMGUI genel HUD'u bu kamera render'ına dahil değildir.

![Kayıt açık: kırmızı REC ve geçen süre](evidence/camera-repair-20261023/camera-rec.png)

![Kayıt durmuş: STBY ve sıfırlanan süre](evidence/camera-repair-20261023/camera-standby.png)

## Tekrar çalıştırma

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P46-ProductDay.ps1 -Players 4 -Port 26287 -RepairOnly -RepairInterruptions
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 26387
./tools/Test-P46-ProductDay.ps1 -Players 1 -Port 26487
```

## Açık kalan kabul

Bu koşular aynı bilgisayardaki gerçek süreçlerdir; ayrı bilgisayar/dört insan/nihai görsel kabul değildir. Hızlandırılmış Midnight testi doğal 20 dakikalık saat koşusu değildir. SaveFailed+kopma/disk rollback bu teslimde EditMode testleriyle kapsanır; runtime disk arızası enjekte edilmedi. Ücretli malzemenin son darbe yolu için mevcut birim regresyonları korunur; yeni dört süreç koşusu ücretsiz malzeme yolunu kullandı.

#187 için nihai Mert el/alet/beden kit'i, birinci/üçüncü kişi darbe videosu ve kalan birleşik kabul satırları açık tutulur. #188/#189 ve #141/#159 tamamlanmış sayılmaz. Rota/koltuk, av, kayıt değerlendirmesi, ekonomi ve D06 kampanya sahipliği değiştirilmez. Test sağlayıcısı normal oyun yoluna kurulmaz.

