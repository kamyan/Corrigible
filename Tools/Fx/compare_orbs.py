#!/usr/bin/env python3
"""Unity'nin 22 RT yakalamasını paket eşikleriyle karşılaştırır; JSON ve temas sayfası üretir."""
from pathlib import Path
import numpy as np,json,sys
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2];sys.dont_write_bytecode=True;pkg=root/'Docs/UI_Fx/SonSalter_Fx_Paketi';sys.path.insert(0,str(pkg/'tools'));from compare import edges
out=root/'Docs/UI_Fx/captures/orb';rows=[];thumb=Image.new('RGB',(6*280,4*310),(2,5,10));draw=ImageDraw.Draw(thumb)
for i,f in enumerate(json.load(open(pkg/'reference/orb/golden/golden.json'))['files']):
 name=f['file'];ref=np.asarray(Image.open(pkg/'reference/orb/golden'/name).convert('RGB')).astype(np.float32);cap=np.asarray(Image.open(out/name).convert('RGB')).astype(np.float32);er=edges(ref);ec=edges(cap);mean=np.abs(ref-cap).mean();ov=(er&ec).sum()/max(1,er.sum());r=dict(file=name,mean=float(mean),edgeOverlap=float(ov),referenceEdges=int(er.sum()),captureEdges=int(ec.sum()),pass_=bool(mean<=6 and ov>=.7));rows.append(r)
 x=i%6*280;y=i//6*310;thumb.paste(Image.fromarray(cap.astype('uint8')).resize((280,280)),(x,y));draw.text((x+4,y+281),name,fill='white');draw.text((x+4,y+295),f'{mean:.2f} / {ov:.1%}',fill='cyan' if r['pass_'] else 'orange')
(out/'comparison.json').write_text(json.dumps(rows,indent=2));thumb.save(out/'contact.png');print('Pass',sum(r['pass_'] for r in rows),'/',len(rows));print('\n'.join(f"{r['file']}: {r['mean']:.2f} / {r['edgeOverlap']:.1%}" for r in rows))
