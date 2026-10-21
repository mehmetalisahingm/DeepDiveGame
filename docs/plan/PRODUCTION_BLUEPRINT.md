# DeepDive üretim, bağımlılık ve kabul planı

13 Ekim 2026 — [oyuncu deneyimi tasarımının](EXPERIENCE_DESIGN.md) uygulanabilir iş parçaları. Faz kapıları [PHASES](PHASES.md), açık durum [STATUS](STATUS.md) içindedir. Bu iş listesi atama/planlama kaydıdır; saat tahmini, ekip onayı veya uygulama başlangıcı değildir.

19 Ekim ek iş kırılımı: [WORLD_QUALITY_DELIVERIES Q01–Q14](WORLD_QUALITY_DELIVERIES.md). Aşağıdaki 7–12 üst işleri korunur; özellikle 10-1, 11-2, 11-3 ve 11-5 artık tek PR veya tek kabul kutusu değildir. Su yüzeyi/iniş/habitat/balık/değer/boss ile şehir ölçeği/mimari/dükkân/ev kendi bağımlılığı, alan sahibi ve gerçek oyun kanıtıyla küçük paketlere ayrılır. Önce kara ve deniz kalite örneği; P4.10'da ölçek, sonra mekânlar; P4.11'de su/iniş, habitat, tür/değer, boss ve yolculuk. Sonraki faz kodunu başlatma kapıları korunur.

## 1. Üç katmanlı iş takibi

- Ana faz: P4.7–P4.12; ürün sonucu ve kabul kapısı.
- Alan işi: Mehmet A, Utku B, Mert C; dosya/sahne/varlık sahipliği.
- Kanıt: derleme, kural testleri, gerçek runtime, ayrı bilgisayar ve ürün/görsel kabulü.

Bir alanın bitmesi fazı kapatmaz. Günlük eylemin başarısı görüntü/ses kadar host/state/save davranışına bağlıdır. Her küçük PR oynanabilir bir sonucu hedefler; “bütün sanat geldi, sonra entegre edilir” teslim sayılmaz.

Mevcut #153–#157'ye paralel yeni kopya işler üretilmez: genişletilmiş sahne/sanat işleri #159/#162/#163'e alt teslim olarak, Mehmet hareket işi #160/#161'e bağlantı olarak izlenir. #156 yönlendirme ve #160 fiziksel tamir farklı sonuçlardır.

## 2. Aşama bazında iş parçaları

### P4.7 / #159 — Örnek kalite ve yerleşim

| Sıra | Alan işi | Sahip | Girdi | Çıktı / kabul |
|---|---|---|---|---|
| 7-1 | Mevcut sahne/placeholder ve yol ölçümü | Utku + Mehmet | #151/#152/#157 world temel | Dünya bounds, yaya süreleri, collider/anchor haritası |
| 7-2 | Modüler sanat örneği | Mert | Mevcut stil ve kaynak seti | Bir ev cephesi, bir dükkân, kıyı/sandal; kaynak kaydı |
| 7-3 | İnsan/nesne yakın kamera testi | Mehmet | Rig/tahta/alet örneği | Kadraj, ölçek, tutuş, dört kişi geçişi |
| 7-4 | Üç görüş ve gece/ses örneği | Utku + Mert | Kısa oynanabilir bölüm | Ön/sonra karşılaştırma; okunur hedef, ortam geçişi |
| 7-5 | Ölçek ve kalite kararı | Üç alan + kullanıcı | Gerçek build, R04/R06/R07 | Hedef örnek kabulü; geniş üretim için tekrar kullanılabilir kit |

İlk derleme bağımlılığı teknik P4.6 temelin kabulüdür; araştırma/çizim/kaynak envanteri beklemez. Yeni geometri dünya otoritesi ve harita dönüşümü için ikinci koordinat sistemi kurmaz.

### P4.8 / #160 — Fiziksel tamir

| Sıra | Alan işi | Sahip | Girdi | Çıktı / kabul |
|---|---|---|---|---|
| 8-1 | R01/R02/R11 ve eylem sözleşmesi | Mehmet + Utku + Mert | Eski mask/2-3 kampanya, otomatik pickup/purchase contribution | Parça/alt-slot, edinme/ücret/tamir ayrımı, iptal/replay/save kararı |
| 8-2 | Parça, keser ve gövde kit'i | Mert | Kabul edilmiş stil/karar | 0/3 ve bütün kısmi kombinasyonlar, tutuş/yuva noktaları |
| 8-3 | Kaldır/taşı/bırak/yerleştir | Mehmet; Utku world | Kit ve yuva | Gerçek input; tek taşıyıcı, güvenli bırakma |
| 8-4 | Darbe ve katkı transaction'ı | Mehmet; Mert save | Eylem sözleşmesi | Son doğru adım tek tüketim; iptal/kopma kayıpsız |
| 8-5 | World erişim ve dört yaklaşma yeri | Utku | #156 ipuçları, #157 toplama kapısı | Ücretsiz parça yolu ve çarpışma doğrulaması |
| 8-6 | Darbe sesi/mesh/uzak görünüm | Üç alan | Aynı authoritative olay | Birinci/üçüncü şahıs gerçek video |
| 8-7 | Kayıt ve hata kabulü | Mert + Mehmet | Eski kayıt + gün/host kopması | Tüm mask kombinasyonları; parça yok olma/çoğalma yok |

Bitmemiş darbe animasyonunu save'de kısmi tamam sayma kararı verilmez. Çok tahta sunumu varsa alt-slot tasarımı önce çözülür; “üç katkı yetiyor” diye her tahta görünümü beklentisi silinmez.

### P4.9 / #161 — Ortak eylemler

| Sıra | Alan işi | Sahip | Girdi | Çıktı / kabul |
|---|---|---|---|---|
| 9-1 | Mevcut eylem/state envanteri | Mehmet | NetworkPlayer/presentation | İstemci niyeti, host sonuç, hareket kilidi listesi |
| 9-2 | Kuşanma ve el tutuşları | Mehmet + Mert | P4.8 kit/rig | Kamera/zıpkın/alet dönüşü, hedefi kapatmayan poz |
| 9-3 | Av ve tezgâh teslimi | Mehmet + Mert | Mevcut catch/turn-in | Dolu çanta/ret/ödeme sunumu; tek otorite |
| 9-4 | Binme/inme ve suda el/beden | Mehmet + Utku | Koltuk/dock/anchor | Yolcu ve gövde senkron; izinli durumlar |
| 9-5 | Seçenek/rahatsızlık kontrolü | Mehmet + Mert | Yakın kamera/UI | Sallanma, input, okunurluk ve iptal değerlendirmesi |
| 9-6 | 1/2/4 oyuncu görünüm | Her alan | Aynı build | Yerel ve arkadaş kamerasında doğru model/eşya |

Animasyon root motion mevcut fiziksel hareketi ikinci kez yürütmez. Yeni input durumları otorite/bag/capture akışından bağımsız ikinci işlem yaratmaz.

### P4.10 / #162 — Kasaba ve ev

| Sıra | Alan işi | Sahip | Girdi | Çıktı / kabul |
|---|---|---|---|---|
| 10-1 | Kısa örneği geniş yerleşime yayma | Utku + Mert | P4.7 kit/ölçek | Meydan/yan yollar/kıyı; tekrar ve boşluk kontrolü |
| 10-2 | Dört esnaf çalışma seti | Mert | Mevcut dört stable hizmet | Tabela/tezgâh/sergi/NPC jestleri, gerçek satın alma yolu |
| 10-3 | Ev/depo/PC/yatak oyuncu yolu | Mehmet + Mert | Anchor rig | Dört kişi aynı odada iş yapabilir |
| 10-4 | Gelişim ve trofe görünümü | Mert | Mevcut development/save olayları | İki ev/üç kasaba farkı; yeniden açmada tek nesne |
| 10-5 | Yüzey ve zone sesleri | Mert; Utku zone, Mehmet adım | Yüzey/zone state | Taş/ahşap/kum/su ve ev/kıyı geçişi |
| 10-6 | UI sadeleştirme/işlem geri bildirimi | Mert + Mehmet | Eylem/servis sonucu | Ekranları örtmeyen HUD; anlaşılır ret |
| 10-7 | Kullanıcı kasaba/ev kabulü | Üç alan | Tam gerçek rota | Yerinde işlem, gece/gündüz, save/load |

NPC dekor animasyonunun ayrı NetworkObject olması gerekmez; yalnız sonucu/etkileşim state'ini etkileyen kısmı otoriteye bağla. Kaynak varlık hazır olduğunda ilgili alan sahibi hemen gerçek hizmete bağlar.

### P4.11 / #163 — Canlı deniz

| Sıra | Alan işi | Sahip | Girdi | Çıktı / kabul |
|---|---|---|---|---|
| 11-1 | #155 spawn/tür zinciri teşhisi | Utku | Katalog→prefab→host spawn→client→erişim | Gerçek tür/habitat matrisi, tek tür nedeni |
| 11-2 | Beş canlı ve üç kesim seti | Mert + Utku | R05, kabul edilmiş stil | Ayrı siluet/davranış, uyumlu çevre |
| 11-3 | Su/ışık/partikül/ses geçişi | Utku + Mert | WaterRules/zone/day | Yüzey ve derinlik okunur, doğru grafik maliyeti |
| 11-4 | Üç tekne yolculuk sunumu | Mehmet + Mert; Utku rota | Gerçek filo/route state | Gövde, koltuk, kürek/motor, ses/su izi |
| 11-5 | Boss işaret/karşılaşma sunumu | Utku + Mehmet + Mert | Mevcut boss evreleri | Üç saldırı hazırlığı, siper, çekim/av |
| 11-6 | Kamera/av/görev/ansiklopedi kimliği | Üç alan | SpeciesId/manifest/observation | Aynı tür her yolda aynı, gizli konum sızmıyor |
| 11-7 | 1–4 kişi ve kayıt maliyeti | Mehmet ölçüm; her alan düzeltme | P4.11 gerçek build | CPU/GPU/kare süresi/disk en pahalı sahneler |

Beş türün görsel kaynaklarını bütün dünya otoriteleri hazır olmadan bir kerede üretme. Önce iki kıyı türü uçtan uca; sonra resif, derin ve boss. Bu alt sıra yeni faz atlama değildir; aynı teslimin entegrasyon sırasıdır.

### P4.12 / #164 — Kabul

| Sıra | Alan işi | Sahip | Kanıt |
|---|---|---|---|
| 12-1 | Sabit Windows build/kayıt fixture tanımı | Mehmet + Mert | Commit/hash, kontrolcü, gerçek kampanya ve senaryo |
| 12-2 | İlk on dakika ve solo av/tamir | Her alan kendi davranışı | Video, takılma noktaları, ilk hedef/gelir |
| 12-3 | İki bilgisayar/dört süreç/üç gün | Üç kişi gerçek katılım | Oturum/donanım/roller/saat ve fiziksel tur |
| 12-4 | Eski kayıt, kopma ve disk | Mehmet + Mert; Utku world restore | Tekil eşya/para/görev/trofe/klip sonuçları |
| 12-5 | Grafik, ses ve içerik kabulü | Mert tutarlılık; kullanıcı/ekip | Ön/sonra ve gerçek hareket/eylem değerlendirmesi |
| 12-6 | Performans/dar düzeltme turu | Her alan | Aynı donanımda rapor; açık kusur sınıflaması |
| 12-7 | P4 kapanış ve main/build | Utku koordinasyon | Gerçek üç kişinin sonuçları; birleşmiş build kontrolü |

P4.6'daki teknik/ayrı bilgisayar/çok gün kontrolleri korunur. Yeni nihai sanat/deneyim karşılığı P4.12'de ayrıca kabul edilir; görev kapısını küçülterek daha önce yapılmayan işi yapılmış sayma.

## 3. Kritik yol ve paralel hazırlık

Kritik yol: teknik world/state/save temel → kısa sanat örneği → fiziksel onarım state/kit → ortak eylemler → kasaba/ev → canlı deniz → nihai kabul. Ana risk Mert'in bütün alanların sanat kaynağına tek başına yetişmesi ve ortak sahnelerin eşzamanlı değişmesidir.

Hazırlıkta paralel:
- Mehmet: eylem/state şeması, existing animation kontrolü, test altyapısı, input/iptal ihtiyaçları.
- Utku: world/anchor envanteri, tür/spawn teşhisi, collider/erişim ölçümü, yerleşim çizimi.
- Mert: kaynak/lisans/kit envanteri, stil tutarlılığı, eski save/ekonomi/UI açıkları.

Hazırlık “sonraki faz kodu yazıldı” sayılmaz. Bir aşamada kit hazır değilse alan sahibi mevcut fazın test/review/support işini alır; geleceğe habersiz geçmez.

Ortak sahne sırası world/anchor sahibi → oyuncu/etkileşim sahibi → sunum/servis sahibi. Ortak sözleşme değişiminde üç etkilenen alanın tüketimi kontrol edilir. Destek devri yapılırsa dosya, süre ve sonuç issue'da kayıtlıdır; kişinin tamamı uydurulmaz.

## 4. Varlık üretim listesi

| Paket | Minimum içerik | Varyant/kalıcılık | Önce bağlanacağı davranış |
|---|---|---|---|
| Kıyı kasaba kit'i | Ev ve dükkân parçaları, çatı/kapı/pencere, taş/ahşap zemin, tabela | Ana cepheler vs arka plan cepheleri | Kısa görsel yaya turu |
| Tamir kit'i | Kırık sandal, takılabilir parçalar, taşıma tahta, keser/çivi | Her mask kombinasyonu; varsa kalıcı alt-slot | Tek tamamlanmış katkı |
| Eller/beden | Mevcut rig uyumu, tutuş/kaldır/yerleştir/darbe | Yerel el ve uzak vücut | Solo + arkadaşın izlediği eylem |
| Esnaf kit'i | Tartı/kasa, ekipman rafı, kayıt ekranı, liman çizimi | Dört hizmet, aynı insan stili | Gerçek satış/satın alma |
| Ev kit'i | Yatak/PC/depo/pano/trofe, sıcak ışık | İki gelişim, idempotent restore | Gün/PC/depo/uyku |
| Deniz kit'i | Kaya/kum/bitki/batık, su materyali | Kıyı/resif/derin | Gerçek tür/rota/geri dönüş |
| Canlı kit'i | Beş species modeli/silueti ve boss | Her id/habitat/davranış uyumu | Av/çekim/ansiklopedi |
| Araç kit'i | Üç ayrı gövde, koltuk ve ambar/raf | Tek aktif araç; sahiplik restore | İskele/binme/rota |
| Ses/efekt | Yüzey/zone, alet, av/kamera/UI, motor/su, boss | Tekrarda farklı kısa varyantlar, mesafe/miks | Gerçek authoritative temas/olay |

Her paket için varlık manifesti: kaynak, izin/atıf, sürüm/tarih, dosya/.meta, ölçek/pivot, shader/material, prefab kullanıcıları, tutuş/anchor, collider, yakın/uzak detay, ses gain/loop ve gerçek build kareleri. Henüz seçilmemiş kaynak hazır kabul edilmez.

## 5. Teknik entegrasyon ilkeleri

| Sistem | Kalıcı olan | Geçici olan | Tek otorite |
|---|---|---|---|
| Onarım | Tamamlanan stable katkı/mask; R02 seçilirse alt-slot | Darbe/taşıma/rezervasyon | Mevcut repair/economy + host doğrulaması |
| Ekipman | Sahiplik/yükseltme/tahsis sınırı | El/beden animasyonu | Loadout/economy |
| Av | Kabul edilen capture/turn-in kimliği ve güvenli checkpoint | Yüzen canlı/av sunumu | Host catch/inventory |
| Ev/şehir | Satın alınan gelişim ve tekil trofe | Dekor/NPC çalışma animasyonu | Development/progression/save |
| Gün/kanal | Gün/sonuç/payment/publication kimliği | UI/PC kontrol/transfer ara durumu | Day/channel/archive |
| Tür/boss | Observation/keşif ve tamamlanan ödül | Canlı/boss transient evresi | World/boss authority |
| Tekne | Sahiplik/aktif seçim | Sefer, koltuk ve su izi | Fleet + trip + mover |

Scene yeniden yüklemede event subscribe/unsubscribe; host ve client aynı mesh state'i kurar. Kaynak nesneye iki kurulum scripti sahip olmaz. Save göçü yalnız kalıcı semantik için yapılır; animasyon ayrıntısı için kalıcı şema gereksiz büyütülmez.

## 6. Ölçüm ve hata bütçesi

Hedef donanım ekipten kaydedilmeden triangle/texture/network byte için kesin bütçe uydurulmaz. İlk örnekte baseline alınır; en pahalı dört durum: dört kişi kasaba, kalabalık yakın su, boss/su efektleri, eşzamanlı gerçek kamera.

Ölçümler: ortalama yerine kare süresi dağılımı ve uzun kareler; CPU/GPU ayrı; memory/VRAM; disk/klip boyutu ve transfer; capture açık/kapalı; sahne değişim/yeniden açma. 60 FPS referansı yaklaşık 16.7 ms kare süresidir; ölçüm hedefi başarılmış sonuç değildir.

Triage sırası:
1. Kayıp/çoğalan para/eşya, bozuk kayıt, host/client ayrışması.
2. Yürüyüş/etkileşim kilidi, su–kara çıkamama, imkânsız görev.
3. Ana oyuncu yolunda görünmez model, kırık animasyon, yanlış species.
4. Hedefi gizleyen ışık/sis, örtüşen UI, rahatsız edici ses/kamera.
5. Tekrarlayan uzun kareler ve bellek/disk yükü.
6. Küçük dekor/varlık çeşitliliği.

Yeni görsel özellik ilk beş türde hatayı artırıyorsa daraltılıp düzeltilir. “Test yeşil” ile kullanıcı hedefi tamam denmez.

## 7. Kanıt paketi ve issue kapanış şablonu

Her alan tesliminde:
- Problem/oyuncu etkisi ve final davranış.
- Tam commit/build ve test yöntemi.
- Değişen sahne/prefab/sözleşme/save.
- Yerel ve uzaktan görüntü; gerekli gerçek input.
- İptal/ret/kopma/eski save/co-op sonuçları.
- Donanım/kare süresi ve ilgili medya/disk ölçümü.
- Yapılmayan kontroller ve açık ürün kararları.

Dört görünüm klibi: ev–meydan–kıyı yürüme; elle tamir; av/teslim/kamera; gece/tekne/sualtı. Kamera/marketing görüntüsü gerçek input ve co-op kanıtı yerine geçmez.

Manuel kabul kaydı ekibin gerçek beyanını içerir; üç kişilik küçük denemeden otomatik dört kişi/iki bilgisayar/üç gün sonucu çıkarılmaz. Faz kapanışı tek raporla yapılır; ayrı imza matrisi üretilmez.

## 8. İlk hazır iş listesi

1. #157: mevcut phase/zone geçişi, Prep/Return parça kapısı ve gerçek ev–su yürüme açıklarını tamamla.
2. #155: gerçek build tür envanteri/spawn nedeni; ilerideki sanat beklenmeden mevcut fazın teşhisi.
3. #159 hazırlığı: kısa sanat seti ve ölçülü yerleşim önerisi; world gate kabulü sonrası uygulama.
4. R01: kürekli sandal semantiği; R02: görünür tahta sayısı/kalıcı alt-slot kararı.
   R11: mevcut pickup ve satın alma anındaki otomatik katkıyı tekil edinme/emanet ve fiziksel tamir transaction'ına ayır.
5. #160: bir tamir parçasını yerel+guest gerçek eylemle uçtan uca göster.
6. Kabul edilmiş parçadan diğer katkı/eylemlere genişle.

Takvim ancak gerçek ekip kapasitesi, seçilen varlıklar, R01/R02 ve ilk örneğin measured maliyetiyle çıkarılır. Bugün tarih vermek yerine bu bağımlılık ve hazır çıktılar kullanılır.

