using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 7: CRT durumları, bekleme ve geçiş metinleri, ızgaranın baskın kamerasına oturması, sahne bağlantıları.</summary>
    public class SiginakCrtTests
    {
        static Simulation NewLiberation(double fon = 100) => LegacySimulation.NewGame(731, new Balance { shelterEventsEnabled = false, startFon = fon });

        [Test]
        public void State_FollowsRaidLifecycle()
        {
            var sim = NewLiberation();
            Assert.AreEqual(CrtState.Waiting, CrtViewBuilder.State(sim, false));
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(CrtState.Establishing, CrtViewBuilder.State(sim, false), "emir verildi, sahne açılmadı");
            Assert.AreEqual(CrtState.Live, CrtViewBuilder.State(sim, true));
            // Teknisyen düşer, yerine çıkacak yedek yok: ekip baskına çıkamaz
            sim.Shelter.Reserves.Clear();
            FacilityDeskKit.ResolveRaid(sim, false, FacilityDeskKit.Dies);
            Assert.AreEqual(CrtState.Cut, CrtViewBuilder.State(sim, false), "bu vardiyada baskın: sonuç teleks");
            sim.Step();
            Assert.AreEqual(CrtState.Disabled, CrtViewBuilder.State(sim, false), "Teknisyen yaralı");
            var screen = CrtViewBuilder.Screen(sim, false);
            Assert.IsTrue(screen.TeamInverse, "yaralı ekip satırı ters video (renk değil biçim)");
            StringAssert.Contains(Loc.F("{0} · TEKNİSYEN YARALI · {1} GÜN", DeskText.Upper(LiberationMapBuilder.TeamName(sim.S.Teams[0])),
                LiberationMapBuilder.TechnicianDown(sim.S.Teams[0])), screen.TeamLine);
        }

        /// <summary>
        /// Alarm buçuklu ilerler (varsayılan vardiya payı 0,5, tek kip denge turu): CRT ondalığı yazar, tam sayıya yuvarlamaz; 9,5
        /// "10/10" görünse eşik dolmuş sanılır. Tam değer ondalıksız kalır.
        /// </summary>
        [Test]
        public void Alarm_HalfStep_ShowsTheDecimal_NotRoundedUpToTheThreshold()
        {
            var sim = NewLiberation();
            Assert.AreEqual(0.5, sim.B.liberationAlarmPerShift, "varsayılan vardiya payı");
            sim.S.Alarm = 9.5;
            Assert.AreEqual(Loc.F("YZ ALARMI {0}/{1}", DeskFormat.Number(9.5, 1), DeskFormat.Number(10, 0)), CrtViewBuilder.Alarm(sim));
            StringAssert.DoesNotContain("10/10", CrtViewBuilder.Alarm(sim));
            sim.S.Alarm = 7;
            Assert.AreEqual(Loc.F("YZ ALARMI {0}/{1}", "7", "10"), CrtViewBuilder.Alarm(sim));
        }

        [Test]
        public void Screen_Waiting_ShowsFourLines()
        {
            var sim = NewLiberation();
            var d = CrtViewBuilder.Screen(sim, false);
            Assert.AreEqual(Loc.T("DİNLEMEDE"), d.Title);
            // Revizyon 2 · S1: kısa cephe satırı (“VERITY / 01”; aşama 3: YZ adı), ipucu yok
            Assert.AreEqual(Loc.F("{0} / {1:00}", CrtViewBuilder.ShortRegion(sim, Simulation.HQ), 1), d.FrontLine);
            Assert.IsFalse(d.TeamInverse);
            Assert.IsNull(d.Hint);
            Assert.IsNotNull(d.Sub, "YZ ALARMI satırı");
        }

        /// <summary>Revizyon 2 · S1: defterde son dört kayıt; hattın geçmişi baskın kaydından, en altta bugünkü hat durumu.</summary>
        [Test]
        public void RadioLog_LastFourEntriesFromRaidHistory()
        {
            var sim = NewLiberation();
            var first = RadioLogBuilder.Entries(sim, false);
            Assert.AreEqual(Loc.T("Dinlemede — emir bekleniyor"), first[first.Count - 1].text);
            Assert.AreEqual(string.Format("{0:000}/02", sim.S.Day + 1), first[first.Count - 1].code, "sayaçtaki vardiya; bugünün ikinci satırı (ekip hazır, sonra hat)");
            string name = LiberationMapBuilder.TeamName(sim.S.Teams[0]);
            Assert.AreEqual(Loc.F("{0} hazır · cephe {1}", name, RadioLogBuilder.Short(sim, Simulation.HQ)), first[0].text);

            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(Loc.T("Hat açık — ekip sahada"), RadioLogBuilder.Entries(sim, true).Last().text);
            int day = sim.S.Day;
            FacilityDeskKit.ResolveRaid(sim, true, null, FacilityDeskKit.Civilians(0, 1));
            var after = RadioLogBuilder.Entries(sim, false);
            Assert.LessOrEqual(after.Count, RadioLogBuilder.Shown);
            Assert.AreEqual(Loc.F("{0} döndü · {1} alındı", name, Loc.F("{0} {1:00}", RadioLogBuilder.Short(sim, "weu"), 1)), after[0].text);
            Assert.AreEqual(Loc.F("{0} sivil sunucu kırıldı", 1), after[1].text);
            Assert.AreEqual(Loc.T("Hat kapandı — sonuç tekste"), after.Last().text);
            Assert.IsTrue(after.All(e => e.code.StartsWith((day + 1).ToString("000") + "/")), "hepsi bu vardiyanın kaydı");
            // Sonraki vardiyalarda eski kayıtlar yukarı kayar, en fazla dört satır kalır
            for (int i = 0; i < 3; i++) sim.Step();
            var later = RadioLogBuilder.Entries(sim, false);
            Assert.AreEqual(RadioLogBuilder.Shown, later.Count);
            Assert.AreEqual(Loc.T("Dinlemede — emir bekleniyor"), later.Last().text);
        }

        /// <summary>Bölgenin CRT adı YZ adının büyük harfli hâlidir (aşama 3): tek sözcük, kısaltılmaz.</summary>
        [Test]
        public void ShortRegion_IsTheUpperAiName()
        {
            var sim = NewLiberation();
            foreach (var r in GameData.Regions)
                Assert.AreEqual(DeskText.Upper(GameData.Region(r.Id).Ai), CrtViewBuilder.ShortRegion(sim, r.Id), r.Id);
        }

        [Test]
        public void Transition_NamesSectorAndMission()
        {
            var sim = NewLiberation();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            var d = CrtViewBuilder.Transition(sim);
            Assert.AreEqual(Loc.F("V.{0:000} · OP-{1:00}", 1, 1), d.Code);
            StringAssert.Contains(DeskText.Upper(Loc.T("Teknik depo")), d.Target);
            Assert.AreEqual(Loc.F("SAHA MASASI · {0} · GÖREV: {1}", DeskText.Upper(Loc.T("Banka veri merkezi")), Loc.T("YAĞMA")), d.Camera, "masadaki pafta binanın maketi");
            Assert.AreEqual(Loc.T("HAT AÇILIYOR"), d.Title, "sığınak ağa bağlanmaz: saha telsizle konuşur");
        }

        [Test]
        public void DeskGameScene_CrtIsWired_AndGridMatchesRaidCamera()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var crt = roots.SelectMany(g => g.GetComponentsInChildren<SiginakCrt>(true)).Single();
                var so = new SerializedObject(crt);
                foreach (var f in new[] { "game", "screen", "overlay" }) Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var link = roots.SelectMany(g => g.GetComponentsInChildren<SiginakRaidLink>(true)).Single();
                Assert.AreSame(crt, new SerializedObject(link).FindProperty("transition").objectReferenceValue, "baskın geçiş kancası");

                // Geçiş katmanı oda kökünün dışında (baskında oda gizlenir), kapalı başlar, odanın HUD'unun üstünde
                var overlay = (CrtTransition)so.FindProperty("overlay").objectReferenceValue;
                Assert.IsNull(overlay.transform.parent, "sahnenin kökünde");
                Assert.IsFalse(overlay.gameObject.activeSelf);
                Assert.Greater(overlay.GetComponent<Canvas>().sortingOrder, 45);
                Assert.IsNotNull(overlay.transform.Find("Tube").GetComponent<RectMask2D>());

                // tokens_sade › raid.camera.screenFromGrid1080: x = 210 + (u + v) · 93,75 ; y = 528 + (v − u) · 53,775
                var lines = overlay.transform.Find("Tube/Grid").GetComponentsInChildren<UILineGraphic>(true);
                Assert.AreEqual(18, lines.Length, "8 × 8 ızgara: iki yönde 9'ar çizgi");
                var points = new SerializedObject(lines[0]).FindProperty("points");
                var a = points.GetArrayElementAtIndex(0).vector2Value;
                var b = points.GetArrayElementAtIndex(1).vector2Value;
                Assert.Less(Vector2.Distance(new Vector2(210 - 960, 540 - 528), a), 0.01f, "(0,0) köşesi ekran (210, 528)");
                Assert.Less(Vector2.Distance(new Vector2(960 - 960, 540 - 958.2f), b), 0.01f, "(0,8) köşesi ekran (960, 958,2)");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
