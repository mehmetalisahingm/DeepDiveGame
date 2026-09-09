# P1-A yerel teslim — 8 Eylül 2026

Mehmet'in kodu: `2c31d72`; [PR #16](https://github.com/mehmetalisahingm/DeepDiveGame/pull/16),
hedef `codex/p1-integration`. Sonraki rapor commit'i yalnız kayıt ve Unity'nin
ürettiği satır sonu boşluklarını düzenler. P1 kapanışı değildir.

Teslim: oda oluştur/katıl/ayrıl, dört oyuncu sınırı, oyuncu oluşumu, host yetkili
yürüme/yüzme, yerel kamera, ağ sahne yükleme, dalışa yeni katılım reddi ve kopma temizliği.

| Kontrol | Sonuç |
|---|---|
| Unity 6000.3.23f1 Windows Development build | PASS |
| 7 Unity testi: rezervasyon/kapasite, faz/protokol, kimlik temizliği, bozuk/eski/tekrarlı girdi, zaman aşımı ve döndürülmüş su hacmi | 7/7 PASS |
| Solo: yürüme, yüzme, hazırlık → sualtı → hazırlık, ayrılma | PASS |
| Dört ayrı Windows süreci: oyuncu listesi, herkesin hareketi/yüzmesi, ortak sahne, yalnız sahibinde kamera/ses dinleyicisi | PASS |
| Beşinci oyuncu | `RoomFull` ile reddedildi |
| Dalış başladıktan sonra yeni oyuncu | `WrongPhase` ile reddedildi |
| Host kapandığında üç katılımcının temiz biçimde bağlantıyı kapatması | PASS |
| Git LFS bütünlüğü, .meta eşleşmeleri | PASS |

[Makine/kişisel bilgi içermeyen sonuçlar](../evidence/P1-A-local-results.json),
[yeniden çalıştırma komutları](../P1_NETWORK.md).

Bu kontroller **aynı bilgisayarda**, gerçek ayrı oyun süreçleriyle yapıldı.
İnternet/ayrı bilgisayar testi, gerçek Mert `SessionState` ve Utku alanıyla entegrasyon,
Utku incelemesi ve insan netcode/yedek gösterimi yapılmadı; P1 açık kalır.
Görsel/fare hissi ve yüksek gecikmeli kullanım ayrıca oynanarak değerlendirilecek.

İlk testte dalışa katılım denemesi oyun açılışından önce zamanlandı ve `RoomFull`
görüldü. Test artık host'un gerçek sahne-yüklendi kaydını bekliyor; son tekrar geçti.
Su hacmi yaşam döngüsü testi Play Mode'a geçirilerek gerçek Unity callback'leriyle doğrulandı.
