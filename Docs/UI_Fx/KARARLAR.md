# Son Şalter · FX kararları

18 Ekim 2026 · F3. Kullanıcının “1.Başlat, 2.Doğrulandı” yanıtıyla F1 ve FX güven terimi onaylandı. Sonraki fazların kapsam kapıları korunur.

| Konu | Başlangıç kararı | Kapsam / gerekçe |
|---|---|---|
| Kaynak ve sıra | Fx paketi 1.0; F0→F1→F2→F3/F4→F5/F6→F7 | Kullanıcının ekindeki brif; her faz sonunda dur/raporla |
| Sade durumu | A, kurulu | Sade F7 ve canlı masa/Baskın mevcut |
| Görünümler | Sade prefab'ları korunur, FX ayrı sandbox'ta | İleride `UiSkin {Fx,Sade}` geliştirici anahtarı; canlı varsayılanı değiştirme ayrı onay |
| Yeni dosyalar | `Assets/Corrigible/Fx/`, `Corrigible.Fx*` | Mevcut proje adlandırması; kaynak paket Docs'ta kalır |
| Sade davranış ayrımı | Gereken en küçük view sözleşmesi/presenter ayrımı önerilir | SadeDeskInteraction, RaidHudView, RaidDataSource somut bağımlılıkları var; ilgili faz öncesi ayrıca kapsam/onay |
| Küre | RT yolu A, birebir kanonik JS portu | Flipbook/video/Shuriken/VFX Graph yok; kendi RNG'si, oyun RNG'sine dokunmaz |
| CPU eşiği | Ana kabul ≤2,5 ms; 1,5 ms iyileştirme hedefi | Ana brif ile ORB belgesindeki iki eşik ayrıştırıldı; platform ölçümü açıkça etiketlenir |
| Küre hızları | 40 fps; uyarlamalı en az16; azaltılmış harekette8 | Brif varsayılanı; görünmeyen güncellenmez |
| Cam | Blur yok | Yeni render pass veya ekran kopyası planlanmıyor |
| Tahta ışıması | B yolu, ikinci 3px yarı saydam kenar | Yeni Bloom/Volume/URP ayarı yok; yüzler Unlit |
| UI ışıması | Pişmiş glow sprite / additive / TMP ön ayarı | UI bloom yok; ortak materyallerin batch maliyeti ölçülür |
| Hâl 1/3 yazısı | #FFE0A8 / #D4ECFF | Çizilmeyen durumlar için brif varsayılanı; tema üzerinden ayarlı |
| Güven terimi | DOĞRULANDI (kullanıcı onayı) | Kullanıcının “1.Başlat, 2.Doğrulandı” yanıtıyla kesinleşti; simülasyon metne göre karar vermez |
| Kart yerleşimi | FX A.9: sağ→sağa yasla→sol | Küre/ad/açık çekmeceyi örtmez; davranış ayrımında regresyon testi |
| Baskın düğüm düzeyi | Mock/VM:58; makinelerde64; son tur86; sonuçta çökmüş | Canlı oyuna bağlanacak değer bir oyun tasarımı kararı; gizli sızmadan otomatik okunmaz |
| Simge ışıması | fx_glow_dot_32 | Brifin kabul edilen görsel yaklaşımı |
| Laboratuvar OTOMATİK simgesi | ic_undo | Yeni simge çizimi gerekmiyor |
| Duraklatma | Görsel döngü devam eder, oyun hızı etkilemez | FxClock unscaled; azaltılmış hareket ayrıca uygulanır |
| Bölge parametreleri | Sabit mesh/regionID + ortak shader parametre dizisi önerilir | Tek materyal/batch; her kare UI mesh'ini kirletmeden senkron renk |
| Doku bütçesi | İki 4K harita: BC7≈15,8MiB; ham≈55,4MiB | Yeni importer ayarı, mipmap yok; görsel kalite ve gerçek bellek ölçülür |
| RT bütçesi | Orb RT≤12MiB, tekrar kullanım/serbest bırakma | Font ve harita ayrı bütçe; altın kareler sırayla alınır |
| Referans videosu | Statik görünüm için ekran/TSV öncelikli | Bazı video örneklerinde katmanlar görünmüyor; eksik görünüm hedef yapılmaz |
| Tarayıcı lab incelemesi | F2’de doğrulandı | Yerel Chrome’da 0→100 sürükleme; sekme ve HTTP sunucusu kapatıldı |
| AYNA tasarım brifi | Bu işin dışında | Son Şalter FX görünümü sırasında oyun kuralları değiştirilmeyecek |

F0 teslimi `PLAN.md`; F1 teslimi `F1_NOTLARI.md`. F1 ayrı FX sahne/prefab’larında tamamlandı. Mevcut Sade sahne/prefab’ları ve oyun kuralları değiştirilmedi.

F2 teslimi `F2_NOTLARI.md`. GPU çiziminde StructuredBuffer + DrawProcedural seçildi; dört köşeyi CPU’da çoğaltmaya göre ölçülen maliyet yarıdan fazla azaldı. Sabit geometri önceden hesaplandı; sayısal parite korundu. 22 altın kare kabul sınırında. F1’deki 8 ortak TMP glow materyaline F2’nin 2 doğrudan laboratuvar materyali eklendi (toplam 10). F2 laboratuvarının dolu Primary düğmesi için `FxButton.solidPrimary` eklendi; F1 prefab’larında varsayılan false kaldı.

F3 teslimi `F3_NOTLARI.md`. Sade davranış kaynağı değiştirilmeden mevcut VM, komut arayüzü ve saf `SadeInteractionState` kullanıldı. Bölge boyası tek materyal/parametre dizisidir; görünür düzey küre olayından alınır. Yeni F3 tema ve gama materyalleri F1 görünümünden ayrıdır. F3 statik HUD/işaretleri ortak mesh gruplarıyla toplanır; ayrı maskeli F4 panelleri bu gruplara otomatik katılmaz. Son Şalter F3'te yalnız onay isteğidir.

Referansta çip TSV x değeri sol kenardır; §A.3'teki “üst-orta” ifadesi yerine JPG/TSV yerleşimi seçildi. Etiketler merkezlenir, DOĞU AVRUPA çip/adı üstte kalır. Harita ve ülkeler cover ile büyür; küre, font ve HUD piksel boyutunu korur. F3'te 1920×1080, 1920×1200, 2560×1440 ve 3440×1440 TR/EN görüntüleri alındı.

Küre `_Brightness` varsayılanı 1, F3 hover 1,25; `startDelay` varsayılanı 0, F3 sahnesinde 0,25 + 0,05·i sn. Renk yardımcı fonksiyonunun inline edilmesi sayısal portu değiştirmez; sonrasında 22 altın kare tekrar geçti. Durağan 33 batch ölçümü motorun UI Details kaydından; aktif panel bütçesi F4/F7'de yeniden ölçülecek. Profiler'ın aynı oturumda ikinci kaydında yerel tampon çökmesi görüldüğünden F3 kanıtı yeni oturumdaki tek kısa kayıttır; motor kök nedeni kesinleştirilmedi.

## F4 · Masa panelleri (18 Ekim 2026)

- Ayrı FX referans sahnesi sürdürüldü. Canlı oyuna otomatik görünüm değişimi veya ikinci simülasyon eklenmedi; Sade VM/InteractionState/IDeskCommands paylaşılır.
- Bölge kartı yerleşimi üç referans adayından sonra adın sol/sağ sınırını ve açık çekmeceyi de dener. Böylece 16:9 ve 16:10'da DoA/GDA adları örtülmez.
- Bir çekmece veya tam dosya; kart + çekmece ve bağımsız lejant serbest; tek ipucu. Esc ortak durum makinesinden gelir.
- Sandbox önizleme maliyetleri ayrı JSON kaynağıdır; görünüm gerçek fon/eylem etkisi hesaplamaz. Canlı veri adaptörü eksik alanlarda mock veriye dönmez. İllüzyon gösterim verisini temizler.
- Ortak font/texture batch'ine RectMask2D kırpması ve üst panel alanının çıkarılması eklendi. TMP mesh'i ve kirlenme kaynak başına önbelleğe alınır. Kaynak CanvasRenderer.GetMesh sarmalayıcılarının her kare topluca üretilmesi önlendi.
- Panel küreleri kendi gerçek zamanlı çizimlerini korur; küçük RT'ler 128 px hücreli ortak panel atlasına kopyalanır. Bu bir sprite dizisi değildir; geçiş/RNG/kanonik çizim listesi aynı kalır. Atlas piksel testi ve 22 altın kare geçer.
- F4 sahnesinin OrbBudget sınırı 2,1 ms: ertelenen küre bir sonraki karede önceliklidir. 19 küreyi her kare zorla çizme maliyeti 3,689 ms olduğundan bu sahnede bütçesiz yol kullanılmaz.
- Yerel Profiler önceki fazdaki yeniden başlatma çökmesi nedeniyle temiz oturum başına bir kez kullanıldı. Son rehber ölçümü 36 batch, FX betiklerinde 0 B/31 kare; oyuncu derlemesi ve en ağır birlikte-açık panel durumu F7'de ayrıca ölçülür.
- Fontta olmayan ✓ glifi yerine paket onay simgesi kullanıldı. TMP taşma bayrağına ek NoWrap gerçek genişlik denetimi yapıldı; otomatik küçültme kullanılmadı.

## F5 · Holo tahta (19 Ekim 2026)

- Sade/Game kaynak koduna skin dalları eklemek yerine canonical mesh'leri ve public mock parser'ı kullanan ayrı FX sunum havuzu seçildi. Fx runtime/editor/test assembly'leri Game ve Desk.Bridge'e başvurur; ters bağımlılık eklenmedi.
- Kenarlar offline kaynaklanmış dörtgenlerdir; 1px+3px/α0,40 yol B. Ortak URP/Bloom/ışık ayarı değişmez. AA yan koordinatı iki uçta korunur; sert ve yumuşak siluet kenarları ayrı veri taşır.
- UI piksel geometrisi zemine geri izdüşürülür; derinlik testi figürlerin üstüne boyama hatasını önler. Tek saatte tarama, titreme, koni, akış ve süzülme yürür. Hareketli bağın iki küçük mesh'i uzunluk değişiminde yenilenir; durağan işaretler yenilenmez.
- Renkler ve paletin işaret ölçüleri ayrı FxRaidTheme varlığına pişirildi; ana FX masa teması korunur.
- Mock `cert` ve `nomark` yalnız sunum içindir. B5 makineler64, son tur86; gizli sızma/turdan türetilmiş düzey yok. Küre modeli düzey değişiminde yeniden kurulmaz. Çöküş/unknown ve HUD F6'dır.
- 3B kaynak geometri farkı kamera doğruluğuyla karıştırılmaz. F5 maskeli karşılaştırmasında F6 HUD'u dışarıda; tüm ekran piksel eşliği veya Steam GPU bütçesi sağlandı denmez.

## F6 · Baskın HUD (19 Ekim 2026)

- Ayrı FxRaidHud_Sandbox ve mock FxRaidSource; Sade RaidDataSource/VM/komut mantığı paylaşılır, canlı rapor/GameRecord bağlanmaz.
- Makine sonucu Baskin.EndTurn ile bir kez hesaplanır; görsel adımlar kopyada ilerler. Üç başlangıçta son durum tamamen eşleşir.
- Render içinde girdi işlenmez; yeniden giriş koruması vardır. EventSystem submit/gezinme kapalı, fare/kaydırma açık; Enter/Esc tek komut sınırındadır.
- FX TSV birim kartı 233 px, üçüncü satırlı Analist 285 px; kurallar metne göre büyür. Sonuç memo 438 px; görsel farklar F6_NOTLARI içinde açıkça tutulur.
- 436/436 EditMode, 102/102 Play ve 75 taşmasız görüntü; dokuz karşılaştırma piksel eşliği kabulü olarak sunulmaz. F7 ve canlı oyuna geçiş ayrı adımlardır.
