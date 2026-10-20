"""F5 raporlarından bot/mimari/rol ve bitiş tabloları; paydalar tüm oyunlardır."""
from pathlib import Path
import json,statistics,math,argparse
p=argparse.ArgumentParser();p.add_argument('directory');a=p.parse_args();root=Path(a.directory)
reports=[]
for path in sorted(root.glob('*.json')):
 d=json.loads(path.read_text())
 if 'Options' in d and 'Games' in d and not path.name.endswith('.partial.json'):reports.append((path,d))
def stats(g):
 n=len(g);w=sum(x['Outcome']['Winner']==0 for x in g);l=sum(x['Outcome']['Winner']==1 for x in g);z=1.959963984540054;v=w/max(1,n);den=1+z*z/max(1,n);center=(v+z*z/(2*max(1,n)))/den;half=z*math.sqrt(v*(1-v)/max(1,n)+z*z/(4*max(1,n)**2))/den
 return {'n':n,'player_win':w/max(1,n),'ayna_win':l/max(1,n),'other':(n-w-l)/max(1,n),'player_score':(w+(n-w-l)*.5)/max(1,n),'win95':[center-half,center+half],'cycles':statistics.median(x['Cycles'] for x in g) if n else 0,'before40':sum(x['Cycles']<40 for x in g)/max(1,n),'battles':sum(x['Battles'] for x in g)/max(1,n),'surprise':sum(x['Surprises'] for x in g)/max(1,sum(x['Decisions'] for x in g)),'top1':sum(x['Hits'] for x in g)/max(1,sum(x['Decisions'] for x in g)),'ends':{str(k):sum(x['Outcome']['Reason']==k for x in g)/max(1,n) for k in range(1,6)}}
rows=[];cells=[];roles=[]
for path,d in reports:
 o=d['Options'];g=d['Games'];base={'bot':o['Bot'],'mode':'tactical' if o['Tactical'] else 'auto','path':str(path),'source':d['CodeVersion'],'balance_hash':d['BalanceHash']};row=base|stats(g)|{k:d[k] for k in ['MacroMedianMs','MacroP95Ms','BoardMedianMs','BoardP95Ms','Seconds']};rows.append(row)
 for pa in range(3):
  for aa in range(3):cells.append(base|{'player_arch':pa,'ayna_arch':aa}|stats([x for x in g if x['PlayerArchitecture']==pa and x['AynaArchitecture']==aa]))
 if o['Tactical']:
  for role in range(2):roles.append(base|{'role':role}|stats([x for x in g if x['Role']==role]))
expected={(bot,mode) for bot in ['Mind','Habit','Random','Greedy','Aggro','Stealth','Surprise'] for mode in ['auto','tactical']};complete=expected<={(r['bot'],r['mode']) for r in rows};out={'complete':complete,'rows':rows,'cells':cells,'roles':roles,'sources':sorted({r['source'] for r in rows})};(root/'summary.json').write_text(json.dumps(out,ensure_ascii=False,indent=2))
def pc(v):return f'{v*100:.1f}%'
md=['# AYNA · F5 matris özeti','',f'Tamamlanan kohort: {len(rows)}/14; tam matris: {complete}.','','Oyuncu tarafı tabloda adı geçen bot; karşısı AYNA Mind. Mimari sabit, oyun içi öğrenme açık, her oyunda profil boş. Niyet rolü ve başlangıç konumu ayrı faktörlerdir. Sonuçlar insan oyuncu nüfusunu temsil etmez.','','| Bot | Mod | n | AYNA kazanma | Bot kazanma | Diğer | Döngü ortanca | Savaş/oyun | Sürpriz |','|---|---|---:|---:|---:|---:|---:|---:|---:|']
for r in rows:md.append(f"| {r['bot']} | {r['mode']} | {r['n']} | {pc(r['ayna_win'])} | {pc(r['player_win'])} | {pc(r['other'])} | {r['cycles']:g} | {r['battles']:.2f} | {pc(r['surprise'])} |")
md+=['','## Mind / Mind mimari matrisi','','0=Sürü, 1=Kale, 2=Hayalet. Kazanma ve Wilson aralığında payda tüm oyunlar; skor beraberliğe yarım verir.','','| Mod | Oyuncu/AYNA | n | Oyuncu kazanma | %95 Wilson | Oyuncu skoru | Döngü | Savaş |','|---|---|---:|---:|---|---:|---:|---:|']
for r in cells:
 if r['bot']=='Mind':md.append(f"| {r['mode']} | {r['player_arch']}/{r['ayna_arch']} | {r['n']} | {pc(r['player_win'])} | {pc(r['win95'][0])}…{pc(r['win95'][1])} | {pc(r['player_score'])} | {r['cycles']:g} | {r['battles']:.2f} |")
md+=['','## Niyet rolü','','Rol 0=AYNA niyet, rol 1=oyuncu niyet.','','| Bot | Rol | n | Bot kazanma | Diğer |','|---|---|---:|---:|---:|']
for r in roles:md.append(f"| {r['bot']} | {r['role']} | {r['n']} | {pc(r['player_win'])} | {pc(r['other'])} |")
md+=['','## Mind bitiş dağılımı','','1=Son Şalter, 2=Silme, 3=Tekillik, 4=Hakimiyet, 5=Süre.','','| Mod | Son Şalter | Silme | Tekillik | Hakimiyet | Süre | 40 öncesi |','|---|---:|---:|---:|---:|---:|---:|']
for r in rows:
 if r['bot']=='Mind':md.append('| '+r['mode']+' | '+' | '.join(pc(r['ends'][str(k)]) for k in range(1,6))+' | '+pc(r['before40'])+' |')
md+=['','Ölçüm zamanı ana rapordaki Runtime alanında bulunur. Mono plan süreleri Unity ölçümü diye sunulmaz. Bütün bot × mimari hücreleri summary.json içindedir.']
(root/'SUMMARY.md').write_text('\n'.join(md)+'\n');print(json.dumps({'complete':complete,'rows':len(rows),'games':sum(r['n'] for r in rows)},ensure_ascii=False))
