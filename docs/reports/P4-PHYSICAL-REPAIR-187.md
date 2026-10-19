# #187 — Fiziksel başlangıç sandalı tamiri

Rapor tarihi: 19 Ekim 2026. Kullanıcı, #141/#159 kabulünü açık tutarak yalnız Mehmet'in #187 işinin şimdi uygulanmasını açıkça yetkilendirdi. P4.6/P4.7 veya ekip kabulü bu raporla kapanmaz.

## Oyuncunun yaptığı iş

- E: kıyıdaki gerçek malzemeyi al; elde taşı; iskelede eşleşen yuvaya yerleştir.
- Sol tık: keserle üç kontrollü darbe. İlk iki darbe katkı değildir; son kabul edilmiş darbe malzemeyi tüketip bir grubu tamamlar. Üç grup toplam dokuz darbedir.
- Q: işi iptal et ve kullanılmamış malzemeyi iskeledeki ortak askıya bırak. Askıdan tekrar almak ücret üretmez. Satın alınmış malzeme de aynı askı/taşıma/yerleştirme yolundan geçer.
- Her tamamlanan grup tahtayı ve çivilerini görünür bırakır. 3/3 sonunda mevcut dört koltuklu sandal görünümüne geçilir; mevcut gerçek binme ve sabit yakın rota korunur.

Oyuncuyu taşıma/tamir için ışınlayan yeni yol yoktur. Eski entegre test sürücüsünün açıkça etiketlenmiş deniz başlangıç fixture'ı ürün yolu değildir; evden başlayan ürün-günü testi bu fixture'ı kurmaz.

## Otorite ve kayıt

Owner RPC'leri istek sırası, host raycast'i, görüş/mesafe, aktiflik, gün kilidi, doğru parça/yuva, tek taşıyıcı ve 0,45 saniyelik darbe aralığıyla doğrulanır. Aynı parça rezerve edilirken farklı parçalar global kilit almaz. Tahta eldeyken zıpkın, kamera kaydı ve tekneye binme reddedilir.

`EconomySaveData` v9: `UnusedBoatPartIds` edinilmiş/kullanılmamış; `BoatPartIds` tamamlanmış katkıdır. Aynı ID iki listede olamaz. Satın alma bir kez 120 harcar. Son darbe kullanılmamışı çıkarıp tamamlanmışa ekler ve tek save yazar; yazım reddinde malzeme, katkı ve revision geri alınır. Client ID/rezervasyon/yarım darbe kaydedilmez. İptal, kopma, pasiflik, yerleştirilmiş işten uzaklaşma ve gün kapanışı kullanılmamış malzemeyi erişilebilir askıya döndürür. D06 host kampanyası korunur.

Eski üç teknik parça kimliği ve tamamlanmış bitler korunur. Eski 2/3 kayıttaki iki grup yeniden istenmez; eski 3/3 sandal bozulmaz. v1-v8 dosyalar kullanılmamış malzeme kazanmaz. Ağ değişkeni düzeni değiştiği için protokol `DeepDive-P4-5` olur.

## Doğrulama

Kod kaynakları: `518dc05` temel mekanik, `5e41651` yuva ipucu/test yaklaşımı, `fda0667` aşama sonu ve regresyon sürücüsü düzeltmeleri, `d90d9b2` dönüşte onarım gözlemi/ilk host boarding ve balığın kaçış mesafesi dışında çekim, `f17c3e3` hostun son katkı kanıtını Return geçişinde koruma. Unity `6000.3.23f1`. Ham kanıtlar [p187-repair-20261018](evidence/p187-repair-20261018/) klasöründedir; klasör adı ilk koşuların gerçek zaman damgasını korur. Her manifest kendi kaynak commit'ini ve sınırını ayrı yazar; ilk koşular son commit'te yeniden koşulmuş gibi gösterilmez.

| Kontrol | Kanıt / durum |
|---|---|
| EditMode | 1310/1310 PASS. Edinme/son darbe ayrımı, yanlış yuva, hız sınırı, aynı parça yarışı, farklı yuvalarda çalışma, replay/payload değişimi, iptal/kopma release'i, save reddi/rollback, satın almada tek ücret |
| Eski kayıt | Sekiz maskenin her biri v1-v8 ile sınandı; üç farklı 2/3 disk kampanyası ve kullanılmamış son malzeme yeni economy/store ile açılıp tamamlandı |
| Dört gerçek Windows süreci | `518dc05` build: üç farklı owner malzemeyi aldı, Q ile bıraktı, askıdan geri aldı, yerleştirip tamamladı; dört süreç taşıma/yerleştirme/darbe aynasını gördü. Dördüncü süreç gözlemcidir. Dört süreçli aynı-parça yarışı yapıldı diye yazılmaz |
| Solo gerçek ürün günü | `5e41651` Windows: sıfır bakiye → gerçek fiziksel tamir → üç dalış → iki balık satışı → kamera → gerçek klip → tekne/harita → fiziksel ev PC'si/yayın → uyku/gün 2; yeni host sürecinde kayıt/klip oynatma ve ikinci yayın reddi PASS |
| Prep/Dive/Return tamiri | `fda0667` Windows: host gövdeyi Prep'te, guest desteği Dive'da, host son grubu Return'da tamamladı. İki süreç PASS, 0/1/2/3 katkı sırası ve ikinci katkının reddi doğrulandı |
| Dört süreçli roller | `fda0667` Windows: dört owner rol/etki/gerçek yerde yürüme/harita ve guest yetki sınırı PASS; room-full ve yanlış fazda giriş retleri de geçti |
| Co-op ürün günü | `d90d9b2` Windows iki gerçek süreç: üç dalış, gerçek av/satış/klip, fiziksel tamir, yakın rota, PC yayını, uyku/gün 2 PASS. Yeni host sürecinde kampanya/klip oynatma ve ikinci yayın reddi PASS |
| Onarım ardından yakın rota | `f17c3e3` Windows iki süreç PASS: gerçek 0/1/2/3 katkı, parçaların gizlenmesi/görev panelinin kapanması, iskelede binme, çift binme reddi, Docked→Outbound→Anchored→Inbound→Docked, guest inme/yeniden binme, iki oyuncu haritası, boş iskele ve disk save/load |

İlk dört süreçli sürücü bütün aktörleri aynı iskele noktasında bekletiyordu. Yaklaşım şeritleri ayrıldı, orta çalışma noktası geriye alındı, işi biten oyuncular geçişi açtı ve sonraki dört süreç koşusu geçti. İlk iki süreçli Prep/Dive/Return koşusunda gerçek 0/1/2/3 katkılar ve iki süreçte onarım/hint/hidden PASS iken guest'in `boatEngineFound` bayrağı Return paketi nedeniyle kaçıyordu; kendi yerleştirilmiş işini görmüş guest artık tamamlanma bitini yeni input faz kapısından önce gözler.

CI `5e41651`: Unity test/build ve sürekli dünya PASS; üç senaryo başarısızdı. Ham artifact'ler okunarak ayrıldı: Trip sürücüsü yeni fiziksel tamir sürerken eski sabit bitiş süresinde çıkıyordu; rol yürüyüşü ilk düşüşü yatay hareketten ayıramıyordu; co-op kayıt sürücüsü duvar/zemin arkasındaki balığı kovalıyordu. `fda0667` gerçek tamamlanma/oturum kapanışını bekler, yerde yürümeyi gerçek aşağı ray ile ölçer ve görünür kayıt hedefini seçer. Sonraki yerel Trip gerçek gidiş/dönüşü yaptı, fakat onarım/hint gözlemi Return'da durduğu ve guest önce bindiği için iki assertion başarısızdı. `d90d9b2` gözlemi inputtan ayırır ve testte hostun gerçek ilk binişini bekler; `f17c3e3` son host katkısını da kendi gözlenmiş yerleştirme/darbe işinden tamamlanma bitine bağlayarak aşama geçişinde kaybetmez. Çekim sürücüsündeki 4,5 metre yaklaşımı da SeaBass'ın belgelenmiş 5 metre kaçış eşiğinin içindeydi (balık 3,2 m/sn, dalgıç 3 m/sn); artık mevcut 5–12 metre kamera bandındaki 8 metreden gerçek çekim denenir. Oyun balığı yavaşlatılmaz, framing/ödül/katkı koşulları kaldırılmaz. Yeni CI koşuları devam etmektedir; bu rapor onları PASS saymaz.

## Teslim sınırı

Bu PR mekanik işin uygulamasıdır. Kırık gövde/tahta/keser/çivi ve kısa vuruş sesi geçici mekanik sunumdur; #189 nihai sanat/ses seti değildir. Runtime slot/askı yerleşimi mevcut iskelede kanıt içindir; #188 nihai world yerleşimi ve dört kişinin oyuncu değerlendirmesi yerine geçmez. Ortak sahne/prefab değiştirilmedi; economy/save seam'i Mert, world anchor'ları Utku alanına temas eder ve PR'da görünürdür. Başka kişi adına onay yazılmaz.

#187 için gerçek dört süreçli aynı-parça yarışının özel kaydı, geç katılma/guest kopması/00:00 sırasında fiziksel işin runtime kaydı ve yerel/arkadaş videosu ayrıca kabul maddeleridir; yalnız saf host testleri bunların tamamı yerine yazılmaz. Ayrı PC/dört insan/üç oyun günü #141/#159 kabulü de açık kalır. #186 ortak karar kaydı ve #188/#189 tamamlanmış sayılmaz. PR #190 kapanış anahtar kelimesi içermez.
