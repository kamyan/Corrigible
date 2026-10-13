# drafts/ · F8.2 taslakları

Tasarımı olmayan ekranların masa tarzı taslakları (referans HTML'lerin CSS diliyle). Onaylanan taslaklar F8.3'te uGUI ile
kurulur ve bu PNG'lerle karşılaştırılır.

- **Stil:**
  - `desk_ref.css` = C1 + C3 referans stilleri, değişmeden.
  - `drafts.css` = yeni bileşenler: resmî yazı, yırtık kâğıt, kaydırıcı, uyarı fişi, eğitim deliği.
- **Çizim:**
  - `../tools/render_drafts.sh [ad …]` yüklü Chrome'la başsız 1920×1080 PNG üretir; referanslar da aynı yolla üretilmişti.
  - Arka planlar `../captures/F8_oyun_*.png`'dir (gerçek oyun sahnesi).

| Dosya | Ekran | Oyundaki karşılığı |
|---|---|---|
| `giris` | Görev dosyası: rakip seçimi, YZ'nin oyuncu profili, kurallar, kampanya, başla/devam/eğitim, ayarlar/dil | IntroModal |
| `ayarlar` | Dizin kartı: ses kanalları (kaydırıcı), ses açık, dil | SettingsModal |
| `kampanya` | Krizler arası dosya + üç yükseltme kartı (1/2/3, Enter) | CampaignModal |
| `uyari` | Haritanın üstünden sarkan teleks fişleri (başarılı / yapılamadı / solan) | ToastView |
| `perde` | Saha raporu (C3 kurgusu: spot + iplik), ekran–ölçüm karşılaştırması | VeilModal |
| `sahte_zafer_a`, `_b` | Kusursuz beyaz resmî duyuru → bozulma (YALAN, YZ satırı) → C4 | FakeWinModal |
| `hediye` | C3 kâğıdıyla hediye teklifi (F8.1'de kuruldu; oyun yakalaması) | GiftModal |
| `baskin_brifing`, `baskin_sonuc` | Operasyon emri (Enter: taktik harita) ve operasyon raporu | RaidView giriş/sonuç |
| `egitim` | Hedefin çevresinde delik bırakan karartma + adım notu; bağlamsal ipucu notu | TutorialController |
