# P4.10 #246 — sepete geç katılan oyuncu (4 Kasım)

Temel: `codex/p4-integration` 41968fc4b2784c935a033847d460349f7747d326. Test yalnız `-p246-late-shop 1` argümanıyla çalışır; normal oyunda bu sürücü etkinleşmez. Windows GPU build SHA-256: `56c28afba054092826b7a8a80ba3767daa87967f182c0c562eb2f98799175b7a`.

Host normal oturum fazlarıyla tek dünya lobisine döner, gerçek yürüyüş ve F etkileşimiyle temel kamerayı raftan seçer. İkinci gerçek Windows süreci **seçimden sonra** bağlanır ve gerçek hareketle hosta yaklaşır. Her iki süreç seçili kimliği ve sepeti gözlemler; misafir kamerası görüntüsü [burada](evidence/p410-shop-late-20261104/player1.json.late-shop.png), [host](evidence/p410-shop-late-20261104/player0.json) ve [misafir](evidence/p410-shop-late-20261104/player1.json) sonuçları `passed=true`. Aynı bilgisayarda iki süreçtir; ayrı bilgisayar/internet kabulü değildir. Çalıştırma: `tools/Test-P410-ShopLateJoin.ps1`; Unity Windows integrated build gerektirir.

Bu koşu satın alma veya yeni hostta kayıt restorasyonu testi değildir. Sepetteki nesnenin daha önceki ödemesiz seçim/iptal, tek ödeme ve iki kamera kanıtı #247'deki önceki koşudadır. #246 rehost ve görsel kalite dahil kalan gereksinimleri için açık kalır. Kamerada kasanın/ürünün ve etiketlerin görsel kalitesi hâlâ erken seviyededir.

Tanı: misafir ilk sahne örneğinde `(1.50, 8.40, -49.50)` raporladı, sonrasında su tarafında görüldü. Önceki tanı koşusunda su ve kara arasındaki yer değiştirme raporlandı; örnekler arasında süre/otorite incelemesi yapılmadı. Nedeni veya oyuncuda algılanan bir atlama olduğu kanıtlanmadı. İlk iki rota denemesi test sürücüsünün ev veya kıyı yoluna takılması nedeniyle iptal edildi; başarılı koşu gerçek kıyı rampasıyla yürüdü. Eski #207 ile aynı hata olduğu iddia edilmez.
