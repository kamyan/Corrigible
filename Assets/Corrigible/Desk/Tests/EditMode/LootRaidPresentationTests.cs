using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    public sealed class LootRaidPresentationTests
    {
        [Test]
        public void LootCommandRoute_PresentsCargoObjectiveAndCollectedResult()
        {
            var root = new GameObject("İkmal görev sunumu");
            root.SetActive(false);
            try
            {
                var source = root.AddComponent<LiveRaidDataSource>();
                var state = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "loot" });
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) state.Cells[y, x] = new Baskin.Cell { T = "floor" };
                state.Cells[3, 3].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
                var technician = state.Units.First(u => u.Type == "tek");
                technician.X = 2; technician.Y = 3;
                // Öteki uzmanlar giriş karelerinin yakınında (üstünde duran kaçış başlarken hemen çıkar)
                var emp = state.Units.First(u => u.Type == "emp"); emp.X = 2; emp.Y = 1;
                var analyst = state.Units.First(u => u.Type == "ana"); analyst.X = 3; analyst.Y = 0;
                state.Spawns.Clear();
                source.OpenForValidation(state);
                Assert.IsTrue(source.Current.CanRetreat);
                Assert.IsTrue(source.Current.IsLootMission);
                Assert.AreEqual(Loc.T("İkmal kasası"), source.Current.Targets.Single().Name);
                source.Select('T');
                Assert.AreEqual(Loc.T("İkmal alma"), source.Current.UnitRows[1].Text);
                source.Arm();
                source.Use(new Vector2Int(3, 3));
                // Kasa açıldı: kaçış başlar, bütün ekip girişten çıkmalı; geri çekilme yok
                Assert.IsFalse(source.Current.Finished);
                Assert.IsTrue(source.Current.Escaping);
                Assert.IsFalse(source.Current.CanRetreat);
                Assert.AreEqual(Loc.T("Amaç: malzemeyle girişten çık"), source.Current.Goal);
                // Düzenleme modunda EMRİ İLET makineleri oynatmaz: tur motorda biter (makineler devre dışı)
                foreach (var e in state.Units.Where(u => u.Side == 'e')) e.Dead = true;
                Assert.IsTrue(Baskin.EndTurn(state));
                source.Select('T');
                source.Move(new Vector2Int(0, 2));
                // Teknisyen malzemeyle çıktı: kartı tahliye edildi, seçilemez; baskın öteki uzmanlar çıkana kadar sürer
                Assert.IsFalse(source.Current.Finished);
                Assert.IsTrue(source.Current.Team.First(t => t.Letter == 'T').Out);
                Assert.AreEqual(Loc.T("Amaç: ekibi girişten çıkar"), source.Current.Goal);
                source.Select('T');
                Assert.AreNotEqual('T', source.Selected);
                source.Select('E');
                source.Move(new Vector2Int(0, 1));
                Assert.IsFalse(source.Current.Finished);
                source.Select('A');
                source.Move(new Vector2Int(0, 0));
                Assert.IsTrue(source.Current.Finished);
                Assert.IsTrue(source.Current.Success);
                Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), source.Current.ResultTitle);
                Assert.IsTrue(source.Current.Targets.Single().Broken);
                Assert.IsTrue(source.Current.ResultRows.Any(r => r.Label == Loc.T("Tahliye edilen") && r.Value == "3/3" && r.Good));
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>Kartlar ve 1–3 sırası gerçek kadrodan; Madenci'nin ve Sis uzmanının yetenek satırı kalan hakla.</summary>
        [Test]
        public void PickedSquad_DrivesTeamCardsAndAbilityRows()
        {
            var root = new GameObject("Seçilen kadro sunumu");
            root.SetActive(false);
            try
            {
                var source = root.AddComponent<LiveRaidDataSource>();
                var state = Baskin.Create(new RaidParams
                {
                    Seed = 11, Roles = new System.Collections.Generic.List<string> { "tek", "maden", "duman" },
                    Squad = new System.Collections.Generic.List<SquadSlot> { new SquadSlot { Type = "tek", Hp = 3 }, new SquadSlot { Type = "maden", Hp = 3 }, new SquadSlot { Type = "duman", Hp = 2 } },
                });
                source.OpenForValidation(state);
                CollectionAssert.AreEqual(new[] { 'T', 'M', 'S' }, source.Current.Team.Select(t => t.Letter).ToArray());
                Assert.AreEqual(Loc.T("MADENCİ"), source.Current.Team[1].Name);
                source.Select('M');
                Assert.AreEqual(Loc.T("Dinamit"), source.Current.UnitRows[1].Text);
                Assert.AreEqual(Loc.F("bitişik · {0} dinamit", 2), source.Current.UnitRows[1].Detail);
                source.Select('S');
                Assert.AreEqual(Loc.F("düz hat, 1–4 · {0} bomba", 3), source.Current.UnitRows[1].Detail);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
