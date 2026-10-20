"""Yeni paket kurmaz. Önce Unity derlemesini tamamla; kökten çalıştır.

Örnek:
 python3 Tools/Ayna/prepare_learning_mono.py --directory Temp/AynaMonoBench
 mono Temp/AynaMonoBench/Runner.exe Habit 100 Docs/AYNA/denge/F3/mono_final 2
Son argüman 0=Kapalı/2=Tam; isteğe bağlı bir sonraki argüman seri ofsetidir.
Oyun sonuçları için saf DLL kullanılır; Unity hız ölçümünün yerine geçmez.
"""
from pathlib import Path
import shutil,subprocess,hashlib,json,argparse
parser=argparse.ArgumentParser(description="Unity’de derlenmiş saf AYNA DLL’leriyle, aynı seri adımlarından bağımsız Mono sürücüsü hazırlar.")
parser.add_argument("--directory",required=True,help="Yeni ve boş çalışma dizini")
args=parser.parse_args()
root=Path(args.directory);root.mkdir(parents=True,exist_ok=False)
source=Path('Assets/Corrigible/Ayna/Editor/AynaLearningBench.cs').read_text()
head=source[:source.index('    public static class AynaLearningBench')].replace('using UnityEditor;','')
steps=source[source.index('        static IEnumerable<int> Steps('):source.rfind('\n    }')]
body='''    public static class AynaLearningBench {
        static string output;
        public static void Run(LearningBenchOptions options) {
            options.Balance.TacticalPlanning = options.Tactical;
            foreach(var count in Steps(options)) if(count % 5 == 0) Console.WriteLine(count);
            Console.WriteLine(output);
        }
'''+steps+'\n    }\n'
stub='''    public static class AynaSelfPlay {
        public static void AssertEditorSafe() {}
        public static IPlanBot Bot(string id,uint seed) {
            switch(id) {
                case "Habit": return new HabitBot();
                case "Random": return new RandomBot(seed);
                case "Surprise": return new SurpriseBot();
                case "Greedy": return new GreedyBot();
                default: throw new ArgumentException(id);
            }
        }
    }
    public static class Entry {
        public static void Main(string[] args) {
            AynaLearningBench.Run(new LearningBenchOptions {Bot=args[0],Series=int.Parse(args[1]),Directory=args[2],Tactical=true,Learning=(LearningLevel)int.Parse(args[3]),SeriesOffset=args.Length>4?int.Parse(args[4]):0,Balance=args.Length>5?Newtonsoft.Json.JsonConvert.DeserializeObject<AynaBalance>(System.IO.File.ReadAllText(args[5])):new AynaBalance()});
        }
    }
}
'''
(root/'Runner.cs').write_text(head+body+stub)
refs=[]
for name in ['Corrigible.Core','Corrigible.Ayna.Core','Corrigible.Ayna.Mind']:
 p=Path('Library/ScriptAssemblies')/(name+'.dll');shutil.copy2(p,root/p.name);refs.append(root/p.name)
jsondll=next(Path('Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'));shutil.copy2(jsondll,root/jsondll.name);refs.append(root/jsondll.name)
refs.append(Path('/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.7.2-api/Facades/netstandard.dll'))
subprocess.run(['csc','-nologo','-langversion:9','-optimize+','-out:'+str(root/'Runner.exe'),*[('-r:'+str(p)) for p in refs],str(root/'Runner.cs')],check=True)
proof={'purpose':'Mevcut saf Core/Mind DLL ile Editor Steps yönteminin değişmeden bağımsız çalışma zamanı tanısı. Kabulün Unity süre ölçümünün yerine geçmez. Yeni paket kurulmadı.','steps_sha256':hashlib.sha256(steps.encode()).hexdigest(),'dlls':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in refs},'runtime':'Mono 6.12.0.182 amd64'}
(root/'driver.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2))
print(str(root/'Runner.exe'))
