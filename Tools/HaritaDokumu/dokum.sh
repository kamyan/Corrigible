#!/bin/bash
# Üretilmiş harita dökümü: dokum.sh <tohumBaş> <tohumSon> <çıktıKlasörü>
# Core + Dokum.cs → $OUT/dokum.exe (Mono), her tohum için seed_XX.svg, sonra Chrome (headless, geçici profil) ile seed_XX.png
# ve 5 sütunlu sheet.png. Unity gerekmez. Chrome yolu CHROME ile değişir.
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${OUT:-$ROOT/Library/HaritaDokumu}"
# CORE: derlenecek Core klasörü (varsayılan depodaki; üretici denemesi için bir kopya verilebilir)
CORE="${CORE:-$ROOT/Assets/Corrigible/Core}"
CSC="${CSC:-/Library/Frameworks/Mono.framework/Versions/Current/Commands/csc}"
MONO="${MONO:-/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono}"
CHROME="${CHROME:-/Applications/Google Chrome.app/Contents/MacOS/Google Chrome}"
S0="$1"; S1="$2"; DIR="$3"
mkdir -p "$OUT" "$DIR"
"$CSC" -nologo -optimize+ -nowarn:0649,0169,0414,0219,1701,8321 -out:"$OUT/dokum.exe" \
  "$CORE"/*.cs "$ROOT/Tools/HaritaDokumu/Dokum.cs"
"$MONO" "$OUT/dokum.exe" "$S0" "$S1" "$DIR"
PROFILE="$(mktemp -d "$OUT/chrome.XXXXXX")"
shot() { "$CHROME" --headless=new --disable-gpu --hide-scrollbars --user-data-dir="$PROFILE" --no-first-run \
  --default-background-color=00000000 --window-size="$2" --screenshot="$3" "file://$1" >/dev/null 2>&1; }
for f in "$DIR"/seed_*.svg; do shot "$f" 2340,1080 "${f%.svg}.png"; done
# Tek sayfa: 5 sütun, her harita 768 px genişlik (yan liste kırpılır)
N=$(ls "$DIR"/seed_*.png | wc -l | tr -d ' '); ROWS=$(( (N + 4) / 5 ))
{
  echo "<html><body style='margin:0;background:#f4efe4'><div style='display:grid;grid-template-columns:repeat(5,768px)'>"
  for f in "$DIR"/seed_*.png; do
    echo "<div style='width:768px;height:432px;overflow:hidden'><img src='$(basename "$f")' style='width:936px;height:432px;object-fit:cover;object-position:left'></div>"
  done
  echo "</div></body></html>"
} > "$DIR/sheet.html"
shot "$DIR/sheet.html" "3840,$((ROWS * 432))" "$DIR/sheet.png"
rm -rf "$PROFILE"
echo "$DIR"
