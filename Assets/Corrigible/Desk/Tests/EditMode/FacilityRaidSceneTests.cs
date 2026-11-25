using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskını Baskin sahnesinde (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §3.3–3.5, §11.5): ilk odanın geçit satırı, donanımı,
    /// amacı ve anteti; kapı, geri alma ve GEÇ ile sıradaki paftanın açılması (raid_room); kanadın tamamı ve sonuç kartı; eski tek
    /// tahtanın değişmemesi. GameController'sız: rapor doğrudan simülasyona (OpenFacilityForValidation). Düzenleme modunda geçiş
    /// eşzamanlıdır (pafta çevrilmez). Boş sahne açıkken koşar (Baskin.unity eklemeli açılır).
    /// </summary>
    public class FacilityRaidSceneTests
    {
        static void WithLive(System.Action<LiveRaidDataSource> body)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var live = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LiveRaidDataSource>(true)).Single();
                body(live);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        static Simulation Facility()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            return sim;
        }

        static char Letter(Baskin.Unit u) => Baskin.UnitTypes[u.Type].Short[0];

        /// <summary>Ayaktaki uzman kapı karesine ışınlanır (kurulum), sonra gerçek yoldan seçilip kapıya sokulur.</summary>
        static void ToGate(LiveRaidDataSource live, Baskin.Unit u)
        {
            var g = FacilityDeskKit.FreeGate(live.State, u);
            Assert.IsTrue(g.HasValue, "boş kapı karesi yok: " + live.State.Room.Id);
            u.X = g.Value.X; u.Y = g.Value.Y;
            live.ClearHover();
            live.Select(Letter(u));
            Assert.AreEqual(Letter(u), live.Selected, "seçilemedi: " + u.Type);
            live.EnterGate();
            Assert.IsTrue(u.AtGate || u.Out, "kapıya girmedi: " + u.Type);
        }

        static void AllToGate(LiveRaidDataSource live)
        {
            FacilityDeskKit.Calm(live.State);
            foreach (var u in FacilityDeskKit.Standing(live.State)) ToGate(live, u);
        }

        /// <summary>
        /// Tek kip · MapSeed bekçisi (tasarım §4.4 madde 9): kampanya emrinden açılan tesis odası bölgenin bina kitini giyer. Emrin
        /// MapSeed'i harekâtın tohumudur (0 olsaydı bütün tesis tahtaları derleme hatası vermeden varsayılan kite düşerdi).
        /// </summary>
        [Test]
        public void FacilityRoom_UsesRegionKit()
        {
            var sim = Facility();
            var p = sim.S.Raid.Params;
            Assert.AreEqual(sim.S.Seed, p.MapSeed, "emrin MapSeed'i harekâtın tohumu");
            Assert.AreNotEqual(0u, p.MapSeed);
            Assert.AreEqual(p.Region, RaidKitText.KitFor(p.Region, p.MapSeed), "tahtasız kural: bölgenin kiti");
            WithLive(live =>
            {
                live.OpenFacilityForValidation(sim);
                Assert.IsNotNull(live.State.Room, "tesis odası");
                Assert.AreEqual(p.Region, live.State.Params.Region);
                Assert.AreEqual(p.Region, live.KitId, "odanın kiti bölgenin kiti");
                Assert.AreNotEqual(RaidKitText.Default, live.KitId);
            });
        }

        [Test]
        public void FacilityRoom_GateRowDeviceAndGoal()
        {
            WithLive(live =>
            {
                var sim = Facility();
                live.OpenFacilityForValidation(sim);
                var vm = live.Current;
                Assert.AreEqual(1, live.State.Room.Index);
                Assert.AreEqual(0, vm.Targets[0].Max, "geçit satırı can taşımaz");
                Assert.AreEqual(Loc.T("İleri geçit"), vm.Targets[0].Name);
                StringAssert.StartsWith("1-", vm.Targets[0].Cell);
                Assert.AreEqual(Loc.T(Baskin.ObjNames[live.State.Room.Device]), vm.Targets[1].Name);
                Assert.IsFalse(vm.Targets[1].Armor, "donanım zırhlı hedef değil");
                Assert.AreEqual("thumb_cooling", vm.Targets[1].Thumb);
                Assert.AreEqual(Loc.T("Amaç: ekibi geçide götür, sonra GEÇ"), vm.Goal);
                Assert.AreEqual(1, vm.BlueprintWing);
                Assert.AreEqual(1, vm.BlueprintSheet);
                Assert.IsFalse(vm.BlueprintReturn);
                Assert.AreEqual(DeskText.Upper(live.State.Room.Name), vm.Place);
                StringAssert.StartsWith("— PAFTA 1/3", vm.Log[0].Text);
                Assert.IsFalse(vm.CanPass);
                Assert.IsFalse(vm.Finished);
                Assert.AreEqual(0, sim.S.Raid.Reports.Count, "açılış rapor yazmaz");
            });
        }

        [Test]
        public void GateAndPass_OpenTheNextSheet()
        {
            WithLive(live =>
            {
                var sim = Facility();
                live.OpenFacilityForValidation(sim);
                AllToGate(live);
                Assert.IsTrue(live.Current.Team.All(t => t.AtGate), "herkes kapıda");
                Assert.IsTrue(live.Current.CanPass);
                Assert.AreEqual(0, sim.S.Raid.Reports.Count, "kapıya girmek odayı bitirmez");

                // Geri alma son kapı girişini kaldırır: GEÇ kalkar; uzman yeniden kapıya girer
                live.Undo();
                Assert.IsFalse(live.Current.CanPass);
                var back = FacilityDeskKit.Standing(live.State).Single();
                ToGate(live, back);
                Assert.IsTrue(live.Current.CanPass);

                live.EndTurn();   // herkes kapıdayken EMRİ İLET = GEÇ
                Assert.AreEqual(2, live.State.Room.Index);
                Assert.AreEqual(1, sim.S.Raid.Reports.Count);
                Assert.AreEqual("raid_room", sim.Record.commands.Last().kind);
                Assert.AreEqual("player", live.State.Phase);
                Assert.AreEqual(1, live.State.Turn);
                Assert.AreEqual(0, live.State.Hist.Count);
                Assert.IsTrue(live.Current.Log.Any(l => l.Text.StartsWith("— PAFTA 1/3 kapandı")));
                Assert.IsTrue(live.Current.Log.Any(l => l.Text.StartsWith("— PAFTA 2/3")));
                Assert.AreEqual(2, live.Current.BlueprintSheet);
                Assert.IsFalse(live.Current.CanPass);
                Assert.IsFalse(live.Passing);
            });
        }

        [Test]
        public void WholeWing_ResultCardSumsTheRooms()
        {
            WithLive(live =>
            {
                var sim = Facility();
                int start = sim.Record.commands.Count;
                live.OpenFacilityForValidation(sim);
                var visits = new List<string>();
                for (int guard = 0; guard < 6 && !live.Current.Finished; guard++)
                {
                    visits.Add(live.State.Room.Index + (FacilityRaidText.IsReturnBoard(live.State) ? "d" : ""));
                    if (live.State.Room.Type == RoomType.Target && !live.State.Escape) FacilityDeskKit.StartEscape(live.State);
                    AllToGate(live);
                    if (live.Current.CanPass) live.EndTurn();
                }
                Assert.IsTrue(live.Current.Finished, "sonuç kartı yok: " + string.Join(" ", visits));
                CollectionAssert.AreEqual(new[] { "1", "2", "3", "2d" }, visits);
                var reps = sim.S.Raid.Reports;
                Assert.AreEqual(4, reps.Count);
                Assert.IsTrue(sim.RaidRoomsClosed);
                CollectionAssert.AreEqual(new[] { "raid_room", "raid_room", "raid_room", "raid_room" }, sim.Record.commands.Skip(start).Select(c => c.kind).ToArray());
                Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), live.Current.ResultTitle);
                Assert.AreEqual(Loc.F("TUR {0} · PAFTA {1}/3", reps.Sum(r => r.Turns), 3), live.Current.ResultTurn);
                Assert.IsTrue(live.Current.ResultRows.Any(r => r.Label == Loc.T("Tahliye edilen") && r.Value == "3/3"));
                Assert.IsTrue(live.Current.Log.Any(l => l.Text == "Gözcü · ekip tesisten yürüyerek çıktı."), "yürüyerek çıkış satırı");
                Assert.IsTrue(live.Current.Log.Any(l => l.Text == "Gözcü · PAFTA 1/3 temiz: ekip yürüyerek geçti."));
                Assert.IsFalse(sim.S.RaidLog.Any(), "sonuç (raidres) yalnız kartın ardından");
                // Kart açıkken dil değişirse hedef odasının satırları da yeni dilde (önbellekteki ad açılış dilinde donmaz)
                Assert.AreEqual(Loc.T("İkmal kasası"), live.Current.ResultRows[0].Label);
                var lang = DeskLoc.Instance.Lang;
                try
                {
                    DeskLoc.Instance.SetLanguage(DeskLang.En);
                    live.ClearHover();
                    Assert.AreEqual("Supply crate", live.Current.ResultRows[0].Label);
                }
                finally { DeskLoc.Instance.SetLanguage(lang); }
            });
        }
    }
}
