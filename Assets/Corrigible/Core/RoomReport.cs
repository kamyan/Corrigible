using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis baskınında biten bir odanın raporu (Docs/TESIS-BASKINLARI-TASARIMI.md §11.2): oda zincirinin tek doğruluk kaynağı.
    /// Sonraki oda (taşınanlar, hazırlık, dönüşte donmuş oda), baskının sonucu ve kayıttan devam yalnız raporlardan kurulur.
    /// Kayda <see cref="Encode"/> ile yazılır (raid_room). Biçim: alanlar '|', liste öğeleri ',', öğe parçaları ':' ile ayrılır;
    /// yeni alan yalnız sona, kişi satırına yeni parça yalnız sona eklenir (çözücü eksik kuyruğu varsayılanla doldurur).
    /// </summary>
    [Serializable]
    public sealed class RoomReport
    {
        /// <summary>Oda sırası: 1 giriş (G), 2 ara oda (A), 3 hedef oda (H).</summary>
        public int Room;
        /// <summary>Dönüş ziyareti (kaçışta batıya geri geçilen oda).</summary>
        public bool Return;
        /// <summary>Odada oynanan tur (Math.Min(Turn, MaxTurns)).</summary>
        public int Turns;
        /// <summary>Tahtanın bitişi: "pass" (geçitten geçildi, zincir sürer), "won" ya da "lost".</summary>
        public string End = "lost";
        /// <summary>Baskın bu odada bitti (sonuç bu rapordan sonra yazılır).</summary>
        public bool Last;
        /// <summary>Baskın kazanıldı (Last iken anlamlı).</summary>
        public bool Success;
        /// <summary>Geri çekilme ya da giriş/ara odada süre sonu (tahtanın Retreated'i).</summary>
        public bool Retreat;
        /// <summary>İkmal yükü: −1 alınmadı (ya da sunucu görevi), −2 kayboldu, k ≥ 0 taşıyanın kadro sırası.</summary>
        public int Cargo = -1;
        /// <summary>Bu ziyarette kırılan sivil sunucu.</summary>
        public int Collateral;
        /// <summary>Odadan çıkışta ayaktaki sivil sunucu.</summary>
        public int Civilians;
        /// <summary>Odanın donanımı kırıksa türü ("pano", "anten", "vana"), değilse boş.</summary>
        public string Device = "";
        /// <summary>Bütün kadro, kadro sırasıyla.</summary>
        public List<RoomMember> Team = new List<RoomMember>();
        /// <summary>Ayaktaki makineler (tahtanın birim sırası; asansör kabinindekiler dahil).</summary>
        public List<RoomMachine> Machines = new List<RoomMachine>();
        /// <summary>Odadaki bütün nesneler (y, x sırası); olay nesneleri (tüp, kaynak kolu, enkaz) hariç.</summary>
        public List<RoomObject> Objects = new List<RoomObject>();
        /// <summary>Oyuncunun yıktığı duvarlar, y*8+x (tahtanın Rubble sırası).</summary>
        public List<int> Rubble = new List<int>();
        /// <summary>Tesis hamleleri (aşama 6): odada yerde kalan yükün karesi (y*8+x; −1 yok), bu ziyarette kalkan kepenkler (y*8+x,
        /// tahtanın y, x sırası; dönüş tahtası onları açık kurar), bu ziyarette büyük sandık açıldı mı.</summary>
        public int CargoAt = -1;
        public List<int> Opened = new List<int>();
        public bool Crate;
        /// <summary>Tesis aşama 8: uzman olmayan ekip birimleri odadan çıkışta (köpekler sahibin kadro sırasıyla, sonra tutsaklar, sonra
        /// odada ayakta kalan aygıtlar); köpeğin kokladığı oda sırası (1..3; 0 yok); yükü köpek taşıyor (<see cref="Cargo"/> sahibin
        /// kadro sırasıdır, K33).</summary>
        public List<RoomUnit> Units = new List<RoomUnit>();
        public int Sniff;
        public bool CargoDog;
        /// <summary>Tesis aşama 10: bu ziyaretin sınıf sayaçları (<see cref="FacilityCounters.Classes"/> sırası; tahtanın Tally alanları).
        /// Çözülen raporda hep altı öğe (eksik kuyruk altı sıfır); dönüş tahtası sıfırdan sayar (çift sayım yok).</summary>
        public List<int> Tally = new List<int>();
        /// <summary>Tesis aşama 12 (Katar): geçişte sonraki vagona geçen makinelerin <see cref="Machines"/> içindeki dizinleri
        /// (<c>Baskin.FollowerIds</c>; dönüş geçişinde, AYIR'sız) ve oda AYIR'la geçildi mi. Sonraki oda takipçilerle kurulur ve temiz olsa
        /// da oynanır (<c>RaidChain.Next</c>).</summary>
        public List<int> Follow = new List<int>();
        public bool Decouple;

        /// <summary>Sınıf sırasındaki sayaç (eksik öğe 0).</summary>
        public int TallyAt(int i) => Tally != null && i >= 0 && i < Tally.Count ? Tally[i] : 0;

        static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
        static string B(bool v) => v ? "1" : "0";
        static int P(string s) => int.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        static List<T> Many<T>(string s, Func<string[], T> one) =>
            string.IsNullOrEmpty(s) ? new List<T>() : s.Split(',').Select(e => one(e.Split(':'))).ToList();

        /// <summary>
        /// Kayıt dizgisi: <c>Room|Return|Turns|End|Last|Success|Retreat|Cargo|Collateral|Civilians|Device|Team|Machines|Objects|Rubble</c>
        /// ve aşama 6'nın kuyruğu <c>|CargoAt|Opened|Crate</c>, aşama 8'in <c>|Units|Sniff|CargoDog</c>, aşama 10'un <c>|Tally</c>
        /// ("a:b:c:d:e:f"), aşama 12'nin <c>|Follow|Decouple</c> ("0,2" ve 0/1): sondaki varsayılanlar kırpılır (hepsi varsayılandaysa eski
        /// 15 alan). Makine: <c>Type:X:Y:Hp:Armor</c>, sökülmüşse
        /// <c>:1</c>. Kişi: <c>State:Hp:Armor:Emp:Charges:Fell:X:Y</c>, sonra <c>:Body:Picked:Gear</c> son
        /// varsayılan dışı parçaya kadar (Body ve Picked 0, Gear −1). Birim: <c>Class:Type:Owner:State:Hp:Armor:X:Y:Dir:Turns</c>. Eski,
        /// gövdesiz ve ekipmansız raporların dizgisi değişmez.
        /// </summary>
        public string Encode()
        {
            var f = new List<string>
            {
                I(Room), B(Return), I(Turns), End ?? "", B(Last), B(Success), B(Retreat), I(Cargo), I(Collateral), I(Civilians), Device ?? "",
                string.Join(",", (Team ?? new List<RoomMember>()).Select(m => string.Join(":", m.State ?? "", I(m.Hp), I(m.Armor), I(m.Emp), I(m.Charges), I(m.Fell), I(m.X), I(m.Y))
                    + ((m.Work != FacilityTitle.None || m.Dragged != 0) ? ":" + I(m.Body) + ":" + I(m.Picked) + ":" + I(m.Gear) + ":" + I((int)m.Work) + ":" + I(m.Dragged)
                        : m.Gear != -1 ? ":" + I(m.Body) + ":" + I(m.Picked) + ":" + I(m.Gear) : m.Body != 0 || m.Picked != 0 ? ":" + I(m.Body) + ":" + I(m.Picked) : ""))),
                string.Join(",", (Machines ?? new List<RoomMachine>()).Select(m => string.Join(":", m.Type ?? "", I(m.X), I(m.Y), I(m.Hp), I(m.Armor)) + (m.Assembled ? (m.Unbolted ? ":1:1" : ":0:1") : m.Unbolted ? ":1" : ""))),
                string.Join(",", (Objects ?? new List<RoomObject>()).Select(o => string.Join(":", o.K ?? "", I(o.X), I(o.Y), I(o.Hp), I(o.Max)) + (o.MachineBuilt ? ":1" : ""))),
                string.Join(",", (Rubble ?? new List<int>()).Select(I)),
            };
            var opened = Opened ?? new List<int>();
            var units = Units ?? new List<RoomUnit>();
            bool tally = Enumerable.Range(0, FacilityCounters.Classes.Length).Any(i => TallyAt(i) != 0);
            var follow = Follow ?? new List<int>();
            int tail = Decouple ? 9 : follow.Count > 0 ? 8 : tally ? 7 : CargoDog ? 6 : Sniff != 0 ? 5 : units.Count > 0 ? 4 : Crate ? 3 : opened.Count > 0 ? 2 : CargoAt != -1 ? 1 : 0;
            if (tail >= 1) f.Add(I(CargoAt));
            if (tail >= 2) f.Add(string.Join(",", opened.Select(I)));
            if (tail >= 3) f.Add(B(Crate));
            if (tail >= 4) f.Add(string.Join(",", units.Select(u => string.Join(":", u.Class ?? "", u.Type ?? "", I(u.Owner), u.State ?? "", I(u.Hp), I(u.Armor), I(u.X), I(u.Y), I(u.Dir), I(u.Turns))
                + (u.Assembled ? ":1" : ""))));
            if (tail >= 5) f.Add(I(Sniff));
            if (tail >= 6) f.Add(B(CargoDog));
            if (tail >= 7) f.Add(string.Join(":", Enumerable.Range(0, FacilityCounters.Classes.Length).Select(i => I(TallyAt(i)))));
            if (tail >= 8) f.Add(string.Join(",", follow.Select(I)));
            if (tail >= 9) f.Add(B(Decouple));
            return string.Join("|", f);
        }

        /// <summary>Sayaç alanı ("a:b:c:d:e:f"); eksik ya da kısa alan sıfırla tamamlanır (hep altı öğe).</summary>
        static List<int> Tallies(string s)
        {
            var parts = string.IsNullOrEmpty(s) ? new string[0] : s.Split(':');
            return Enumerable.Range(0, FacilityCounters.Classes.Length).Select(i => i < parts.Length && parts[i].Length > 0 ? P(parts[i]) : 0).ToList();
        }

        /// <summary>Kayıt dizgisinden rapor; null ya da boş dizgide null. Eksik kuyruk alanları ve kişi parçaları varsayılan değerini alır.</summary>
        public static RoomReport Decode(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var p = s.Split('|');
            string F(int i) => p.Length > i ? p[i] : null;
            int N(int i, int def) => string.IsNullOrEmpty(F(i)) ? def : P(F(i));
            return new RoomReport
            {
                Room = N(0, 0), Return = F(1) == "1", Turns = N(2, 0), End = F(3) ?? "lost", Last = F(4) == "1", Success = F(5) == "1",
                Retreat = F(6) == "1", Cargo = N(7, -1), Collateral = N(8, 0), Civilians = N(9, 0), Device = F(10) ?? "",
                Team = Many(F(11), q =>
                {
                    int Q(int i, int def) => q.Length > i && q[i].Length > 0 ? P(q[i]) : def;
                    return new RoomMember
                    {
                        State = q.Length > 0 && q[0].Length > 0 ? q[0] : "a", Hp = Q(1, 0), Armor = Q(2, 0), Emp = Q(3, 0), Charges = Q(4, -1),
                        Fell = Q(5, 0), X = Q(6, -1), Y = Q(7, -1), Body = Q(8, 0), Picked = Q(9, 0), Gear = Q(10, -1),
                        Work = (FacilityTitle)Q(11, 0), Dragged = Q(12, 0),
                    };
                }),
                Machines = Many(F(12), q => new RoomMachine { Type = q[0], X = P(q[1]), Y = P(q[2]), Hp = P(q[3]), Armor = q.Length > 4 ? P(q[4]) : 0, Unbolted = q.Length > 5 && q[5] == "1", Assembled = q.Length > 6 && q[6] == "1" }),
                Objects = Many(F(13), q => new RoomObject { K = q[0], X = P(q[1]), Y = P(q[2]), Hp = P(q[3]), Max = q.Length > 4 ? P(q[4]) : P(q[3]), MachineBuilt = q.Length > 5 && q[5] == "1" }),
                Rubble = string.IsNullOrEmpty(F(14)) ? new List<int>() : F(14).Split(',').Select(P).ToList(),
                CargoAt = N(15, -1),
                Opened = string.IsNullOrEmpty(F(16)) ? new List<int>() : F(16).Split(',').Select(P).ToList(),
                Crate = F(17) == "1",
                Units = Many(F(18), q =>
                {
                    int Q(int i, int def) => q.Length > i && q[i].Length > 0 ? P(q[i]) : def;
                    return new RoomUnit
                    {
                        Class = q[0], Type = q.Length > 1 ? q[1] : "", Owner = Q(2, -1), State = q.Length > 3 && q[3].Length > 0 ? q[3] : "a",
                        Hp = Q(4, 0), Armor = Q(5, 0), X = Q(6, -1), Y = Q(7, -1), Dir = Q(8, -1), Turns = Q(9, 0),
                        Assembled = Q(10, 0) == 1,
                    };
                }),
                Sniff = N(19, 0),
                CargoDog = F(20) == "1",
                Tally = Tallies(F(21)),
                Follow = string.IsNullOrEmpty(F(22)) ? new List<int>() : F(22).Split(',').Select(P).ToList(),
                Decouple = F(23) == "1",
            };
        }
    }

    /// <summary>Oda raporunda kadronun bir kişisi (kadro sırasıyla).</summary>
    [Serializable]
    public sealed class RoomMember
    {
        /// <summary>"a" ayakta (kapıda, tahliyede ya da baskın bitince tahtada), "w" giriş ya da ara odada süre dolarken kapıya varamadı, ekip onsuz geçti (sağ geri çekildi), "d" düştü, "l" geride kaldı, "k" Katar'da açık kenardan düştü
        /// (aşama 12: can 0, Fell oda, X/Y son kare, Body 0; sonraki odalara konmaz, ölmez, yatar).</summary>
        public string State = "a";
        /// <summary>t: sızmacı tesiste kaldı; x: döndükten sonra etkisizleştirildi.</summary>
        public bool FieldTurned => State == "t" || State == "x";
        /// <summary>Can, zırh plakası, kalan EMP hücresi, sınırlı yetenek hakkı (−1 sınırsız).</summary>
        public int Hp, Armor, Emp, Charges = -1;
        /// <summary>Düştüğü ya da geride kaldığı oda (1..3; ekiple taşınıp başka odada bırakılan gövdenin yattığı oda); ayaktaysa 0.</summary>
        public int Fell;
        /// <summary>Son karesi (düşen ve geride kalan için yattığı kare).</summary>
        public int X = -1, Y = -1;
        /// <summary>Düşenin gövdesi (aşama 6): 0 yerde (<see cref="Fell"/> odasında X, Y'de yatar), 1 ekiple (geçitten sürüklendi ya da
        /// atlanan temiz odadan toplandı; sonraki odaya konur), 2 tesisten çıkarıldı (yaşar). Ayaktaki ve geride kalan için 0.</summary>
        public int Body;
        /// <summary>1: gövde dönüşte atlanan temiz odadan en az bir kez toplandı (ölçüm ve telsiz için).</summary>
        public int Picked;
        /// <summary>Tesis aşama 8: taşıdığı ekipmanın kalan baskın hakkı (<see cref="Baskin.Unit.GearLeft"/>); ekipmansızda ve köpekte −1.</summary>
        public int Gear = -1;
        public FacilityTitle Work;
        public int Dragged;
    }

    /// <summary>
    /// Oda raporunda uzman olmayan bir ekip birimi (tesis aşama 8): köpek (sahibiyle odalar arasında taşınır; düşen köpek "d" olarak
    /// baskın sonuna kadar), tutsak (ekiple gider), aygıt (odada kalır, dönüş tahtası onu kendi karesine kurar).
    /// </summary>
    [Serializable]
    public sealed class RoomUnit
    {
        /// <summary>Sınıf (<see cref="Baskin.ClassDog"/>, <see cref="Baskin.ClassGadget"/>, <see cref="Baskin.ClassCaptive"/>) ve birim türü.</summary>
        public string Class, Type;
        /// <summary>Sahibin (köpek), kuranın (aygıt) ya da kelepçeleyenin (tutsak) kadro sırası.</summary>
        public int Owner = -1;
        /// <summary>"a" ayakta, "d" düştü (yalnız köpek).</summary>
        public string State = "a";
        public int Hp, Armor;
        public int X = -1, Y = -1;
        /// <summary>Aygıtın yönü (<see cref="Baskin.Dirs"/> dizini; −1 yok) ve kalan turu (aygıtta çalışma, tutsakta oyuncu turu).</summary>
        public int Dir = -1, Turns;
        public bool Assembled;
    }

    /// <summary>Oda raporunda ayaktaki bir makine.</summary>
    [Serializable]
    public sealed class RoomMachine
    {
        public string Type;
        public int X, Y, Hp, Armor;
        /// <summary>Tesis aşama 10: cıvatası söküldü (<see cref="Baskin.Unit.Unbolted"/>); dönüş tahtası sökük kurar.</summary>
        public bool Unbolted;
        public bool Assembled;
    }

    /// <summary>Oda raporunda bir nesne (kimlik, kare, can, azami can).</summary>
    [Serializable]
    public sealed class RoomObject
    {
        public string K;
        public int X, Y, Hp, Max;
        public bool MachineBuilt;
    }
}
