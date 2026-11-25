using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Üretilmiş harita (anahtar 1) gerçek ekran ve kamerayla: <see cref="MapPaperPlayTests"/>'in haritaya tık testinin
    /// karşılığı. Kara ve deniz noktaları sabit değil, harekâtın haritasından hesaplanır (hücre merkezleri; etiket, damga,
    /// ekip, S-07 ve lejanttan uzak). Anahtar <see cref="BoardPlayKit.OverrideMapGen"/> ile yalnız bu sahnede açılır.
    /// </summary>
    public sealed class GeneratedMapPlayTests
    {
        [UnitySetUp] public IEnumerator SetUp() { yield return BoardPlayKit.LoadDesk(BoardPlayKit.DefaultSeed, mapGen: 1); }
        [UnityTearDown] public IEnumerator TearDown() { yield return BoardPlayKit.TearDown(); }

        [UnityTest]
        public IEnumerator GeneratedMap_CanvasClick_OpensMissions_AndLandSelectsRegion()
        {
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var layer = Object.FindFirstObjectByType<LiberationMapLayer>();
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            var world = BoardPlayKit.Sim.Map;
            Assert.IsNotNull(world, "anahtar 1: harekâtın üretilmiş haritası");
            Assert.AreSame(world, layer.Map, "katman harekâtın haritasını çiziyor");
            Assert.IsNotNull(layer.Geometry);

            // Kâğıt: dünya dokuları gizli, üretilmiş katmanlar açık
            var root = layer.transform.parent;
            Assert.IsFalse(root.Find("MapBase").gameObject.activeSelf, "map_base gerçek kıyıyı taşır: gizli");
            Assert.IsFalse(root.Find("MapLines").gameObject.activeSelf, "map_lines gerçek sınırları taşır: gizli");
            foreach (var n in new[] { "GenPaper", "GenHalo", "GenLines" }) Assert.IsTrue(root.Find(n).gameObject.activeSelf, n);

            var (landPoint, region) = LandPoint(layer, map.State);
            var seaPoint = SeaPoint(layer);
            Assert.AreEqual(region.RegionId, layer.RegionAt(layer.MapToWorld(landPoint)));
            Assert.IsNull(layer.RegionAt(layer.MapToWorld(seaPoint)), "deniz noktası hiçbir bölgede değil");
            var before = BoardPlayKit.Snapshot();

            ClickMap(seaPoint, null);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            Assert.AreEqual(map.State.Front, slip.RegionId);
            Assert.IsTrue(map.HandleEscape());
            yield return BoardPlayKit.WaitSeconds(.5f);
            Assert.IsFalse(map.SlipOpen);
            ClickMap(seaPoint, null);
            Assert.AreEqual(map.State.Front, slip.RegionId);

            ClickMap(landPoint, region.Hit);
            Assert.AreEqual(region.RegionId, slip.RegionId);
            BoardPlayKit.Rig.ResetView();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            ClickMap(landPoint, region.Hit);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            Assert.AreEqual(map.State.Front, slip.RegionId);
            Assert.AreEqual(before, BoardPlayKit.Snapshot(), "haritaya tık komut yazmaz");

            void ClickMap(Vector2 point, RegionMeshGraphic land)
            {
                var screen = BoardPlayKit.ScreenOfWorld(layer.MapToWorld(point));
                var hit = BoardPlayKit.FirstHit(screen);
                Assert.IsNotNull(hit.gameObject, point.ToString());
                if (land != null) Assert.AreSame(land.gameObject, hit.gameObject, point + " kara noktası bölgenin dolgusuna düşmeli");
                else Assert.IsNotNull(hit.gameObject.GetComponent<DeskHitArea>(), point + " deniz noktası: " + hit.gameObject.name);
                Assert.IsTrue(BoardPlayKit.ClickHit(hit));
                Assert.IsTrue(map.SlipOpen);
            }
        }

        [UnityTest]
        public IEnumerator NewCampaign_RebuildsTheMap_AndTheTeamStandsInItsRegion()
        {
            var layer = Object.FindFirstObjectByType<LiberationMapLayer>();
            var first = layer.Geometry;
            Assert.IsNotNull(first);
            BoardPlayKit.Game.NewGame(5);
            yield return BoardPlayKit.Frames(2);
            var world = BoardPlayKit.Sim.Map;
            Assert.AreSame(world, layer.Map, "yeni harekâtın haritası çizildi");
            Assert.AreNotSame(first, layer.Geometry, "geometri yeni harekâtta yeniden kuruldu");
            foreach (var m in layer.Regions)
                Assert.AreEqual(m.RegionId, layer.RegionAt(layer.MapToWorld(layer.TeamAnchor(m.RegionId))), m.RegionId + " ekip noktası kendi bölgesinde");
            var shelter = layer.transform.Find("S-07") as RectTransform;
            var p = layer.ShelterPoint;
            Assert.AreEqual(new Vector2(p.x, -p.y), shelter.anchoredPosition, "S-07 yerleşimin seçtiği yerde (karargâhta)");
            Assert.AreEqual(Simulation.HQ, layer.RegionAt(layer.MapToWorld(p)));
        }

        /// <summary>
        /// Kayıttan devam (menünün yolu; DeskGame yeniden yüklenir, lejant ilk karede yeniden ölçülebilir): aynı harita, S-07 ve
        /// lejant aynı yerde, karargâhın ad, cephe bloğu ve figür noktası aynı. Dil değişince de kımıldamazlar (yerleşim iki dilin
        /// büyüğüyle yapılır).
        /// </summary>
        [UnityTest]
        public IEnumerator ContinueSaved_KeepsTheMap_TheShelterAndTheHqLayout_InBothLanguages()
        {
            var layer = Object.FindFirstObjectByType<LiberationMapLayer>();
            yield return BoardPlayKit.Frames(2);
            string fingerprint = layer.Map.Fingerprint;
            var shelter = layer.ShelterPoint;
            var legend = layer.Layout.Legend;
            var hq = layer.Layout.Of(Simulation.HQ);
            (Vector2 label, LiberationRegionMarks.StripSide side, Vector2 offset, bool pin, Vector2 team) before = (hq.Label, hq.StripSide, hq.StripOffset, hq.PinRight, hq.Team);
            BoardPlayKit.Game.Save();

            yield return BoardPlayKit.ContinueDesk();
            yield return BoardPlayKit.Frames(3);
            var previous = DeskLoc.Instance.Lang;
            try
            {
                foreach (var lang in new[] { DeskLang.En, DeskLang.Tr })
                {
                    DeskLoc.Instance.SetLanguage(lang);
                    yield return BoardPlayKit.Frames(3);
                    layer = Object.FindFirstObjectByType<LiberationMapLayer>();
                    Assert.AreEqual(fingerprint, layer.Map.Fingerprint, lang + ": aynı harita");
                    Assert.AreEqual(shelter, layer.ShelterPoint, lang + ": S-07 aynı yerde");
                    Assert.AreEqual(legend, layer.Layout.Legend, lang + ": lejant aynı köşede");
                    var slip = layer.transform.Find("S-07") as RectTransform;
                    Assert.AreEqual(new Vector2(shelter.x, -shelter.y), slip.anchoredPosition, lang + ": S-07 fişi yerleşimin noktasında");
                    var now = layer.Layout.Of(Simulation.HQ);
                    Assert.AreEqual(before, (now.Label, now.StripSide, now.StripOffset, now.PinRight, now.Team), lang + ": karargâhın yerleşimi aynı");
                }
            }
            finally { DeskLoc.Instance.SetLanguage(previous); }
        }

        /// <summary>İşgal altındaki (cephe ve aday olmayan) bir bölgenin, işaretlerden en uzak kara hücresinin merkezi.</summary>
        static (Vector2, LiberationRegionMarks) LandPoint(LiberationMapLayer layer, LiberationMapState state)
        {
            var world = layer.Map;
            var avoid = Avoid(layer);
            (Vector2 p, LiberationRegionMarks m, float score) best = (default, null, -1);
            foreach (var m in layer.Regions)
            {
                var s = state.Find(m.RegionId);
                if (s == null || s.Kind != MapRegionKind.Occupied) continue;
                foreach (int ci in world.Region(m.RegionId).Cells)
                {
                    var p = GeneratedMapGeometry.Center(world.Cells[ci]);
                    if (!Central(p) || layer.LegendRect.Contains(p)) continue;
                    float score = avoid.Min(a => Vector2.Distance(a, p));
                    if (score > best.score) best = (p, m, score);
                }
            }
            Assert.IsNotNull(best.m, "işgal altında, merkezde bir kara hücresi yok");
            Assert.Greater(best.score, 60f, "kara noktası işaretlere çok yakın");
            return (best.p, best.m);
        }

        /// <summary>Haritanın ortasına en yakın, işaretlerden uzak bir deniz hücresinin merkezi.</summary>
        static Vector2 SeaPoint(LiberationMapLayer layer)
        {
            var world = layer.Map;
            var avoid = Avoid(layer);
            var center = MapProjection.DesignSize / 2;
            var seas = world.Cells.Where(c => !c.Land).Select(GeneratedMapGeometry.Center)
                .Where(p => Central(p) && !layer.LegendRect.Contains(p) && avoid.Min(a => Vector2.Distance(a, p)) > 90)
                .OrderBy(p => Vector2.Distance(p, center)).ToList();
            Assert.IsNotEmpty(seas, "merkezde işaretlerden uzak deniz hücresi yok");
            return seas[0];
        }

        static bool Central(Vector2 p) => p.x > 300 && p.x < 1650 && p.y > 200 && p.y < 860;

        static List<Vector2> Avoid(LiberationMapLayer layer)
        {
            var list = new List<Vector2> { layer.ShelterPoint };
            foreach (var m in layer.Regions)
            {
                list.Add(m.LabelPoint);
                list.Add(m.StampPoint);
                list.Add(layer.TeamAnchor(m.RegionId));
            }
            return list;
        }
    }
}
