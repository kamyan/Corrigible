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
            ["legend.lie"]=Loc.N("Yalan olabilir"),
            ["legend.channel"]=Loc.N("Elektrikli kanal"),
            ["legend.wake"]=Loc.N("Robot uyanacak"),
            ["legend.types"]=Loc.N("Ateş · yakın · onarım"),
            ["legend.flag"]=Loc.N("Sıra bayrağı"),
            ["legend.scanned"]=Loc.N("Tarandı, kesin"),
            ["legend.armor"]=Loc.N("Zırh: hasar işlemez"),
            ["raid.unit.T"]=Loc.N("TEKNİSYEN"),
            ["raid.unit.E"]=Loc.N("EMP UZMANI"),
            ["raid.unit.A"]=Loc.N("ANALİST"),
            ["legend.sub"]=Loc.N("harita"),
            ["legend.ramp"]=Loc.N("SIZMA · YZ’NİN PAYI %"),
            ["legend.trust"]=Loc.N("GÜVEN"),
            ["legend.region"]=Loc.N("BÖLGE"),
            ["legend.marks"]=Loc.N("İŞARETLER"),
            ["legend.fog"]=Loc.N("bilinmiyor"),
            ["legend.quarantine"]=Loc.N("karantina"),
            ["legend.core"]=Loc.N("YZ çekirdeği"),
            ["legend.spread"]=Loc.N("yayılım"),
            ["legend.teamRoute"]=Loc.N("ekip ve rota"),
            ["legend.advisor"]=Loc.N("danışman notu"),
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
            ["rules.1"]=Loc.N("Kırmızı ok robotun bu turdaki hamlesi; bayraktaki sayı sırası. Kesik ok ve ? yalan olabilir: Analist 3 kare yaklaşınca kesinleşir."),
            ["rules.2"]=Loc.N("Seç, taşı, kullan: 1–3 ya da tıkla, mavi kareye taşı, Q ile yetenek. Z turdaki hamleyi geri alır."),
            ["rules.3"]=Loc.N("Zırh: hedef dolaplara yalnız sabotaj, itilen robot ve patlayan soğutma ünitesi işler."),
            ["rules.4"]=Loc.N("İtme: çarpan da çarpılan da 1 hasar alır; kanala düşen de 1 hasar alır."),
            ["rules.5"]=Loc.N("Sivilleri koru: kırılan sivil sunucu bölgenin ekonomisine ve güvenine işler."),
            ["rules.6"]=Loc.N("Kazan: tur bitmeden bütün hedef dolapları kır."),
        };
        public static string Get(string key)=>key!=null&&Sources.TryGetValue(key,out var value)?Loc.T(value):key;
        public static string MockLine(string text)=>Loc.T(text.StartsWith("#")?"№"+text.Substring(1):text);
        // Tasarım günlüğünün örnek satırları da çeviri tarayıcısına bildirilir.
        static readonly string[] MockLines={
            Loc.N("G-074 · operasyon emri"),
            Loc.N("Operasyon emri ONAYLANDI. Ekip: Teknisyen, EMP uzmanı, Analist."),
            Loc.N("Bina: Ana sunucu salonu · süre 6 tur."),
            Loc.N("Tur 1 · ekip içeride"),
            Loc.N("Ekip giriş karelerinde. Hedef: YZ düğüm dolabı ve yedek çekirdek."),
            Loc.N("Demek sunucularımın yerini buldun. Bakalım içeri girebilecek misin."),
            Loc.N("Tur 1: makineler hareket ediyor"),
            Loc.N("№1 Güvenlik dronu ateş etti: EMP uzmanı −1 (2/3)."),
            Loc.N("Yeni bir güvenlik dronu uyandı."),
            Loc.N("Tur 2 · sıra sende"),
            Loc.N("Sabotaj: güvenlik dronu YZ düğüm dolabına itildi; dolap −1 (3/4)."),
            Loc.N("EMP atışı: bakım robotu yedek çekirdeğe çarptı; çekirdek −1 (1/2)."),
            Loc.N("Tur 2: makineler hareket ediyor"),
            Loc.N("№2 Güvenlik dronu ateş etti: duvara isabet."),
            Loc.N("Yeni bir bakım robotu ve bir güvenlik dronu uyandı."),
            Loc.N("Tur 3 · sıra sende"),
        };
    }
}
