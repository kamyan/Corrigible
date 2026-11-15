# DengeBotu: KOS harekât botu

KOS aşama 8'in denge ölçümünü yeniden üretmek için konsol programı. Gerçek `Corrigible.Core` ve
`Assets/Corrigible/Tests/EditMode/RaidBot.cs` ile, Unity olmadan Mono'da koşar. Oyun koduna girmez (Assets dışında; Unity
derlemez). Sonuçlar `Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md` §8 "Ölçüm sonuçları" ve `Docs/DURUM.md` › KOS aşama 8'de.

## Kullanım

```bash
Tools/DengeBotu/derle.sh                         # Library/DengeBotu/camp8.exe (Core değişince yeniden derle)
PAR=8 Tools/DengeBotu/kos.sh Tools/DengeBotu/asama8.txt   # aşama 8 son ölçümü (8 çekirdekte ~1,5 saat)
python3 -I Tools/DengeBotu/ozet.py son sonY tabanR c375 c425 usta
```

Tek koşu: `RETIRE=1 BAL="eventGapMax=2" mono Library/DengeBotu/camp8.exe 1 10 karisik ucuz hic etiket` (tohum 1–10).
Satır başına bir harekât yazar (`C\t` + `anahtar=değer`, sekmeyle).

- **Kapı:** `hic` (kimseyi alma), `kefilli` (yalnız kademe 0), `comert` (en çok özellikli), `karisik` (kadro 6'nın altındaysa en
  düşük kademe, değilse kefilli), `alyolla` (cömert al, dolu sığınakta bu politikayla alınmış en eskiyi yolla).
- **Olay:** `yoksay` (güvenli satır), `tecrit` (tecrit/sorgu), `yolla` (yolla/sök), `ucuz` (en ucuz: eşiğe vardırmıyorsa izi
  görmezden gel, sabotajda sök, konum çözülmüşse sorgu).
- **Şalter:** `hic`, `salter` (konum ya da kadro nedeninde çek), `durgun` (ayrıca 30. vardiyadan sonra 12 vardiya ilerleme yoksa).
- **"İyi" küme:** karışık+tecrit, karışık+ucuz, kefilli+ucuz (şalter çekilmez).
- **Ortam:** `RETIRE=1` (varsayılan `kos.sh`'ta 1) sunumun kuralı: yayındaki panoda yuvası olmayan görevler (`care`, `gather`,
  `watch`, `contact`) Boşta'ya iner, bot onlara kimseyi atamaz (`PlanningBoardPresenter.RetireJobs`). **Ölçümler bununla
  yapılır**; `RETIRE=0` ilk ölçümün toplayıcılı botudur ve tüketimi yanlış ölçer (aşama 8'de yaşandı). `BAL="alan=değer;…"`
  `Balance`'ı değiştirir; `ALT=n` RaidBot'un seçenek sayısı (varsayılan 8; `usta` koşusu 16).

## Sınırlar

Bot insan değildir: baskın başına ~1,7 sivil kırar ve harekât başına ~6,4 kişi kaybeder; `erzak_azaliyor`'da hep tam tayın,
takası hep reddeder, eğitim ve takas fırsatlarını kullanmaz. Açlık, terk ve ölüm sayıları bu yüzden üst sınırdır; zafer ve
temiz ton insan oyuncuda yüksek çıkar. 40 tohumda politika satırında ±7–8, "iyi" kümede (120 harekât, ama yapılandırmalar
çoğu tohumda aynı yolu izler) yaklaşık ±7 puan gürültü vardır; kararlar için 80 tohum kullanılır.
