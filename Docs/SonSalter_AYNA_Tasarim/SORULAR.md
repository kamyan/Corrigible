# AYNA · Sorular

Brifte eksik ya da çelişkili gördüğüm noktalar. Kural eklemedim, çıkarmadım; çizmek için bir seçim gerektiğinde **çizilen** satırında ne seçtiğimi yazdım. Örnek veri JSON’larında bu seçimler `derived` listesinde.

---

**1 · Bölgeler.** Brif “Fx haritasındaki çokgenler” diyor; ama Fx’te Orta Amerika var, Japonya-Kore yok.
- Çizilen: Orta Amerika → Kuzey Amerika’ya katıldı. Japonya, Güney Kore ve Kuzey Kore → yeni Japonya-Kore çokgeni. Doğu Asya = Çin, Tayvan, Moğolistan, Hong Kong, Makao.

**2 · Orta Doğu.** Fx’teki “O. Doğu · K. Afrika” çokgeni Kuzey Afrika’yı da kapsıyor. Böyle mi kalsın?
- Çizilen: Fx çokgeni aynen, adı “Orta Doğu”.

**3 · Bağlantı ağı.** Brifte bağlantılar yok; yalnız türleri var (fiber, deniz altı kablosu, uydu).
- Çizilen: 22 bağlantı önerisi (`spec/data/ortak.json › links`). Fiber düz çizgi, deniz altı kablosu kesik.
- Soru: uydu hatları (Görü kademe 3) neleri bağlar?

**4 · Savun.** AYNA’nın planında “Savun · Güney Asya” var; emir tablosunda Savun yok.
- Çizilen: bölgede bekleyen birliğin durumu olarak (mavi kalkan rozeti). Bir emir mi, birliğin varsayılan duruşu mu?

**5 · Sürpriz ödülü.** Kural: haritada +3 İşlem, savaşta İLK VURUŞ. Örnek veride ikisi de var (A3’te +3 İşlem, A6’da İLK VURUŞ kullanıldı).
- Soru: aynı sürprizde ikisi birden mi, bağlama göre biri mi?

**6 · Üretim bedelleri.** Brifte yalnız “Üret 3–8” var.
- Çizilen yer tutucular: Gözcü 3, Nişancı 5, Muhafız 6, Örümcek 7, EMP 4, Aşırı Yük 5, Sızma 4, Röle 5, Yem 3, Hayalet Emir 8.

**7 · A11 ve A12.** Döngü 19 ve döngü 22 için harita ve sayı yok.
- Çizilen: döngü 7’den türetildi (`A11.json`, `A12.json › derived`).

**8 · Okunabilirlik eğrisi.** A10 “son 12 döngü” istiyor; örnek oyun döngü 7’de.
- Çizilen: ilk 5 nokta önceki oyundan (v6, sönük), 7’si bu oyundan.

**9 · Tahmin karnesi.** “14 doğru, 9 yanlış” = 23 tahmin. Döngü başına 3 tahminle 22 döngü 66 eder.
- Soru: karneye hangi tahminler girer (yalnız en güçlü tahmin mi, yalnız sonuçlanabilenler mi)?

**10 · B birliğinin durumu.** PLANLA’da Güney Asya’da AYNA birliği de var.
- Çizilen: SAVAŞTA. Savaş AÇ’tan sonra başlıyorsa PLANLA’da “hazır” mı olmalı?

**11 · Gözcü’nün menzili.** “3 karedeki tuzakları açar”: 3 adım mı, 7×7 kare mi?
- Çizilen: A6 dizilimi ikisinde de doğru olacak biçimde seçildi.

**12 · Üretim hattı kesik.** Hâl brifte var, örnek veride yok.
- Çizilen: A8 lejantında ve P1’de gösterildi; örnek veride kesik hat yok.

**13 · Tahta mesafesi.** Köşeden köşeye 14 kare, hareket 2–4. Tur 2’de temas zor.
- Çizilen: A6 dizilimi birimlerin 1. turda ilerlediğini varsayar. Konuş kareleri ya da tahta boyu yeniden düşünülsün mü?

**14 · Çekirdek baskını.** AYNA’nın çekirdeği sisli bölgede (Doğu Asya).
- Soru: oyuncu baskın tahtasına girmeden bunu öğrenebilir mi (Tarama, Gözcü)? Öğrenemiyorsa A12’deki silme zaferine nasıl gidilir?

**15 · İngilizce kısayollar ve birim harfleri.** Brif EN metinleri istiyor; tuşlar ve birim harfleri için bir şey demiyor.
- Önerilen: tuşlar değişmez (G, B, M, L, H, Q, Z). Birim harfleri EN’de değişir: Gözcü S (Scout), Nişancı N, Muhafız G (Guard), Örümcek P (Spider), EMP E, Aşırı Yük O, Sızma I, Röle R, Yem D (Decoy), Hayalet Emir H.
