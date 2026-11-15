#!/bin/bash
# Botu derler: Core + Tests/EditMode/RaidBot.cs + Camp8.cs → $OUT/camp8.exe.
# OUT varsayılanı Library/DengeBotu (git'e girmez). Mono gerekir (csc). Core değişince yeniden derle.
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${OUT:-$ROOT/Library/DengeBotu}"
CSC="${CSC:-/Library/Frameworks/Mono.framework/Versions/Current/Commands/csc}"
mkdir -p "$OUT"
"$CSC" -nologo -optimize+ -nowarn:0649,0169,0414,0219,1701,8321 -out:"$OUT/camp8.exe" \
  "$ROOT"/Assets/Corrigible/Core/*.cs "$ROOT/Assets/Corrigible/Tests/EditMode/RaidBot.cs" "$ROOT/Tools/DengeBotu/Camp8.cs"
echo "$OUT/camp8.exe"
