using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using K = Corrigible.Desk.Tests.KosDeskKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 6 (sunum · ifşa): İFŞA şeridi (<see cref="ExposurePanel"/>), üzerine gelme satırı, teleksin ifşa satırları (günlük
    /// toplamıyla), CRT ipucunun önceliği, devir fişinin İFŞA ve SIĞINAK EKSİK kutuları ve kâğıttaki EŞİK etiketi. Hepsi Core'un public
    /// durumundan, saf okuma.
    /// </summary>
    public sealed class Rev3ExposurePanelTests
    {
        readonly List<Object> made = new List<Object>();

        [SetUp] public void Turkish() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);

        [TearDown]
        public void Clean()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
            made.Clear();
        }

        // ---------------------------------------------------------------- şerit

        [Test]
        public void CellsFor_ClipsAtThreshold_AndScalesWhenThresholdDiffers()
        {
            Assert.AreEqual(0, ExposurePanel.CellsFor(0, 6, 6));
            Assert.AreEqual(3, ExposurePanel.CellsFor(3, 6, 6));
            Assert.AreEqual(6, ExposurePanel.CellsFor(6, 6, 6));
            Assert.AreEqual(6, ExposurePanel.CellsFor(8, 6, 6), "sayaç tavansız; şerit eşikte kırpılır");
            Assert.AreEqual(2, ExposurePanel.CellsFor(1, 4, 6), "eşik 4: orantı yukarı yuvarlanır");
            Assert.AreEqual(6, ExposurePanel.CellsFor(4, 4, 6));
            Assert.AreEqual(1, ExposurePanel.CellsFor(1, 10, 6));
            Assert.AreEqual(6, ExposurePanel.CellsFor(9, 10, 6), "9/10: 5,4 yukarı yuvarlanır");
            Assert.AreEqual(5, ExposurePanel.CellsFor(8, 10, 6));
            Assert.AreEqual(0, ExposurePanel.CellsFor(3, 0, 6), "eşik kapalı");
            Assert.AreEqual(0, ExposurePanel.BeaconFor(5, 6, false));
            Assert.AreEqual(1, ExposurePanel.BeaconFor(6, 6, false));
            Assert.AreEqual(2, ExposurePanel.BeaconFor(6, 6, true));
        }

        ExposurePanel Panel(out Renderer[] cells, out Renderer lamp, out Material[] mats)
        {
            var root = new GameObject("Exposure test");
            made.Add(root);
            cells = new Renderer[6];
            for (int i = 0; i < 6; i++) cells[i] = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
            lamp = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
            foreach (var c in cells) { c.transform.SetParent(root.transform); }
            lamp.transform.SetParent(root.transform);
            mats = Enumerable.Range(0, 6).Select(i => new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")) { name = "m" + i }).ToArray();
            made.AddRange(mats);
            var p = root.AddComponent<ExposurePanel>();
            var so = new SerializedObject(p);
            var arr = so.FindProperty("cells");
            arr.arraySize = 6;
            for (int i = 0; i < 6; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
            so.FindProperty("cellEmpty").objectReferenceValue = mats[0];
            so.FindProperty("cellEmptyHigh").objectReferenceValue = mats[1];
            so.FindProperty("cellLow").objectReferenceValue = mats[2];
            so.FindProperty("cellHigh").objectReferenceValue = mats[3];
            so.FindProperty("lampOn").objectReferenceValue = mats[4];
            so.FindProperty("lampOff").objectReferenceValue = mats[5];
            so.FindProperty("beacon").objectReferenceValue = lamp;
            so.ApplyModifiedPropertiesWithoutUndo();
            return p;
        }

        [Test]
        public void Show_CreamBelowAlarmAt_RedFromIt_AndBeaconLevels()
        {
            var p = Panel(out var cells, out var lamp, out var m);
            p.Show(2, 6, 3, false, true, "x");
            Assert.AreEqual(2, p.LitCount, "Edit modda canlandırma yok: anında");
            Assert.AreSame(m[2], cells[0].sharedMaterial, "1. hücre krem");
            Assert.AreSame(m[2], cells[1].sharedMaterial, "2. hücre krem");
            Assert.AreSame(m[1], cells[2].sharedMaterial, "3. hücre boş ve kırmızı bölgede (kesik kenar)");
            Assert.AreSame(m[1], cells[5].sharedMaterial, "eşik hücresi boş: kesik kırmızı kenar");
            Assert.AreEqual(0, p.BeaconLevel);
            Assert.AreSame(m[5], lamp.sharedMaterial, "KONUM lambası sönük");

            p.Show(4, 6, 3, false, true, "x");
            Assert.AreSame(m[3], cells[2].sharedMaterial, "alarmAt (3) ve üstü kırmızı");
            Assert.AreSame(m[3], cells[3].sharedMaterial);

            p.Show(6, 6, 3, false, false, "x");
            Assert.AreEqual(6, p.LitCount);
            Assert.AreEqual(1, p.BeaconLevel, "eşikte lamba yanık");
            Assert.AreSame(m[4], lamp.sharedMaterial);
            p.Show(8, 6, 3, true, false, "x");
            Assert.AreEqual(6, p.LitCount, "tavansız sayaç kırpılır");
            Assert.AreEqual(2, p.BeaconLevel, "son vardiya: yanıp söner");
        }

        [Test]
        public void Hint_ThreeStates_NumbersFromBalance([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = K.New(tweak: b => { b.exposureThreshold = 7; b.exposureWarnAt1 = 3; b.exposureWarnAt2 = 5; b.exposureAlarmAt = 4; });
            var B = sim.B;
            Assert.IsTrue(ShelterEventSnapshotBuilder.ExposureVisible(sim));
            string low = ShelterEventSnapshotBuilder.ExposureHint(sim);
            Assert.AreEqual(Loc.F("İfşa {0}. Görmezden gelinen iz {1}, sabotaj {2}, sorgu {3}. Teleks uyarısı: {4} ve {5}. Eşik {6}: konum çözülür; izle varılırsa {9} vardiyan, sabotajla bir vardiyan kalır. {7} ve üstünde alarm her vardiya {8} fazla artar.",
                Loc.F("{0}/{1}", 0, 7), DeskFormat.Signed(B.exposureIgnoredTrace, 0), DeskFormat.Signed(B.exposureSabotage, 0), DeskFormat.Signed(B.exposureInterrogation, 0),
                3, 5, 7, 4, DeskFormat.Signed(B.exposureAlarmBonus, 1), B.exposureDecisionWarnShifts), low);
            StringAssert.Contains("7", low, "eşik Balance'tan");
            sim.Shelter.Exposure = 7;
            Assert.AreEqual(Loc.F("İfşa {0}: eşikte. Bu kapanışta konum çözülür; sonraki kapanışa kadar ifşa düşmezse sığınak düşer.", Loc.F("{0}/{1}", 7, 7)),
                ShelterEventSnapshotBuilder.ExposureHint(sim));
            Assert.AreEqual(1, ShelterEventSnapshotBuilder.ExposureLevel(sim));
            sim.Shelter.Exposure = 9;
            StringAssert.Contains(Loc.F("{0} (eşik {1})", 9, 7), ShelterEventSnapshotBuilder.ExposureHint(sim), "eşiği aşınca gerçek değer");
            sim.Shelter.FallPending = true;
            // 1. vardiyada son şalter mühürlü: metin onu seçenek olarak önermez
            Assert.AreEqual(Loc.T("KONUM ÇÖZÜLDÜ · bu vardiya son vardiya: son baskın ya da sorgu."), ShelterEventSnapshotBuilder.ExposureHint(sim));
            sim.S.Day = B.lastSwitchMinShift - 1;
            Assert.AreEqual(Loc.T("KONUM ÇÖZÜLDÜ · bu vardiya son vardiya: son baskın, sorgu ya da son şalter."), ShelterEventSnapshotBuilder.ExposureHint(sim),
                "son şalterin vardiyasında üç seçenek");
            Assert.AreEqual(Loc.T("KONUM ÇÖZÜLDÜ · bu vardiya son vardiya: son baskın ya da sorgu."), ShelterEventSnapshotBuilder.ExposureHint(sim, tutorialSeal: true),
                "rehber şalteri mühürlüyken vardiyası gelse de önermez");
            Assert.AreEqual(2, ShelterEventSnapshotBuilder.ExposureLevel(sim));
            if (lang == DeskLang.En) Assert.IsFalse(ShelterEventSnapshotBuilder.ExposureHint(sim).Any(c => "çğışÇĞİŞ".IndexOf(c) >= 0), "İngilizcede Türkçe harf yok");

            // İkinci tur kararı: kararla varılan eşikte uyarı iki vardiya (FallGrace); metinler kalan vardiyayı söyler
            sim.Shelter.FallGrace = 1;
            Assert.AreEqual(2, sim.FallShiftsLeft);
            Assert.AreEqual(Loc.F("KONUM ÇÖZÜLDÜ · bu vardiya dahil {0} vardiya: son baskın, sorgu ya da son şalter.", 2), ShelterEventSnapshotBuilder.ExposureHint(sim));
            Assert.AreEqual(Loc.F("KONUM ÇÖZÜLDÜ · {0} VARDİYA KALDI", 2), ShelterEventSnapshotBuilder.CrtHint(sim));
            sim.Shelter.FallPending = false;
            Assert.AreEqual(Loc.F("İfşa {0}: eşikte. Bu kapanışta konum çözülür; ifşa düşmezse sığınak ondan {1} kapanış sonra düşer.", Loc.F("{0} (eşik {1})", 9, 7), 2),
                ShelterEventSnapshotBuilder.ExposureHint(sim));
            if (lang == DeskLang.En) Assert.IsFalse(ShelterEventSnapshotBuilder.ExposureHint(sim).Any(c => "çğışÇĞİŞ".IndexOf(c) >= 0), "İngilizcede Türkçe harf yok");
        }

        /// <summary>
        /// A4 kabul ölçütü: kâğıttaki "İfşa +n" bedeli şeridin değeriyle satırda okunur (kâğıt elde şeridi örttüğü için): "İfşa 2/6 → 3/6.",
        /// alarm ara bedelinin sınırı geçilirse alarm satırı, sorguda iniş ve bedelin kalkması; ifşayı değiştirmeyen seçenekte yok.
        /// </summary>
        [Test]
        public void Preview_OnPaperRow_FromToAndAlarmCrossing([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = K.New();
            var B = sim.B;
            string V(int e) => e <= B.exposureThreshold ? Loc.F("{0}/{1}", e, B.exposureThreshold) : Loc.F("{0} (eşik {1})", e, B.exposureThreshold);
            sim.Shelter.Exposure = B.exposureAlarmAt - B.exposureIgnoredTrace;
            int e0 = sim.Shelter.Exposure, e1 = e0 + B.exposureIgnoredTrace;
            Assert.AreEqual(Loc.F("{0} Alarm her vardiya {1}.", Loc.F("İfşa {0} → {1}.", V(e0), V(e1)), DeskFormat.Signed(B.exposureAlarmBonus, 1)),
                ShelterEventSnapshotBuilder.ExposurePreview(sim, "yoksay"), "alarm sınırını geçen iz");
            sim.Shelter.Exposure = 0;
            Assert.AreEqual(Loc.F("İfşa {0} → {1}.", V(0), V(B.exposureIgnoredTrace)), ShelterEventSnapshotBuilder.ExposurePreview(sim, "yoksay"));
            Assert.AreEqual(Loc.F("İfşa {0} → {1}.", V(0), V(0)), ShelterEventSnapshotBuilder.ExposurePreview(sim, "sorgula"), "sıfırın altına inmez");
            sim.Shelter.Exposure = B.exposureAlarmAt;
            Assert.AreEqual(Loc.F("{0} Alarm ara bedeli kalkar.", Loc.F("İfşa {0} → {1}.", V(B.exposureAlarmAt), V(B.exposureAlarmAt + B.exposureInterrogation))),
                ShelterEventSnapshotBuilder.ExposurePreview(sim, "sorgula"));
            Assert.IsNull(ShelterEventSnapshotBuilder.ExposurePreview(sim, "tecrit"), "ifşayı değiştirmeyen seçenek");
            Assert.IsNull(ShelterEventSnapshotBuilder.ExposurePreview(K.New(tweak: b => b.exposureThreshold = 0), "yoksay"), "şerit yokken önizleme yok");
            if (lang == DeskLang.En)
                Assert.IsFalse(ShelterEventSnapshotBuilder.ExposurePreview(sim, "sorgula").Any(c => "çğışÇĞİŞ".IndexOf(c) >= 0));
        }

        /// <summary>"Konum çözüldü" teleksi de son şalteri ancak vardiyası gelince önerir.</summary>
        [Test]
        public void LocationTelex_OffersLastSwitchOnlyFromItsShift()
        {
            var sim = K.New();
            var g = new ShelterEventLogEntry { Shift = sim.S.Day, Kind = "konum" };
            Assert.AreEqual(Loc.T("YZ sığınağın yerini buldu. Bu vardiya son: son baskın ya da sorgu."), ShelterEventSnapshotBuilder.TelexLine(sim, g).Value.Message);
            sim.S.Day = sim.B.lastSwitchMinShift - 1;
            Assert.AreEqual(Loc.T("YZ sığınağın yerini buldu. Bu vardiya son: son baskın, sorgu ya da son şalter."), ShelterEventSnapshotBuilder.TelexLine(sim, g).Value.Message);
            // Kararla varılan eşik: günlüğün miktarı uyarının süresi
            g.Amount = 2;
            Assert.AreEqual(Loc.F("YZ sığınağın yerini buldu. {0} vardiyan var: son baskın, sorgu ya da son şalter.", 2), ShelterEventSnapshotBuilder.TelexLine(sim, g).Value.Message);
            g.Amount = 1;
            Assert.AreEqual(Loc.T("YZ sığınağın yerini buldu. Bu vardiya son: son baskın, sorgu ya da son şalter."), ShelterEventSnapshotBuilder.TelexLine(sim, g).Value.Message);
            // Rehber sürerken şalter "mühürlü · rehber": teleks onu önermez
            Assert.AreEqual(Loc.T("YZ sığınağın yerini buldu. Bu vardiya son: son baskın ya da sorgu."), ShelterEventSnapshotBuilder.TelexLine(sim, g, -1, tutorialSeal: true).Value.Message);
        }

        [Test]
        public void Hidden_WhenEventsOff_OrThresholdOff()
        {
            var off = K.New(tweak: b => b.shelterEventsEnabled = false);
            Assert.IsFalse(ShelterEventSnapshotBuilder.ExposureVisible(off));
            Assert.IsNull(ShelterEventSnapshotBuilder.ExposureHint(off));
            var noThreshold = K.New(tweak: b => b.exposureThreshold = 0);
            Assert.IsFalse(ShelterEventSnapshotBuilder.ExposureVisible(noThreshold));
            Assert.AreEqual(0, ShelterEventSnapshotBuilder.ExposureLevel(noThreshold));
        }

        // ---------------------------------------------------------------- teleks

        [Test]
        public void Telex_RunningSum_WarnLinesAndLocation([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = K.New();
            var T = sim.B.exposureThreshold;
            K.Expose(sim, "iz", 1);
            K.Expose(sim, "iz", 1);          // 2: warnAt1
            K.Expose(sim, "sabotaj", 2);     // 4: warnAt2
            K.Expose(sim, "sorgula", -1);    // 3
            K.Expose(sim, "iz", 1);          // 4 (yeniden warnAt2)
            K.Expose(sim, "sabotaj", 2);     // 6: eşik
            sim.ShelterEvents.Log.Add(new ShelterEventLogEntry { Shift = sim.S.Day, Kind = "konum" });
            K.Expose(sim, "sorgula", -1);
            sim.ShelterEvents.Log.Add(new ShelterEventLogEntry { Shift = sim.S.Day, Kind = "konum_kalkti" });
            var lines = ShelterEventSnapshotBuilder.TelexLines(sim, 0, out int next);
            Assert.AreEqual(sim.ShelterEvents.Log.Count, next);
            var msgs = lines.Select(l => l.Status + " | " + l.Message).ToList();
            Assert.AreEqual(9, lines.Count, string.Join("\n", msgs));
            Assert.AreEqual(Loc.F("{0} ({1} {2})", Loc.F("{0}/{1}", 1, T), Loc.T("görmezden gelinen iz"), DeskFormat.Signed(1, 0)), lines[0].Message);
            Assert.AreEqual(TelexKind.Info, lines[0].Kind);
            Assert.AreEqual(Loc.F("{0}: YZ dinleme istasyonları sığınağın yayınlarını arıyor.", Loc.F("{0}/{1}", 2, T)), lines[1].Message);
            Assert.AreEqual(TelexKind.Alarm, lines[1].Kind);
            Assert.AreEqual(Loc.F("{0}: arama daralıyor; {1}'da konum çözülür.", Loc.F("{0}/{1}", 4, T), T), lines[2].Message);
            Assert.AreEqual(Loc.F("{0} ({1} {2})", Loc.F("{0}/{1}", 3, T), Loc.T("sorgu"), DeskFormat.Signed(-1, 0)), lines[3].Message);
            Assert.AreEqual(TelexKind.Alarm, lines[4].Kind, "sorgudan sonra 3→4 yeniden çıkış uyarıyı yeniden basar");
            Assert.AreEqual(Loc.F("{0}: eşikte. Bu kapanışta konum çözülür.", Loc.F("{0}/{1}", 6, T)), lines[5].Message);
            Assert.AreEqual(Loc.T("KONUM ÇÖZÜLDÜ"), lines[6].Status);
            Assert.AreEqual(TelexKind.Alarm, lines[6].Kind);
            Assert.AreEqual(Loc.T("Sorgu izi sildi: ifşa eşiğin altında, konum uyarısı kalktı."), lines[8].Message);
            Assert.AreEqual(TelexKind.Done, lines[8].Kind);
            foreach (var l in lines) Assert.IsFalse(l.Message.Contains(sim.Board.People.First(p => p.Alive).Name), "ifşa satırı kişinin adını yazmaz");

            // Kalınan yerden devam da aynı değeri taşır
            var tail = ShelterEventSnapshotBuilder.TelexLines(sim, 5, out _);
            Assert.AreEqual(lines[5].Message, tail[0].Message, "günlük toplamı baştan sayılır");
        }

        // ---------------------------------------------------------------- CRT, fiş, kâğıt

        [Test]
        public void CrtHint_Priority_LocationThresholdShortThenBlackout()
        {
            var sim = K.New();
            Assert.IsNull(ShelterEventSnapshotBuilder.CrtHint(sim));
            K.Understaffed(sim, 2);
            Assert.AreEqual(Loc.F("SIĞINAK EKSİK {0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts), ShelterEventSnapshotBuilder.CrtHint(sim));
            sim.Shelter.Exposure = sim.B.exposureThreshold;
            Assert.AreEqual(Loc.T("İFŞA EŞİKTE · KAPANIŞTA KONUM ÇÖZÜLÜR"), ShelterEventSnapshotBuilder.CrtHint(sim));
            sim.Shelter.FallPending = true;
            Assert.AreEqual(Loc.T("KONUM ÇÖZÜLDÜ · SON VARDİYA"), ShelterEventSnapshotBuilder.CrtHint(sim));
        }

        [Test]
        public void Slip_ExposureAndShelterShortBoxes_TrEn([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = K.New();
            var clean = HandoverSlipFactsBuilder.Build(sim);
            Assert.IsFalse(clean.warnings.Any(w => w.kind == SlipWarningKind.Exposure || w.kind == SlipWarningKind.ShelterShort));
            Assert.AreEqual(6, (int)SlipWarningKind.Exposure, "yeni tür sona eklendi (yan dosya sayıyla saklar)");
            Assert.AreEqual(7, (int)SlipWarningKind.ShelterShort);

            sim.Shelter.Exposure = sim.B.exposureThreshold;
            var warned = HandoverSlipFactsBuilder.Build(sim);
            Assert.IsTrue(warned.exposureWarned);
            Assert.IsFalse(warned.exposurePending);
            var lines = HandoverSlipFormat.Format(warned);
            var box = lines.Warnings.Single(b => b.Kind == SlipWarningKind.Exposure);
            Assert.AreEqual(Loc.T("İFŞA ·"), box.Prefix);
            Assert.AreEqual(Loc.T("eşikte · kapanışta konum çözülür"), box.Text);

            sim.Shelter.FallPending = true;
            K.Understaffed(sim, 2);
            var both = HandoverSlipFactsBuilder.Build(sim);
            var l2 = HandoverSlipFormat.Format(both);
            Assert.AreEqual(Loc.T("konum çözüldü · bu vardiya son"), l2.Warnings.Single(b => b.Kind == SlipWarningKind.Exposure).Text);
            var shortBox = l2.Warnings.Single(b => b.Kind == SlipWarningKind.ShelterShort);
            Assert.AreEqual(Loc.T("SIĞINAK EKSİK ·"), shortBox.Prefix, "baskın kadrosunun KADRO EKSİK'inden ayrı ad");
            Assert.AreEqual(Loc.F("{0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts), shortBox.Text);
            int ie = l2.Warnings.FindIndex(b => b.Kind == SlipWarningKind.Exposure), isS = l2.Warnings.FindIndex(b => b.Kind == SlipWarningKind.ShelterShort);
            Assert.Less(ie, isS, "sıra: İFŞA, SIĞINAK EKSİK");
            Assert.IsFalse(string.Join(" ", l2.Warnings.Select(b => b.Prefix + b.Text)).Contains(Loc.T("KADRO EKSİK")), "fişte KADRO EKSİK geçmez");

            both.shelterShort = both.shelterShortMax;
            Assert.AreEqual(Loc.F("{0}/{1} · bu kapanışta boşalır", both.shelterShortMax, both.shelterShortMax), HandoverSlipFormat.ShelterShortText(both));

            // Kararla varılan eşiğin ilk uyarı vardiyası (ikinci tur kararı): kutu kalan vardiyayı yazar
            sim.Shelter.FallGrace = 1;
            var grace = HandoverSlipFactsBuilder.Build(sim);
            Assert.AreEqual(2, grace.exposureShiftsLeft);
            Assert.AreEqual(Loc.F("konum çözüldü · {0} vardiya kaldı", 2), HandoverSlipFormat.ExposureText(grace));
        }

        [Test]
        public void Paper_SafeTagBecomesThreshold_OnlyWhenIgnoringReachesIt()
        {
            var sim = K.New();
            var alive = sim.Board.People.Where(p => p.Alive).ToArray();
            sim.ShelterEvents.Pending = new ShelterEventInstance { Kind = "iz", OpenedShift = sim.S.Day, PersonId = alive[0].Id, Area = "uretim" };
            string Tag() => ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()).Rows.FirstOrDefault(r => r.Id == "yoksay")?.SafeTag;
            var safe = Tag();
            if (safe == null) Assert.Inconclusive("iz kâğıdında 'yoksay' güvenli değil");
            Assert.AreEqual(Loc.T("GÜVENLİ"), safe);
            sim.Shelter.Exposure = sim.B.exposureThreshold - sim.B.exposureIgnoredTrace;
            Assert.AreEqual(Loc.T("EŞİK"), Tag(), "eşiğe vardıran 'görmezden gel'");
            Assert.IsTrue(ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()).Rows.First(r => r.Id == "yoksay").SafeWarn);
            sim.Shelter.Exposure = sim.B.exposureThreshold;
            Assert.AreEqual(Loc.T("GÜVENLİ"), Tag(), "konum zaten çözülmüşse uyarı etiketi yok");
        }
    }
}
