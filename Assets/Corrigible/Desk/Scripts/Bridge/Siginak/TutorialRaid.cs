using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Eğitim baskınının bir arayüz adımı: uzman seç, yürü, yeteneği seç (Q), hedefe tıkla ya da turu bitir.</summary>
    public sealed class RaidGuideStep
    {
        public const string Select = "select", Move = "move", Arm = "arm", Use = "use", End = "end";
        public string Kind;
        public char Letter;
        public int X, Y;
        /// <summary>Hamlenin dersi (talimatın altına yazılır); yoksa null.</summary>
        public string Lesson;
    }

    /// <summary>
    /// İlk baskının eğitim senaryosu: Batı Avrupa'nın ilk sektörü sabit bir tohumla kurulur ve oyuncu bilinen bir çözümü adım
    /// adım oynar (beş tur: EMP itişiyle zırh kırmak, kancayla çekmek, makinelerin sırasını okumak, atıştan kaçmak, kasayı
    /// almak, EMP ile dönüş yolunu açmak ve kaçış: bütün ekip aynı çıkıştan tek tek çıkar). Makineler temel akılla oynar
    /// (<c>RaidParams.Tactics</c> 0).
    /// Taktik motor aynı parametre ve hamlelerle aynı savaşı verir; simülasyon yalnız sonucu görür, kayıt bozulmaz.
    /// </summary>
    public static class TutorialRaid
    {
        public const int Seed = 129;

        /// <summary>Çözüm (uzman harfiyle): m = yürü, a = yetenek, e = turu bitir. <c>TutorialRaidTests</c> zafere vardığını kilitler.</summary>
        public static readonly string[] Moves =
        {
            "mT.1.4", "aE.5.3", "mA.3.4", "aA.1.4", "e",
            "mT.4.3", "mE.0.5", "aE.5.5", "e",
            "mT.6.4", "aT.6.3", "aE.3.5", "e",
            "mT.3.4", "e",
            // Kaçış: Teknisyen çıkınca (0,4) boşalır; EMP uzmanı ve Analist de oradan çıkar
            "mT.0.4", "mE.0.4", "mA.0.4",
        };

        /// <summary>Arayüz adımlarının dersleri (adım sırası: <see cref="Steps"/>).</summary>
        static readonly Dictionary<int, string> Lessons = new Dictionary<int, string>
        {
            [0] = Loc.N("Bu ilk baskını birlikte oynayalım; sıradaki hedef sarı çerçeveli. Kırmızı oklar makinelerin bu turdaki hamlesi, bayraktaki sayı sırası."),
            [1] = Loc.N("Mavi kareler Teknisyenin gidebileceği yerler."),
            [3] = Loc.N("EMP atışı 2–5 kare öteye, engellerin üstünden gider; hedefin çevresindeki robotları iter."),
            [4] = Loc.N("Zırhlı dronun üstündeki duvara at: dron aşağıdaki duvara itilir, çarpınca zırhı kırılır. Oklara bak: 2 numaralı bakım robotu ona vuracak."),
            [7] = Loc.N("Kanca düz hatta 5 kare gider: çarptığı robota 1 hasar verip onu bir kare çeker, dost uzmanı hasarsız çeker."),
            [8] = Loc.N("Teknisyene at: kasaya bir kare yaklaşır."),
            [9] = Loc.N("Makineler sırayla oynar: 1 numaralı dron bakım robotunu, 2 numaralı bakım robotu zırhı kırılan dronu vurur. Zırhlı dron ateş edemeden düşer."),
            [11] = Loc.N("Uzmanlar birbirinin üstünden geçebilir."),
            [15] = Loc.N("Dronun altındaki duvara at: dron yukarı itilir, duvara çarpıp 1 hasar alır."),
            [16] = Loc.N("Analist bakım robotunun hedefinde, ama 1 numaralı dron önce ateş edip bakım robotunu düşürür: sıra önemli. Analist bu tur boşta kalır."),
            [18] = Loc.N("Dron Teknisyenin sütununa ateş edecek: kasanın yanındaki kareye geç."),
            [20] = Loc.N("Kasaya tıkla: Teknisyen malzemeleri alır ve kaçış başlar. Bütün ekip yeşil kesik çerçeveli giriş karelerinden çıkmalı; süre dolarken tahtada kalan uzman baskında düşmüş sayılır."),
            [21] = Loc.N("Dron dönüş yolunda. Yolu EMP uzmanı açacak."),
            [23] = Loc.N("Analistin altındaki kareye at: hasarlı dron duvara itilip düşer, Analist yukarı itilir ve Teknisyen'e yol açılır. EMP dostları da iter."),
            [26] = Loc.N("Teknisyen kablo kanalının üstünden yürüyebilir; kanal yalnız üstüne itilene hasar verir."),
            [29] = Loc.N("Yeni dron Teknisyen'in sütununa nişan aldı: çıkışa yürü. Çıkışa varan uzman tahtadan çıkar; malzeme Teknisyen'le dışarıda."),
            [31] = Loc.N("EMP uzmanı da çıkmalı: Teknisyen'in boşalttığı kareye yürü."),
            [33] = Loc.N("Son kişi Analist: çıkışa yürü. Bütün ekip dışarıda, baskın kazanılır."),
        };

        /// <summary>Çözümün arayüz adımları: her hamle gerekirse uzmanı seçer; yetenek önce Q ile seçilir; tur bitince seçim kalkar.</summary>
        public static List<RaidGuideStep> Steps()
        {
            var list = new List<RaidGuideStep>();
            char selected = '\0';
            foreach (var mv in Moves)
            {
                if (mv == "e") { list.Add(new RaidGuideStep { Kind = RaidGuideStep.End }); selected = '\0'; continue; }
                char letter = mv[1];
                var xy = mv.Substring(3).Split('.');
                int x = int.Parse(xy[0]), y = int.Parse(xy[1]);
                if (selected != letter) { list.Add(new RaidGuideStep { Kind = RaidGuideStep.Select, Letter = letter }); selected = letter; }
                if (mv[0] == 'm') list.Add(new RaidGuideStep { Kind = RaidGuideStep.Move, Letter = letter, X = x, Y = y });
                else
                {
                    list.Add(new RaidGuideStep { Kind = RaidGuideStep.Arm, Letter = letter });
                    list.Add(new RaidGuideStep { Kind = RaidGuideStep.Use, Letter = letter, X = x, Y = y });
                }
            }
            for (int i = 0; i < list.Count; i++) if (Lessons.TryGetValue(i, out var lesson)) list[i].Lesson = lesson;
            return list;
        }

        /// <summary>
        /// Eğitim baskınının parametreleri: harekâtın ilk baskını (Batı Avrupa'nın ilk sektörü, kademe 1, tam ve sağlam kadro)
        /// senaryonun tohumuyla kurulur. Başka her baskın olduğu gibi kalır (null).
        /// </summary>
        public static RaidParams For(RaidParams campaign)
        {
            if (campaign == null || campaign.SectorId != "weu_1" || campaign.MissionKind != "loot" || campaign.Tier != 1) return null;
            if (!campaign.Traits.SequenceEqual(new[] { "guvenli" }) || campaign.Signature != "zdron") return null;
            var squad = campaign.Squad;
            // Senaryo Teknisyen, EMP uzmanı ve Analist'e göre yazıldı (giriş sırası dahil)
            bool fresh = squad != null && squad.Select(s => s.Type).SequenceEqual(new[] { "tek", "emp", "ana" })
                && squad.All(s => !s.Rookie && s.Armor == 0 && s.Hp == Baskin.UnitTypes[s.Type].Hp);
            if (!fresh) return null;
            return new RaidParams
            {
                Seed = Seed, Region = campaign.Region, RegionName = campaign.RegionName, Compute = campaign.Compute, RaidNumber = campaign.RaidNumber,
                Tier = campaign.Tier, MissionKind = campaign.MissionKind, SectorId = campaign.SectorId, IsCore = campaign.IsCore,
                Traits = new List<string>(campaign.Traits), Signature = campaign.Signature, Squad = campaign.Squad,
                ExtraTurns = campaign.ExtraTurns,
            };
        }

        /// <summary>Talimat: adımın türüne ve uzmana göre (ders ayrı).</summary>
        public static string Instruction(RaidGuideStep s) => s.Kind switch
        {
            RaidGuideStep.Select => s.Letter == 'T' ? Loc.T("Teknisyeni seç: 1'e bas ya da ona tıkla.")
                : s.Letter == 'E' ? Loc.T("EMP uzmanını seç: 2'ye bas ya da ona tıkla.") : Loc.T("Analisti seç: 3'e bas ya da ona tıkla."),
            RaidGuideStep.Move => Loc.T("Sarı çerçeveli kareye tıkla: oraya yürür."),
            RaidGuideStep.Arm => s.Letter == 'T' ? Loc.T("Q ile ikmal almayı seç.") : s.Letter == 'E' ? Loc.T("Q ile EMP atışını seç.") : Loc.T("Q ile kancayı seç."),
            RaidGuideStep.Use => Loc.T("Sarı çerçeveli kareye tıkla."),
            _ => Loc.T("Enter ile emri ilet ya da EMRİ İLET'e bas."),
        };
    }

    /// <summary>
    /// Eğitim baskınını yürüten rehber: yalnız sıradaki adıma izin verir, yapılınca bir sonrakine geçer. Beklenen hamle tahtada
    /// yapılamaz hâle gelirse (olmaması gerekir) rehber çekilir, baskın serbest oynanır.
    /// </summary>
    public sealed class RaidGuide
    {
        public readonly List<RaidGuideStep> Steps = TutorialRaid.Steps();
        public int Index { get; private set; }
        public bool Done => Index >= Steps.Count;
        public RaidGuideStep Current => Done ? null : Steps[Index];

        public bool Expects(string kind, char letter = '\0', int x = -1, int y = -1)
        {
            var s = Current;
            if (s == null || s.Kind != kind) return false;
            if ((kind == RaidGuideStep.Select || kind == RaidGuideStep.Arm || kind == RaidGuideStep.Move || kind == RaidGuideStep.Use) && s.Letter != letter) return false;
            return kind != RaidGuideStep.Move && kind != RaidGuideStep.Use || s.X == x && s.Y == y;
        }

        public void Advance() { if (!Done) Index++; }

        /// <summary>Sarı çerçevenin karesi: seçilecek uzman, yürünecek ya da hedeflenecek kare; turu bitirirken yok.</summary>
        public (int X, int Y)? Tile(Baskin.BState state)
        {
            var s = Current;
            if (s == null || s.Kind == RaidGuideStep.End) return null;
            if (s.Kind == RaidGuideStep.Move || s.Kind == RaidGuideStep.Use) return (s.X, s.Y);
            var u = state.Units.FirstOrDefault(v => v.Side == 'p' && Baskin.OnBoard(v) && Baskin.UnitTypes[v.Type].Short[0] == s.Letter);
            return u == null ? ((int, int)?)null : (u.X, u.Y);
        }

        /// <summary>Sıradaki adım tahtada yapılabilir mi (uzman ayakta, kare yürünür ya da hedeflenir).</summary>
        public bool Possible(Baskin.BState state, char selected)
        {
            var s = Current;
            if (s == null || s.Kind == RaidGuideStep.End) return true;
            var u = state.Units.FirstOrDefault(v => v.Side == 'p' && Baskin.OnBoard(v) && Baskin.UnitTypes[v.Type].Short[0] == s.Letter);
            if (u == null) return false;
            if (s.Kind == RaidGuideStep.Move) return Baskin.CanMove(state, u) && Baskin.Reachable(state, u).Any(p => p.X == s.X && p.Y == s.Y);
            if (s.Kind == RaidGuideStep.Use || s.Kind == RaidGuideStep.Arm)
                return Baskin.CanAct(state, u) && (s.Kind == RaidGuideStep.Arm || Baskin.Targets(state, u).Any(p => p.X == s.X && p.Y == s.Y));
            return true;
        }
    }
}
