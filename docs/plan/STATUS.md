# Güncel durum ve görev takibi

Plan **3.1 — 13 Ekim 2026**. Temel plan 3.0 korunur; oyuncu deneyimi ve sanat teslimleri genişletildi.

Aktif faz: **P4**. P0–P3 kapalıdır. P4.5 PR #135 ile birleşti; P4.6 kabulü açık. Yeni P4.7–P4.12 planı [oyuncu deneyimi genişletmesinde](PLAYER_EXPERIENCE_EXPANSION.md) kayıtlıdır. P5/P6 kilitlidir; yeni plan uygulama veya tamam kaydı değildir.

21 Ekim teknik aday `bf31302`: #190 fiziksel tamir, #194 kuru malzeme, #195 kıyı sıkışma düzeltmesi ve #196 S1 kızak aynı kaynakta doğrulandı. 1339/1339 EditMode, Windows build, dört süreçli taşıma/bırakma/geri alma/tamir, iki süreçli tam ürün günü + yeni host reload/gerçek clip ve tamir sonrası yakın sefer, dört süreçli integrated Boat PASS. Güvenli askı `repair-drop-0`'a bağlandı; istek replay reddi ve tamir geri bildirimi düzeltildi. [Rapor ve gerçek sonuç dosyaları](../reports/P48-INTEGRATION-20261021.md). Bu tarihli aday kanıtı merge/CI veya #141/#159/#187/#188/#189 nihai kabulü yerine geçmez; P5/P6 açılmaz.

19 Ekim kullanıcı kalite geri bildirimi: su/hareket, mercan ve habitat, balık model/animasyonu, derinlikte değerli av, bulunabilir boss, uzak sahil ve gerçek ev/dükkân mekânları ürün açığı olarak kaydedildi. [Dünya kalite teslimleri](WORLD_QUALITY_DELIVERIES.md), P4.7–P4.12 üst işleri altında Q01–Q14 ayrı oyun gösterimi ve kabul kapısını tanımlar. Mevcut faz/issue kapanmaz; bu plan turu oyun kodu, yeni sanat veya nihai görsel kabul teslimi değildir. Teknik boss kabulü ile bulunabilir/son kaliteli boss deneyimi ayrılır. Beş normal tür ve bir boss hedefi korunur; mevcut üç türün eşit 120 taban değeri derinlik ekonomisi kabulü değildir.

18 Ekim kullanıcı kapsam kararı: mevcut faz kabulünü kapatmadan **15 avlanabilir kıyı balığı** ve kullanıcının sağladığı **Tripo motorbotunun görsel entegrasyonu** ayrıca istendi. Bu iki dar içerik işi şimdi uygulanır; P4.7–P4.12'nin bütünü açılmaz. Balıklar mevcut levrek modeli/türünden 15 ayrı sahne nesnesidir; beş tür/gelişmiş ekosistem veya fiziksel sandal onarımı teslimi değildir. Motorbot `boat-2` görünümüdür; başlangıç sandalı `boat-1` ve satın alma zinciri korunur. Dünya sahnesi/balık yazımında Utku, görsel kaynak/uyumda Mert inceleme sahibidir; onların onayı üretilmez. Kanıt ve kalanlar [teslim raporunda](../reports/P4-FISH15-TRIPO-MOTORBOAT.md) tutulur.

Unity sürümü: `6000.3.23f1`.

19 Ekim #187 dar uygulama: PR #190'da gerçek E ile al/taşı/yerleştir, Q ile ortak askıya bırak/yeniden al ve üç kontrollü keser darbesinin sonunda tek katkı uygulanmıştır. v9 edinilmiş malzemeyi tamamlanmış katkıdan ayırır; eski 2/3 ve 3/3 kayıtları korunur. Yerel 1310/1310 EditMode, solo tam ürün günü/yeni süreçte reload, dört gerçek süreçte üç ayrı taşıyıcıyla iptal/geri alma/tamir ve iki süreçli Prep-Dive-Return tamiri PASS. İki süreçli gerçek yakın rota/binme/guest inme-yeniden binme/boş iskele/save-load ve iki süreçli tam ürün günü/yeni host reload da PASS; yeni CI koşuları sürüyor. Kaynaklar ve sonuçlar [#187 raporunda](../reports/P4-PHYSICAL-REPAIR-187.md) kaynaklarıyla izlenir. Mekanik tahta/keser/gövde/ses geçici sunumdur; #188/#189 nihai world/sanat ve #141/#159 ekip kabulü açık kalır. Bu istisna P5/P6'yı açmaz.

18 Ekim Mehmet ilerlemesi: #185 `6816fba` olarak `codex/p4-integration` içine birleşti. Bu kaynakta Unity test/Windows build job'u, günlük ürün döngüsü ve 1/2/4 süreçli fiziksel dünya + yeni host reload CI PASS doğrulandı. Bu not aynı workflow'un fleet/boss alt işleri veya gerçek ekip kabulü değildir. #170 GitHub'da kapalıdır; aşağıdaki eski tarihli bekleme notu tarihsel kayıttır. #186 için [somut tamir sözleşme önerisi ve sekiz maskenin geçiş tablosu](P48_REPAIR_CONTRACT_PROPOSAL.md), #173 için güncel kit/elde nesne ölçümü ve 2/2 EditMode kanıtı [Mehmet raporundadır](../reports/P4-MEHMET-REPAIR-PREP-186-173.md). R01/R02/R11 ve #141/#159 kabulü açık; #187 için ilk not hazırlık kaydıdır. Sonraki açık kullanıcı talimatı: “Evet, benim tamir işimi şimdi uygula”. Bu dar istisna ile yalnız #187 uygulaması açılmıştır; #141/#159 ve diğer faz kapıları açık kalır. Edinilmiş malzeme/son darbe ayrımı v9 kayıt ve `DeepDive-P4-5` protokolüyle uygulanır; #188/#189 nihai world/sanat kabulü üretilmez.

18 Ekim ek kullanıcı talebi: sağlanan üç FBX arşivinden uygun bina, sandal, ev eşyası ve balık görselleri şimdi oyuna bağlanır. 15 balığın beşi levrek, beşi ton balığı, beşi kırmızı mercan olur; önceki tek tür notu bu dar teslimle genişletilir. Yeni türlerde av/çekim/satış fiyatı ve ansiklopedi bağlantısı eklenir; tür başına yeni görev veya boss üretilmez. Kasaba yolları, ev etkileşimleri, dört koltuk ve satın alma otoriteleri korunur. P4.6/faz/insan kabulü kapanmaz; World değişiklikleri Utku, kasaba/kaynak/materyal değişiklikleri Mert incelemesine gider.

16 Ekim #157 teknik ilerleme: PR #169, kullanıcının doğrudan birleşim talimatıyla `72c5bbc` olarak `codex/p4-integration` dalına birleşti. Günlük Prep/Dive/Return/Lobby tek dünyada kalır; ilk dünya girişi ve yeni gün başlangıcı ev anchor'larını kullanır. Kaynak `989b556`: 1254/1254 EditMode, Windows build, aynı bilgisayarda gerçek 1/2/4 süreçli ev→dükkân→iskele→su→ev PC→uyku turu ve her koşudan sonra yeni süreçte kayıt açma PASS; PR CI test/Windows build kontrolleri de geçti. [Rapor](../reports/P4-CONTINUOUS-SESSION-157.md). Bu işlem diğer ekip üyeleri adına inceleme/kabul veya faz/issue kapanışı değildir.

16 Ekim birleşim sonrası kontrol: `72c5bbc` üstünde yeniden alınan Windows build ve iki gerçek süreçli fiziksel dünya turu PASS; aynı kampanya yeni host sürecinde gün 2/önceki özet/ev başlangıcıyla açıldı. Yeni alan görevleri: #170 Mehmet — tek oturumda tam günlük ürün döngüsü (#157 alt işi); #171 Mert — görsel örnek kiti; #172 Utku — yol ölçümü/örnek yerleşim; #173 Mehmet — yakın kamera/insan ölçeği/dört kişi geçişi (#159 alt işleri). [İş sırası ve kapılar](P4_NEXT_WORK_ORDERS.md). P4.7 araştırma/ölçüm/kaynak hazırlığı yapılabilir; oyun uygulaması #138/#140/#141 teknik temel kabulünü bekler. Yeni sanat/içerik ve ayrı bilgisayar/üç gün kabulü açık kalır.

## Ekip

16 Ekim Mehmet alan işi, PR #175: #170'in sıfır bakiyeden gerçek av/satış/kamera/klip/tekne/PC/uyku ve diskten açma zinciri `5cea97d` Windows build'inde aynı PC'de 1/2/4 gerçek süreçle PASS; üç kampanya da yeni host sürecinde doğru açıldı, tekrar yayın ikinci gelir üretmedi. İkinci avı engelleyen balık yaşam döngüsü ve arka koltuklar dolunca kuru iskeleden öndeki koltuklara binme sorunu düzeltildi. 1256/1256 EditMode ve Windows build PASS. [Rapor, klip ve süreç kanıtları](../reports/P4-PRODUCT-DAY-170.md). CI, Utku incelemesi, merge ve birleşmiş build kontrolü bekler; #170 açık kalır. #173 mevcut oyuncu/kamera/ev/tezgâh/iskele ölçüm hazırlığı [rapor ve ham ölçümle](../reports/P4-PLAYER-SCALE-173.md) teslim edildi; örnek kit, yakın görüntü ve görsel/dört kişi kabulü bekler. Ayrı PC/dört insan/üç gün ve #141 ölüm/00:00/host kaybı kabulü bu yerel koşularla kapanmaz. P4.6/P4.7 kapıları korunur.

| Rol | Kişi | GitHub hesabı | Ana alan |
|---|---|---|---|
| A | Mehmet | `mehmetalisahingm` | Oyuncu, ağ, ekipman kullanımı, capture ve araç hareket bağlantıları |
| B | Utku | `Utkuuzun14` | World, sualtı, canlılar, rotalar, keşif ve kamera hedef/world doğrulaması |
| C | Mert | `MertKAYAR` | Ev/kasaba, ekonomi, UI, save/load, kanal ve progression |

P4 birleştirme koordinatörü: **Utku**. Herkes kendi alanının kodunu, entegrasyon seam'ini ve gerçek co-op kanıtını birlikte teslim eder.

## Faz durumu

| Faz | Durum | Not |
|---|---|---|
| P0 | KAPALI | Ortak Unity temeli |
| P1 | KAPALI | Ağ oturumu / temel co-op |
| P2 | KAPALI | Oksijen, av, çanta ve güvenli dönüş |
| P3 | KAPALI | İnsan/kıyı/NPC/sandal/harita/kayıt-ekonomi temel döngüsü |
| P4 | **AÇIK** | Ev, keşif, medya, ileri ekipman/tekne, boss ve yaşayan dünya |
| P5 | KİLİTLİ | Hata, denge, performans ve sertleştirme |
| P6 | KİLİTLİ | Final teslim |

## P4.1 — Ev, gün ve keşif

**Durum: TAMAMLANDI / entegrasyonda.**

Teslim edilen ana parçalar:
- ortak ev, dört yatak, fiziksel uyku etkileşimi ve sabah normalize akışı,
- ortak saat, 00:00 kapanışı, erken uyku kapısı ve tek günlük özet,
- ortak depo ve kalıcı depolama,
- gün/gece world modeli,
- keşif hücreleri, derinlik bandı ve tür gözlem otoriteleri,
- keşif haritası/sis, ansiklopedi ve save/load,
- gerçek oyuncu/world binding'i, player/boat/ping harita bağlantısı.

Önemli birleşen PR'lar: `#91`, `#92`, `#93`, `#94`, `#95`, `#96`, `#97`, `#98`, `#99`.

## P4.2 — Çek, izle ve yayınla

**Durum: TAMAMLANDI. Üç ana teslim merge edildi ve birleşik acceptance kapısı #112 / #106 ile PASS olarak kapandı.**

### A — Mehmet · gerçek clip capture / playback
- PR `#104` merge edildi.
- Gerçek oyuncu kamerasından host-authoritative frame capture.
- Deterministik `recordingId -> clipId`.
- Yerel timestamp'li clip container, SHA-256 ve boyut manifesti.
- Güvenli dönüş sonrası `ClipArchive` kabulü.
- `ClipPlayback` seam'i.
- PR kanıtı: **812/812 EditMode PASS**, Windows build PASS ve gerçek 2-process koşuda oynatılabilir clip üretimi.

### B — Utku · recording world context
- PR `#105` merge edildi.
- `RecordingWorldContext`: subject kind, region, cell, depth band ve first-recording bilgisi.
- Gizli world/fish pozisyonu medya verisine sızdırılmaz.
- Mevcut species observation / exploration otoriteleri tüketilir; ikinci world authority yoktur.
- PR kanıtı: **856/856 EditMode PASS**.

### C — Mert · ev PC / kanal / medya save
- PR `#103` merge edildi.
- Ev PC arşivi, playback bağlantısı, başlık/yayın akışı ve ortak kanal.
- Aynı clip için duplicate yayın engeli.
- NPC veya kanal için tek ticari hak.
- Gün kapanışında izlenme/gelir settlement ve kalıcı medya kaydı.
- Gerçek 2-process `-Media` smoke host + client PASS; save/load sonrası tekrar ödeme/yayın yok.

### P4.2 kapanış kapısı

**KAPANDI.** Acceptance harness/raporu PR `#112` ile integration'a alındı; issue `#106` completed olarak kapatıldı.

Birleşik ürün akışında aynı güncel integration build'i üzerinde şu zincir doğrulandı:

1. Gerçek oyuncu kamerası ile kayıt başlatılır ve gerçek clip oluşur.
2. Recording world context clip manifestine aynı authoritative akışta bağlanır.
3. Güvenli dönüşten sonra clip ev PC arşivinde görünür.
4. Clip gerçekten playback edilebilir.
5. Geçerli clip kanala publish edilir; duplicate publish ikinci sonuç üretmez.
6. Gün kapanır; ertesi gün izlenme/gelir yalnız bir kez işlenir.
7. Oyun yeniden açıldığında clip, yayın, hak ve gelir state'i çoğalmadan geri gelir.
8. `-Explore`, `-Storage`, `-Day`, `-HomeSleep`, `-Trip` regresyonları korunur.
9. En az iki gerçek süreçte fixture'sız birleşik medya smoke PASS olur.

P4.2 acceptance tamamlandığı için **P4.3 aktif geliştirmeye açıldı**.

## P4.3 — Kamera ve büyük tekneler

**Durum: TAMAMLANDI / entegrasyonda.** PR #129, kabul edilen head `60e6306`, merge `515f5b6`. Merge sonrası kaynak ağacı aynı.

Gerçek iki süreçli filo satın alma/gerçek Reef/aktif seçim/restore; motorlu-resif ve araştırma-derin rota için binme → gidiş → demirleme → iniş/yeniden binme → dönüş → boş dock geçti. İki süreçte doğru gövde ve harita doğrulandı. #107/#108 kapalı; #109 önceden tamamlanmıştı. Eski PR #127, #129 tarafından karşılandı.

## P4.4 — Derin keşif ve boss

**Durum: TAMAMLANDI / entegrasyonda.** #124 final kabulü PR #129 ile geçti; #121/#122/#123 kapalı.

Gerçek iki süreçli akış: ansiklopedi → gerçek Reef → üç fiziksel iz → arena → iki ayrı oyuncunun zıpkın vuruşu → kuru güvenli dönüş → tek kalıcı boss completion → ikinci host açılışında restore. Aktif transient karşılaşma geri yüklenmiyor.

CI `37699987867` ve `37699982093`: Unity/EditMode + Windows build, P4.3 filo/rota ve P4.4 boss/restore kontrolleri SUCCESS. [Kapanış kanıtı](../reports/P4-3-P4-4-ACCEPTANCE.md).

Bu kanıt iki gerçek yerel/CI sürecidir; ayrı bilgisayar/internet, nihai sanat ve P4.6 üç günlük ortak kabul yerine geçmez. Başkaları adına manuel oynanış onayı üretilmedi. Kullanıcı iki ara teslimin kapanışını ve sonraki görevlerin açılışını istedi.

## P4.5 — Yaşayan dünya

**Durum: ARA TESLİM BİRLEŞTİ.** PR #135 merge `a1dc5c6`; Unity ve gerçek dünya CI #37922626883/#37922621369 SUCCESS; #130/#131/#132 kapalı. Ayrı bilgisayar ve nihai sanat kabulü bu sonuçtan türetilmez.

| Sahip | GitHub | Teslim |
|---|---|---|
| Mehmet | [#130](https://github.com/mehmetalisahingm/DeepDiveGame/issues/130) | Hafif rol etkileri, akıntı/hareket bağlantısı, ekip uyarıları ve pingler |
| Utku | [#131](https://github.com/mehmetalisahingm/DeepDiveGame/issues/131) | Sakin/rüzgarlı hava, gece davranışı, bir akıntı alanı, görev world uygunluğu |
| Mert | [#132](https://github.com/mehmetalisahingm/DeepDiveGame/issues/132) | Üç sipariş/üç sponsor, iki ev seviyesi/üç kasaba iyileştirmesi, rol UI/save ve dar denge turu |

Koordinatör Utku; hedef `codex/p4-integration`. P4.6 çalışması sürer; P5/P6 kilitlidir.

## Sıradaki ortak kapı

P4.6 #138/#140/#141 açık. PR #146 teknik kabul senaryoları merge edildi; `587cd17` başındaki Unity ve üç gün/mağaza/tekne/medya CI SUCCESS. PR #151/#152/#158 integration'a birleşti; son birleşim `a5acf2a`. Birleşim sonucu bütün ürün kabulü anlamına gelmez.

Kullanıcı üç kişi birlikte oynandığını ve genel oynanabilirliği onayladı. Dünya küçük, görsel kalite zayıf, yalnız tek balık gözlenmiş ve sandal 2/3 kalmış. Kullanıcının bildiriminden dört oyuncu/ayrı bilgisayar/üç gün veya nihai görsel kabulü üretilmez.

#153 dünya ölçeği, #154 görsel kalite, #155 tür görünürlüğü, #156 sandal yönlendirmesi ve #157 Mehmet hareket/ağ işleri açık ürün teslimleridir. #156'nın paneli elle keser/çivi tamiri sayılmaz.

**Yeni sıra:** P4.6 teknik temel → P4.7 kısa görsel örnek → P4.8 fiziksel sandal tamiri → P4.9 ortak el/beden eylemleri → P4.10 kasaba/ev → P4.11 canlı deniz → P4.12 gerçek nihai kabul. Hazırlıklar paralel; uygulama faz kapılarına bağlı. [Detaylı plan](PLAYER_EXPERIENCE_EXPANSION.md).

P4.7–P4.12 görevleri GitHub #159–#164 olarak açık; ayrıntılı sahiplik/başlama kapısı ilgili issue ve plan tablosundadır.

13 Ekim ayrıntılı planı: [oyuncu deneyimi](EXPERIENCE_DESIGN.md), [üretim/iş parçaları](PRODUCTION_BLUEPRINT.md), [ileriye dönük yol haritası](FUTURE_ROADMAP.md). R01/R02 gibi açık tasarım kararları kayıtlıdır; kalıcı onarım şeması seçilmiş veya gelecek P7–P10 açılmış sayılmaz.
