"""Hazır F5 Mono sürücüsünü bot/mod işleri halinde çalıştırır; oyun kuralı değiştirmez."""
from pathlib import Path
import argparse,json,subprocess,concurrent.futures,time
p=argparse.ArgumentParser();p.add_argument('--runner',required=True);p.add_argument('--output',required=True);p.add_argument('--seeds',type=int,default=8);p.add_argument('--seed',type=int,default=5102026);p.add_argument('--offset',type=int,default=0);p.add_argument('--workers',type=int,default=4);p.add_argument('--bots',default='Mind,Habit,Random,Greedy,Aggro,Stealth,Surprise');p.add_argument('--balance');a=p.parse_args()
output=Path(a.output);output.mkdir(parents=True,exist_ok=True);jobs=output/'jobs';jobs.mkdir(exist_ok=True)
balance=json.loads(Path(a.balance).read_text()) if a.balance else json.loads(subprocess.check_output(['mono',a.runner,'--defaults'],text=True))
(output/'balance.json').write_text(json.dumps(balance,indent=2));tasks=[]
for bot in a.bots.split(','):
 for tactical in [False,True]:
  options={'Bot':bot,'Seeds':a.seeds,'Seed':a.seed,'SeedOffset':a.offset,'Tactical':tactical,'Directory':str(output),'Balance':balance};f=jobs/(bot+('_tactical' if tactical else '_auto')+'.json');f.write_text(json.dumps(options));tasks.append(f)
started=time.time();done=[]
def run(f):
 result=subprocess.run(['mono',a.runner,str(f)],text=True,capture_output=True);(f.with_suffix('.log')).write_text(result.stdout+result.stderr)
 if result.returncode:raise RuntimeError(str(f)+': '+result.stderr[-2000:])
 return {'job':f.name,'report':result.stdout.strip().splitlines()[-1]}
with concurrent.futures.ThreadPoolExecutor(max_workers=a.workers) as pool:
 future={pool.submit(run,f):f for f in tasks}
 for x in concurrent.futures.as_completed(future):
  done.append(x.result());(output/'progress.json').write_text(json.dumps({'complete':len(done),'total':len(tasks),'seconds':time.time()-started,'jobs':done},indent=2));print(json.dumps(done[-1]),flush=True)
print(json.dumps({'complete':len(done),'seconds':time.time()-started}),flush=True)
