# Corrigible — Unity Editor'ü CLI'dan sürmek için yardımcılar (bash/zsh).
# Kullanım:  source Tools/unity-helpers.sh   (Unity Editor bu proje açıkken çalışıyor olmalı)
#
#   ueval '<C# kodu; değer döndürmek için return>'   Editor'de C# çalıştırır, sonucu yazar
#   ucompile                                       yeniden derler ve YALNIZCA bu derlemeden sonra çıkan hataları yazar
#   utest                                          EditMode testlerini çalıştırır (domain reload sürerken yeniden dener)
#   uplay / ustop                                  Play moduna gir / çık
#   ushot <dosya.png>                              arayüzün ekran görüntüsü (Temp/ altına yaz; proje dışına yazılamaz)
#   uerrors <ISO-zaman>                            verilen zamandan sonraki konsol hataları
#
# Not: unity CLI'nin recompile_status komutu derleme hatasını güvenilir bildirmez; hatalar konsoldan okunur.

CORRIGIBLE_ROOT="${CORRIGIBLE_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]:-${(%):-%x}}")/.." && pwd)}"

_u() { (cd "$CORRIGIBLE_ROOT" && unity command --caller plugin --skill unity-cli --no-banner --format json "$@" 2>&1); }

ueval() {
  _u eval --code "$1" | python3 -c "
import sys, json
d = json.load(sys.stdin)
r = (d.get('data') or {}).get('result') or {}
if d.get('errors'): print('HATA:', d['errors'][0].get('message'))
elif isinstance(r, dict):
    print(r.get('result'))
    if r.get('diagnostics'): print(r['diagnostics'])
else: print(r)"
}

uerrors() {
  _u console | python3 -c "
import sys, json
d = json.load(sys.stdin)['data']['result']
L = d if isinstance(d, list) else d.get('logs', d.get('entries', []))
errs = [x for x in L if x.get('level') in ('error', 'exception') and x['timestampUtc'][:19] >= '$1' and 'NoSubscription' not in x['message']]
print('HATA YOK' if not errs else '\n'.join(x['message'][:400] for x in errs[-25:]))"
}

ucompile() {
  local t; t=$(date -u +%Y-%m-%dT%H:%M:%S)
  _u recompile >/dev/null; sleep 18
  uerrors "$t"
}

utest() {
  local i out
  for i in 1 2 3 4 5; do
    out=$(_u run_tests | python3 -c "
import sys, json
d = json.load(sys.stdin); r = (d.get('data') or {}).get('result')
if r:
    print(r['Summary'])
    for x in r['Results']:
        if x['Status'] != 'Passed': print(x['FullName'], x['Message'])
else: print('RETRY', d['errors'][0]['message'])")
    case "$out" in RETRY*) sleep 15 ;; *) echo "$out"; return ;; esac
  done
  echo "$out"
}

uplay() { _u editor_play >/dev/null; sleep 5; echo "Play"; }
ustop() { _u editor_stop >/dev/null; sleep 2; echo "Durdu"; }
ushot() { ueval "return Corrigible.EditorTools.CorrigibleDebug.Capture(\"${1:-Temp/shot.png}\");"; }
