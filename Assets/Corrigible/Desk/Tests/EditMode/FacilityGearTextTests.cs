using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;
using static Corrigible.Desk.Tests.FacilityGearTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Ekipman ve uzman olmayan birimlerin saf metinleri ve seçenekleri (aşama 9; tasarım §2.3–2.4, §3.1–3.3, §11.1): yedi ekipmanın
    /// seçenekleri (örme iki, öbürleri bir) ve kullanılabilirliği (Core'un kararı), nedenler <see cref="Baskin.CanAction"/> sırasıyla,
    /// köpeğin ve tutsağın komutları, uzmanın altı hamlesi değişmez; tutsağın kalan turu ve aygıtın kalan çalışması Core'un tur
    /// döngüsüyle eşlik; boş zemin kopyası Core'un hedeflerini kapsar; bütün yeni metinlerin EN çevirisi var. Sahnesiz.
    /// </summary>
    public class FacilityGearTextTests
    {
        static readonly Regex TurkishLetters = new Regex("[ğüşıöçĞÜŞİÖÇ]");
        DeskLang lang;

        [SetUp] public void Remember() => lang = DeskLoc.Instance.Lang;
        [TearDown] public void Restore() => DeskLoc.Instance.SetLanguage(lang);

        /// <summary>Pafta 1, düz zemin, ekip köşede, makineler kalkmış; Teknisyen dört yanı boş karede.</summary>
        static Baskin.BState Board(out Baskin.Unit tech)
        {
            var (_, _, B) = Room(1);
            Floor(B); Park(B);
            tech = Team(B, "tek");
            Clear(B, tech);
            return B;
        }

        static readonly (string gear, string[] kinds)[] Kits =
        {
            (FacilityGear.Wire, new[] { Baskin.ActWire, Baskin.ActPlank }),
            (FacilityGear.Charge, new[] { Baskin.ActCharge }),
            (FacilityGear.Turret, new[] { Baskin.ActTurret }),
            (FacilityGear.Decoy, new[] { Baskin.ActDecoy }),
            (FacilityGear.Winch, new[] { Baskin.ActWinch }),
            (FacilityGear.Clamp, new[] { Baskin.ActClamp }),
        };

        [Test]
        public void GearOptions_SevenKits_CoreDecides()
        {
            var B = Board(out var t);
            // Lokum için bitişik duvar, kelepçe için bitişik makine
            var wall = new Baskin.Pt(t.X + 1, t.Y);
            var m = Machine(B, t.X - 1, t.Y);
            Baskin.CellAt(B, wall.X, wall.Y).T = "wall";
            foreach (var (gear, kinds) in Kits)
            {
                Gear(t, gear, 2);
                var o = FacilityGearText.GearOptions(B, t);
                CollectionAssert.AreEqual(kinds, o.Select(x => x.Kind).ToArray(), gear);
                foreach (var x in o)
                {
                    bool core = Baskin.CanAction(B, t, x.Kind) && Baskin.ActionTargets(B, t, x.Kind).Count > 0;
                    Assert.AreEqual(core, x.Usable, gear + " " + x.Kind);
                    Assert.IsTrue(x.Usable, "kurulumda kullanılabilir: " + x.Kind);
                    Assert.IsFalse(string.IsNullOrEmpty(x.Name));
                    Assert.IsFalse(string.IsNullOrEmpty(x.Detail));
                }
                // Uzmanın E listesinde ekipman yok: altı hamlenin anlamlı olanları
                Assert.IsTrue(FacilityMoveText.Options(B, t).All(x => Baskin.ActionKinds.Contains(x.Kind)), "E listesi " + gear);
            }
            // Köpek ekipmanı uzmanın eylemi değil (dördüncü birim); ekipmansız boş
            Gear(t, FacilityGear.Dog, -1);
            CollectionAssert.IsEmpty(FacilityGearText.GearOptions(B, t));
            Gear(t, null, -1);
            CollectionAssert.IsEmpty(FacilityGearText.GearOptions(B, t));
            Assert.IsNull(FacilityGearText.GearRow(B, t, null));
            Assert.IsTrue(m.Side == 'e');
        }

        [Test]
        public void Reasons_FollowCanActionOrder()
        {
            var B = Board(out var t);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            // Hak: kalas iki hak ister; bir hakla tel kullanılabilir, kalas değil
            Gear(t, FacilityGear.Wire, 1);
            var o = FacilityGearText.GearOptions(B, t);
            Assert.IsTrue(o.Single(x => x.Kind == Baskin.ActWire).Usable);
            var plank = o.Single(x => x.Kind == Baskin.ActPlank);
            Assert.IsFalse(plank.Usable);
            Assert.AreEqual(Loc.F("kalas için {0} hak gerekir", B.Moves.PlankCost), plank.Reason);
            Assert.AreEqual(Loc.F("tel {0} · kalas {1} · {2} hak", 1, B.Moves.PlankCost, 1), FacilityGearText.GearRow(B, t, null).Detail, "örme setinde iki bedel ve kalan hak");
            Gear(t, FacilityGear.Wire, 2);
            Assert.AreEqual(Loc.F("tel {0} · kalas {1} · {2} hak", 1, B.Moves.PlankCost, 2), FacilityGearText.GearRow(B, t, null).Detail, "örme setinde iki eylemin bedeli");
            // Hak bitti
            Gear(t, FacilityGear.Wire, 0);
            Assert.AreEqual(Loc.T("hak kalmadı"), FacilityGearText.GearOptions(B, t)[0].Reason);
            var row = FacilityGearText.GearRow(B, t, null);
            Assert.IsTrue(row.Used);
            Assert.AreEqual(Loc.T("hak kalmadı"), row.Detail);
            // Yetenek kullanıldı önce gelir (CanAct)
            Gear(t, FacilityGear.Wire, 2);
            t.Acted = true;
            Assert.AreEqual(Loc.T("yetenek kullanıldı"), FacilityGearText.GearOptions(B, t)[0].Reason);
            t.Acted = false;
            // Bitişikte boş zemin yok: dört yan duvar
            foreach (var d in Baskin.Dirs) Baskin.CellAt(B, t.X + d[0], t.Y + d[1]).T = "wall";
            Assert.AreEqual(Loc.T("bitişikte boş zemin yok"), FacilityGearText.GearOptions(B, t)[0].Reason);
            Gear(t, FacilityGear.Turret, 1);
            Assert.AreEqual(Loc.T("bitişikte boş zemin yok"), FacilityGearText.GearOptions(B, t)[0].Reason);
            Gear(t, FacilityGear.Clamp, 1);
            Assert.AreEqual(Loc.T("bitişikte makine yok"), FacilityGearText.GearOptions(B, t)[0].Reason);
            foreach (var d in Baskin.Dirs) Baskin.CellAt(B, t.X + d[0], t.Y + d[1]).T = "floor";
            Gear(t, FacilityGear.Charge, 1);
            Assert.AreEqual(Loc.T("bitişikte yıkılacak duvar yok"), FacilityGearText.GearOptions(B, t)[0].Reason);
            // Kart satırı: kip açıkken kurulu eylem ve kalan hak
            Gear(t, FacilityGear.Wire, 2);
            var armed = FacilityGearText.GearRow(B, t, Baskin.ActPlank);
            Assert.IsTrue(armed.Armed);
            Assert.AreEqual(Loc.F("{0} · {1} hak", Loc.T("Kalas kur"), 2), armed.Detail);
            Assert.AreEqual(PlanningBoardData.EquipmentName(FacilityGear.Wire), armed.Text);
        }

        [Test]
        public void Dog_Captive_Gadget_Options()
        {
            var B = Board(out var t);
            var dog = AddDog(B, t.Slot, t.X + 1, t.Y);
            // Odanın ileri geçidi var, kaçış yok: havla ve kokla; kokla geçidin yanında değilken kullanılamaz
            Assert.Greater(B.Room.Forward.Count, 0, "pafta 1'in ileri geçidi");
            var o = FacilityMoveText.Options(B, dog);
            CollectionAssert.AreEqual(new[] { Baskin.ActBark, Baskin.ActSniff }, o.Select(x => x.Kind).ToArray());
            Assert.IsTrue(o[0].Usable);
            Assert.IsFalse(o[1].Usable);
            Assert.AreEqual(Loc.T("ileri geçidin yanında değil"), o[1].Reason);
            // Yük yerde: getir (yanında değilken kullanılamaz)
            B.CargoX = 0; B.CargoY = 0;
            o = FacilityMoveText.Options(B, dog);
            CollectionAssert.AreEqual(new[] { Baskin.ActBark, Baskin.ActFetch, Baskin.ActSniff }, o.Select(x => x.Kind).ToArray());
            Assert.AreEqual(Loc.T("yük yanında değil"), o[1].Reason);
            B.CargoX = B.CargoY = -1;
            // Geçidin yanında kokla kullanılabilir; koklandıysa neden
            var gate = B.Room.Forward[0];
            var near = FreeNeighbor(B, gate, true);
            Put(dog, near.X, near.Y);
            Assert.IsTrue(FacilityMoveText.Options(B, dog).Single(x => x.Kind == Baskin.ActSniff).Usable);
            B.Sniffed = B.Room.Index + 1;
            Assert.AreEqual(Loc.T("bu oda koklandı"), FacilityMoveText.Options(B, dog).Single(x => x.Kind == Baskin.ActSniff).Reason);
            B.Sniffed = 0;
            // Kaçışta kokla satırı yok
            B.Escape = true;
            Assert.IsFalse(FacilityMoveText.Options(B, dog).Any(x => x.Kind == Baskin.ActSniff));
            Assert.AreEqual(Loc.T("kaçışta koklanmaz"), FacilityGearText.Reason(B, dog, Baskin.ActSniff));
            B.Escape = false;
            // Tutsak: komuta; çoğaltıcıda neden
            var m = Machine(B, 1, 1);
            Captive(B, m, t.Slot, 2);
            var c = FacilityMoveText.Options(B, m);
            CollectionAssert.AreEqual(new[] { Baskin.ActCommand }, c.Select(x => x.Kind).ToArray());
            Assert.AreEqual(Baskin.CanAction(B, m, Baskin.ActCommand) && Baskin.ActionTargets(B, m, Baskin.ActCommand).Count > 0, c[0].Usable);
            if (!c[0].Usable) Assert.AreEqual(m.Type == "cogalt" ? Loc.T("bu makinenin komutu yok") : Loc.T("vuracak hedef yok"), c[0].Reason);
            // Aygıt: hiçbir şey; uzmanın altı hamlesi aynı (ekipman ve birim eylemi yok)
            var g = AddGadget(B, "taret", 6, 1, 1, 3, t.Slot);
            CollectionAssert.IsEmpty(FacilityMoveText.Options(B, g));
            CollectionAssert.IsEmpty(FacilityGearText.GearOptions(B, g));
            Gear(t, FacilityGear.Turret, 1);
            Assert.IsTrue(FacilityMoveText.Options(B, t).All(x => Baskin.ActionKinds.Contains(x.Kind)));
            CollectionAssert.IsEmpty(FacilityGearText.GearOptions(B, dog), "köpekte ekipman satırı yok");
        }

        [Test]
        public void CaptiveLeft_And_DeviceTurns_FollowCoreTurns()
        {
            var B = Board(out var t);
            B.MaxTurns = 20;
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            // Kelepçe gerçek eylemle; makineler başka yerde yok (uyanma kapalı)
            var m = Machine(B, t.X + 1, t.Y);
            Gear(t, FacilityGear.Clamp, 1);
            Assert.IsTrue(Baskin.DoAction(B, t.Id, Baskin.ActClamp, m.X, m.Y), "kelepçe");
            var cap = B.Units.Single(u => u.Id == m.Id);
            Assert.IsTrue(Baskin.IsCaptive(cap));
            int full = B.Moves.ClampTurns;
            var seen = new List<int>();
            for (int turn = 0; turn < full + 2 && Baskin.IsCaptive(B.Units.Single(u => u.Id == m.Id)); turn++)
            {
                cap = B.Units.Single(u => u.Id == m.Id);
                seen.Add(FacilityGearText.CaptiveLeft(B, cap));
                Assert.AreEqual(seen.Last() <= 1 ? Loc.T("SON TUR") : Loc.F("TUTSAK · {0} TUR", seen.Last()), FacilityGearText.CaptiveText(B, cap));
                foreach (var u in B.Units.Where(Baskin.IsSpecialist)) u.Moved = u.Acted = true;
                Assert.IsTrue(Baskin.EndTurn(B), "tur " + turn);
                Assert.AreEqual("player", B.Phase);
            }
            // Yakalandığı tur tam süre; sonraki turlarda oynayacağı tur sayısı birer azalır; sonra bırakılır (oyuncu ClampTurns tur oynar)
            var expect = new List<int> { full };
            for (int k = full; k >= 1; k--) expect.Add(k);
            CollectionAssert.AreEqual(expect, seen);
            Assert.AreEqual('e', B.Units.Single(u => u.Id == m.Id).Side, "süre bitince serbest");
            Assert.AreEqual(full, seen.Count - 1, "yakalandıktan sonra oynanan tutsak turu");

            // Aygıt: kurulunca DeviceTurns; her makine evresinde bir düşer; sıfırda hurda
            var B2 = Board(out var t2);
            B2.MaxTurns = 20;
            Gear(t2, FacilityGear.Turret, 1);
            var at = new Baskin.Pt(t2.X + 1, t2.Y);
            Assert.IsTrue(Baskin.DoAction(B2, t2.Id, Baskin.ActTurret, at.X, at.Y));
            var g = B2.Units.Single(u => Baskin.IsGadget(u));
            var turns = new List<int>();
            while (!B2.Units.Single(u => u.Id == g.Id).Dead && turns.Count < 10)
            {
                var live = B2.Units.Single(u => u.Id == g.Id);
                turns.Add(live.Turns);
                Assert.AreEqual(Loc.F("{0} TUR", live.Turns), FacilityGearText.DeviceTurnsText(live));
                Assert.IsTrue(Baskin.EndTurn(B2));
            }
            CollectionAssert.AreEqual(Enumerable.Range(1, B2.Moves.DeviceTurns).Reverse().ToArray(), turns.ToArray());
            Assert.AreEqual("hurda", Baskin.CellAt(B2, at.X, at.Y).O?.K, "süre sonunda hurda");
        }

        [Test]
        public void FreeFloor_CoversCoreTargets_SafetyReason()
        {
            var rng = new System.Random(9);
            int checkedBoards = 0, safety = 0;
            var rooms = new[] { Room(1).B, Room(2).B, Room(3).B };
            foreach (var r in rooms) { Floor(r); Park(r); }
            for (int n = 0; n < 200; n++)
            {
                var B = rooms[n % 3].Clone();
                var t = Team(B, "tek");
                // Rastgele duvar, kalas ve tek gövde; uzman rastgele zemin karesinde
                for (int k = 0; k < 14; k++)
                {
                    int x = rng.Next(Baskin.N), y = rng.Next(Baskin.N);
                    var c = Baskin.CellAt(B, x, y);
                    if (c.Door != 0 || c.Vent != 0 || c.Crate || c.Shutter || Baskin.UnitAt(B, x, y) != null || B.Room.West.Concat(B.Room.Forward).Any(p => p.X == x && p.Y == y)) continue;
                    if (rng.Next(2) == 0) c.T = "wall"; else c.O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 };
                }
                var spots = Enumerable.Range(0, 64).Select(i => new Baskin.Pt(i % 8, i / 8)).Where(p => Open(B, p.X, p.Y)).ToList();
                if (spots.Count == 0) continue;
                var at = spots[rng.Next(spots.Count)];
                Put(t, at.X, at.Y);
                Gear(t, FacilityGear.Wire, 2);
                var free = FacilityGearText.FreeNeighbors(B, t);
                var targets = Baskin.ActionTargets(B, t, Baskin.ActWire);
                foreach (var p in targets) Assert.IsTrue(free.Any(f => f.X == p.X && f.Y == p.Y), "hedef boş zeminde değil: " + p.X + "," + p.Y);
                Gear(t, FacilityGear.Turret, 1);
                CollectionAssert.AreEquivalent(free.Select(p => p.Y * 8 + p.X).ToArray(), Baskin.ActionTargets(B, t, Baskin.ActTurret).Select(p => p.Y * 8 + p.X).ToArray(), "aygıt hedefi = boş zemin");
                Gear(t, FacilityGear.Wire, 2);
                if (targets.Count == 0)
                {
                    var reason = FacilityGearText.Reason(B, t, Baskin.ActWire);
                    Assert.AreEqual(free.Count == 0 ? Loc.T("bitişikte boş zemin yok") : Loc.T("engel ekibi çıkıştan koparır"), reason);
                    if (free.Count > 0) safety++;
                }
                checkedBoards++;
            }
            Assert.Greater(checkedBoards, 150);
            TestContext.WriteLine("tahta " + checkedBoards + ", güvence nedeni " + safety);
        }

        [Test]
        public void Texts_AllKinds_TrAndEn()
        {
            var B = Board(out var t);
            var dog = AddDog(B, t.Slot, t.X + 1, t.Y);
            var m = Machine(B, 1, 1);
            Captive(B, m, t.Slot, 1);
            var g = AddGadget(B, "vinc", 6, 1, 3, 2, t.Slot);
            var missing = new HashSet<string>();
            void OnMissing(string s) => missing.Add(s);
            Loc.Missing += OnMissing;
            try
            {
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    var texts = new List<string>();
                    foreach (var kind in Baskin.GearActionKinds.Concat(Baskin.UnitActionKinds))
                    {
                        var actor = Baskin.GearOf(kind) != null ? t : kind == Baskin.ActCommand ? m : dog;
                        texts.Add(FacilityGearText.Name(kind));
                        texts.Add(FacilityGearText.Detail(B, actor, kind));
                        texts.Add(FacilityGearText.Reason(B, actor, kind));
                        texts.AddRange(FacilityGearText.Effects(B, actor, kind).Texts);
                        Assert.AreEqual(FacilityGearText.Name(kind), FacilityMoveText.Name(B, actor, kind), "devir");
                    }
                    foreach (var (gear, _) in Kits) { Gear(t, gear, 1); texts.Add(FacilityGearText.GearLine(B, t)); }
                    foreach (var type in new[] { "dron", "bakim", "havan", "bomba", "kilit", "kalkan", "cogalt", "zdron" }) texts.Add(FacilityGearText.CommandDetail(type));
                    for (int d = 0; d < 4; d++) texts.Add(FacilityGearText.DirName(d));
                    texts.Add(FacilityGearText.CaptiveText(B, m));
                    texts.Add(FacilityGearText.DeviceTurnsText(g));
                    texts.Add(FacilityGearText.DogName(B, dog));
                    texts.Add(FacilityGearText.UnitTitle(dog)); texts.Add(FacilityGearText.UnitTitle(g)); texts.Add(FacilityGearText.UnitTitle(m));
                    dog.Bark = true; B.Carrier = dog.Id;
                    foreach (var u in new[] { dog, g, m }) texts.AddRange(FacilityGearText.UnitRows(B, u).Select(r => r.Text));
                    B.Carrier = 0; dog.Bark = false;
                    foreach (var k in new[] { "tel", "kalas", "hurda" }) texts.AddRange(FacilityGearText.ObjectRows(B, new Baskin.Obj { K = k, Hp = 1, Max = 1 }).Select(r => r.Text));
                    foreach (var t0 in texts)
                    {
                        Assert.IsFalse(string.IsNullOrEmpty(t0), l.ToString());
                        // Kişi adları Türkçe olabilir: yalnız adsız metinler denetlenir
                        if (l == DeskLang.En && t0.IndexOf(FacilityMoveText.Who(t)) < 0) Assert.IsFalse(TurkishLetters.IsMatch(t0), t0);
                    }
                }
            }
            finally { Loc.Missing -= OnMissing; }
            CollectionAssert.IsEmpty(missing, "çevirisi olmayan: " + string.Join(" | ", missing));
        }

        [Test]
        public void Slips_EachKind()
        {
            var B = Board(out var t);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            string Cell(int x, int y) => FacilityRaidText.Cell(B.Room.Index, x, y);
            Gear(t, FacilityGear.Wire, 2);
            var at = new Baskin.Pt(t.X + 1, t.Y);
            Assert.IsTrue(Baskin.DoAction(B, t.Id, Baskin.ActWire, at.X, at.Y));
            Assert.AreEqual(Loc.F("{0} · tel · {1}", "T", Cell(at.X, at.Y)), FacilityMoveText.Slip(B, Baskin.ActWire, t.Id, at.X, at.Y, 0, id => "T"));
            Baskin.Undo(B);
            t = Team(B, "tek");
            Gear(t, FacilityGear.Decoy, 1);
            Assert.IsTrue(Baskin.DoAction(B, t.Id, Baskin.ActDecoy, at.X, at.Y));
            Assert.AreEqual(Loc.F("{0} · {1} kuruldu · {2}", "T", DeskText.Lower(Loc.T("Yürüyen yem"), DeskLang.Tr), Cell(at.X, at.Y)), FacilityGearText.Slip(B, Baskin.ActDecoy, t.Id, at.X, at.Y, id => "T"));
            B.Sniffed = 2;
            Assert.AreEqual(Loc.F("Köpek · koklandı · PAFTA {0}", 2), FacilityGearText.Slip(B, Baskin.ActSniff, t.Id, 0, 0));
            Assert.AreEqual(Loc.T("Köpek · havladı"), FacilityGearText.Slip(B, Baskin.ActBark, t.Id, 0, 0));
            Assert.IsNull(FacilityGearText.Slip(B, "bilinmeyen", t.Id, 0, 0));
        }
    }
}
