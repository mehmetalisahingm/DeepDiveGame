# Dalış geçişinde sürüm uyumsuzluğu — 12 Eylül 2026

- Kullanıcı ekranındaki `3779525944`, `DiveTestArea/Fish_SeaBass` ağ nesnesinin kimliği. Karşı istemci bu nesneyi bulamıyor. Arkadaşın build'i yerelde mevcut olmadığından iki dosya paketi doğrudan karşılaştırılamadı.
- P1, P2 ve P3 başlangıç build'leri, balık/oyuncu ağ düzeni değişmesine rağmen `DeepDive-P1-2` protokolünü kullanıyordu. Eski build'in lobiye kabul edilmesi mümkündü.
- Güncel protokol `DeepDive-P3-1`; ekranda görünür. Eski protokol lobiden önce `ProtocolMismatch` ile reddedilir ve aynı güncel oyun klasörünü kullanma uyarısı gösterilir. Bu sürüm etiketi otomatik dosya hash'i değildir; ileride ağ nesnesi/sahne düzeni değiştiğinde artırılmalıdır.
- Windows build başarılı. `Test-P1-Integrated.ps1 -Players 2 -Port 21777`: host ve güncel istemci PASS; eski protokollü istemci beklenen ret ile PASS. Lobby → Prep → Dive → Return, iki oyuncunun yürüme/yüzme senkronizasyonu ve yeniden katılma doğrulandı; kaydedilen hata yok.
- Yerel kanıt: `Logs/P1-integrated-20260912-125950-873-2`. Ayrı bilgisayar/internet testi yapılmadı. Her iki kişi güncel paketin tamamını kullanmalı; yalnız exe değiştirmek yeterli değildir.
- Önceki `-Hunt` denemesinde sahne geçişi ve balık senkronizasyonu hatasızdı; ancak host avın çantaya eklendiğini doğrulayamadı. Tam av döngüsü bu çalışmada PASS sayılmadı; bağlantı düzeltmesinin dışında kaldı.
