"""F5 sonrası F3'ün 100×5 Tam/Kapalı öğrenme koruma matrisi."""
from pathlib import Path
import argparse,subprocess,concurrent.futures,json,time
p=argparse.ArgumentParser();p.add_argument('--runner',required=True);p.add_argument('--directory',required=True);p.add_argument('--workers',type=int,default=4);a=p.parse_args();root=Path(a.directory);root.mkdir(parents=True,exist_ok=True)
tasks=[(b,2) for b in ['Habit','Random','Surprise','Greedy']]+[(b,0) for b in ['Habit','Random']];done=[];start=time.time()
def run(t):
 bot,level=t;dest=root/('final' if level==2 else 'control');dest.mkdir(exist_ok=True);r=subprocess.run(['mono',a.runner,bot,'100',str(dest),str(level)],text=True,capture_output=True);(root/(bot+'_'+str(level)+'.log')).write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError(bot+': '+r.stderr[-1500:])
 return {'Bot':bot,'Level':level,'Output':r.stdout.strip().splitlines()[-1]}
with concurrent.futures.ThreadPoolExecutor(max_workers=a.workers) as pool:
 for f in concurrent.futures.as_completed([pool.submit(run,t) for t in tasks]):
  done.append(f.result());(root/'progress.json').write_text(json.dumps({'complete':len(done),'total':6,'seconds':time.time()-start,'jobs':done},indent=2));print(json.dumps(done[-1]),flush=True)
