# P4 #246 / #250 — Geç katılan oyuncunun mağaza sepetini görmesi: kanıt koşusu

Mevcut `ProductDayShopPresentation.LateShopTick`, **host gerçek raftan temel kamerayı seçtikten sonra** misafir Unity sürecinin katılmasını bekliyor; gerçek `SelectedShopItem` NGO aynası ve `ShopCarryView` kadrajı ölçülüyor. Daha önce test sürücüsü sabit UDP 25946 kullandığı için paralel ürün/dünya testleriyle çakışabiliyor ve sonuç yorumunu zorlaştırabiliyordu. Sürücü ayrıca yalnız `passed` boolean'a bakıyordu.

`tools/Test-P410-ShopLateJoin.ps1` artık script konumuna göre doğru proje dizinini kullanır, isteğe bağlı gerçek Windows build yolu ve UDP portu alır; varsayılan port serbest UDP soketinden seçilir. Çalıştırmaların kanıt klasörleri benzersizdir. İki gerçek süreç için `passed`, `lateShopJoined`, `lateShopSawSelection`, `lateShopSawBasket` ayrı doğrulanır; misafirin `.observed` işareti ve oyun kamerasından `.late-shop.png` ekran görüntüsü zorunludur. Başarısızlıkta her iki süreç kapatılır, ham kanıt dizini korunur.

Windows proje kökünden veya başka bir dizinden çalıştırma örneği:

```powershell
pwsh -File tools/Test-P410-ShopLateJoin.ps1
pwsh -File tools/Test-P410-ShopLateJoin.ps1 -Port 26127 -BuildPath Builds/P1-Integrated/DeepDiveGame-P1.exe
```

**Sınır:** Script test koşumunu güçlendirir; bu değişiklik sırasında gerçek Windows/Unity süreci çalıştırılmamıştır. `#249` girilebilir mağaza binası, `#250` ev–mağaza–sahil çok oyunculu akış ve `#164` ayrı PC/insan kabulü ayrı açık kalır. Bu PR ödeme/save/raf çarpışması veya görsel kaliteyi değiştirmez.
