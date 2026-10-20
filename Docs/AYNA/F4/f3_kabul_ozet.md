# AYNA · F3 seri özeti

Tamamlandı: True

| Bot / öğrenme | İlk → beşinci AYNA kazanma | Fark (%95 seri aralığı) | Top-1 (3–5) | Sürpriz (1–5) | Sürpriz (3–5) | Oyuncu kazanma |
|---|---:|---:|---:|---:|---:|---:|
| Random / 0 | 100.00% → 100.00% | 0.00% (0.00%…0.00%) | 0.95% | 0.00% | 0.00% | 0.00% |
| Random / 2 | 100.00% → 100.00% | 0.00% (0.00%…0.00%) | 2.33% | 0.00% | 0.00% | 0.00% |
| Habit / 0 | 14.00% → 34.00% | 20.00% (9.00%…30.00%) | 23.61% | 0.00% | 0.00% | 67.40% |
| Greedy / 2 | 11.00% → 36.00% | 25.00% (13.00%…36.00%) | 97.18% | 6.57% | 4.60% | 67.20% |
| Surprise / 2 | 80.00% → 76.00% | -4.00% (-15.00%…7.00%) | 52.07% | 37.57% | 47.22% | 16.40% |
| Habit / 2 | 21.00% → 62.00% | 41.00% (29.00%…53.00%) | 97.78% | 4.15% | 3.12% | 40.60% |

| Kapı | Ölçülen | Hedef | Sonuç |
|---|---:|---:|---|
| Habit kazanma artışı | 0.4100 | ≥0.15 | Geçti |
| Habit top-1 (3–5) | 0.9778 | ≥0.60 | Geçti |
| Habit Sürpriz (1–5) | 0.0415 | <0.05 | Geçti |
| Random kazanma farkı | 0.0000 | ±0.05 | Geçti |
| SurpriseBot Sürpriz | 0.3757 | ≥0.30 | Geçti |
| SurpriseBot kazanması ≤Greedy | -0.5080 | ≤0 | Geçti |
| Random/0 plan p50 | 4.7091 | ≤50ms | Geçti |
| Random/2 plan p50 | 5.3542 | ≤50ms | Geçti |
| Habit/0 plan p50 | 5.0752 | ≤50ms | Geçti |
| Greedy/2 plan p50 | 6.1884 | ≤50ms | Geçti |
| Surprise/2 plan p50 | 5.8955 | ≤50ms | Geçti |
| Habit/2 plan p50 | 5.8894 | ≤50ms | Geçti |

Çalışma zamanları: Mono 6.12 (Unity Core/Mind DLL)
2.000 tekrar; 100 serinin her biri beş oyunu birlikte tutan eşli bootstrap, tohum 3101026. Genel insan oyuncu nüfusuna güven aralığı değildir.

Öğrenme 0=Kapalı, 2=Tam. Paydalar ham raporlardadır; kısmi durdurulan işler bu tablolara eklenmez.

Surprise−Greedy kazanma farkı: -50.80%; %95 seri aralığı -56.40%…-45.40%.
Surprise eksi Greedy oyuncu kazanma oranı. Aynı seri tohumları beş oyun birlikte örneklenir; uyarlanan rakip nedeniyle oyun sonu durumları eşit değildir.

Top-1 planın herhangi bir geçerli emrine isabettir; Sürpriz yeni politikada ana emre bakar. Ham OrdersByKind/SurpriseByKind tanı alanı eski minimum-olasılık etiketleyicisini kullanır; yeni politikanın ödül kaynağı dağılımı olarak yorumlanmaz. Kabulte gerçek LastSurprise bayrağından sayılan SurpriseRate kullanılır.
