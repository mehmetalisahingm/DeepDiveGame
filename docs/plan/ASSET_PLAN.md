# Görsel ve ses kaynak planı — taslak (P0-C)

Durum: TASLAK. Bu belge [P0_MEETING T01](P0_MEETING.md) ve [Görev #3](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3) kapsamında Mert'in hazırladığı kaynak/üretim planıdır. Nihai varlık üretimi veya satın alma değildir; hiçbir dış varlık bu belgeyle repoya eklenmiş sayılmaz. P0_MEETING'de belirtildiği gibi ücretli paket seçimi ve kapsamlı üretim P0'ı beklemez — burada yalnızca yöntem ve kontrol listesi netleşir.

Kaynaklar 2026-09-06 itibarıyla bilinen genel şartlarıyla listelendi; her paket/dosya indirilmeden önce kendi lisans sayfası ayrıca kontrol edilir. Lisans şartları kaynak tarafından değiştirilebilir; burada yazılan özet resmi lisans metninin yerini tutmaz.

## Görsel ihtiyaç kategorileri

| Kategori | Örnek içerik | Önerilen kaynak yöntemi | Lisans notu |
|---|---|---|---|
| Karakter / dalgıç ekipmanı | Dalgıç modeli, tüp, palet, maske, çanta | [Unity Asset Store](https://assetstore.unity.com/) (3D karakter/ekipman paketleri); alternatif olarak [Sketchfab](https://sketchfab.com/) üzerinde CC0/CC-BY işaretli modeller | Asset Store: paket başına EULA farklı, [Unity Asset Store şartları](https://unity.com/legal/as-terms) her paket öncesi tekrar okunur. Sketchfab: model başına yükleyenin seçtiği lisans (CC0'dan CC-BY-NC-ND'ye kadar değişir) — her model tek tek kontrol edilir |
| Deniz canlıları (balık, avcı, nadir tür) | Yüzen balık modelleri, animasyonlu canlılar | Unity Asset Store (sualtı/deniz canlısı paketleri); Sketchfab (CC0/CC-BY modeller) | Aynı — paket/model başına lisans kontrolü şart; NC (ticari olmayan) etiketli modeller ticari hedef netleşmeden kullanılmaz |
| Kasaba / dükkân sahneleri | Bina, iskele, dükkân içi mobilya, çevre kiti | Unity Asset Store (çevre/yapı kitleri); [Kenney.nl](https://kenney.nl/assets) (CC0 yapı/mobilya kitleri); [Poly Haven](https://polyhaven.com/) (CC0 texture/HDRI, model) | Kenney ve Poly Haven: **CC0**, atıf gerekmez. Asset Store: paket EULA'sı geçerli |
| VFX (su kabarcığı, ışık huzmesi, çekim efekti) | Parçacık efektleri, sualtı ışık/sis | Kenney.nl (CC0 parçacık paketleri); Unity Asset Store (VFX Graph paketleri) | Kenney: CC0. Asset Store: paket EULA'sı |
| UI ikonları | Envanter, dükkân, hazır olma ekranı ikonları | Kenney.nl (CC0 UI/ikon paketleri); [game-icons.net](https://game-icons.net/) | Kenney: CC0, atıf gerekmez. game-icons.net: **CC-BY 3.0**, her ikon için atıf gerekir (siteden atıf metni alınır) |

## Ses ihtiyaç kategorileri

| Kategori | Örnek içerik | Önerilen kaynak yöntemi | Lisans notu |
|---|---|---|---|
| Sualtı ambiyansı | Dalış sırasında sürekli ortam sesi | [Freesound.org](https://freesound.org/); [Zapsplat](https://www.zapsplat.com/) | Freesound: ses başına lisans değişir (**CC0, CC-BY, CC-BY-NC** — her dosyanın sayfasında ayrı yazar). Zapsplat: ücretsiz hesapla **atıf gerekir**, ücretli abonelikte atıfsız kullanım |
| Oksijen / nefes sesi | Nefes alma, düşük oksijen uyarısı | Freesound.org; Zapsplat | Aynı — dosya başına kontrol |
| Zıpkın / av sesi | Atış, vuruş, av yakalama geri bildirimi | Freesound.org; [Mixkit Sound Effects](https://mixkit.co/free-sound-effects/) | Freesound: dosya başına kontrol. Mixkit: kendi ücretsiz lisansı (ticari kullanıma izin verir, atıf zorunlu değil) — CC değil, [Mixkit lisans sayfası](https://mixkit.co/license/) okunur |
| Kamera kayıt sesi | Deklanşör, kayıt başladı/bitti sesi | Freesound.org; Mixkit | Aynı |
| UI sesleri | Buton tıklama, envanter, satış geri bildirimi | Kenney.nl (CC0 arayüz ses paketleri); Mixkit | Kenney: CC0. Mixkit: kendi lisansı |

## Lisans tiplerinin kısa özeti

- **CC0 / Public Domain** (Kenney.nl, Poly Haven, bazı Freesound dosyaları): atıf gerekmez, ticari kullanım serbest. En düşük risk.
- **CC-BY**: atıf zorunlu; atıf metni [CREDITS](../assets/CREDITS.md) benzeri bir dosyada (ilk dış varlık eklenirken oluşturulur) tutulur.
- **CC-BY-NC / CC-BY-NC-SA**: ticari olmayan kullanımla sınırlı. Proje ticari hedefi henüz netleşmedi ([P0_MEETING T03](P0_MEETING.md)); T03 kararına kadar bu tür varlıklar kullanılmaz veya yalnızca geçici/yerel test amaçlı tutulup repoya eklenmez.
- **CC-BY-SA**: paylaşım aynı lisansla yapılmalıdır; türetilmiş/değiştirilmiş varlığın da aynı şartla paylaşılması gerekebilir — kullanılacaksa bu yükümlülük ayrıca değerlendirilir.
- **Marketplace'e özgü EULA** (Unity Asset Store, Zapsplat, Mixkit): CC lisansı değildir, kendi şartları geçerlidir; "ücretsiz indirme" tek başına ham dosyayı public repoda paylaşma izni anlamına gelmez.

## Ham varlık public repoya eklenmeden önce kontrol listesi

- [ ] Kaynağın güncel lisans sayfası (paket/dosya bazında) okundu; yalnızca "ücretsiz" etiketine güvenilmedi.
- [ ] Ticari kullanım izni var mı kontrol edildi; proje ticari hedefi netleşmeden (T03 açık) NC lisanslı varlık kullanılmadı.
- [ ] Atıf gerekiyorsa atıf metni not edildi ve CREDITS dosyasına eklenecek şekilde kaydedildi.
- [ ] Lisans, dosyanın **olduğu gibi public repoda paylaşılmasına** izin veriyor mu (bazı Asset Store EULA'ları ve CC-BY-NC-ND gibi lisanslar yalnızca projede kullanım izni verir, yeniden dağıtımı/paylaşımı yasaklayabilir).
- [ ] Ekip kullanım hakkı: satın alınan/lisanslanan varlık tek kişi hesabına mı bağlı, yoksa ekip/proje için kullanılabilir mi.
- [ ] Kaynak bağlantısı, indirilen sürüm/tarih ve lisans özeti kayıt altına alındı (lisans ileride değişebilir; indirme anındaki hâli referans alınır).
- [ ] Büyük ikili dosya ise [WORKFLOW.md](WORKFLOW.md) uyarınca Git LFS kurulumu önceden yapıldı.
- [ ] Kişisel ödeme/hesap bilgisi repoya yazılmadı.
- [ ] İzin belirsizse varlık eklenmedi; yerine geçici kendi üretimimiz kullanıldı ([P0_MEETING](P0_MEETING.md) sınırı).

## Sonraki adımlar / açık noktalar

- Bu plan taslaktır; nihai kaynak seçimi ve satın alma kararı verilmedi. P4'te liste tamamlanacak ([P0_MEETING](P0_MEETING.md)).
- İlk dış varlık eklenmeden önce bu kontrol listesi tekrar uygulanır; sonucu docs/assets/CREDITS.md (o an oluşturulacak) içinde kaydedilir.
- T01–T07 ekip görüşmesi, branch push/pull erişim testi, temiz klondan kurulum ve örnek Windows build testi bu belgenin kapsamı dışındadır; ayrıca ele alınacaktır.
