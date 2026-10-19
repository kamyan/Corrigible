"""F7: resmi araçla 19 maskeli karşılaştırma; ölçümler eşlik kabulü sayılmaz."""
from pathlib import Path
import subprocess,sys,json
root=Path(__file__).resolve().parents[2];pkg=root/'Docs/UI_Fx/SonSalter_Fx_Paketi';out=root/'Docs/UI_Fx/captures/F7';rows=[]
for name in [f'F-Masa{i}' for i in range(1,10)]+[f'F-Baskin{i}' for i in range(1,10)]+['F-YZ']:
 args=[sys.executable,str(pkg/'tools/compare.py'),str(out/f'{name}_Tr_1920x1080.png'),str(pkg/f'reference/screens/{name}.jpg'),'--tsv',str(pkg/f'reference/measure/{name}.tsv'),'--out',str(out/f'{name}_cmp')]
 mask=pkg/f'reference/measure/{name}_mask.json'
 if mask.exists():args+=['--mask',str(mask)]
 r=subprocess.run(args,capture_output=True,text=True);rows.append({'screen':name,'exitCode':r.returncode,'report':r.stdout,'errors':r.stderr})
(out/'reference-comparison.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2));print('\n'.join(r['screen']+': '+r['report'].split('\n')[0] for r in rows));assert all(r['exitCode']==0 for r in rows)
