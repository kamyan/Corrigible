"""F3: Tamamlanmış seri raporlarını toplar; kapıları ve seri kümeli aralıkları gösterir."""
import json,random,sys,hashlib,argparse
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('directory',nargs='?',default='Docs/AYNA/denge/F3');parser.add_argument('--runtime');parser.add_argument('--source');args=parser.parse_args();root=Path(args.directory)
source_hash=hashlib.sha256("\n".join(f.read_text() for f in sorted(Path("Assets/Corrigible/Ayna").rglob("*.cs"))).encode()).hexdigest()
source_hash=args.source or source_hash
rng=random.Random(3101026);reports={};previous=[]
for folder in ['mono_final','final','control']:
 for path in sorted((root/folder).glob('*.json')):
  d=json.loads(path.read_text());o=d['Options']
  if d['CodeVersion']!=source_hash:
   previous.append({'path':str(path),'CodeVersion':d['CodeVersion'],'Bot':o['Bot'],'Learning':o['Learning']});continue
  if o.get('SeriesOffset',0)!=0 or o['Series']!=100 or o['Games']!=5 or not o['Tactical'] or len(d['Games'])!=500:continue
  reports[(o['Bot'],o['Learning'])]=(path,d)
def pct(v):return f'{v*100:.2f}%'
def interval(values):
 values=sorted(values);return [values[int((len(values)-1)*.025)],values[int((len(values)-1)*.975)]]
rows=[]
for (bot,level),(path,d) in reports.items():
 groups={s:[g for g in d['Games'] if g['Series']==s] for s in range(100)}
 assert all(len(v)==5 for v in groups.values())
 gains=[]
 for _ in range(2000):
  ids=rng.choices(range(100),k=100);gains.append(sum(next(g for g in groups[s] if g['Game']==4)['AynaWon']-next(g for g in groups[s] if g['Game']==0)['AynaWon'] for s in ids)/100)
 late=[g for g in d['Games'] if g['Game']>=2];last=[g for g in d['Games'] if g['Game']==4]
 row={k:d[k] for k in ['FirstWinRate','LastWinRate','Gain','Top1FromThird','SurpriseRate','PlayerWinRate','PlannerMedianMs','PlannerP95Ms','Seconds','Failures']}
 row.update(Runtime=args.runtime or d.get('Runtime','Mono 6.12 (Unity Core/Mind DLL)' if path.parent.name=='mono_final' else 'Unity Editor'),Bot=bot,Learning=level,Path=str(path),CodeVersion=d['CodeVersion'],Gain95CI=interval(gains),LateSurpriseRate=sum(g['SurpriseCycles'] for g in late)/max(1,sum(g['Decisions'] for g in late)),FifthSurpriseRate=sum(g['SurpriseCycles'] for g in last)/max(1,sum(g['Decisions'] for g in last)),Generations=sorted(set(g['Generation'] for g in d['Games'])))
 rows.append(row)
gates=[]
def gate(name,measurement,target,ok):gates.append({'name':name,'measurement':measurement,'target':target,'passed':ok})
for bot,level in [('Habit',2),('Random',2),('Surprise',2),('Greedy',2)]:
 if (bot,level) not in reports:continue
 d=reports[(bot,level)][1]
 if bot=='Habit':
  gate('Habit kazanma artışı',d['Gain'],'≥0.15',d['Gain']>=.15);gate('Habit top-1 (3–5)',d['Top1FromThird'],'≥0.60',d['Top1FromThird']>=.6);gate('Habit Sürpriz (1–5)',d['SurpriseRate'],'<0.05',d['SurpriseRate']<.05)
 if bot=='Random':gate('Random kazanma farkı',d['Gain'],'±0.05',abs(d['Gain'])<=.05)
 if bot=='Surprise':gate('SurpriseBot Sürpriz',d['SurpriseRate'],'≥0.30',d['SurpriseRate']>=.3)
 if bot=='Surprise' and ('Greedy',2) in reports:
  greedy=reports[('Greedy',2)][1]['PlayerWinRate'];gate('SurpriseBot kazanması ≤Greedy',d['PlayerWinRate']-greedy,'≤0',d['PlayerWinRate']<=greedy)
for row in rows:gate(row['Bot']+'/'+str(row['Learning'])+' plan p50',row['PlannerMedianMs'],'≤50ms',row['PlannerMedianMs']<=50)
controls=[]
for bot in ['Habit','Random']:
 if (bot,2) in reports and (bot,0) in reports:
  full=reports[(bot,2)][1];off=reports[(bot,0)][1];controls.append({'Bot':bot,'FullFifth':full['LastWinRate'],'OffFifth':off['LastWinRate'],'FullMinusOffAtFifth':full['LastWinRate']-off['LastWinRate'],'OffGain':off['Gain']})
tradeoff=None
if ('Surprise',2) in reports and ('Greedy',2) in reports:
 surprise=reports[('Surprise',2)][1];greedy=reports[('Greedy',2)][1]
 series_differences=[]
 for series in range(100):
  sw=[g['PlayerWon'] for g in surprise['Games'] if g['Series']==series]
  gw=[g['PlayerWon'] for g in greedy['Games'] if g['Series']==series]
  assert len(sw)==len(gw)==5
  series_differences.append((sum(sw)-sum(gw))/5)
 boot=[sum(rng.choices(series_differences,k=100))/100 for _ in range(2000)]
 tradeoff={'Difference':surprise['PlayerWinRate']-greedy['PlayerWinRate'],'PairedSeries95CI':interval(boot),'Meaning':'Surprise eksi Greedy oyuncu kazanma oranı. Aynı seri tohumları beş oyun birlikte örneklenir; uyarlanan rakip nedeniyle oyun sonu durumları eşit değildir.'}
expected={('Habit',2),('Random',2),('Surprise',2),('Greedy',2),('Habit',0),('Random',0)}
out={'CodeVersion':source_hash,'PreviousReportsNotUsed':previous,'Complete':expected<=set(reports),'Rows':rows,'Gates':gates,'Controls':controls,'Tradeoff':tradeoff,'Hashes':sorted({r['CodeVersion'] for r in rows}),'ConfidenceMethod':'2.000 tekrar; 100 serinin her biri beş oyunu birlikte tutan eşli bootstrap, tohum 3101026. Genel insan oyuncu nüfusuna güven aralığı değildir.'}
(root/'ozet.json').write_text(json.dumps(out,ensure_ascii=False,indent=2))
md=['# AYNA · F3 seri özeti','',f"Tamamlandı: {out['Complete']}",'','| Bot / öğrenme | İlk → beşinci AYNA kazanma | Fark (%95 seri aralığı) | Top-1 (3–5) | Sürpriz (1–5) | Sürpriz (3–5) | Oyuncu kazanma |','|---|---:|---:|---:|---:|---:|---:|']
for r in rows:
 lo,hi=r['Gain95CI'];md.append(f"| {r['Bot']} / {r['Learning']} | {pct(r['FirstWinRate'])} → {pct(r['LastWinRate'])} | {pct(r['Gain'])} ({pct(lo)}…{pct(hi)}) | {pct(r['Top1FromThird'])} | {pct(r['SurpriseRate'])} | {pct(r['LateSurpriseRate'])} | {pct(r['PlayerWinRate'])} |")
md+=['','| Kapı | Ölçülen | Hedef | Sonuç |','|---|---:|---:|---|']
for g in gates:md.append(f"| {g['name']} | {g['measurement']:.4f} | {g['target']} | {'Geçti' if g['passed'] else 'Açık'} |")
md+=['','Çalışma zamanları: ' + '; '.join(sorted({r['Runtime'] for r in rows})),out['ConfidenceMethod'],'','Öğrenme 0=Kapalı, 2=Tam. Paydalar ham raporlardadır; kısmi durdurulan işler bu tablolara eklenmez.']
if tradeoff:
 lo,hi=tradeoff['PairedSeries95CI'];md+=['',f"Surprise−Greedy kazanma farkı: {pct(tradeoff['Difference'])}; %95 seri aralığı {pct(lo)}…{pct(hi)}.",tradeoff['Meaning']]
md+=['','Top-1 planın herhangi bir geçerli emrine isabettir; Sürpriz yeni politikada ana emre bakar. Ham OrdersByKind/SurpriseByKind tanı alanı eski minimum-olasılık etiketleyicisini kullanır; yeni politikanın ödül kaynağı dağılımı olarak yorumlanmaz. Kabulte gerçek LastSurprise bayrağından sayılan SurpriseRate kullanılır.']
(root/'OZET.md').write_text('\n'.join(md)+'\n')
print(json.dumps({'Complete':out['Complete'],'reports':len(rows),'gates':gates},ensure_ascii=False,indent=2))
