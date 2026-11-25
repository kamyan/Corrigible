using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;
using static Corrigible.Desk.Tests.FacilityGearTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 10, parça D (aşama 10 tasarımı §9.5; K42–K44): önlemlerin baskın tahtasındaki sunumu, sahnesiz (veri kaynağı). Kademeler
    /// baskın emrine Core'dan kopyalanır (<c>RaidParams.Counters</c>, kurulumda <c>Facility.Counter</c> elle); tahta Core'un önlem kuralıyla
    /// açılır. Cıvatalı makinenin "CIVATALI" etiketi (imza makinesinde, bakımda yalnız 2. kademede, sökülünce yok), hover satırları (makine,
    /// tel ve kalas, koku yayıcı, çift cidarlı duvar), kullanılamama nedenleri (köpek, lokum, koşu darbesi), gözcü satırları, önizlemenin
    /// aygıt ve kelepçe süresi (Core'un yardımcısı), vincin cıvatalıyı çekmemesi; önlemsiz tahtada hiçbiri.
    /// </summary>
    public class FacilityCounterPresentationTests
    {
        GameObject root;
        LiveRaidDataSource source;
        DeskLang lang;

        void Setup(int[] levels, params string[] answers)
        {
            lang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            root = new GameObject("Önlem sunumu");
            root.SetActive(false);
            source = root.AddComponent<LiveRaidDataSource>();
            var sim = FacilityDeskKit.New();
            sim.Facility.Counter.Clear();
            sim.Facility.Counter.AddRange(levels);
            foreach (var a in answers) sim.Facility.Recipes.Add(a);
            FacilityDeskKit.StartFirst(sim);
            source.OpenFacilityForValidation(sim);
            FacilityDeskKit.Calm(B); Floor(B); Park(B);
            B.Ev = null;
            Republish(source);
        }

        [TearDown]
        public void Close()
        {
            if (root != null) Object.DestroyImmediate(root);
            DeskLoc.Instance.SetLanguage(lang);
        }

        Baskin.BState B => source.State;
        Baskin.Unit T => Team(B, "tek");

        static readonly int[] All1 = { 1, 1, 1, 1, 1, 1 };

        [Test]
        public void Order_CopiesLevels_IntoTheBoard()
        {
            Setup(new[] { 1, 0, 2, 0, 1, 0 });
            Assert.IsNotNull(B.Moves);
            Assert.AreEqual(1, B.Moves.Bolt);
            Assert.AreEqual(2, B.Moves.Cutter);
            Assert.AreEqual(1, B.Moves.Emitter);
            Assert.IsTrue(B.Moves.Tally, "defter açıkken tahta sayar");
        }

        [Test]
        public void BoltTag_SignatureMachine_NotDrone_MaintenanceOnlyAtTwo_GoneWhenUnbolted()
        {
            Setup(All1);
            var sig = AddMachine(B, "zdron", 3, 3);
            var drone = AddMachine(B, "dron", 4, 3);
            var maint = AddMachine(B, "bakim", 5, 3);
            Republish(source);
            var marks = source.MarksFor(source.Current);
            Assert.AreEqual((Loc.T("CIVATALI"), true), marks.Tags[sig.Id], "imza makinesi cıvatalı (kırmızı)");
            Assert.AreEqual("CIVATALI", marks.Tags[sig.Id].text);
            Assert.IsFalse(marks.Tags.ContainsKey(drone.Id), "dron cıvatasız");
            Assert.IsFalse(marks.Tags.ContainsKey(maint.Id), "bakım robotu 1. kademede cıvatasız");
            sig.Unbolted = true;
            Republish(source);
            Assert.IsFalse(source.MarksFor(source.Current).Tags.ContainsKey(sig.Id), "sökülünce etiket kalkar");
            Close();

            Setup(new[] { 2, 0, 0, 0, 0, 0 });
            var m2 = AddMachine(B, "bakim", 5, 3);
            Republish(source);
            Assert.AreEqual("CIVATALI", source.MarksFor(source.Current).Tags[m2.Id].text, "2. kademede bakım da");
            Close();

            Setup(new int[6]);
            var plain = AddMachine(B, "zdron", 3, 3);
            Republish(source);
            Assert.IsFalse(source.MarksFor(source.Current).Tags.ContainsKey(plain.Id), "önlemsiz tahtada etiket yok");
        }

        [Test]
        public void Hover_Machine_Wire_Plank_Emitter_DoubleWall()
        {
            Setup(new[] { 1, 1, 2, 0, 1, 0 });
            var sig = AddMachine(B, "zdron", 3, 3);
            Baskin.CellAt(B, 5, 5).O = new Baskin.Obj { K = "tel", Hp = 3, Max = 3 };
            Baskin.CellAt(B, 6, 5).O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 };
            Baskin.CellAt(B, 5, 2).O = new Baskin.Obj { K = "yayici", Hp = 1, Max = 1 };
            var wall = Baskin.CellAt(B, 6, 1); wall.T = "wall"; wall.Double = true;
            Republish(source);

            var card = Card(source, new Vector2Int(3, 3));
            Assert.IsTrue(card.Rows.Any(r => r.Text == "ÖNLEM · Cıvatalı ayak · itilmez, çekilmez"), Rows(card));
            sig.Unbolted = true;
            card = Card(source, new Vector2Int(3, 3));
            Assert.IsTrue(card.Rows.Any(r => r.Text == "Cıvatası sökük"), Rows(card));
            Assert.IsTrue(Card(source, new Vector2Int(5, 5)).Rows.Any(r => r.Text == "Kesici kol: makineler +1 hasar"), "tel");
            Assert.IsTrue(Card(source, new Vector2Int(6, 5)).Rows.Any(r => r.Text == "Kesici kol: makineler +1 hasar"), "kalas (2. kademe)");
            card = Card(source, new Vector2Int(5, 2));
            Assert.AreEqual("Koku yayıcı", card.Title);
            Assert.AreEqual("thumb_emitter", card.Thumb);
            Assert.IsTrue(card.Rows.Any(r => r.Text == "Koku yayıcı: 3 kare içinde köpek komut alamaz · kırılınca biter"), Rows(card));
            card = Card(source, new Vector2Int(6, 1));
            Assert.AreEqual("Çift cidar", card.Title);
            Assert.IsTrue(card.Rows.Any(r => r.Text == "ÖNLEM · Çift cidar · yıkmak 2 hak"), Rows(card));
            Assert.LessOrEqual(card.Rows.Length, 5, "kart beş satır");
            Close();

            // Kesici kol 1. kademe: kalas etkilenmez
            Setup(new[] { 0, 0, 1, 0, 0, 0 });
            Baskin.CellAt(B, 6, 5).O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 };
            Republish(source);
            Assert.IsFalse(Card(source, new Vector2Int(6, 5)).Rows.Any(r => r.Text.StartsWith("Kesici kol")), "kalas yalnız 2. kademede");
        }

        static string Rows(RaidInspectorModel card) => string.Join(" | ", card.Rows.Select(r => r.Text));

        [Test]
        public void Reasons_Dog_Charge_Rush()
        {
            Setup(All1);
            Clear(B, T);
            // Köpek: erimde ayakta yayıcı → komut yok, neden
            var dog = AddDog(B, T.Slot, 6, 6);
            Assert.AreSame(dog, Baskin.UnitAt(B, 6, 6));
            var emitter = Baskin.CellAt(B, 6, 4);
            emitter.O = new Baskin.Obj { K = "yayici", Hp = 1, Max = 1 };
            Assert.IsTrue(Baskin.EmitterBlocks(B, dog));
            Assert.IsFalse(Baskin.CanAction(B, dog, Baskin.ActBark), "Core köpeğin komutunu keser");
            Assert.AreEqual("koku yayıcı yakında", FacilityGearText.Reason(B, dog, Baskin.ActBark));
            Assert.AreEqual("koku yayıcı yakında", FacilityGearText.Reason(B, dog, Baskin.ActSniff));
            emitter.O = null;
            Assert.IsNull(FacilityCounterText.Reason(B, dog, Baskin.ActBark), "yayıcı yokken önlem nedeni yok");
            dog.Dead = true;

            // Lokum: bitişikte yalnız çift cidar, 1 hak
            foreach (var d in Baskin.Dirs)
            {
                var c = Baskin.CellAt(B, T.X + d[0], T.Y + d[1]);
                c.T = "floor"; c.O = null;
            }
            var w = Baskin.CellAt(B, T.X + 1, T.Y); w.T = "wall"; w.Double = true;
            Gear(T, FacilityGear.Charge, 1);
            Assert.AreEqual("çift cidar: 2 hak gerekir", FacilityGearText.Reason(B, T, Baskin.ActCharge));
            Gear(T, FacilityGear.Charge, 2);
            Assert.IsNull(FacilityCounterText.Reason(B, T, Baskin.ActCharge), "hak yeterken neden yok");
            Assert.IsTrue(Baskin.ActionTargets(B, T, Baskin.ActCharge).Any(p => p.X == T.X + 1 && p.Y == T.Y), "Core 2 hakla hedef alır");
            w.T = "floor"; w.Double = false;

            // Koşu darbesi: hattın makinesi cıvatalı
            var E = Team(B, "emp");
            Put(E, 1, 6);
            for (int x = 2; x < 8; x++) { var c = Baskin.CellAt(B, x, 6); c.T = "floor"; c.O = null; }
            foreach (var u in B.Units.Where(u => u != E && u.Y == 6 && !u.Dead)) Put(u, u.X, 4);
            var sig = AddMachine(B, "zdron", 1 + B.Moves.RushRun + 1, 6);
            E.Moved = false; E.Acted = false;
            Assert.AreEqual(0, Baskin.ActionTargets(B, E, Baskin.ActRush).Count(p => p.X == sig.X && p.Y == sig.Y), "Core cıvatalıyı hedef almaz");
            Assert.AreEqual("cıvatalı: itilmez", FacilityCounterText.Reason(B, E, Baskin.ActRush));
            if (Baskin.ActionTargets(B, E, Baskin.ActRush).Count == 0)
                Assert.AreEqual("cıvatalı: itilmez", FacilityMoveText.Reason(B, E, Baskin.ActRush), "kullanılamayan koşunun nedeni");
            sig.Unbolted = true;
            Assert.IsTrue(Baskin.ActionTargets(B, E, Baskin.ActRush).Any(p => p.X == sig.X && p.Y == sig.Y), "sökülünce hedef");
            Assert.IsNull(FacilityCounterText.Reason(B, E, Baskin.ActRush), "sökülünce önlem nedeni yok");
        }

        [Test]
        public void Spotter_ListsOnlyWhatIsFound()
        {
            Setup(All1);
            Assert.IsFalse(FacilityRaidText.Spotter(B).Any(l => l.Text.Contains("cıvatalı")), "makine yok");
            var sig = AddMachine(B, "zdron", 5, 2);
            AddMachine(B, "dron", 6, 2);
            Baskin.CellAt(B, 4, 4).O = new Baskin.Obj { K = "yayici", Hp = 1, Max = 1 };
            var w = Baskin.CellAt(B, 2, 2); w.T = "wall"; w.Double = true;
            var lines = FacilityRaidText.Spotter(B).Select(l => l.Text).ToList();
            int room = B.Room.Index;
            CollectionAssert.Contains(lines, "Gözcü · çift cidar: " + FacilityRaidText.Cell(room, 2, 2));
            CollectionAssert.Contains(lines, "Gözcü · koku yayıcı: " + FacilityRaidText.Cell(room, 4, 4));
            CollectionAssert.Contains(lines, "Gözcü · cıvatalı: " + Baskin.UnitTypes["zdron"].Name + " " + FacilityRaidText.Cell(room, sig.X, sig.Y));
            Assert.AreEqual(1, lines.Count(l => l.StartsWith("Gözcü · cıvatalı")), "dron cıvatasız");
            Close();
            Setup(new int[6]);
            AddMachine(B, "zdron", 5, 2);
            Assert.IsEmpty(FacilityCounterText.Spotter(B), "önlemsiz tahta");
        }

        [Test]
        public void Preview_DeviceAndClampTurns_FromCore_WinchSkipsBolted()
        {
            Setup(new[] { 1, 0, 0, 1, 0, 1 });
            Clear(B, T);
            Assert.AreEqual(B.Moves.DeviceTurns - B.Moves.JamTurns, Baskin.DeviceTurnsFor(B, T), "parazit");
            StringAssert.Contains(Loc.F("{0} tur", Baskin.DeviceTurnsFor(B, T)), FacilityGearText.Detail(B, T, Baskin.ActTurret));
            StringAssert.Contains(Loc.F("{0} tur ele geçirir", Baskin.ClampTurnsFor(B, T)), FacilityGearText.Detail(B, T, Baskin.ActClamp));
            Assert.AreEqual(B.Moves.ClampTurns - B.Moves.ShortClampTurns, Baskin.ClampTurnsFor(B, T), "kısa bağ");
            // Kelepçenin kalan tur yazısı Core'un verdiği süreyle başlar
            var cap = Captive(B, AddMachine(B, "dron", 6, 6), T.Slot, Baskin.ClampTurnsFor(B, T));
            Assert.AreEqual(Baskin.ClampTurnsFor(B, T), FacilityGearText.CaptiveLeft(B, cap), "yakalandığı tur tam süre");

            // Vinç cıvatalıyı çekmez: hat orada biter, çekiş yok
            foreach (var u in B.Units.Where(u => u.Y == 1 && !u.Dead)) Put(u, u.X, 5);
            for (int x = 0; x < 4; x++) { var c = Baskin.CellAt(B, x, 1); c.T = "floor"; c.O = null; }
            var sig = AddMachine(B, "zdron", 2, 1);
            var (line, hit, pulled) = FacilityGearView.WinchLine(B, 0, 1, 1);
            Assert.AreEqual(2, line.Count, "hat makinede biter");
            Assert.AreEqual(-1, hit, "cıvatalı makine çekilmez");
            Assert.IsNull(pulled);
            sig.Unbolted = true;
            (line, hit, pulled) = FacilityGearView.WinchLine(B, 0, 1, 1);
            Assert.AreSame(sig, pulled, "sökülünce çekilir");
            Assert.AreEqual(1, hit);
        }
    }
}
