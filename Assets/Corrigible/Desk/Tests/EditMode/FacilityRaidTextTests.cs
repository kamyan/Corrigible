using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskınının telsiz ve tahta metinleri (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §7.3, §11.5): kare adının pafta öneki,
    /// gözcü raporu, oda kapanışının satırları, yürünen temiz odalar ve İngilizce tablo. Saf sınıflar (FacilityRaidText,
    /// FacilityRaidSession.Skipped); sahne açmaz.
    /// </summary>
    public class FacilityRaidTextTests
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        [Test]
        public void CellName_PrefixesRoom()
        {
            Assert.AreEqual("C3", RaidBoard3D.CellName(2, 5));
            Assert.AreEqual("C3", RaidBoard3D.CellName(0, 2, 5), "eski tek tahta (oda 0) öneksiz");
            Assert.AreEqual("2-C3", RaidBoard3D.CellName(2, 2, 5));
            Assert.AreEqual("1-H8", RaidBoard3D.CellName(1, 7, 0));
            Assert.AreEqual("3-A1", RaidBoard3D.CellName(3, 0, 7));
            Assert.AreEqual("2-C3", FacilityRaidText.Cell(2, 2, 5));
        }

        [Test]
        public void Spotter_FirstRoom_ListsMachinesDeviceAndGate()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var B = sim.OpenRaidRoom();
            string before = Baskin.Signature(B);
            var lines = FacilityRaidText.Spotter(B);
            Assert.AreEqual(before, Baskin.Signature(B), "gözcü raporu tahtayı değiştirmez");

            Assert.AreEqual("turn", lines[0].Kind);
            Assert.AreEqual("— PAFTA 1/3 · " + B.Room.Name + " —", lines[0].Text);
            var foes = B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();
            Assert.Greater(foes.Count, 0, "giriş odasında makine var");
            foreach (var u in foes)
            {
                string cell = "1-" + RaidBoard3D.CellName(u.X, u.Y);
                Assert.IsTrue(lines.Any(l => l.Text.StartsWith("Gözcü · ") && l.Text.Contains(cell)), "makinenin karesi: " + cell + "\n" + Dump(lines));
            }
            Assert.IsTrue(lines.Any(l => l.Text.StartsWith("Gözcü · ileri geçit: 1-")), Dump(lines));
            Assert.IsTrue(lines.Any(l => l.Text.EndsWith(" · yan hedef") && l.Kind == "good"), Dump(lines));
            Assert.IsFalse(lines.Any(l => l.Text.Contains("batı geçidi")), "gidişte batı geçidi yazılmaz");
            StringAssert.StartsWith("gözcü · PAFTA 1/3 · ", FacilityRaidText.SpotterSlip(B));

            // Eski tek tahta: metin yok
            B.Room = null;
            Assert.AreEqual(0, FacilityRaidText.Spotter(B).Count);
            Assert.IsNull(FacilityRaidText.SpotterSlip(B));
            Assert.AreEqual(0, FacilityRaidText.BoardNotes(B).Count);
            Assert.IsNull(FacilityRaidText.GateRow(B));
            Assert.IsNull(FacilityRaidText.Goal(B, false));
        }

        [Test]
        public void GateRowNotesAndGoal_FollowTheTeam()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var B = sim.OpenRaidRoom();
            FacilityDeskKit.Calm(B);
            var gates = Baskin.GateTiles(B);
            var row = FacilityRaidText.GateRow(B);
            Assert.AreEqual("İleri geçit", row.Name);
            Assert.AreEqual(0, row.Max);
            Assert.AreEqual("1-" + RaidBoard3D.CellName(gates[0].X, gates[0].Y) + "/" + RaidBoard3D.CellName(gates[1].X, gates[1].Y), row.Cell);
            Assert.AreEqual("Amaç: ekibi geçide götür, sonra GEÇ", FacilityRaidText.Goal(B, false));
            int gateKey = gates[0].Y * 8 + gates[0].X;
            Assert.IsTrue(FacilityRaidText.BoardNotes(B).Contains((gateKey, "GEÇİT")));

            var first = FacilityDeskKit.Standing(B)[0];
            var g = FacilityDeskKit.FreeGate(B, first).Value;
            first.X = g.X; first.Y = g.Y;
            var rows = FacilityRaidText.GateRows(B, first, g.X, g.Y);
            Assert.AreEqual(4, rows.Count, "seçili uzman karedeyken dört satır");
            Assert.AreEqual("Kapıda: 0/3 · herkes gelince GEÇ (Enter)", rows[1].Text);
            Assert.IsTrue(Baskin.EnterGate(B, first.Id));
            Assert.IsTrue(FacilityRaidText.BoardNotes(B).Contains((gateKey, "KAPIDA 1/3")));
            Assert.AreEqual(3, FacilityRaidText.GateRows(B, null, g.X, g.Y).Count);
            var plain = Enumerable.Range(0, 64).Select(k => (x: k % 8, y: k / 8)).First(t => !Baskin.IsGateTile(B, t.x, t.y));
            Assert.AreEqual(0, FacilityRaidText.GateRows(B, null, plain.x, plain.y).Count, "kapı karesi değil");
        }

        [Test]
        public void Skipped_MatchesRaidChainNext()
        {
            var rep = (System.Func<int, bool, string, RoomReport>)((room, ret, end) => FacilityDeskKit.Rep(room, ret, end));
            CollectionAssert.AreEqual(new[] { 2 }, FacilityRaidSession.Skipped(rep(3, false, "pass"), new RoomStep(1, true)));
            CollectionAssert.AreEqual(new[] { 1 }, FacilityRaidSession.Skipped(rep(2, true, "pass"), null));
            CollectionAssert.AreEqual(new[] { 2, 1 }, FacilityRaidSession.Skipped(rep(3, false, "pass"), null));
            CollectionAssert.IsEmpty(FacilityRaidSession.Skipped(rep(3, false, "pass"), new RoomStep(2, true)));
            CollectionAssert.IsEmpty(FacilityRaidSession.Skipped(rep(1, false, "pass"), new RoomStep(2, false)), "gidişte atlama yok");
            CollectionAssert.IsEmpty(FacilityRaidSession.Skipped(rep(2, true, "lost"), null), "geçişsiz biten oda");
            CollectionAssert.IsEmpty(FacilityRaidSession.Skipped(null, null));
        }

        [Test]
        public void Closed_WritesSeparatorDownAndClear()
        {
            var p = new RaidParams();
            var r = FacilityDeskKit.Rep(2);
            r.Turns = 4;
            r.Team[1].State = "d"; r.Team[1].Fell = 2; r.Team[1].X = 2; r.Team[1].Y = 5;
            var lines = FacilityRaidText.Closed(p, r, new int[0]);
            Assert.AreEqual("— PAFTA 2/3 kapandı · 4 tur —", lines[0].Text);
            Assert.AreEqual("turn", lines[0].Kind);
            Assert.IsTrue(lines.Any(l => l.Text.Contains("düştü · 2-C3") && l.Kind == "bad"), Dump(lines));
            Assert.AreEqual(Baskin.UnitTypes["emp"].Name + " düştü · 2-C3", lines[1].Text, "kişisiz kadroda rolün adı");

            // Ara odada düşen dönüşte aynı odanın raporunda da "d" taşınır: yeniden yazılmaz
            var back = FacilityDeskKit.Rep(2, true);
            back.Team[1].State = "d"; back.Team[1].Fell = 2; back.Team[1].X = 2; back.Team[1].Y = 5;
            back.Last = true;
            var again = FacilityRaidText.Closed(p, back, new[] { 1 }, r);
            Assert.AreEqual("— DÖNÜŞ · PAFTA 2/3 kapandı · 0 tur —", again[0].Text);
            Assert.IsFalse(again.Any(l => l.Text.Contains("düştü")), Dump(again));
            Assert.IsTrue(again.Any(l => l.Text == "Gözcü · PAFTA 1/3 temiz: ekip yürüyerek geçti."), Dump(again));
            Assert.IsTrue(again.Any(l => l.Text == "Gözcü · ekip tesisten yürüyerek çıktı."), Dump(again));
            Assert.AreEqual("gözcü · ekip tesisten çıktı", FacilityRaidText.WalkedSlip(new[] { 1 }, true));
            Assert.AreEqual("gözcü · PAFTA 1/3 temiz, geçildi", FacilityRaidText.WalkedSlip(new[] { 1 }, false));
            Assert.IsNull(FacilityRaidText.WalkedSlip(new int[0], false));

            // Devam: History her rapor için ayraç ve düşenler (önceki raporda düşmüş olan yinelenmez)
            var history = FacilityRaidText.History(p, new[] { FacilityDeskKit.Rep(1), r, back });
            Assert.AreEqual(1, history.Count(l => l.Text.Contains("düştü")), Dump(history));
            Assert.AreEqual(3, history.Count(l => l.Kind == "turn"));

            // Geride kalan
            var left = FacilityDeskKit.Rep(1, true, "won");
            left.Team[0].State = "l"; left.Team[0].Fell = 1; left.Team[0].X = 0; left.Team[0].Y = 7;
            Assert.IsTrue(FacilityRaidText.Closed(p, left, null).Any(l => l.Text == Baskin.UnitTypes["tek"].Name + " çıkamadı · 1-A1"));
        }

        [Test]
        public void DeviceEffect_ReadsTheBalance()
        {
            var b = new Balance();
            StringAssert.Contains(b.facilityPanelQuietTurns.ToString(), FacilityRaidText.DeviceEffect("pano", b, true));
            StringAssert.Contains(b.facilityAntennaMachines.ToString(), FacilityRaidText.DeviceEffect("anten", b, false));
            StringAssert.Contains("yürüyemez", FacilityRaidText.DeviceEffect("vana", b, true));
            StringAssert.Contains("can eksik", FacilityRaidText.DeviceEffect("vana", b, false));
            Assert.IsNull(FacilityRaidText.DeviceEffect("vana", null, true));
            Assert.IsNull(FacilityRaidText.DeviceEffect("cool", b, true));
        }

        /// <summary>Bütün metinler İngilizce tabloda: kanadın üç odası, dönüş ve çıkış boyunca hiçbir kaynak eksik değil.</summary>
        [Test]
        public void English_AllTextsTranslated()
        {
            var english = Loc.ParseTable(File.ReadAllText(TablePath))[Loc.English];
            var missing = new HashSet<string>();
            void OnMissing(string s) => missing.Add(s);
            var produced = new List<string>();
            Loc.Missing += OnMissing;
            try
            {
                Loc.SetLanguage(Loc.English, english);
                var sim = FacilityDeskKit.New();
                FacilityDeskKit.StartFirst(sim);
                var session = new FacilityRaidSession(sim, null);
                var b = sim.B;
                foreach (var kind in new[] { "pano", "anten", "vana" }) { produced.Add(FacilityRaidText.DeviceEffect(kind, b, true)); produced.Add(FacilityRaidText.DeviceEffect(kind, b, false)); }
                for (int guard = 0; guard < 6 && !session.Closed; guard++)
                {
                    var B = session.Open();
                    produced.AddRange(FacilityRaidText.Spotter(B).Select(l => l.Text));
                    produced.Add(FacilityRaidText.SpotterSlip(B));
                    produced.Add(FacilityRaidText.Goal(B, false));
                    produced.Add(FacilityRaidText.Goal(B, true));
                    var row = FacilityRaidText.GateRow(B);
                    if (row != null) produced.Add(row.Name);
                    produced.AddRange(FacilityRaidText.BoardNotes(B).Select(n => n.text));
                    var gates = Baskin.GateTiles(B);
                    if (gates.Count > 0) produced.AddRange(FacilityRaidText.GateRows(B, FacilityDeskKit.Standing(B).FirstOrDefault(), gates[0].X, gates[0].Y).Select(r => r.Text));
                    for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) produced.Add(FacilityRaidText.VentOther(B, x, y));
                    // Ara odada bir makine kalır: dönüşte oda yeniden oynanır, giriş odası temiz (yürüyerek çıkış)
                    FacilityDeskKit.PlayRoom(B, B.Room.Type == RoomType.Middle && !B.Escape ? 1 : 0);
                    produced.AddRange(FacilityRaidText.BoardNotes(B).Select(n => n.text));
                    var report = session.Preview(B);
                    var prev = session.Filed.LastOrDefault();
                    Assert.IsTrue(session.File(report).Ok);
                    var skipped = FacilityRaidSession.Skipped(report, session.NextStep);
                    produced.AddRange(FacilityRaidText.Closed(sim.S.Raid.Params, report, skipped, prev).Select(l => l.Text));
                    produced.Add(FacilityRaidText.WalkedSlip(skipped, report.Last && report.End == "pass"));
                }
                Assert.IsTrue(session.Closed, "baskın bitmedi");
                produced.AddRange(FacilityRaidText.History(sim.S.Raid.Params, session.Filed).Select(l => l.Text));
            }
            finally { Loc.Missing -= OnMissing; Loc.SetLanguage(Loc.Turkish); }
            CollectionAssert.IsEmpty(missing, "çevirisi olmayan: " + string.Join(" | ", missing));
            Assert.IsTrue(produced.Any(t => t != null && t.StartsWith("Spotter · forward gate: 1-")), string.Join("\n", produced.Where(t => t != null)));
            Assert.IsTrue(produced.Any(t => t != null && t.StartsWith("— RETURN · SHEET 2/3")), "dönüş odası oynandı");
            Assert.IsTrue(produced.Contains("Spotter · the team walked out of the facility."), "yürüyerek çıkış");
        }

        static string Dump(IEnumerable<RaidLogModel> lines) => string.Join("\n", lines.Select(l => "[" + l.Kind + "] " + l.Text));
    }
}
