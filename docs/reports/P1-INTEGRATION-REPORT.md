# P1 entegrasyon kaydÄ± â€” 9 EylÃ¼l 2026

Durum: yerel doÄŸrulama sÃ¼rÃ¼yor; P1 kapanÄ±ÅŸÄ± deÄŸildir.

## BirleÅŸen teslimler

- Mehmet: PR #16, `65a3f75` â€” baÄŸlantÄ±, oyuncu ve hareket senkronizasyonu.
- Mert: PR #17, `eb0b870` â€” oda/hazÄ±r ekranÄ± ve oturum aÅŸamalarÄ±.
- Utku: PR #18, `9f0e9f3` â€” hazÄ±rlÄ±k ve tek sualtÄ± test sahnesi.

KullanÄ±cÄ±nÄ±n aÃ§Ä±k birleÅŸtirme ve devam talebiyle Mehmet/Codex entegrasyon desteÄŸi
verdi. P1 koordinatÃ¶rÃ¼ Utku ve sistem sahipleri deÄŸiÅŸmedi. Otomatik kontrol,
Utku/Mert adÄ±na insan incelemesi veya faz tamam onayÄ± deÄŸildir; bunlar Ã¼retilmedi.

## BaÄŸlantÄ± deÄŸiÅŸiklikleri

- Ortak klasÃ¶r `.meta` Ã§akÄ±ÅŸmasÄ±nda mevcut `Assets/DeepDive` GUID'i korundu.
- Mert'in geÃ§ici string `PlayerId` tipi kaldÄ±rÄ±ldÄ±; ortak `Core.Contracts.PlayerId`
  (ulong, host=0) kullanÄ±lÄ±yor. Oturum/UI sahipliÄŸi Mert'te kaldÄ±.
- `SessionNetworkAdapter`, gerÃ§ek aÄŸ oyuncu listesini Mert'in hazÄ±r/aÅŸama durumuna
  baÄŸlar. Ä°stemci yalnÄ±z kendi hazÄ±r isteÄŸini gÃ¶nderir. Kimlik baÄŸlantÄ±dan alÄ±nÄ±r;
  eski/tekrarlanan istek ve istemcinin aÅŸama deÄŸiÅŸtirme giriÅŸimi reddedilir.
- Sahne yÃ¼kleme kabul edilmeden aÅŸama/revision ilerlemez. Kabul sonrasÄ± yÃ¼kleme
  baÅŸarÄ±sÄ±zsa yarÄ±m oturum devam ettirilmez; baÄŸlantÄ± kapatÄ±lÄ±r.
- Utku'nun spawn noktalarÄ±, su hacmi, giriÅŸ/Ã§Ä±kÄ±ÅŸ kutularÄ± gerÃ§ek bileÅŸenlere baÄŸlandÄ±.
  Oyun giriÅŸ sahnesi `PrepArea`; dalÄ±ÅŸ `DiveTestArea`. AyrÄ± test sahnesi kullanÄ±lmaz.
- GerÃ§ek oyun yolunda sahte oyuncu veya test saÄŸlayÄ±cÄ±sÄ± yoktur. Otomasyon yalnÄ±z
  Development build'de aÃ§Ä±k komut satÄ±rÄ± bayraÄŸÄ±yla baÅŸlar.

## DoÄŸrulama

GÃ¼ncel sonuÃ§lar test tamamlandÄ±ÄŸÄ±nda bu bÃ¶lÃ¼me ve `docs/evidence` altÄ±na kaydedilir.

## AÃ§Ä±k faz koÅŸullarÄ±

- Ä°ki ayrÄ± bilgisayar Ã¼zerinden internet 2/4 oyuncu kabulÃ¼.
- GerÃ§ek ekip incelemesi/tamam kaydÄ± ve netcode yedeÄŸinin akÄ±ÅŸÄ± tekrar etmesi.
- Basit branch korumasÄ±nÄ±n kurulup doÄŸrulanmasÄ±.
- Faz kapanÄ±ÅŸÄ±ndan sonra main birleÅŸimi ve birleÅŸmiÅŸ build kontrolÃ¼.

P2 ve sonrasÄ± kapalÄ±dÄ±r. [Ã‡alÄ±ÅŸtÄ±rma adÄ±mlarÄ±](../P1_PLAY.md).
