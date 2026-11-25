using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Rehberin "şuraya bas" katmanı (geliştirici, 2026-11-24: "tutorial ekleyelim başlangıca adım adım göstererek ilk baskın bitene kadar
    /// oyunu tanıtsın, şuraya bas şuraya bas gibi, ilk defa olan her şeyi tanıtsın"): baskın adımlarının ilerleyişe göre geçmesi, "ilk kez"
    /// notları, adım hedefleri (sahnedeki işaret, pano alanı, baskın karesi), vardiya şalterinin ilk baskın mührü ve sahne bağları.
    /// </summary>
    public sealed class TutorialGuideTests
    {
        static void At(PlanningTutorialFlow f, string id) => Assert.AreEqual(id, f.Current?.Id);

        /// <summary>Oyuncu rehberden hızlı giderse (ya da devamda baskın ilerlemişse) bilgi adımları da kendiliğinden geçer: ikinci paftaya
        /// varan oyuncu "ikinci pafta" notunda bulunur.</summary>
        [Test]
        public void RaidProgress_SkipsPastStepsIncludingInfo()
        {
            var f = new PlanningTutorialFlow();
            var c = new PlanningTutorialContext { RaidOrdered = true, RaidLoaded = true };
            f.Update(c); At(f, "raidBoard");
            c.RaidProgress = 2; f.Update(c); At(f, "raidMiddle");
            c.RaidProgress = 4; f.Update(c); At(f, "raidEscape");
            c.RaidProgress = 5; c.RaidOver = true; f.Update(c); At(f, "result");
        }

        /// <summary>Yetenek adımı isteğe bağlıdır: ilk turda hedef olmayabilir, SONRAKİ ile de geçilir.</summary>
        [Test]
        public void AbilityStep_CanBeAcknowledged()
        {
            var f = new PlanningTutorialFlow(PlanningTutorialFlow.IndexOf("raidAbility"));
            var c = new PlanningTutorialContext { RaidOrdered = true, RaidLoaded = true, RaidSelected = true, RaidMoved = true };
            f.Update(c); At(f, "raidAbility");
            Assert.IsTrue(f.Current.Info);
            f.Acknowledge(); f.Update(c); At(f, "raidTurn");
        }

        [Test]
        public void FirstNotes_AreUniqueTopicalAndSeparateFromSteps()
        {
            var ids = PlanningTutorialFlow.FirstNotes.Select(n => n.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.IsEmpty(ids.Intersect(PlanningTutorialFlow.Steps.Select(s => s.Id)));
            foreach (var n in PlanningTutorialFlow.FirstNotes)
            {
                Assert.IsNotNull(n.Topic, n.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(n.Title) || string.IsNullOrWhiteSpace(n.Body), n.Id);
            }
            var c = new PlanningTutorialContext();
            Assert.IsFalse(PlanningTutorialFlow.FirstNotes.Any(n => n.Topic(c)), "boş bağlamda not yok");
            c.RaidLastTurn = true;
            Assert.AreEqual("firstLastTurn", PlanningTutorialFlow.FirstNotes.First(n => n.Topic(c)).Id);
            c.RaidDown = true;
            Assert.AreEqual("firstDown", PlanningTutorialFlow.FirstNotes.First(n => n.Topic(c)).Id, "öncelik: düşen uzman");
        }

        /// <summary>Her adımın hedefi bilinen bir sahne işaretidir (ya da pano alanı / baskın karesi); masa ve baskın adımlarının çoğu bir
        /// yer gösterir.</summary>
        [Test]
        public void EveryStepTarget_UsesAKnownAnchor()
        {
            var known = TutorialTargets.Anchors.ToList();
            int pointed = 0;
            var contexts = new[]
            {
                new PlanningTutorialContext(), new PlanningTutorialContext { BoardOpen = true }, new PlanningTutorialContext { SlipOpen = true },
                new PlanningTutorialContext { SquadOpen = true, SlipOpen = true },
            };
            foreach (var s in PlanningTutorialFlow.Steps)
            {
                bool any = false;
                foreach (var c in contexts)
                {
                    var spec = TutorialTargets.For(s, c, null, null);
                    if (spec == null) continue;
                    any = true;
                    if (spec.Anchor != null) CollectionAssert.Contains(known, spec.Anchor, s.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(spec.Label), s.Id + ": etiket");
                }
                if (any) pointed++;
            }
            Assert.GreaterOrEqual(pointed, 18, "adımların çoğu bir yer gösterir");
            foreach (var id in new[] { "board", "person", "assign", "back", "slip", "order", "raidSelect", "raidTurn", "result", "report", "print", "file" })
                Assert.IsTrue(contexts.Any(c => TutorialTargets.For(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf(id)], c, null, null) != null), id + " bir yer gösterir");
            var order = PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("order")];
            Assert.AreEqual(TutorialTargets.SlipOrder, TutorialTargets.For(order, new PlanningTutorialContext { SlipOpen = true }, null, null).Anchor);
            Assert.AreEqual(TutorialTargets.SquadGo, TutorialTargets.For(order, new PlanningTutorialContext { SlipOpen = true, SquadOpen = true }, null, null).Anchor);
        }

        /// <summary>Baskın karelerinin hedefleri gerçek ilk kanattan seçilir: yürünecek kare seçili uzmanın ulaşabildiği ve kapıya
        /// yaklaştıran bir karedir; kapı karesi geçittendir; niyetli makine tahtadadır; kasa hedef odasındadır.</summary>
        [Test]
        public void RaidTargets_ComeFromTheRealBoard()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var B = RaidChain.Open(sim.B, sim.FacilityPlan, sim.S.Raid.Params, sim.S.Raid.Reports);
            var u = B.Units.First(v => Baskin.IsSpecialist(v));
            var hint = TutorialTargets.MoveHint(B, u);
            Assert.IsNotNull(hint);
            Assert.IsTrue(Baskin.Reachable(B, u).Any(p => p.X == hint.Value.X && p.Y == hint.Value.Y), "ulaşılabilir");
            var gates = Baskin.GateTiles(B);
            int Near(int x, int y) => gates.Min(g => System.Math.Abs(g.X - x) + System.Math.Abs(g.Y - y));
            Assert.LessOrEqual(Near(hint.Value.X, hint.Value.Y), Near(u.X, u.Y), "kapıya yaklaştırır");
            var gate = TutorialTargets.GateTile(B);
            Assert.IsTrue(gates.Any(g => g.X == gate.Value.X && g.Y == gate.Value.Y));
            var intent = TutorialTargets.IntentTile(B);
            if (intent.HasValue) Assert.IsTrue(B.Units.Any(m => m.Side == 'e' && !m.Dead && m.X == intent.Value.X && m.Y == intent.Value.Y), "niyetli makine");
            Assert.IsNull(TutorialTargets.CrateTile(B), "giriş odasında kasa yok");

            var move = PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("raidMove")];
            var c = new PlanningTutorialContext { RaidLoaded = true };
            Assert.AreEqual(TutorialTargets.RaidTeam1, TutorialTargets.For(move, c, B, null).Anchor, "seçim yokken önce seç");
            Assert.AreEqual(hint, TutorialTargets.For(move, c, B, u).Tile);
            u.Moved = true;
            Assert.IsNull(TutorialTargets.MoveHint(B, u), "yürüdü: kare yok");
        }

        /// <summary>Önceki sürümün kaydı yalnız adım dizinini taşır: eski dizin eski tablodan kimliğe çevrilir (araya adım eklendi).</summary>
        [Test]
        public void LegacyIndex_MapsToTheSameStep()
        {
            Assert.AreEqual(PlanningTutorialFlow.IndexOf("rest"), PlanningTutorialFlow.FromLegacyIndex(14));
            Assert.AreEqual(PlanningTutorialFlow.IndexOf("print"), PlanningTutorialFlow.FromLegacyIndex(15));
            Assert.AreEqual(PlanningTutorialFlow.IndexOf("exposure"), PlanningTutorialFlow.FromLegacyIndex(23));
            Assert.AreEqual(PlanningTutorialFlow.IndexOf("raidBoard"), PlanningTutorialFlow.FromLegacyIndex(11), "eski tek baskın adımı");
            Assert.AreEqual(0, PlanningTutorialFlow.FromLegacyIndex(-1));
            Assert.AreEqual(0, PlanningTutorialFlow.FromLegacyIndex(999));
        }

        /// <summary>Pano kapanırsa pano adımları SIĞINAK tuşunu, pano açıkken masa adımları MASAYA DÖN'ü gösterir; ara oda bir bekleme
        /// adımıdır (SONRAKİ ile hedef odasının metnine atlanmaz).</summary>
        [Test]
        public void Targets_FollowWhereThePlayerIs()
        {
            PlanningTutorialStep S(string id) => PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf(id)];
            var closed = new PlanningTutorialContext();
            var open = new PlanningTutorialContext { BoardOpen = true };
            Assert.AreEqual(TutorialTargets.KeyShelter, TutorialTargets.For(S("person"), closed, null, null).Anchor);
            Assert.AreEqual(TutorialTargets.KeyShelter, TutorialTargets.For(S("assign"), closed, null, null).Anchor);
            Assert.AreEqual("cook", TutorialTargets.For(S("assign"), open, null, null).Pocket);
            foreach (var id in new[] { "order", "print", "file" })
                Assert.AreEqual(TutorialTargets.BoardBack, TutorialTargets.For(S(id), open, null, null).Anchor, id);
            Assert.IsFalse(S("raidMiddle").Info, "ara oda bekleme adımı");
        }

        /// <summary>Rehberin ilk baskını bitene dek vardiya şalteri mühürlüdür; baskın sürerken baskının nedeni önce gelir.</summary>
        [Test]
        public void ShiftLock_TutorialSealsUntilTheFirstRaid()
        {
            Loc.SetLanguage(Loc.Turkish);
            var (plate, line) = SiginakShiftClose.LockReason(false, false, false, false, false, null, null, true);
            Assert.AreEqual("rehber · önce baskın", plate);
            StringAssert.Contains("EĞİTİMİ BİTİR", line);
            Assert.AreEqual("baskın sürüyor", SiginakShiftClose.LockReason(true, false, false, false, false, null, null, true).plate);
            Assert.IsNull(SiginakShiftClose.LockReason(false, false, false, false, true, null, null, true).plate, "basılı fişte kilit yok");
            Assert.IsNull(SiginakShiftClose.LockReason(false, false, false, false, false, null, null, false).plate);
        }

        /// <summary>Sahne bağları: DeskGame'de işaretçi rehbere bağlı, masa hedefleri sahnede; baskının hedefleri HUD ve panel prefab'larında.</summary>
        [Test]
        public void Scene_AnchorsAndPointerAreWired()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
                var tutorial = all.OfType<SiginakTutorial>().Single();
                var so = new SerializedObject(tutorial);
                Assert.IsNotNull(so.FindProperty("pointer").objectReferenceValue, "işaretçi bağlı");
                Assert.IsNotNull(so.FindProperty("deskCamera").objectReferenceValue, "masa kamerası bağlı");
                var ids = all.OfType<TutorialAnchor>().Select(a => a.Id).ToList();
                foreach (var id in TutorialTargets.DeskAnchors) CollectionAssert.Contains(ids, id, "DeskGame: " + id);
                var pointer = all.OfType<TutorialPointerView>().Single();
                Assert.IsTrue(pointer.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget), "işaretçi tıklamayı yutmaz");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
            var raid = new[] { "Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidHud.prefab", "Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidPanels.prefab" }
                .SelectMany(p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentsInChildren<TutorialAnchor>(true)).Select(a => a.Id).ToList();
            foreach (var id in TutorialTargets.RaidAnchors) CollectionAssert.Contains(raid, id, "baskın: " + id);
        }
    }
}
