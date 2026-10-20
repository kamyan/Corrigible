# AYNA · F3 seri özeti

Tamamlandı: True

| Bot / öğrenme | İlk → beşinci AYNA kazanma | Fark (%95 seri aralığı) | Top-1 (3–5) | Sürpriz (1–5) | Sürpriz (3–5) | Oyuncu kazanma |
|---|---:|---:|---:|---:|---:|---:|
| Random / 2 | 100.00% → 100.00% | 0.00% (0.00%…0.00%) | 2.04% | 10.43% | 12.16% | 0.00% |
| Surprise / 2 | 9.00% → 8.00% | -1.00% (-9.00%…7.00%) | 62.82% | 41.84% | 53.02% | 92.40% |
| Greedy / 2 | 10.00% → 24.00% | 14.00% (3.00%…25.00%) | 94.03% | 5.92% | 6.03% | 77.40% |
| Habit / 2 | 28.00% → 67.00% | 39.00% (26.00%…52.00%) | 93.44% | 3.71% | 1.61% | 43.60% |
| Random / 0 | 100.00% → 100.00% | 0.00% (0.00%…0.00%) | 0.57% | 0.00% | 0.00% | 0.00% |
| Habit / 0 | 22.00% → 36.00% | 14.00% (1.00%…27.00%) | 14.59% | 0.00% | 0.00% | 67.20% |

| Kapı | Ölçülen | Hedef | Sonuç |
|---|---:|---:|---|
| Habit kazanma artışı | 0.3900 | ≥0.15 | Geçti |
| Habit top-1 (3–5) | 0.9344 | ≥0.60 | Geçti |
| Habit Sürpriz (1–5) | 0.0371 | <0.05 | Geçti |
| Random kazanma farkı | 0.0000 | ±0.05 | Geçti |
| SurpriseBot Sürpriz | 0.4184 | ≥0.30 | Geçti |
| SurpriseBot kazanması ≤Greedy | 0.1500 | ≤0 | Açık |
| Random/2 plan p50 | 5.9379 | ≤50ms | Geçti |
| Surprise/2 plan p50 | 5.7215 | ≤50ms | Geçti |
| Greedy/2 plan p50 | 6.1737 | ≤50ms | Geçti |
| Habit/2 plan p50 | 6.6032 | ≤50ms | Geçti |
| Random/0 plan p50 | 6.2190 | ≤50ms | Geçti |
| Habit/0 plan p50 | 6.3375 | ≤50ms | Geçti |

Çalışma zamanları: Mono 6.12 (Unity Core/Mind DLL)
2.000 tekrar; 100 serinin her biri beş oyunu birlikte tutan eşli bootstrap, tohum 3101026. Genel insan oyuncu nüfusuna güven aralığı değildir.

Öğrenme 0=Kapalı, 2=Tam. Paydalar ham raporlardadır; kısmi durdurulan işler bu tablolara eklenmez.

Surprise−Greedy kazanma farkı: 15.00%; %95 seri aralığı 10.80%…19.40%.
Surprise eksi Greedy oyuncu kazanma oranı. Aynı seri tohumları beş oyun birlikte örneklenir; uyarlanan rakip nedeniyle oyun sonu durumları eşit değildir.

Top-1 planın herhangi bir geçerli emrine isabettir; Sürpriz yeni politikada ana emre bakar. Ham OrdersByKind/SurpriseByKind tanı alanı eski minimum-olasılık etiketleyicisini kullanır; yeni politikanın ödül kaynağı dağılımı olarak yorumlanmaz. Kabulte gerçek LastSurprise bayrağından sayılan SurpriseRate kullanılır.
