"""Unity'de derlenmiş saf DLL'lerle F5 ölçümünü çalıştıran ayrı sürücüyü hazırlar.
Kökten: python3 Tools/Ayna/prepare_balance_mono.py --directory Temp/AynaF5
Çalıştır: mono Temp/AynaF5/Runner.exe seçenekler.json
"""
from pathlib import Path
import argparse,shutil,subprocess,json,hashlib
p=argparse.ArgumentParser();p.add_argument('--directory',required=True);args=p.parse_args()
root=Path(args.directory);root.mkdir(parents=True,exist_ok=False)
source=Path('Assets/Corrigible/Ayna/Editor/AynaBalanceBench.cs');shutil.copy2(source,root/source.name)
stub='''using System;using System.IO;using Corrigible.Ayna.Core;using Corrigible.Ayna.Mind.Bots;
namespace UnityEditor { public static class EditorApplication { public static event Action update; } }
namespace Corrigible.Ayna.EditorTools {
public static class AynaSelfPlay {
 public static void AssertEditorSafe() {}
 public static IPlanBot Bot(string id,uint seed) { switch(id) {
 case "Mind": return new MindBot(seed);case "Habit":return new HabitBot();case "Random":return new RandomBot(seed);
 case "Greedy":return new GreedyBot();case "Aggro":return new AggroBot();case "Stealth":return new StealthBot();case "Surprise":return new SurpriseBot();default:throw new ArgumentException(id); } }
}
public static class Entry { public static void Main(string[] args) {
 if(args[0]=="--defaults"){Console.WriteLine(AynaJson.Write(new AynaBalance()));return;}
 var o=Newtonsoft.Json.JsonConvert.DeserializeObject<BalanceBenchOptions>(File.ReadAllText(args[0]));o.Runtime="Mono 6.12 / Unity Core-Mind DLL";Console.WriteLine(AynaBalanceBench.Run(o));
} } }
'''
(root/'Runner.cs').write_text(stub);refs=[]
for name in ['Corrigible.Core','Corrigible.Ayna.Core','Corrigible.Ayna.Mind']:
 f=Path('Library/ScriptAssemblies')/(name+'.dll');shutil.copy2(f,root/f.name);refs.append(root/f.name)
f=next(Path('Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'));shutil.copy2(f,root/f.name);refs.append(root/f.name)
refs.append(Path('/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.7.2-api/Facades/netstandard.dll'))
subprocess.run(['csc','-nologo','-langversion:9','-nowarn:67','-optimize+','-out:'+str(root/'Runner.exe'),*[('-r:'+str(f)) for f in refs],str(root/'Runner.cs'),str(root/source.name)],check=True)
(root/'driver.json').write_text(json.dumps({'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'DLL':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in refs},'note':'Ana Steps/Run değiştirilmedi; Unity callback/Assert ve Bot fabrikası için sürücü kabuğu vardır. Oyun kuralı saf Unity DLL’lerinden gelir; Mono süreleri Unity diye yorumlanmaz.'},ensure_ascii=False,indent=2))
print(root/'Runner.exe')
