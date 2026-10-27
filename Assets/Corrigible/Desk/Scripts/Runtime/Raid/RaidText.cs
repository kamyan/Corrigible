using System.Collections.Generic;
using Corrigible.Core;
namespace Corrigible.Desk
{
    public static class RaidText
    {
        static readonly Dictionary<string,string> Sources=new Dictionary<string,string>
        {
            ["legend.title"]=Loc.N("LEJANT"),
            ["legend.move"]=Loc.N("Hareket"),
            ["legend.target"]=Loc.N("Yetenek hedefi"),
            ["legend.hit"]=Loc.N("Robot bu kareyi vuracak"),
            ["legend.channel"]=Loc.N("Elektrikli kanal"),
            ["legend.wake"]=Loc.N("Robot uyanacak"),
            ["legend.types"]=Loc.N("Ateş · yakın · onarım"),
            ["legend.flag"]=Loc.N("Sıra bayrağı"),
            ["legend.armor"]=Loc.N("Zırh: hasar işlemez"),
            ["raid.unit.T"]=Loc.N("TEKNİSYEN"),
            ["raid.unit.E"]=Loc.N("EMP UZMANI"),
            ["raid.unit.A"]=Loc.N("ANALİST"),
            ["raid.event.fireNorth"]=Loc.N("{robot} kuzeye ateş ediyor"),
            ["raid.retreat.title"]=Loc.N("GERİ ÇEKİLME · 2. ONAY"),
            ["raid.retreat.body"]=Loc.N("Ekip binadan çıkar; operasyon başarısız sayılır. Hedef dolaplar ayakta kalır."),
            ["raid.retreat.foot"]=Loc.N("İkinci basış geri çeker."),
            ["raid.result.targetBroken"]=Loc.N("HEDEF KIRILDI"),
            ["rules.title"]=Loc.N("KURALLAR KARTI"),
            ["rules.sub.open"]=Loc.N("operasyon emrinden"),
            ["rules.sub.folded"]=Loc.N("oklar · seçim · zırh · siviller"),
            ["rules.open"]=Loc.N("Aç"),
            ["rules.fold"]=Loc.N("Katla"),
            ["rules.1"]=Loc.N("Kırmızı ok robotun bu turdaki hamlesi; bayraktaki sayı sırası."),
            ["rules.2"]=Loc.N("Seç, taşı, kullan: 1–3 ya da tıkla, mavi kareye taşı, Q ile yetenek. Z turdaki hamleyi geri alır."),
            ["rules.3"]=Loc.N("Zırh: hedef dolaplara yalnız sabotaj, itilen robot ve patlayan soğutma ünitesi işler."),
            ["rules.4"]=Loc.N("İtme: çarpan da çarpılan da 1 hasar alır; kanala düşen de 1 hasar alır."),
            ["rules.5"]=Loc.N("Sivilleri koru: makineler sivile nişan almaz, ama atıştan kaçarsan arkadaki sunucu vurulur. Kırılan her sunucu GÜVEN'i düşürür."),
            ["rules.6"]=Loc.N("Kazan: tur bitmeden bütün hedef dolapları kır. Alınan hasar sığınağa yara olarak döner; zırh plakası ilk darbeyi karşılar."),
        };
        public static string Get(string key)=>key!=null&&Sources.TryGetValue(key,out var value)?Loc.T(value):key;
    }
}
