# Son Şalter · Revizyon 3 + Ek 11-05 · tasarım paketi

Tasarım panoları (R1–R19, karşılaştırmalar, öncelik, sorunlar, tokenlar) tuvalde ve `tasarim/` klasöründe; bu paket uygulamanın kaynağıdır.
Sayı, konum, açı ve malzeme için tahmin etme: modellerin manifestlerinden, token 1.3’ten ya da three.js sahnesinden oku.

| Klasör | İçerik |
|---|---|
| `PROMPT.md` | **Claude Code için Unity uygulama talimatı** (aşamalar, kabul ölçütleri, değişmez kurallar, açık sorular). İşe buradan başla. |
| `modeller/` | 198 model: `obj/` (OBJ + MTL, Y yukarı, metre), `manifest/` (boyut, üçgen, parçalar ve pivotlar, malzeme anahtarları, `TXT_*` yuvaları, çapalar, ışık noktaları, dünya yerleşimi), `sayfa/` (model sayfaları), `MODELLER.md` tablosu, `modeller.csv`, `models.json`. Okuma sırası: `modeller/README.md`. |
| `threejs/` | Ekranları üreten referans sahne (`r3/`), Revizyon 2 kabuğu (`rev2/`), R10 kâğıt dokuları (`r10/papers_new/`). Okuma: `threejs/README.md`. |
| `tokens/` | `tokens_siginak.json` ve `raid_tokens.json`, sürüm 1.3 (ek 11-05 dahil). |
| `ekranlar/` | Render’lar: `R01-R14/` (Revizyon 3), `ek_11-05/` (R5, R6, R9, R15–R19, kart sayfaları, karşılaştırma görüntüleri). |
| `tasarim/` | Tuvaldeki 92 panonun hepsi: `panolar/*.jpg` (görüntü), `notlar/*.md` (metin; `*-not.md` uygulama notları), briefler, portreler (`portre/`), R10 kâğıtlarının HTML kaynağı (`kagit_kaynak/`). Dizin: `tasarim/README.md`. |

## Ek 11-05 kısaca

- Maket ve oda yükseltmeleri kalktı. Yerinde **ayaklı kartlı pano** duruyor (R15, K4): sekiz isimli kişi (en çok 10),
  yedi alan (TEKNİSYEN, EMP UZMANI, ANALİST, ÜRETİM EKİBİ, MUTFAK, EĞİTİM, BOŞTA/DİNLENME), Boşta destesi, iş emri, kişi
  dosyası, depo (4 hammadde + kitler) ve hurda kutusu. SIĞINAK tuşu K4’e götürür; MASAYA DÖN levhası panonun başlığında.
- **Kart ailesi** (R16): kişi 80 × 120, teçhizat 80 × 50, hammadde 80 × 56, fiş 56 × 40, tarif 112 × 80 mm; hepsi 0,6 mm.
  1920 × 1080’de K4’te kart adı 21 px (kural ≥ 18), değerler 14–17 px (kural ≥ 14).
- **Tarif defteri ve üretim** (R18): 3 açılım × 2 tarif; tarif kartı ve hammadde fişleri iş emrine sürüklenir.
- **Kapıda belgeli kabul** (R17, K8A/K8G/K8R): talimat (4. ve 8. vardiyada değişir), kimlik/sevk belgesi, bulgular,
  dört inceleme dört araçla (büyüteç + talimat şeridi, interkom, buğu camı, telsiz), dört karar dört damgayla. Aday ve
  robot görünmez; yeni son SON KİŞİ.
- **Vardiya kapanışı** (R19, K6): kapak → kol → devir fişi şalterin solundaki ağızdan 12 cm kayar (0,35 s; çıkış noktası
  şalter yerelinde (−0,30; 0,14; 0,015)) → EVRAK’a sürüklenir → 0,2 s kararma → teleks “VARDİYA DEVREDİLDİ”.
- Değişen ekranlar R5, R6, R9, R10, R13; kalkan R7. Baskın ekibi üç isimli kişi; madenci ve sis uzmanı yok.
- **Öğretici yok:** her adımı nesnenin kendisi gösterir (kabarık cep çerçeveleri, pano lambası, fiş ağzının amber lambası,
  şalter plakasının ikinci satırı, vuruş lambası).
- **Uygulama sırası:** önce R15, R16, R19; sonra R17, R18 (öncelik panosunda her bölümün ilk beşi).

## Token işaretleri

`_new_1_3` yeni yolların listesi, `_changed_1_3` değişen yollar, `_was_1_3` onların eski değerleri. Ekle kalkan bir şey
`{"_": "KALDIRILDI (ek 11-05)", "removed": true, "why": "…"}` olarak durur; eski değeri `_was_1_3`’te. Ek bölümleri:
`board.*` (yerleşim mm cinsinden `board.layoutMm`), `cards.*`, `recipeBook`, `personFile`, `shiftClose.*`, `admission.*`,
`cameras.K4/K6/K8A/K8G/K8R`, `motion.board`, `motion.shiftClose`, `lightsRecipe` (K4, K6, K8, K8A…), `models3d.removed_1_3`.

## Unity kuralları (değişmedi)

URP Lit, özel shader yok, bloom yok; parlama yalnız emission. Bir pozda en çok 4 gölgeli ışık ve gölgeyi spot taşır.
Modellerde kelime yok: her yazı bir `TXT_*` TMP yuvası (TR örnek + EN örnek); kişi adları çevrilmez. Malzeme anahtarı başına
bir URP Lit malzeme, değerler token 1.3’ten.

## Açık kararlar

1. DIŞ KAYIT gözetmeni (Gözetim kalktı): öneri Boşta destesinin üst kartı; Boşta boşsa talep defteri kilitlenir.
2. Sayaç rafındaki MALZEME tekeri dört hammaddenin toplamı: plakanın HAMMADDE olması önerildi.
3. Baskında giriş kapısı yüksekliği: öneri alçak (≤ 0,30) ya da açık kasa (Karşılaştırma 2).
4. Menü kamerası M0: öneri yeni değerler (Karşılaştırma 1). Piyonun “tur bitti” yüzü: öneri koyu kraft (Karşılaştırma 3).
5. Pano ayaklı mı duvarda mı: öneri ayaklı (Karşılaştırma 4). Kabul kapıda mı masada mı: öneri kapıda (Karşılaştırma 5).
