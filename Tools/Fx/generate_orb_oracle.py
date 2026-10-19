#!/usr/bin/env python3
"""Kanonik JS'yi değiştirmeden, geçici ölçüm kancasıyla sayısal test verisini üretir."""
from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2];src=(root/'Docs/UI_Fx/SonSalter_Fx_Paketi/art/orb/fx_orb.js').read_text();sourcehash=hashlib.sha256(src.encode()).hexdigest()
patched=src.replace('  var orbs = [];','  globalThis.__fx={rng:rng,ICO:ICO,build:build,draw:draw,sweep:sweep,ss:ss};\n  var orbs = [];')
setup='''var window={devicePixelRatio:2};
function context(){return {globalAlpha:1,setTransform:function(){},clearRect:function(){},fillRect:function(){},beginPath:function(){},arc:function(){},fill:function(){},stroke:function(){},moveTo:function(){},lineTo:function(){},closePath:function(){},setLineDash:function(){},drawImage:function(){},createRadialGradient:function(){return {addColorStop:function(){}}},createLinearGradient:function(){return {addColorStop:function(){}}}};}
var document={createElement:function(){return {getContext:context};}};
function cv(level,kind,seed){return {dataset:{r:48,level:level,kind:kind,seed:seed},clientWidth:211,width:422,getContext:context};}
'''
run='''fxOrbs({querySelectorAll:function(){return [];}},{manual:true});
var x=__fx, samples={};[3,5,7,9].forEach(function(seed){var r=x.rng(seed*9973+17),a=[];for(var i=0;i<10;i++)a.push(r());samples[seed]=a;});
var initial=x.build(cv(60,'normal',3));var built={shell:initial.P,bands:initial.B,blocks:initial.K,jag:initial.jag,faces:initial.kap,edges:initial.ekap,shards:initial.SH,next:initial.rnd()};
var cases=[];[40,100].forEach(function(steps){[[0,'normal',3],[15,'normal',3],[30,'normal',3],[45,'normal',3],[60,'normal',3],[75,'normal',3],[90,'normal',3],[100,'normal',3],[0,'unknown',5],[46,'quar',7],[71,'core',9]].forEach(function(c){var canvas=cv(c[0],c[1],c[2]),o=x.build(canvas),t=0;for(var i=0;i<steps;i++){t+=.025;x.draw(o,t,.025);}cases.push({level:c[0],kind:c[1],seed:c[2],steps:steps,time:t,angle:o.ang,displayed:o.lv,nextFil:o.nextFil,acc:o.spAcc||0,fil:o.fil,arcs:o.arcs,sparks:o.sparks,next:o.rnd()});});});
var sweep=[0,1,2,3,5,8,9,10,13,16,17,18,19].map(function(t){return [t,x.sweep(t)];});var weights=[0,.3,.5,.7,1].map(function(s){var a=1-x.ss(.18,.5,s),c=x.ss(.55,.86,s);return [s,a,Math.max(0,1-a-c),c,x.ss(.24,.8,s)];});
print(JSON.stringify({sourceHash:SOURCEHASH,rng:samples,ico:x.ICO,build:built,cases:cases,sweep:sweep,weights:weights}));
'''.replace('SOURCEHASH',json.dumps(sourcehash))
import subprocess, tempfile, os
jsc=os.environ.get('JSC','/System/Library/Frameworks/JavaScriptCore.framework/Versions/Current/Helpers/jsc')
with tempfile.TemporaryDirectory(prefix='corrigible-orb-oracle-') as temp:
    script=Path(temp)/'oracle.js';script.write_text(setup+patched+run)
    result=subprocess.check_output([jsc,str(script)],text=True)
    oracle=json.loads(result)
    destination=root/'Assets/Corrigible/Fx/Data/OrbParity.json'
    destination.write_text(json.dumps(oracle,ensure_ascii=False,separators=(',',':'))+'\n')
    print(str(destination)+': '+str(len(oracle['cases']))+' kare durumu')
