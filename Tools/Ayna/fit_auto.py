"""F2: Ayrı tohumlarda ölçülmüş tahta sonuçlarına otomatik güç katsayısı uydurur."""
import json,sys,math,random
from pathlib import Path
src=Path(sys.argv[1]);data=json.loads(src.read_text())
games=[]
class_pairs={'Kara/Hava':(5,6),'Hava/Siber':(6,7),'Siber/Kara':(7,5)}
for original in data['Games']:
 g=dict(original)
 if g['PlayerSquad'] in class_pairs:
  p,a=class_pairs[g['PlayerSquad']];g['PlayerSquad']=str(p);g['AynaSquad']=str(a)
 if g['PlayerSquad'].isdigit() and g['AynaSquad'].isdigit():games.append(g)
squads=[[0,4,4],[0,2,2],[0,8,8],[0,1,5],[6,7,9],[2,3,2],[0,1,4],[6,7,8]]
classes=[1,1,0,0,1,1,2,2,2,2]
def beats(a,b):return (a,b) in [(0,1),(1,2),(2,0)]
def predict(p,a,w):
 def strength(left,right):
  return sum(w[u]*(1+w[10]*sum(beats(classes[u],classes[e]) for e in right)/len(right)) for u in left)
 x=strength(squads[p],squads[a]);y=strength(squads[a],squads[p]);return x/(x+y)
train=[g for g in games if g['Seed']%2==0];test=[g for g in games if g['Seed']%2==1]
def loss(w,cohort):return sum((predict(int(g['PlayerSquad']),int(g['AynaSquad']),w)-g['Score'])**2 for g in cohort)/len(cohort)
# Aynı kadro çifti için tek hedef ortalama: optimizasyonda tekrarlı hesap azaltılır.
groups={}
for g in train:groups.setdefault((int(g['PlayerSquad']),int(g['AynaSquad'])),[]).append(g['Score'])
targets=[(p,a,sum(v)/len(v),len(v)) for (p,a),v in groups.items()]
baseline=[data['Balance']['UnitHp'][i]+data['Balance']['AutoUtility'][i] for i in range(10)]
active=sorted(set(u for squad in squads for u in squad))+[10]
def objective(w):return sum(n*(predict(p,a,w)-t)**2 for p,a,t,n in targets)/len(train)+.001*sum(math.log(w[i]/baseline[i])**2 for i in active if i<10)/10
rng=random.Random(241026);best=None
for start in range(12):
 w=[data['Balance']['UnitHp'][i]+data['Balance']['AutoUtility'][i] for i in range(10)]+[data['Balance']['TriangleStrength']]
 if start:w=[rng.uniform(.1,15) if i in active else baseline[i] for i in range(10)]+[rng.uniform(0,2)]
 for step in [4,2,1,.5,.25,.1,.05]:
  changed=True
  while changed:
   changed=False
   for i in active:
    old=w[i];base=objective(w);chosen=old
    for x in [max(.05 if i<10 else 0,old-step),min(40 if i<10 else 3,old+step)]:
     w[i]=x;err=objective(w)
     if err<base-1e-12:base=err;chosen=x;changed=True
    w[i]=chosen
 if best is None or objective(w)<objective(best):best=w[:]
scale=sum(baseline)/sum(best[:10]);best=[x*scale for x in best[:10]]+[best[10]]
output={'source':str(src),'train':'çift tohumlar','heldout':'tek tohumlar','trainGames':len(train),'heldoutGames':len(test),'AutoTacticalValue':best[:10],'TriangleStrength':best[10],'trainBrier':loss(best,train),'heldoutBrier':loss(best,test),'baselineTrainBrier':loss(baseline+[data['Balance']['TriangleStrength']],train),'baselineHeldoutBrier':loss(baseline+[data['Balance']['TriangleStrength']],test),'heldoutBuckets':[]}
for k in range(10):
 bucket=[(predict(int(g['PlayerSquad']),int(g['AynaSquad']),best),g['Score']) for g in test if min(9,int(predict(int(g['PlayerSquad']),int(g['AynaSquad']),best)*10))==k]
 if bucket:output['heldoutBuckets'].append({'bucket':k,'n':len(bucket),'prediction':sum(x for x,y in bucket)/len(bucket),'actual':sum(y for x,y in bucket)/len(bucket)})
Path(sys.argv[2]).write_text(json.dumps(output,ensure_ascii=False,indent=2));print(json.dumps(output,ensure_ascii=False,indent=2))
