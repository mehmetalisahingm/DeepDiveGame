# P1 kapanışı ve P2 açılışı — 10 Eylül 2026

Kullanıcı farklı evlerden bağlantı denemesinden sonra “tamam bu aşama bitti” ve
“sonraki aşamayı aç” talimatlarını verdi. P1 bu açık kullanıcı kararıyla kapatıldı;
P2 açıldı. Bu, aşağıdaki eksik kontrollerin PASS veya ekip onayı sayılması değildir.

- Teslimler: Mehmet #16, Mert #17, Utku #18; ortak entegrasyon #19.
- Entegrasyon: `ec9b27a`; test edilen oyun kaynağı: `9f9b99cd31841f8cd31457de2919376837e89b8b`.
- Mevcut Windows build: `Builds/P1-Integrated/DeepDiveGame-P1.exe`.
- [Yerel kanıt](../evidence/P1-integrated-local-results.json): 12 otomatik test,
  solo/2/4 oyunculu ayrı süreç testleri başarılı. Bu kapanışta yeni build alınmadı.
- [İnternet bağlantısı](P1-INTERNET-CONNECTION.md): farklı evlerden bağlanma ve
  diğer oyuncuyu görme kullanıcı bildirimiyle tamamlandı.

## P2'ye devredilen açık kontroller

- Mehmet (#20): dört oyunculu internet, ortak dalış/dönüş, ayrılma/yeniden katılma
  ve host kopması senaryolarının ayrı bilgisayarlarda sonuçları; insan netcode
  incelemesi ve yedek sorumlunun belirlenmesi. Yerel sonuçlar internet PASS değildir.
- Mert (#22): branch koruması ve eksik ekip kabul kayıtlarının takibi. Utku/Mert
  adına onay üretilmedi. Bu eksikler P2 kapanışında ayrıca değerlendirilir.

P2 koordinatörü Mert. Görevler: Mehmet #20, Utku #21, Mert #22.
Ortak dal `codex/p2-integration`; özellik dalları buradan açılır ve buraya birleşir.
İlk ortak iş P2 av/vuruş/çanta sözleşmesini netleştirmektir; uzlaşı henüz yapılmış sayılmaz.
P2'nin 1/2/4 oyuncu ve kısa oynanış değerlendirmesi şartları korunur. P3–P6 kilitli.
Bu işlem yalnızca faz ve görev kayıtlarını açar; P2 oyun kodu eklenmedi.
