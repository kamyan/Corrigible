# Daha önemli sorunlar
*Pano: `Sorunlar.dc.html` · görüntü: `../panolar/Sorunlar.jpg`*

### DAHA ÖNEMLİ SORUNLAR · GEREKÇE VE ÖNERİ
BRİFİN VE EKİN DIŞINDA GÖRÜLENLER · ÖNEM SIRASIYLA
1 Gölge atlasını dolduran şey nokta ışığının kendisi
Gerekçe. URP'de gölgeli her nokta ışığı küp gölge çizer: atlasta 6 dilim; spot ışığı 1 dilim. “Bir pozda en çok 4 gölgeli nokta ışığı” 24 dilim demek; Unity atlası sığdırmak için her dilimi küçültür. Dolabın panelindeki milimetrik pürüz bunun sonucu (ekle kalkan maketin parıldayan pirinci de).
Öneri. Gölgeyi spot taşısın, nokta ışığı yalnız dolgu yapsın. Tavan ampulü: aşağı bakan 120° gölgeli spot + gölgesiz nokta (aynı yer, %40). Sahanlık lambaları zaten aşağı bakan başlıklı: 110° spot. Masa lambası ve koridor zaten spot. Böylece pozlar 2–3 dilime iner, 2048 atlasta dilim 1024 kalır. Işık tarifi (R14) bu kuralla yazıldı; three.js önizlemesindeki nokta ışıkları yalnız görünüş içindir.
KAZANÇ: BÜTÜN POZLARDA GÖLGE · MALİYET: S
2 Menü sahanlığı ile kapının ardındaki koridor aynı yer değil
Gerekçe. Oyuncu menüde merdivenin önünde durur; oyunda kapıyı açınca (K8) aynı sahanlığı içeriden görür. Bugün iki ayrı kurulum var (Giris'te genişletilmiş sahanlık, DeskGame'de Corridor) ve merdivenin yönü, lambalar, fayans farklı. Kurgunun en güçlü fikri (“menüde kapıya gelen komutanın kendisi”) içeriden bakınca bozuluyor.
Öneri. Tek bir Sahanlık prefab'ı: kapı duvarı, fayans/beton, iki lamba, merdiven ve tırabzan aynı konumda. Menü yalnız sola (dolap, saat) ve sağa (son şalter) genişler; DeskGame bu genişlikleri çizmez. Merdiven yönü Corridor'dan alınır (R1 görüntüleri bu yönle güncellenecek kadar ucuz).
KAZANÇ: KURGU SÜREKLİLİĞİ · MALİYET: S–M
3 Baskında batı kenarındaki her yüksek nesne A sütununu örter
Gerekçe. Kamera güneybatıdan bakar (35° / 45°): tahtanın −x kenarındaki 0,6 m'lik giriş kapısı ve A sütunundaki dolap ya da makine, arkalarındaki piyonları ve işaretleri kapatır. Piyonlar baskına tam bu kenardan girer.
Öneri. Giriş kapıları kenarda 0,30 m'yi aşmasın: kanadı keçeye doğru açık çizilmiş, boş bir kasa ya da “kesit” gibi yarı boy kapı (Karşılaştırma K2). Kitin kapı biçimi yine okunur (kepenk, çift kanat, lomboz). Planlamada A sütunu karelerine düşen işaretler kapının önünde çizilir.
KAZANÇ: OKUNURLUK · MALİYET: S
4 Oyunun baskın kamerası 88 px/kare ise ince parça kuralı sıkılaşır
Gerekçe. Referans görüntüler 132,6 px/kare; raid_tokens.json'daki kamera 88,4 px/kare. 88'de 3 px = 0,034 birim: bugün 0,025'lik kenar çıtaları, tutamaklar ve tel kafes TAA'da titrer; rölyefin gri tondaki farkı da küçülür.
Öneri. Önce oyundaki ölçek ölçülsün (Aşama 0). 88 ise baskın kuralı ≥ 0,035 birim olur; kit rölyeflerinde 0,025'lik parçalar dokuya ya da 0,035'e çıkar (liste model sayfalarında “ince” notuyla). 1080p'de R12 sayfası 88 px ölçekte de verildi.
KAZANÇ: KİTLERİN AYRILMASI KORUNUR · MALİYET: S
5 Malzeme anahtarı çok: Unity’de aile başına paylaşılan malzemeye indir
Gerekçe. Kütüphane ~280 anahtar taşıyor (Rev1–2’den gelenler + yeni + ek). Her anahtar ayrı bir Unity malzemesi olursa SRP Batcher yine çalışır, ama emission ve saydam varyantları ayrı çizim; baskında 64 kopya parça × 4–6 malzeme çizim sayısını şişirir.
Öneri. Oda ve menü için ~45 paylaşılan URP Lit malzemesi (aynı değerli anahtarlar birleşir: steel/steelBare, olive/paintOlive…), baskında kit başına 9 malzeme (token kits.*), makineler tek atlas dokusu. Eşleme tablosu tokens 1.3 → materials.*.usedBy.
KAZANÇ: ÇİZİM SAYISI · MALİYET: M
6 DIŞ KAYIT talebini kim gözetecek? (Gözetim kalktı)
EK 11-05
Gerekçe. Ekte dış kayıt, bir kişinin telsizle talep etmesiyle başlıyor (R10 kâğıdında İpek “telsizle” istiyor); eski Gözetim alanı yok. Kapıdaki talep defteri bir gözetmen bekliyor, ama kuralda tanımlı değil.
Öneri. Boşta destesinin üst kartı gözetmen olur, adı talep fişine basılır; Boşta boşsa defter kilitlenir: “gözetmen yok”. Kural tasarımcının açık kararı (R17-not).
KAZANÇ: KURAL BOŞLUĞU KAPANIR · MALİYET: S
7 Sayaçtaki MALZEME dört hammaddeyi tek sayıya indiriyor
EK 11-05
Gerekçe. Ekte dört hammadde var (metal, devre, kumaş, kimyasal); sayaç rafındaki tek teker yalnız toplamı gösterebilir. Toplam 8 iken iş emri “2 metal” isteyip yapılamayabilir: K1’deki sayı yanıltır.
Öneri. Teker toplamı göstermeye devam etsin ama plakası HAMMADDE olsun; ayrıntı panonun deposunda (stok kartı, ayrılan adet kırmızı halka). Bekleyen iş emri girdisi eksikse plakada küçük kırmızı nokta.
KAZANÇ: K1 YANILTMAZ · MALİYET: S
