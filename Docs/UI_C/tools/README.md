# tools/

| Betik | Ne yapar | Gerekenler |
|---|---|---|
| `compare.py` | Unity ekran görüntüsünü referans PNG ile karşılaştırır: kenar bindirme (`_edges`), soğan kâğıdı (`_onion`), fark (`_diff`), yan yana (`_side`) + bölge bazında fark yüzdesi | `pip install pillow` |
| `sprite_kit.py` | `art/sprites`, `art/textures`, `art/map` içeriğini SVG/CSS tanımlarından **@2x** yeniden üretir; manifest ve kontak sayfalarını yazar. `SCALE` değişkeniyle @3x/@4x üretilebilir (o zaman PPU = 100 × SCALE) | `pip install playwright pillow` + `playwright install chromium` |
| `measure_dump.py` | Bir referans HTML'den ölçü dökümü (TSV) üretir: `python3 tools/measure_dump.py reference/html/C1_main.html reference/measure/C1_main.tsv "C1"` | Playwright |

Örnek karşılaştırma:

```
python3 tools/compare.py captures/C1_2026-10-12.png reference/screens/C1_main.png --regions C1 --out captures/diff
```

`--regions`: `C1`, `C2`, `C3`, `C4` (adlandırılmış bölgeler) ya da boş (3×3 ızgara).
