#!/bin/bash
# Tek iş (kos.sh çağırır): is.sh <etiket> <BAL|-> <ALT> <kapı> <olay> <şalter> <s0> <s1>
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${OUT:-$ROOT/Library/DengeBotu}"
MONO="${MONO:-/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono}"
B="$2"; [ "$B" = "-" ] && B=""
RETIRE="${RETIRE:-1}" ALT="$3" BAL="$B" "$MONO" "$OUT/camp8.exe" "$7" "$8" "$4" "$5" "$6" "$1" \
  > "$OUT/out/$1/$4-$5-$6-$7.txt" 2> "$OUT/out/$1/$4-$5-$6-$7.err"
