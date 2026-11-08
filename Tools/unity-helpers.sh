# Corrigible — Unity Editor'ü CLI'dan sürmek için yardımcılar (bash/zsh).
# Kullanım:  source Tools/unity-helpers.sh   (Unity Editor bu proje açıkken çalışıyor olmalı)
#
#   ueval '<C# kodu; değer döndürmek için return>'   Editor'de C# çalıştırır, sonucu yazar
#   ucompile                                       yeniden derler ve YALNIZCA bu derlemeden sonra çıkan hataları yazar
#   utest [filtre]                                 EditMode testleri (--mode editor, uzun CLI zaman aşımı; domain reload
#                                                  sürerken yeniden dener). Önce boş sahne aç.
#   uptest [filtre]                                PlayMode testleri (Corrigible.Desk.PlayTests): async başlatır, test_status'u
#                                                  bitene dek yoklar. Önce boş sahne aç; sonra *Dyn SDF.asset'i git checkout et.
#   uplay / ustop                                  Play moduna gir / çık
#   ushot <dosya.png>                              arayüzün ekran görüntüsü (Temp/ altına yaz; proje dışına yazılamaz)
#   uerrors <ISO-zaman>                            verilen zamandan sonraki konsol hataları
#
# Not: unity CLI'nin recompile_status komutu derleme hatasını güvenilir bildirmez; hatalar konsoldan okunur.
# Not: --mode verilmeyen run_tests 'all' koşar: EditMode'dan sonra PlayMode'u arka planda başlatır. utest bu yüzden
# --mode editor verir. UTEST_TIMEOUT (sn, varsayılan 1500) CLI isteğinin zaman aşımıdır (global --timeout);
# UPTEST_WAIT (sn, varsayılan 900) PlayMode yoklamasının üst sınırı.

CORRIGIBLE_ROOT="${CORRIGIBLE_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]:-${(%):-%x}}")/.." && pwd)}"

_u() { (cd "$CORRIGIBLE_ROOT" && unity command --project-path "$CORRIGIBLE_ROOT" --caller plugin --skill unity-cli --no-banner --format json "$@" 2>&1); }

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
  local i out filter=()
  [ -n "$1" ] && filter=(--filter "$1")
  for i in 1 2 3 4 5; do
    out=$(_u --timeout "${UTEST_TIMEOUT:-1500}" run_tests --mode editor "${filter[@]}" | python3 -c "
import sys, json
try: d = json.load(sys.stdin)
except Exception as e: print('RETRY yanıt okunamadı:', e); sys.exit()
r = (d.get('data') or {}).get('result')
if r and r.get('Success') is False: print('HATA', r.get('Error') or r.get('Message'))
elif r:
    print(r['Summary'])
    for x in r['Results']:
        if x['Status'] != 'Passed': print(x['FullName'], x['Message'])
else: print('RETRY', ((d.get('errors') or [{}])[0]).get('message'))")
    case "$out" in RETRY*) sleep 15 ;; *) echo "$out"; return ;; esac
  done
  echo "$out"
}

uptest() {
  local out filter=() waited=0 limit="${UPTEST_WAIT:-900}"
  [ -n "$1" ] && filter=(--filter "$1")
  # PlayMode HTTP üstünden eşzamanlı koşamaz (Play'e giriş domain reload yapar, istek düşer): async başlat, sonucu yokla
  out=$(_u run_tests --mode playmode --async_tests true "${filter[@]}" | python3 -c "
import sys, json
try: d = json.load(sys.stdin)
except Exception as e: print('HATA yanıt okunamadı:', e); sys.exit()
r = (d.get('data') or {}).get('result')
if d.get('errors'): print('HATA', d['errors'][0].get('message'))
elif not r or r.get('Success') is False: print('HATA', (r or {}).get('Error') or (r or {}).get('Message') or d)
else: print('BASLADI', r.get('Result'))")
  case "$out" in HATA*) echo "$out"; return 1 ;; esac
  while [ "$waited" -lt "$limit" ]; do
    sleep 5; waited=$((waited + 5))
    out=$(_u test_status | python3 -c "
import sys, json
try: d = json.load(sys.stdin)
except Exception: print('BEKLE okunamadı'); sys.exit()
r = (d.get('data') or {}).get('result') if isinstance(d, dict) else None
if r is None and isinstance(d, dict) and 'status' in d: r = d
if not isinstance(r, dict): print('BEKLE', ((d.get('errors') or [{}])[0]).get('message') if isinstance(d, dict) else d); sys.exit()
s = r.get('status')
if s == 'completed':
    m = r.get('summary') or {}
    print('Toplam %s · geçti %s · kaldı %s · atlandı %s · belirsiz %s · %s sn' % (m.get('total'), m.get('passed'), m.get('failed'), m.get('skipped'), m.get('inconclusive'), r.get('duration')))
    for x in r.get('results') or []:
        st = x.get('Status') or x.get('status')
        if st != 'Passed': print(x.get('FullName') or x.get('fullName'), st, (x.get('Message') or x.get('message') or '')[:400])
elif s in ('error', 'malformed', 'unreadable'): print('HATA', r.get('message'))
else: print('BEKLE', s)")
    case "$out" in BEKLE*) ;; *) echo "$out"; return ;; esac
  done
  echo "ZAMAN AŞIMI ($limit sn): test_status bitmedi; son durum: $out"
  return 1
}

uplay() { _u editor_play >/dev/null; sleep 5; echo "Play"; }
ustop() { _u editor_stop >/dev/null; sleep 2; echo "Durdu"; }
ushot() { ueval "return Corrigible.EditorTools.CorrigibleDebug.Capture(\"${1:-Temp/shot.png}\");"; }
