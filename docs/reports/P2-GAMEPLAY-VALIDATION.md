# P2 oyuncu ve balık teslimi — 11 Eylül 2026

Kullanıcı, Utku'nun kalan P2 işlerini bu çalışmada devralmamızı istedi. Mehmet #28 ve Utku #30 test edilip P2 dalına birleşti. Mert'in envanter/UI/dönüş işleri ve P2 koordinatörlüğü korunur.

- Birleşik #28/#30 üzerinde **99/99 EditMode PASS**.
- Gerçek Windows build başarılı. Oyun kaynağı: `fe87135cddf8b05a587d46c87ef59911ab04fde3`.
- Opt-in av testi iki gerçek süreçte başarılı: misafir oyuncu mevcut girdi/RPC yoluyla sahnedeki hareketli balığı vurdu/öldürdü/topladı; host üzerindeki çantasına tek av eklendi; av her iki tarafta despawn oldu. Balık veya av test sağlayıcısıyla değiştirilmedi.
- Bağlantı, yürüme/yüzme, sahne dönüşü, ready reset ve yeniden katılma regresyonu geçti. Bu farklı bilgisayar/internet testi değildir.
- Kaynak: [yerel test özeti](../evidence/P2-gameplay-local-results.json). EditMode koşusundan sonra yalnız isteğe bağlı runtime av testi eklendi ve yeni build üzerinde çalıştırıldı; kanıtta iki kaynak commit ayrı tutulur.
- Ses assetleri oyuncu prefabına bağlı; headless test sesin insanlarca duyulmasını veya kontrol hissini değerlendirmez.

CI dosyası eklendi; GitHub'da Unity secret'ları yok. Eksik lisans kontrolü build çalıştırmadan hata verir. Otomatik Unity build PASS değildir; [engel, sorumlu ve yerel yöntem](../CI.md) kayıtlıdır. Actions kodu sabit commit'lerden kullanılır; kişisel runner/ücretli lisans kurulmadı, oyun yayımlanmadı.

P2 kapanışı için Mert'in istemci çanta/UI ve güvenli dönüş bağlantısı; gerçek 1/2/4 oyunculu dolu çanta, av yarışı, oksijen/pasiflik ve ardışık iki dalış; en az iki oyuncuyla 10–15 dakikalık oynama değerlendirmesi ve üç kişinin gerçek sonuç kaydı hâlâ gerekir. P1'den devreden kontroller kapanışta ayrıca ele alınır. P2 açık, P3 kilitli.
