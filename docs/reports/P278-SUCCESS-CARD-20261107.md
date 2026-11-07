# #278 (Mert) — İlk gerçek başarıda sonuç kartı, ses ve kutlama

7 Kasım 2026 · Mert · dal `p4/mert-town-placement` (PR #281) · Mehmet'in #279'u (ilk 150 saniyelik hedef rehberi) ile birlikte çalışır, onu değiştirmez.

## Ne eklendi

Host'un kabul ettiği gerçek bir av satışı veya kayıt teslimi sonrası, işlemi yapan oyuncunun ekranında bir **sonuç kartı**:

- Başlık (`AV SATILDI!` / `KAYIT TESLIM!`), teslim edilen adet.
- **Önce → sonra:** `PARA 120 > 360 (+240)`; fark kısa süre sayarak yükselir.
- **Sıradaki hedef:** kamera basamağına göre (Temel 150 → Gelişmiş 400 → Profesyonel 900 → tekne/kanal), "X kredi eksik" ya da "paran yetiyor, dükkâna git" ve ne işe yaradığı.
- **Ses:** kısa, üretilen yükselen çan (lisanslanacak varlık yok); ilk başarıda dört notalı daha uzun sürüm.
- **İlk başarı kutlaması:** süreç başına ilk kabul edilen teslimde "ILK BASARI!" başlığı, altın çerçeve, 9 sn kart ve konfeti.

Yalnızca çoğaltılmış durumdan beslenir (`LastServiceRequestId/Accepted/Amount/Items`, `SharedBalance`, `EquippedCameraTier`); hiçbir kredi, kayıt veya sayaç yazmaz, yeni RPC/NetworkVariable yok. İstek kimliğine bağlı olduğu için aynı satış iki kez kutlanmaz; reddedilen, 0 kredili ya da başka servis (satın alma) kart çıkarmaz. Yalnız sahibin ekranında görünür.

Dosyalar: `SuccessFeedbackRules.cs` (saf kurallar + ses üretimi), `SuccessFeedbackView.cs` (kart, ses, kutlama), `SuccessFeedbackBinding.cs` (sahibin oyuncusuna ekler), `P278SuccessFeedbackTests.cs` (7 test).

## Kanıt

- EditMode `P278SuccessFeedbackTests`: 7/7.
- Gerçek Windows oynatıcısında kartın ekran görüntüsü (oyun penceresi): `docs/reports/evidence/p278-success-card/card-first.png` (ilk başarı) ve `card-plain.png`. Ağ olmadan `TownLookProbe` içinden gösterildi; IMGUI kamera çıktısında olmadığı için ekran yakalama kullanıldı.
- Gerçek ağ yolu: `Test-P46-ProductDay -Players 1` sonucu ve `P278_SUCCESS_CARD` günlük satırı aşağıda.

`Test-P46-ProductDay -Players 1` geçti (gün 2, hata yok). Günlükte gerçek ağ satışlarında kart oluştu: `P278_SUCCESS_CARD title=ILK BASARI! AV SATILDI! before=0 after=120 first=True` ve sonraki satışta `title=AV SATILDI! before=200 after=320 first=False` (ilk başarı yalnız bir kez).

## Yapılmadı / açık

- **PC'den klip yayını** için önce/sonra (izlenme, abone) kartı yok; kanal sayıları #255 ve Mehmet'in medya aynası ile birlikte ele alınmalı.
- Mehmet'in #279 paneli ile aynı ekranda yan yana görüntüsü ve iki oyunculu (biri avlanırken diğeri çeken) insan kabulü yapılmadı; #164'e kalır.
- Kart IMGUI'dir (projede mevcut HUD gibi); nihai UI sanatı değil.
- Ses üretilmiş basit bir çandır; kulakla değerlendirme yapılmadı, yalnız uzunluk/tepe/kırpma testi var.
