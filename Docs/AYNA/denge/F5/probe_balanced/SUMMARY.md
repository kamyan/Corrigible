# AYNA · F5 matris özeti

Tamamlanan kohort: 5/14; tam matris: False.

Oyuncu tarafı tabloda adı geçen bot; karşısı AYNA Mind. Mimari sabit, oyun içi öğrenme açık, her oyunda profil boş. Niyet rolü ve başlangıç konumu ayrı faktörlerdir. Sonuçlar insan oyuncu nüfusunu temsil etmez.

| Bot | Mod | n | AYNA kazanma | Bot kazanma | Diğer | Döngü ortanca | Savaş/oyun | Sürpriz |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Mind | auto | 72 | 48.6% | 50.0% | 1.4% | 28 | 7.39 | 16.9% |
| Mind | tactical | 144 | 50.0% | 49.3% | 0.7% | 27 | 8.99 | 16.8% |
| Greedy | auto | 72 | 19.4% | 80.6% | 0.0% | 24 | 16.36 | 6.5% |
| Greedy | tactical | 144 | 27.8% | 71.5% | 0.7% | 21 | 12.26 | 6.7% |
| Habit | auto | 72 | 6.9% | 93.1% | 0.0% | 17 | 4.21 | 23.0% |

## Mind / Mind mimari matrisi

0=Sürü, 1=Kale, 2=Hayalet. Kazanma ve Wilson aralığında payda tüm oyunlar; skor beraberliğe yarım verir.

| Mod | Oyuncu/AYNA | n | Oyuncu kazanma | %95 Wilson | Oyuncu skoru | Döngü | Savaş |
|---|---|---:|---:|---|---:|---:|---:|
| auto | 0/0 | 8 | 25.0% | 7.1%…59.1% | 25.0% | 26 | 5.88 |
| auto | 0/1 | 8 | 62.5% | 30.6%…86.3% | 62.5% | 27 | 4.75 |
| auto | 0/2 | 8 | 87.5% | 52.9%…97.8% | 87.5% | 27.5 | 6.38 |
| auto | 1/0 | 8 | 62.5% | 30.6%…86.3% | 62.5% | 27.5 | 7.00 |
| auto | 1/1 | 8 | 37.5% | 13.7%…69.4% | 43.8% | 28.5 | 8.62 |
| auto | 1/2 | 8 | 75.0% | 40.9%…92.9% | 75.0% | 29.5 | 8.50 |
| auto | 2/0 | 8 | 25.0% | 7.1%…59.1% | 25.0% | 28 | 8.25 |
| auto | 2/1 | 8 | 37.5% | 13.7%…69.4% | 37.5% | 28.5 | 7.88 |
| auto | 2/2 | 8 | 37.5% | 13.7%…69.4% | 37.5% | 30 | 9.25 |
| tactical | 0/0 | 16 | 37.5% | 18.5%…61.4% | 37.5% | 26 | 8.31 |
| tactical | 0/1 | 16 | 37.5% | 18.5%…61.4% | 40.6% | 27 | 8.25 |
| tactical | 0/2 | 16 | 75.0% | 50.5%…89.8% | 75.0% | 28 | 7.75 |
| tactical | 1/0 | 16 | 62.5% | 38.6%…81.5% | 62.5% | 28 | 11.19 |
| tactical | 1/1 | 16 | 37.5% | 18.5%…61.4% | 37.5% | 28 | 10.00 |
| tactical | 1/2 | 16 | 81.2% | 57.0%…93.4% | 81.2% | 27 | 9.62 |
| tactical | 2/0 | 16 | 25.0% | 10.2%…49.5% | 25.0% | 24 | 7.88 |
| tactical | 2/1 | 16 | 31.2% | 14.2%…55.6% | 31.2% | 26 | 9.44 |
| tactical | 2/2 | 16 | 56.2% | 33.2%…76.9% | 56.2% | 27.5 | 8.44 |

## Niyet rolü

Rol 0=AYNA niyet, rol 1=oyuncu niyet.

| Bot | Rol | n | Bot kazanma | Diğer |
|---|---|---:|---:|---:|
| Mind | 0 | 72 | 69.4% | 1.4% |
| Mind | 1 | 72 | 29.2% | 0.0% |
| Greedy | 0 | 72 | 94.4% | 0.0% |
| Greedy | 1 | 72 | 48.6% | 1.4% |

## Mind bitiş dağılımı

1=Son Şalter, 2=Silme, 3=Tekillik, 4=Hakimiyet, 5=Süre.

| Mod | Son Şalter | Silme | Tekillik | Hakimiyet | Süre | 40 öncesi |
|---|---:|---:|---:|---:|---:|---:|
| auto | 0.0% | 0.0% | 48.6% | 48.6% | 2.8% | 95.8% |
| tactical | 0.0% | 13.9% | 26.4% | 59.0% | 0.7% | 98.6% |

Ölçüm zamanı ana rapordaki Runtime alanında bulunur. Mono plan süreleri Unity ölçümü diye sunulmaz. Bütün bot × mimari hücreleri summary.json içindedir.
