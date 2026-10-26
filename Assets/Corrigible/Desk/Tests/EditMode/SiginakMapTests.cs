using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 5: kurtuluş haritasının durumları, fiş içerikleri, figür kuralları ve sahne bağlantıları.</summary>
    public class SiginakMapTests
    {
        static Simulation NewLiberation(double fon = 100) => Simulation.NewGame(null, "manipulator", 731, new Balance { startFon = fon },
            rules: new RuleOptions { LiberationCampaign = true });

        /// <summary>V.014'ün ilk yarısı: Batı Avrupa'nın üç sektörü alınır, yeni cephe henüz seçilmez.</summary>
        static Simulation WeuFreed()
        {
            var sim = NewLiberation();
            foreach (var id in new[] { "weu_1", "weu_2", "weu_3" })
            {
                Assert.IsTrue(sim.StartLiberationRaid(id).Ok, id);
                Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok, id);
            }
            return sim;
        }

        [Test]
        public void MapState_NewGame_FrontIsShelterRegionAndTeamStandsThere()
        {
            var sim = NewLiberation();
            var s = LiberationMapBuilder.Build(sim);
            Assert.AreEqual(Simulation.HQ, s.Front);
            Assert.IsTrue(s.FrontOpen);
            Assert.AreEqual(MapRegionKind.Front, s.Find(Simulation.HQ).Kind);
            Assert.AreEqual(100, s.Find(Simulation.HQ).Occupation, 1e-9);
            Assert.IsTrue(s.Regions.Where(r => r.Id != Simulation.HQ).All(r => r.Kind == MapRegionKind.Occupied), "cephe açıkken aday yok");
            Assert.AreEqual(GameData.Regions.Length, s.Regions.Count);
            Assert.IsTrue(s.Team.Ready);
            Assert.AreEqual(Simulation.HQ, s.Team.Region, "açılışta S-07 ile cephe aynı yer");
            Assert.IsFalse(s.Team.AtShelter);
        }

        [Test]
        public void MapState_FreedRegion_StampsAndDrawsFrontierCandidates()
        {
            var sim = WeuFreed();
            var s = LiberationMapBuilder.Build(sim, pendingCandidate: "eeu");
            Assert.IsFalse(s.FrontOpen);
            var weu = s.Find("weu");
            Assert.AreEqual(MapRegionKind.Free, weu.Kind);
            Assert.AreEqual(sim.S.Day + 1, weu.FreeShift, "damga: özgürleştiği vardiya");
            Assert.IsTrue(weu.FreshStamp);
            var candidates = s.Regions.Where(r => r.Kind == MapRegionKind.Candidate).Select(r => r.Id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(sim.LiberationFrontier.OrderBy(x => x).ToArray(), candidates, "adaylar = simülasyonun sınırı");
            Assert.IsTrue(s.Find("eeu").Selected, "seçili aday sarı");
            Assert.AreEqual("weu", s.Team.Region, "bölge özgür: figür özgürleşen bölgede bekler");
        }

        [Test]
        public void MapState_RaidLiftsFigure_FailureSendsItToShelterWithPawnsDown()
        {
            var sim = WeuFreed();
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.IsTrue(sim.StartLiberationRaid("eeu_1").Ok);
            Assert.IsTrue(LiberationMapBuilder.Build(sim).Team.Hidden, "baskında figür haritadan kalkar");
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Down = 1 }).Ok);
            var team = LiberationMapBuilder.Build(sim).Team;
            Assert.IsTrue(team.AtShelter);
            Assert.AreEqual(Simulation.HQ, team.Region);
            Assert.AreEqual(1, team.Down, "düşen üye kadar piyon yatık");
            Assert.AreEqual(sim.B.liberationInjuryDays, team.Days);
            StringAssert.Contains(sim.B.liberationInjuryDays.ToString(), TelexReasons.Pick(sim), "yaralı ekip alınamaz: gün sayısıyla");
        }

        [Test]
        public void Slip_Front_MarksSectorsAndKeepsReadyWithoutFunds()
        {
            var sim = NewLiberation(5);
            var d = LiberationMapBuilder.Slip(sim, Simulation.HQ);
            Assert.IsTrue(d.HasAction);
            CollectionAssert.AreEqual(new[] { SectorMark.Ready, SectorMark.Locked, SectorMark.Locked }, d.Sectors.Select(x => x.Mark).ToArray(),
                "Karar 3: fon eksik olsa da ilk sektör BASKINA HAZIR");
            Assert.IsTrue(d.Sectors[0].Highlight);
            Assert.IsFalse(d.ActionEnabled);
            StringAssert.Contains(DeskFormat.Number(sim.B.liberationRaidCost, 0), d.Reason, "gerekçe sayı içerir");
            Assert.AreEqual(Loc.F("HAREKÂT FORMU · OP-{0:00}", 1), d.Code);
            Assert.AreEqual(Loc.F("{0} fon", DeskFormat.Signed(-sim.B.liberationRaidCost, 0)), d.ActionCost);

            var rich = NewLiberation();
            Assert.IsTrue(LiberationMapBuilder.Slip(rich, Simulation.HQ).ActionEnabled);
        }

        [Test]
        public void Slip_OtherRegions_AreInformationOnly()
        {
            var sim = NewLiberation();
            var info = LiberationMapBuilder.Slip(sim, "cn");
            Assert.IsFalse(info.HasAction);
            Assert.AreEqual(Loc.T("Cephe açmak için komşusunu özgürleştir."), info.Info);
            Assert.IsTrue(info.Sectors.All(x => x.Mark == SectorMark.Locked));

            var freed = WeuFreed();
            Assert.IsTrue(freed.SelectLiberationRegion("eeu").Ok);
            var neighbor = LiberationMapBuilder.Slip(freed, "na");
            StringAssert.Contains(Loc.T(GameData.Region("eeu").Name), neighbor.Info, "cephe açıkken sınırdaki bölge: tek cephe kuralı");
            var free = LiberationMapBuilder.Slip(freed, "weu");
            Assert.IsFalse(free.HasAction);
            Assert.IsTrue(free.Sectors.All(x => x.Mark == SectorMark.Retaken));
        }

        [Test]
        public void FrontSlip_EnabledOnlyForCandidates()
        {
            var sim = WeuFreed();
            var ok = LiberationMapBuilder.Front(sim, "eeu");
            Assert.IsTrue(ok.ActionEnabled);
            StringAssert.Contains(Loc.T(GameData.Region("weu").Name), ok.Neighbor);
            Assert.AreEqual(3, ok.Sectors.Count);
            var far = LiberationMapBuilder.Front(sim, "oc");
            Assert.IsFalse(far.ActionEnabled);
            Assert.IsNotNull(far.Reason);

            // K3 notu: cephe açıkken aday seçimi teleks hatası verir (“cephe zaten açık (Doğu Avrupa 0/3)”)
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.AreEqual(Loc.F("{0}: cephe zaten açık ({1} {2}/{3})", Loc.T(GameData.Region("na").Name), Loc.T(GameData.Region("eeu").Name), 0, 3),
                TelexReasons.Front(sim, "na"));
        }

        [Test]
        public void Drop_FollowsFrontRules()
        {
            var sim = NewLiberation();
            Assert.IsNull(TelexReasons.Drop(sim, Simulation.HQ), "cephe açıkken kendi cephesi geçerli");
            Assert.AreEqual(Loc.F("Cephe açık: {0} {1}/{2}", Loc.T(GameData.Region(Simulation.HQ).Name), 0, 3), TelexReasons.Drop(sim, "na"));
            Assert.IsNotNull(TelexReasons.Drop(sim, null), "deniz: geçersiz");

            var freed = WeuFreed();
            Assert.IsNull(TelexReasons.Drop(freed, "eeu"), "aday geçerli");
            Assert.IsNull(TelexReasons.Drop(freed, "weu"), "kendi yerine bırakmak geçerli");
            Assert.AreEqual(Loc.F("{0}: özgür bölgeye komşu değil", Loc.T(GameData.Region("oc").Name)), TelexReasons.Drop(freed, "oc"));
            Assert.IsNull(TelexReasons.Pick(freed));
        }

        [Test]
        public void Fixture_V014_MatchesReferenceState()
        {
            var sim = NewLiberation(60);
            SiginakFixtures.ApplyV014(sim);
            var s = LiberationMapBuilder.Build(sim);
            Assert.AreEqual(13, sim.S.Day, "vardiya 014");
            Assert.AreEqual("eeu", s.Front);
            Assert.AreEqual(1, s.Find("eeu").Captured);
            Assert.AreEqual(MapRegionKind.Free, s.Find("weu").Kind);
            Assert.AreEqual(1, s.Regions.Count(r => r.Kind == MapRegionKind.Free));
            Assert.IsTrue(s.Team.Ready);
        }

        [Test]
        public void DeskGameScene_LiberationMapIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var map = roots.SelectMany(g => g.GetComponentsInChildren<SiginakMap>(true)).Single();
                var so = new SerializedObject(map);
                foreach (var f in new[] { "game", "surface", "rig", "telex", "sadeMap", "layer", "slip", "frontSlip", "figure", "drag", "figureTarget" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var desk = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDesk>(true)).Single();
                Assert.AreSame(map, new SerializedObject(desk).FindProperty("map").objectReferenceValue, "Enter/Esc önce harita fişine");
                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                Assert.AreSame(map, new SerializedObject(surface).FindProperty("siginakMap").objectReferenceValue);

                var layer = roots.SelectMany(g => g.GetComponentsInChildren<LiberationMapLayer>(true)).Single();
                Assert.AreEqual(GameData.Regions.Length, layer.Regions.Count);
                foreach (var m in layer.Regions)
                {
                    Assert.IsNotNull(m.Hit, m.RegionId);
                    Assert.IsTrue(m.Hit.ContainsMapPoint(layer.TeamAnchor(m.RegionId)), m.RegionId + " ekip noktası kendi bölgesinde olmalı");
                }

                Assert.IsTrue(layer.gameObject.activeSelf, "kurtuluş katmanı sahnede açık (tek sunum)");
                Assert.IsTrue(map.transform.root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Liberation fills" && t.gameObject.activeSelf));
                var figure = map.transform.root.GetComponentsInChildren<TeamFigure>(true).Single();
                Assert.Greater(figure.GetComponent<BoxCollider>().size.x, 0.1f, "tık hedefi K1'de de ≥ 44 px");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
