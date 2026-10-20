# AYNA · F5 matris özeti

Tamamlanan kohort: 6/14; tam matris: False.

Oyuncu tarafı tabloda adı geçen bot; karşısı AYNA Mind. Mimari sabit, oyun içi öğrenme açık, her oyunda profil boş. Niyet rolü ve başlangıç konumu ayrı faktörlerdir. Sonuçlar insan oyuncu nüfusunu temsil etmez.

| Bot | Mod | n | AYNA kazanma | Bot kazanma | Diğer | Döngü ortanca | Savaş/oyun | Sürpriz |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Mind | auto | 72 | 45.8% | 48.6% | 5.6% | 29.5 | 9.68 | 25.5% |
| Mind | tactical | 144 | 45.8% | 53.5% | 0.7% | 27 | 10.16 | 24.7% |
| Greedy | auto | 72 | 33.3% | 65.3% | 1.4% | 31.5 | 19.10 | 6.5% |
| Greedy | tactical | 144 | 21.5% | 78.5% | 0.0% | 24 | 13.02 | 7.2% |
| Habit | auto | 72 | 2.8% | 97.2% | 0.0% | 23 | 6.32 | 18.6% |
| Habit | tactical | 144 | 25.0% | 75.0% | 0.0% | 22 | 6.77 | 18.8% |

## Mind / Mind mimari matrisi

0=Sürü, 1=Kale, 2=Hayalet. Kazanma ve Wilson aralığında payda tüm oyunlar; skor beraberliğe yarım verir.

| Mod | Oyuncu/AYNA | n | Oyuncu kazanma | %95 Wilson | Oyuncu skoru | Döngü | Savaş |
|---|---|---:|---:|---|---:|---:|---:|
| auto | 0/0 | 8 | 50.0% | 21.5%…78.5% | 56.2% | 35 | 9.62 |
| auto | 0/1 | 8 | 50.0% | 21.5%…78.5% | 50.0% | 39 | 12.25 |
| auto | 0/2 | 8 | 50.0% | 21.5%…78.5% | 62.5% | 31.5 | 9.12 |
| auto | 1/0 | 8 | 87.5% | 52.9%…97.8% | 87.5% | 32.5 | 11.62 |
| auto | 1/1 | 8 | 62.5% | 30.6%…86.3% | 62.5% | 28 | 8.75 |
| auto | 1/2 | 8 | 62.5% | 30.6%…86.3% | 62.5% | 31.5 | 7.88 |
| auto | 2/0 | 8 | 25.0% | 7.1%…59.1% | 25.0% | 27.5 | 10.12 |
| auto | 2/1 | 8 | 25.0% | 7.1%…59.1% | 25.0% | 31 | 8.12 |
| auto | 2/2 | 8 | 25.0% | 7.1%…59.1% | 31.2% | 37.5 | 9.62 |
| tactical | 0/0 | 16 | 62.5% | 38.6%…81.5% | 62.5% | 27 | 9.81 |
| tactical | 0/1 | 16 | 37.5% | 18.5%…61.4% | 37.5% | 28 | 12.00 |
| tactical | 0/2 | 16 | 62.5% | 38.6%…81.5% | 65.6% | 26.5 | 10.00 |
| tactical | 1/0 | 16 | 43.8% | 23.1%…66.8% | 43.8% | 27 | 9.31 |
| tactical | 1/1 | 16 | 68.8% | 44.4%…85.8% | 68.8% | 32 | 13.38 |
| tactical | 1/2 | 16 | 75.0% | 50.5%…89.8% | 75.0% | 27 | 10.31 |
| tactical | 2/0 | 16 | 43.8% | 23.1%…66.8% | 43.8% | 26 | 8.31 |
| tactical | 2/1 | 16 | 25.0% | 10.2%…49.5% | 25.0% | 27.5 | 9.56 |
| tactical | 2/2 | 16 | 62.5% | 38.6%…81.5% | 62.5% | 27.5 | 8.75 |

## Niyet rolü

Rol 0=AYNA niyet, rol 1=oyuncu niyet.

| Bot | Rol | n | Bot kazanma | Diğer |
|---|---|---:|---:|---:|
| Mind | 0 | 72 | 69.4% | 0.0% |
| Mind | 1 | 72 | 37.5% | 1.4% |
| Greedy | 0 | 72 | 94.4% | 0.0% |
| Greedy | 1 | 72 | 62.5% | 0.0% |
| Habit | 0 | 72 | 62.5% | 0.0% |
| Habit | 1 | 72 | 87.5% | 0.0% |

## Mind bitiş dağılımı

1=Son Şalter, 2=Silme, 3=Tekillik, 4=Hakimiyet, 5=Süre.

| Mod | Son Şalter | Silme | Tekillik | Hakimiyet | Süre | 40 öncesi |
|---|---:|---:|---:|---:|---:|---:|
| auto | 0.0% | 0.0% | 0.0% | 59.7% | 40.3% | 59.7% |
| tactical | 0.0% | 17.4% | 0.0% | 65.3% | 17.4% | 81.9% |

Ölçüm zamanı ana rapordaki Runtime alanında bulunur. Mono plan süreleri Unity ölçümü diye sunulmaz. Bütün bot × mimari hücreleri summary.json içindedir.
