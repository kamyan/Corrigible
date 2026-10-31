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
    /// <summary>
    /// Sığınağın kapısı (Revizyon 2 · S4): kâğıdın evreleri (kapalı, açık, sonuç), gözlem blokları, bedel/etki ve gerekçe,
    /// yetersiz kaynak, ölümcül sonuç, kart → siluet tablosu, vardiya raporu satırı, sahne bağlantıları.
    /// </summary>
    public class SiginakDoorTests
    {
        static Simulation New()
        {
            var sim = Simulation.NewGame(77, new Balance { startFon = 100 });
            while (sim.S.Day < 3) sim.Step();
            sim.S.Door.Visit = null;
            return sim;
        }

        static void Knock(Simulation sim, string card) => sim.S.Door.Visit = new DoorVisit { Day = sim.S.Day, Card = card };

        [Test]
        public void ClosedDoor_OffersPeekAskOpenAndRefuse_AndRevealsWhatWasSeen()
        {
            var sim = New();
            Knock(sim, "anne");
            var card = DoorCards.Get("anne");
            var d = DoorViewBuilder.Build(sim, DoorPhase.Closed, sim.DoorVisitor, false, false);
            Assert.AreEqual(new[] { DoorViewBuilder.Peek, DoorViewBuilder.Ask, Simulation.DoorOpen, Simulation.DoorRefuse }, d.Buttons.Select(b => b.Id).ToArray());
            Assert.IsTrue(d.Buttons.All(b => b.Enabled && b.Used == null && b.Detail == null), "kapalı kapıda bedel ve etki yazmaz");
            Assert.AreEqual(DeskText.Upper(Loc.T(card.Who)), d.Title);
            Assert.AreEqual(Loc.T(card.Knock), d.Body, "bakmadan ve sormadan yalnız duyulan");
            Assert.IsEmpty(d.Observations);
            Assert.AreEqual(Loc.F("VARDİYA {0:000}", sim.S.Day + 1), d.Code);
            Assert.AreEqual("Esc", d.HintKey);
            Assert.IsNull(d.Action, "seçim yapılmadan geniş düğme yok");

            d = DoorViewBuilder.Build(sim, DoorPhase.Closed, sim.DoorVisitor, true, true);
            Assert.AreEqual(Loc.T(card.Knock), d.Body, "görülen ve duyulan ayrı bloklarda");
            Assert.AreEqual(new[] { Loc.T(card.Peek), Loc.T(card.Ask) }, d.Observations.Select(o => o.Text).ToArray());
            Assert.AreEqual(Loc.T("GÖZETLEME DELİĞİ"), d.Observations[0].Label);
            Assert.IsFalse(d.Observations[0].Intercom);
            Assert.IsTrue(d.Observations[1].Intercom, "interkom cevabı daktiloyla");
            Assert.AreEqual(Loc.T("BAKILDI"), d.Buttons[0].Used, "bir kez bakılır");
            Assert.IsFalse(d.Buttons[0].Enabled);
            Assert.AreEqual(Loc.T("SORULDU"), d.Buttons[1].Used, "bir kez sorulur");
            Assert.IsFalse(d.Buttons[1].Enabled);
        }

        [Test]
        public void ArmedVisitor_CanBeTalkedToThroughTheDoor()
        {
            var sim = New();
            Knock(sim, "silahli");
            var ids = DoorViewBuilder.Build(sim, DoorPhase.Closed, sim.DoorVisitor, false, false).Buttons.Select(b => b.Id).ToArray();
            Assert.AreEqual(new[] { DoorViewBuilder.Peek, DoorViewBuilder.Ask, "konus", Simulation.DoorOpen, Simulation.DoorRefuse }, ids);
        }

        [Test]
        public void OpenDoor_ShowsPricesAndWhyAnOptionIsClosed()
        {
            var sim = New();
            Knock(sim, "anne");
            Assert.IsTrue(sim.AnswerDoor(Simulation.DoorOpen).Ok);
            sim.Shelter.Supplies = 3;
            var d = DoorViewBuilder.Build(sim, DoorPhase.Open, sim.DoorVisitor, true, false);
            Assert.AreEqual(Loc.T(DoorCards.Get("anne").Opened), d.Body);
            string price = Loc.F("{0} erzak", DeskFormat.Signed(-8, 0));
            // Yetersiz kaynak: satır açık kalır (basınca komut denenir), bedel üstü çizik + “erzak yetersiz”
            var give = d.Buttons.Single(b => b.Id == "erzak");
            Assert.AreEqual(SlipButtonState.Short, give.State);
            Assert.IsTrue(give.Enabled);
            Assert.IsNotNull(sim.DoorAnswerCheck("erzak"));
            Assert.AreEqual("<s>" + price + "</s> · " + Loc.T("erzak yetersiz"), give.Detail);
            // Koşulu tutmayan seçenek kapalı; neden sağda küçük harfle ve noktasız
            var clinic = d.Buttons.Single(b => b.Id == "revir");
            Assert.AreEqual(SlipButtonState.Disabled, clinic.State);
            Assert.IsFalse(clinic.Enabled);
            string why = Loc.T("Revirde görevli yok.").TrimEnd('.');
            Assert.AreEqual(DeskText.Lower(why.Substring(0, 1), DeskFormat.Lang) + why.Substring(1), clinic.Detail);
            sim.Shelter.Supplies = 20;
            d = DoorViewBuilder.Build(sim, DoorPhase.Open, sim.DoorVisitor, true, false);
            give = d.Buttons.Single(b => b.Id == "erzak");
            Assert.AreEqual(SlipButtonState.Normal, give.State);
            Assert.AreEqual(price, give.Detail, "bedel düğmede");
            // Açık kapıda güvene etkisi de sağda (S4b: “güven −1”)
            Assert.AreEqual(Loc.F("güven {0}", DeskFormat.Signed(-1, 0)), d.Buttons.Single(b => b.Id == "yok").Detail);
        }

        [Test]
        public void Result_ShowsTheOutcomeAndItsEffects_AndADeadlyDoorHoldsTheFinalReport()
        {
            var sim = New();
            Knock(sim, "anne");
            var visit = sim.DoorVisitor;
            sim.AnswerDoor(Simulation.DoorRefuse);
            var d = DoorViewBuilder.Build(sim, DoorPhase.Result, visit, false, false);
            StringAssert.Contains(Loc.T(DoorCards.Get("anne").Ignore.Text), d.Body);
            StringAssert.Contains(Loc.F("Güven {0}", DeskFormat.Signed(-1, 0)), d.Body);
            Assert.AreEqual(Loc.F("SEÇİM · {0} {1}", 4, DeskText.Upper(Loc.T("Açma"))), d.Choice, "bak, sor, aç, açma: 4");
            Assert.IsEmpty(d.Buttons, "sonuçta seçenek yok, tek geniş düğme");
            Assert.AreEqual(DoorViewBuilder.Close, d.ActionId);
            Assert.AreEqual(Loc.T("KAPIYI KAPAT"), d.Action);
            Assert.IsFalse(d.Fatal);
            Assert.IsNull(d.Stamp);

            Knock(sim, "kopya");
            visit = sim.DoorVisitor;
            sim.AnswerDoor(Simulation.DoorOpen);
            Assert.AreEqual("sizma", sim.S.Ending);
            d = DoorViewBuilder.Build(sim, DoorPhase.Result, visit, true, true);
            Assert.IsTrue(d.Fatal);
            Assert.AreEqual(DeskText.Upper(GameData.Ending("sizma").Title), d.Stamp);
            Assert.IsNotNull(d.Epilogue);
            Assert.AreEqual(Loc.F("SEÇİM · {0} {1}", 3, DeskText.Upper(Loc.T("Kapıyı aç"))), d.Choice);
            Assert.IsEmpty(d.Buttons, "ölümcülde yalnız SON RAPORA GEÇ");
            Assert.AreEqual(DoorViewBuilder.Ending, d.ActionId);
            Assert.AreEqual(Loc.T("SON RAPORA GEÇ"), d.Action);
            Assert.IsNull(d.Hint);
            var state = new DeskBridgeState { HoldEnding = true };
            Assert.IsNull(new SimDeskBuilder().Build(sim, state).FinalReport, "kâğıt kapanana kadar son rapor bekler");
            state.HoldEnding = false;
            var final = new SimDeskBuilder().Build(sim, state).FinalReport;
            Assert.IsNotNull(final);
            Assert.AreEqual(DeskText.Upper(Loc.T(GameData.Ending("sizma").Title)), final.Reason, "son raporda SIZMA neden damgası");
            Assert.AreEqual(Loc.F("Kapı açıldı · {0}", Loc.T(DoorCards.Get("kopya").Who)), final.Moments.Last().Text, "son kırılma anı kapı olayı");
            Assert.IsTrue(final.Moments.Last().Alert);
        }

        [Test]
        public void ArmedVisitor_EndsInShot_FinalReportNamesTheDoor()
        {
            var sim = New();
            Knock(sim, "silahli");
            sim.AnswerDoor(Simulation.DoorOpen);
            Assert.AreEqual("vuruldu", sim.S.Ending);
            var final = FinalReportBuilder.Build(sim);
            Assert.AreEqual(Loc.T("KAYBEDİLDİ"), final.Stamp);
            Assert.AreEqual(DeskText.Upper(Loc.T(GameData.Ending("vuruldu").Title)), final.Reason);
            Assert.AreEqual(Loc.F("Kapı açıldı · {0}", Loc.T(DoorCards.Get("silahli").Who)), final.Moments.Last().Text);
            Assert.AreEqual(Loc.F("V.{0:000}", sim.S.Day + 1), final.Moments.Last().Code);
        }

        [Test]
        public void EveryCard_HasASilhouette_AndNobodyCardsShowNone()
        {
            var kinds = new[] { VisitorSilhouette.Anne, VisitorSilhouette.Yasli, VisitorSilhouette.Silahli, VisitorSilhouette.Asker,
                VisitorSilhouette.Yarali, VisitorSilhouette.Kopya, VisitorSilhouette.Generic, VisitorSilhouette.Child };
            foreach (var c in DoorCards.All)
            {
                Assert.IsTrue(DoorViewBuilder.HasSilhouette(c.Id), c.Id + " için siluet eşlemesi yok (IMPLEMENTATION_MAP §4)");
                var s = DoorViewBuilder.Silhouette(c.Id);
                if (c.Nobody) Assert.IsEmpty(s, c.Id + ": kapıda kimse yok");
                else Assert.That(s.Length, Is.InRange(1, 3), c.Id);
                foreach (var k in s) CollectionAssert.Contains(kinds, k, c.Id);
            }
            // Altı tür de bir karta düşer; ölümcül kartlar kendi biçimiyle
            foreach (var k in kinds.Take(6)) Assert.IsTrue(DoorCards.All.Any(c => DoorViewBuilder.Silhouette(c.Id).Contains(k)), k);
            Assert.AreEqual(VisitorSilhouette.Kopya, DoorViewBuilder.Silhouette("kopya").Single());
            Assert.AreEqual(VisitorSilhouette.Silahli, DoorViewBuilder.Silhouette("silahli").Single());
        }

        [Test]
        public void SilhouetteVariation_IsSmallAndStablePerVisit()
        {
            var seen = new System.Collections.Generic.HashSet<bool>();
            for (int day = 0; day < 40; day++)
            {
                var v = new DoorVisit { Day = day, Card = "anne" };
                var (h, m) = DoorViewBuilder.Variation(v);
                Assert.That(h, Is.InRange(0.96f, 1.04f), "boy ±%4");
                Assert.AreEqual((h, m), DoorViewBuilder.Variation(new DoorVisit { Day = day, Card = "anne" }), "aynı ziyaret aynı görünür");
                seen.Add(m);
            }
            Assert.AreEqual(2, seen.Count, "aksesuar iki tarafa da düşer");
        }

        [Test]
        public void ShiftReport_ListsTheVisitorLeftAtTheDoor()
        {
            var sim = New();
            Knock(sim, "kurye");
            var before = ShiftReportBuilder.Capture(sim);
            sim.Step();
            var input = ShiftReportBuilder.Input(sim, before, ShiftReportBuilder.Ledger(sim), null);
            Assert.AreEqual("kurye", input.door.Single().card);
            var d = ShiftReportBuilder.Build(input);
            var row = d.Teams.Last();
            Assert.AreEqual(Loc.F("Kapı · {0}", Loc.T(DoorCards.Get("kurye").Who)), row.Label);
            Assert.AreEqual(Loc.T("AÇILMADI"), row.Value);
            // Kurye koliyi kapıda bıraktı: erzak satırı kapıyı ayrı gösterir, toplam simülasyonla aynı
            var door = d.Production.Single(r => r.Label == Loc.T("Kapı · açılmadı"));
            Assert.AreEqual(ShiftReportBuilder.Delta(input.door.Single().supplies, 1), door.Value);
            Assert.IsFalse(d.Production.Any(r => r.Label == Loc.T("Erzak tükendi")), "kapının erzağı düzeltme satırına düşmez");
        }

        [Test]
        public void DeskGameScene_DoorIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var door = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDoor>(true)).Single();
                var so = new SerializedObject(door);
                foreach (var f in new[] { "game", "surface", "source", "rig", "telex", "prop", "card", "target", "soundPoint", "view", "bulb", "tally" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var roomLights = so.FindProperty("roomLights");
                Assert.GreaterOrEqual(roomLights.arraySize, 2, "ölümcülde kırmızıya dönen tavan ampulü ve banker lambası");
                for (int i = 0; i < roomLights.arraySize; i++) Assert.IsNotNull(roomLights.GetArrayElementAtIndex(i).objectReferenceValue);
                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                Assert.AreSame(door, new SerializedObject(surface).FindProperty("siginakDoor").objectReferenceValue);

                var card = (DoorCardView)so.FindProperty("card").objectReferenceValue;
                Assert.IsFalse(card.gameObject.activeSelf, "kâğıt kapalı başlar");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, card.GetComponentInParent<Canvas>(true).renderMode);
                var cs = new SerializedObject(card);
                var slots = cs.FindProperty("buttons");
                Assert.GreaterOrEqual(slots.arraySize, DoorCards.All.Max(c => c.Options.Length));
                Assert.GreaterOrEqual(slots.arraySize, DoorCards.All.Max(c => c.Closed.Length) + 4, "bak, sor, kapalı seçenekler, aç, açma");
                foreach (var f in new[] { "sheet", "paper", "shadow", "group", "head", "code", "title", "choice", "body", "epilogue", "eyeIcon",
                             "intercomIcon", "stampBlock", "stamp", "stampText", "optionBlock", "actionBlock", "action", "actionHover", "actionFace",
                             "actionShadow", "actionLabel", "hintRow", "hintKey", "hint", "frameSolid", "frameDashed" })
                    Assert.IsNotNull(cs.FindProperty(f).objectReferenceValue, f);
                Assert.AreEqual(2, cs.FindProperty("observations").arraySize, "gözetleme ve interkom blokları");
                for (int i = 0; i < slots.arraySize; i++)
                    foreach (var f in new[] { "root", "layout", "button", "hover", "face", "fill", "edge", "shadow", "keyBox", "keyFill", "key", "label", "detail", "used", "usedFrame", "usedText" })
                        Assert.IsNotNull(slots.GetArrayElementAtIndex(i).FindPropertyRelative(f).objectReferenceValue, "seçenek " + (i + 1) + " · " + f);
                // S4: Overlay, sol 40 · üst 64, 470 px, dönüş üst ortadan
                var sheet = (RectTransform)cs.FindProperty("sheet").objectReferenceValue;
                Assert.AreEqual(new Vector2(0, 1), sheet.anchorMin);
                Assert.AreEqual(new Vector2(0.5f, 1), sheet.pivot);
                Assert.AreEqual(470, sheet.sizeDelta.x, 0.5f);
                Assert.AreEqual(new Vector2(40 + 235, -64), sheet.anchoredPosition);

                var prop = (DoorProp)so.FindProperty("prop").objectReferenceValue;
                var ps = new SerializedObject(prop);
                foreach (var f in new[] { "leaf", "wheel", "lamp", "lampOn", "lampOff", "lampFatal", "lampLight", "peepCover", "intercomLed", "ledOn", "ledOff", "silhouette" })
                    Assert.IsNotNull(ps.FindProperty(f).objectReferenceValue, f);
                Assert.IsFalse(((Light)ps.FindProperty("lampLight").objectReferenceValue).enabled, "lamba yalnız biri çalınca");
                var stairs = ps.FindProperty("stairLights");
                Assert.GreaterOrEqual(stairs.arraySize, 2, "merdiven boşluğunda iki ışık");
                for (int i = 0; i < stairs.arraySize; i++)
                    Assert.IsFalse(((Light)stairs.GetArrayElementAtIndex(i).objectReferenceValue).enabled, "merdiven ışığı yalnız kapı açılınca");
                // Altı tür + genel biçim (kalabalık için en az üç); kapı kapalıyken hiçbiri görünmez
                var silhouette = (VisitorSilhouette)ps.FindProperty("silhouette").objectReferenceValue;
                var figures = new SerializedObject(silhouette).FindProperty("figures");
                var byKind = Enumerable.Range(0, figures.arraySize).Select(i => figures.GetArrayElementAtIndex(i))
                    .Select(e => (kind: e.FindPropertyRelative("kind").stringValue, root: (Transform)e.FindPropertyRelative("root").objectReferenceValue)).ToList();
                foreach (var k in new[] { VisitorSilhouette.Anne, VisitorSilhouette.Yasli, VisitorSilhouette.Silahli, VisitorSilhouette.Asker,
                             VisitorSilhouette.Yarali, VisitorSilhouette.Kopya })
                    Assert.AreEqual(1, byKind.Count(f => f.kind == k), k);
                Assert.GreaterOrEqual(byKind.Count(f => f.kind == VisitorSilhouette.Generic), 3, "heyet ve çocuklar için üç genel biçim");
                Assert.IsTrue(byKind.All(f => f.root != null && !f.root.gameObject.activeSelf), "siluet kapı kapalıyken görünmez");
                Assert.GreaterOrEqual(new SerializedObject(silhouette).FindProperty("slots").arraySize, 3);

                // Ölümcülde kenar satırının tuşları kesik ve soluk (S4c)
                var edge = new SerializedObject(roots.SelectMany(g => g.GetComponentsInChildren<EdgeLine>(true)).Single());
                Assert.AreEqual(edge.FindProperty("chips").arraySize, edge.FindProperty("chipKeys").arraySize);
                Assert.IsNotNull(edge.FindProperty("chipGroup").objectReferenceValue);
                Assert.IsNotNull(edge.FindProperty("keyDashed").objectReferenceValue);
                var target = (DeskTarget)so.FindProperty("target").objectReferenceValue;
                Assert.AreEqual("SIĞINAK KAPISI", target.Title);
                Assert.AreEqual("K", target.KeyHint);

                var rig = (SiginakCameraRig)so.FindProperty("rig").objectReferenceValue;
                Assert.IsTrue(rig.TryGetPose(SiginakPose.Door, out var pose), "kapı pozu");
                // Kapı kâğıdın sağında, ekranda görünür
                var cam = (Camera)so.FindProperty("view").objectReferenceValue;
                rig.Snap(SiginakPose.Door);
                var p = cam.WorldToViewportPoint(prop.transform.position + Vector3.up * 1.0f);
                rig.Snap(SiginakPose.Desk);
                Assert.That(p.x, Is.InRange(0.45f, 0.8f), "kapı kâğıdın sağında");
                Assert.That(p.y, Is.InRange(0.2f, 0.8f));
                Assert.Greater(p.z, 0);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
