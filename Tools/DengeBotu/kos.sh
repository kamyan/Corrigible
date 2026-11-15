#!/bin/bash
# Koşu listesi: kos.sh <liste> (ör. asama8.txt). Satır: etiket|BAL (ya da -)|ALT|yapılandırma dosyası|tohum parçaları
# Yapılandırma dosyası satır başına "kapı olay şalter". Paralellik PAR (varsayılan 8), RETIRE varsayılanı 1 (sunumun kuralı).
# Çıktı: $OUT/out/<etiket>/<kapı>-<olay>-<şalter>-<s0>.txt; özet: python3 -I ozet.py <etiket>…
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"; HERE="$ROOT/Tools/DengeBotu"
export OUT="${OUT:-$ROOT/Library/DengeBotu}"
[ -f "$OUT/camp8.exe" ] || "$HERE/derle.sh" || exit 1
while IFS='|' read tag bal alt cfg chunks; do
  case "$tag" in ''|'#'*) continue ;; esac
  mkdir -p "$OUT/out/$tag"; echo "BAL=$bal RETIRE=${RETIRE:-1} ALT=$alt" > "$OUT/out/$tag/BAL.txt"
  while read door ev sw; do
    [ -z "$door" ] && continue
    for ch in $chunks; do echo "$tag ${bal:--} $alt $door $ev $sw ${ch/-/ }"; done
  done < "$HERE/$cfg"
done < "$1" | xargs -P "${PAR:-8}" -L 1 "$HERE/is.sh"
echo BITTI
