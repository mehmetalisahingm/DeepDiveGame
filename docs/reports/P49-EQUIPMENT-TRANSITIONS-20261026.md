# P4.9 / #161 — ilk kuşanma geçişi paketi

26 Ekim 2026, Mehmet / 9-2. Unity uygulama kaynağı
`4e6ac1c1a1bbb983720be09b3f7213fda4eef9bb`, Unity `6000.3.23f1`, Windows.
EditMode kaynağı `b9f1ae1`; sonraki commit yalnız gözlemci test rotasını
düzeltir, üretim kodu ve Editor testleri aynıdır. Kanıtlar aşağıda kaynak ve
binary SHA256 ile ayrılır.

## Görünen davranış

Kamera/zıpkın değişiminde eldeki ekipman 0,14 saniyede aşağı iner; görünür
ekipman altta değiştirilir, yenisi 0,14 saniyede kalkar. Yerel model ve eller,
arkadaşın gördüğü el/beden ve ekipman aynı kabul edilmiş durumdan sürülür.
Hızlı geri seçim mevcut ara pozdan tersine döner; kamera kademesi değişimi de
aynı geçişi kullanır. Geç katılan görünüm eski bir kuşanmayı tekrar oynatmaz.

Kamera transformu sallanmaz; eller ve prop hareket eder. Model sabit pozuna
göre hareket eder, her frame biriken offset yoktur. Mevcut rig/kamera/zıpkın
modelleri kullanılır; yeni sanat kaynağı veya Animator klibi gerekmedi.

`EquipmentVisualTransition` yalnız kozmetik durumdur. Oyun kodu
`CurrentHeldEquipment` / host aynasını kullanmaya devam eder. `DisplayedEquipment`
geçişin o anki görünümüdür, oyun yetkisi olarak tüketilmez.

## Bu paketin eylem/state sınırı

| Gerçek kaynak | Görünen sonuç | Öncelik / kontrol |
| --- | --- | --- |
| Owner'ın kuşanma isteği → mevcut host Dive/aktiflik/kamera sahipliği doğrulaması | Kabul edilmiş `HeldEquipment` + `CameraTier` geçiş hedefi olur | Reddedilen istekte host görünümü değişmez; kamera satın almayan guest ile denendi |
| Kabul edilmiş aktif `Recording` | Kamera hazır pozuna alınır | Kayıt/REC/clip işlemi animasyonu beklemez |
| Host `RepairPart/RepairHammer` | Mevcut tahta/keser IK'si | Sıradan kuşanma geçişi iptal edilir, çivi teması önceliklidir |
| Passive / seated state | Kozmetik ara poz temizlenir | Var olan hareket/oturum kuralları korunur |

Animasyon giriş, ateş, av, sahiplik, kayıt, ödeme veya oyuncu kökünü yönetmez.
Yeni RPC/protokol/save alanı yoktur. Avı ele alma/NPC'ye fiziksel teslim ve
binme/inme eylemlerinin yeni sunumları bu dar 9-2 paketine dahil değildir;
9-3/9-4 ve #161'in tüm kabulü açık kalır.

## Doğrulama

| Kontrol | Sonuç |
| --- | --- |
| EditMode | **1368/1368 PASS**; 11 yeni geçiş/geç katılım/geri seçim/öncelik/kademe/kare aralığı senaryosu |
| Windows integrated development build | **PASS**, `P1_BUILD_SUCCEEDED` |
| İki gerçek Windows süreci | **Host + guest PASS**, `Logs/P46-product-20261026-212427-765-2` |
| İki bakışta indirme/kaldırma | İki raporda da `equipmentLoweredSeen` ve `equipmentRaisedSeen` true |
| Hızlı geri seçim | Host'ta `equipmentCancelled:true` |
| Kamera sahibi olmayan guest isteği | `unownedCameraRefused:true`; guest zıpkın görünümünde kaldı |
| Normal ürün günü | Üç gerçek dalış, iki satış, kazanılmış kamera, gerçek clip/oynatma/yayın, onarılmış tekne yakın seferi, uyku ve gün 2 PASS |
| Yeni host sürecinde kayıt | **PASS**; clip ilerleme ve tekrar yayının reddi dahil |

Reload, kuşanmayı tekrar oynatmaz; reload JSON'undaki yeni görsel bayraklar
bu yüzden false olabilir. Geçiş denemesi normal ürün gününde gerçek kamera
satın alındıktan sonra `SetHeldEquipmentLocal` üzerinden yapılır. Sahiplik,
para, actor konumu veya kayıt fixture'ı kurulmaz. Test özelliği yalnız
`-EquipmentVideo` / açık development sürücüsüyle aktiftir.

Tekrar:

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 25421 -EquipmentVideo -Mute
```

## Gerçek kamera kanıtı

- [Oyuncu kamerası videosu](evidence/p49-equipment-20261026/equipment-owner.mp4): 64 gerçek kare, yaklaşık 3,87 saniye.
- [Arkadaş kamerası videosu](evidence/p49-equipment-20261026/equipment-peer.mp4): 118 gerçek kare, yaklaşık 7,03 saniye; yaklaşma/bekleme de içerir.
- [Yerel kamera/eller](evidence/p49-equipment-20261026/equipment-owner.png), [uzak karakter](evidence/p49-equipment-20261026/equipment-peer.png).
- [Manifest, XML, üç süreç raporu ve kare zamanları](evidence/p49-equipment-20261026/manifest.json).

Kareler el pozu sonrasında gerçek owner kamera render isteğinden alınır;
masaüstü kaydı değildir. CSV: kare, gerçek zaman, görünen mod, geçiş aktif,
indirme oranı, gerçek host modu. VFR video gerçek süreleri korur; üretilmiş
görüntü/ara kare/hızlandırma yoktur. Videolar sessizdir; ses kabulü yapılmadı.

İlk `b9f1ae1` oyun koşusu gözlemci rotası kıyı duvarına yöneldiği için
durdurulmuş FAIL'dir (`211857-479-2`); iki hata raporu da korunur. Sonraki
rota mevcut kuru sahil yolundan yürür. Bu eski FAIL son PASS ile silinmedi.

#209, `2b40743` ile entegrasyona birleşti. #207 eski 1,535 m vaka ve #164
nihai ayrı PC/insan/kare süresi/görsel kabulü açık kalır. Bu iki süreç teslimi
P4.9'un bütünü veya P4 faz kapanışı değildir.
