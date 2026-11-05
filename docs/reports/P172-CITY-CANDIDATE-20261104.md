# #172/#153 — Uzak şehir adayı ve mağaza kapısı ölçümü

4 Kasım 2026, Utku (B). Taban `codex/p4-integration` @ `6446840`. Bu rapor ölçüm ve **öneridir**: sahne, Layout, prefab ve servis konumu değişmedi. #172, #153, #249 veya #162 kabulü değildir.

## Tekrar üretim

Unity 6000.3.23f1 batch mode:
- Bugünkü kasaba: `DeepDive.Editor.P4RouteAudit.Run` (gerçek `DiveTestArea`).
- Aday: `DeepDive.Editor.P172CityCandidateAudit.Run` ([kaynak](../../Assets/Editor/P172CityCandidateAudit.cs), [ham sonuç](evidence/p172-city/city-candidate.json)). Boş ve kaydedilmeyen sahnede düz zemin kurar, gerçek `NetworkDiver` kontrolcü değerleriyle yürür.

Kontrolcü: yarıçap 0,35 m, boy 1,80 m, skin 0,08 m, step 0,30 m, slope 45°, hız 4 m/s. Araç, zemin yoksa, 0,5 m'lik basamak çıkılırsa veya 0,6 m'lik aralıktan geçilirse sonuç yazmadan hata verir.

## 1. Bugünkü kasaba

Mesafe ve anchor değerleri 19 Ekim ölçümüyle aynı; 55 anchor'dan hiçbiri taşınmamış. Bütün yollar düşmeden ve takılmadan tamamlandı.

| Yol | Mesafe | Süre |
|---|---|---|
| Ev → iskele | 58,6 m | 14,7 s |
| Ev → ekipman → iskele | 69,7 m | 17,5 s |
| İskele → balıkçı → kayıt → PC → yatak | 67,9 m | 17,0 s |

Dört hizmet hâlâ z −29'da yan yana tezgâhlardır, kıyı kapısına yaklaşık 15 m uzaklıktadır. 4 Kasım kararındaki uzak şehir ve girilebilir mağaza henüz dünyada yoktur.

## 2. Aday: Q03-B, ekipman mağazası bina içinde

Yerleşim [P4-Q03-SCALE](P4-Q03-SCALE.md) B adayıdır: meydan x −10…26, z −122…−94; ev kapısı (−4, −330); kıyı ve iskele değişmez. Ekipman (−8, −112) içeride raf ve kasayla, kapısı meydana bakan (−2, −112) olarak varsayıldı.

| Yol | Mesafe | Süre |
|---|---|---|
| Ev → meydan | 228,1 m | 57,0 s |
| Meydan → kapı → raf → kasa → kapı → meydan | 41,7 m | 10,4 s |
| Balıkçı → kıyı kapısı | 84,6 m | 21,2 s |
| Balıkçı → iskele (satışın bir yönü) | 101,4 m | 25,4 s |
| Ev → iskele | 360,5 m | 90,1 s |
| Ev → mağaza içi → iskele | 388,2 m | 97,1 s |

Okuma:
- Ev içi yol bu araçta düz alındı. Bugünkü 14,5 m'lik gerçek ev içi yolla ev → iskele yaklaşık 367 m ve 92 s olur; bu Q03 modeliyle uyumlu ama hesaptır, yürünmedi.
- Mağazaya girmek doğrudan yola 28 m ve 7 s ekler. Raf, kasa ve ödeme süresi buna dahil değildir.
- Satış gidiş-dönüş yürüyüşü 50,8 s, 10 s teslim varsayımıyla yaklaşık 61 s. Q03 modeli bu tempoda günde 5 dalış veriyor (bugün 7); bu model sonucudur, oynanmadı.
- 23:00 uyarısında kıyıdan balıkçıya süre yaklaşık 35 s, 75 s'lik pencerenin içinde.

## 3. Mağaza kapısı ve koridor (#249)

Dört dalgıç aynı anda; bekleme ve insan yol vermesi yok. Karşılıklı geçişte herkes kendi sağındaki şeritte kalıyor. Bu yüzden FAIL, insanların yol vererek çözebileceği bir tıkanma olabilir.

| Açık genişlik | Dördü aynı yönde | 2 giren + 2 çıkan, kapı (0,3 m) | 2 giren + 2 çıkan, koridor (4 m) |
|---|---|---|---|
| 0,6 m | geçemez (kontrol) | geçemez | geçemez |
| 0,8–1,2 m | geçer, 3,8–3,9 s | **tıkanır** | **tıkanır** |
| 1,5 m | geçer, 3,8 s | geçer, 3,1 s | geçer, 3,2 s, 7 kare temas |
| 1,8 m | geçer, 3,8 s | geçer, temassız | geçer, 6 kare temas |
| 2,2 m | geçer, 3,8 s | geçer, temassız | geçer, 2 kare temas |
| 2,6–3,0 m | geçer, 3,8 s | geçer, temassız | geçer, temassız |

Basamak: 0,35 m'ye kadar çıkıldı, 0,5 m çıkılamadı. 0,35 m kapsülün yuvarlak altından geliyor; ayarlanan step 0,30 m'dir.

**#249 için öneri (karar değil):**
- Kapı açık genişliği en az 1,8 m; 1,5 m çalışan alt sınır ama payı yok.
- Raf ve kasa önü koridorları en az 1,8 m; dört kişi için rahat değer 2,6 m.
- Kapı eşiği 0,30 m'yi geçmesin, tercihen düz veya rampa.
- Bu değerler boş kutularla ölçüldü. Elde sepet, esnaf, raf collider'ı ve kamera ayrı denenmeli.

## 4. Ölçülmedi

- Gerçek bina, zemin eğimi, teras ve sokak engelleri (aday düz zemin ve boş duvardır).
- 1/2/4 gerçek süreç, ağ gecikmesi, kare süresi.
- Görüş hattı, yön bulma, gece okunurluğu, kullanıcı ölçek değerlendirmesi.
- Gerçek av/satış turu ve günlük tempo.

## 5. Sahne sırası için karar gerekenler

| Konu | Öneri | Karar sahibi |
|---|---|---|
| Ölçek | B ile blokaj; satış turu 1 dakikayı aşarsa merkezi kıyıya yaklaştır (A) | Mehmet, Mert |
| Günde 5 dalış (bugün 7) kabul mü | Gerçek turda ölçülene kadar aday kalsın | Mert, kullanıcı testi |
| Düz mü teras mı | İlk blokaj düz | Mehmet, Mert |
| Mağaza kapısının konumu ve yönü | (−2, −112), meydana bakan | Mert |
| Sıra | Önce Utku yol/zemin blokajı, sonra Mert bina kabuğu ve kapı, sonra Mehmet raf/kasa bağlama | Üçü birlikte |

Ölçüm #244'ün birleşmesinden önceki tabanda alındı. #244 deniz tabanını değiştirdi, kasaba anchor'larını taşımadı, ama aynı sahne dosyasını kullandığından blokaj güncel entegrasyondan başlamalıdır. Q03 raporundaki 25 sabit koordinatlı dosya ve smoke sürücüleri de blokajla birlikte taşınmalıdır.
