#!/bin/bash
# F8.2 taslaklarını 1920×1080 PNG'ye çizer (yüklü Google Chrome, başsız). Kullanım: tools/render_drafts.sh [ad ...]
# Çıktı: drafts/<ad>.png. Referans PNG'leri de aynı yolla üretildiği için ölçek ve yazı tipleri birebir.
set -e
cd "$(dirname "$0")/../drafts"
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
PROFILE="$(mktemp -d)"
names=("$@"); [ ${#names[@]} -eq 0 ] && names=($(ls *.html | sed 's/\.html$//'))
for n in "${names[@]}"; do
  rm -f "$n.png"
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 --window-size=1920,1080 \
    --user-data-dir="$PROFILE" --virtual-time-budget=2000 --screenshot="$PWD/$n.png" "file://$PWD/$n.html" >/dev/null 2>&1 &
  pid=$!
  for i in $(seq 1 40); do sleep 0.5; [ -f "$n.png" ] && break; done
  kill $pid 2>/dev/null || true; wait $pid 2>/dev/null || true
  echo "$n.png"
done
rm -rf "$PROFILE"
