using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 devir fişinin yüzü (PLAN W3, §4; C §7): biçimleyicinin R19c v001/v031 metinleri (TR ve EN, PLAN
    /// kararlarıyla: D3 PAPERS, FIELD · RAID TEAM, injured), uyarıların tür başına birleşmesi ve "… +n" kesimi; yüz ölçüleri
    /// (tasarım HTML'inin CSS pikselinden, ± 0,1 mm); prefab'ın yerleşimi, en kötü fişin taşmaması, kıvrım ofseti, dil değişimi,
    /// prefab ve malzeme kuralları. Prefab yoksa (Aşama 4'ün tek seferlik kurulumu yapılmamışsa) prefab testleri
    /// atlanır. Testler açık sahneye dokunmaz (yalnız örnek oluşturup siler).
    /// </summary>
    public class Rev3HandoverSlipFaceTests
    {
        const string PrefabPath = DeskSpriteManifest.Root + "/Prefabs/Siginak/HandoverSlipView.prefab";
        const string MaterialPath = Rev3ModelImporter.ArtDir + "/SceneMaterials/Handover slip.mat";
        const string ModelGuid = "a2883c656ae81416a88818a1704ffc12";
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const float Px = 1f / 4.8f;   // tasarımın 1× pikseli (mm)
        /// <summary>Tek kâğıt akışı: fiş teleks kâğıdının boyunda (260 × 225 mm = tasarım 832 × 720 px × 1,5); içerik genişliği px.</summary>
        const float DesignW = 832, ContentW = 832 - 2 - 26 - 26;
        const float Tol = 0.1f;       // mm (E14)
        const string Minus = "−";

        readonly List<Object> spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.DestroyImmediate(o);
            spawned.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ================================================================== biçim · R19c

        [Test]
        public void Format_V001_TrEn_MatchesR19c()
        {
            var tr = HandoverSlipFormat.Format(V001());
            Assert.AreEqual("KYGA · SIĞINAK 07", tr.HeaderLeft);
            Assert.AreEqual("No. 001/1", tr.HeaderRight);
            Assert.AreEqual("VARDİYA 001 · DEVİR FİŞİ", tr.Title);
            Assert.AreEqual("8/10 kişi · erzak 24/60", tr.Subtitle);
            Assert.AreEqual("SAHA · BASKIN KADROSU", tr.FieldLabel);
            Rows(tr.Field, "saha", ("T", "Cem", "kit · zırh", ""), ("E", "İpek", "kit · zırh", ""), ("A", "Zeynep", "kit", ""));
            Assert.AreEqual("SIĞINAK", tr.ShelterLabel);
            Rows(tr.Shelter, "sığınak", ("", "Suna", "ÜRETİM", ""), ("", "Deniz", "MUTFAK", ""), ("", "Selin", "EĞİTİM · TEKNİSYEN", ""),
                ("", "Eren", "BOŞTA", ""), ("", "Nermin", "BOŞTA", ""));
            Assert.AreEqual("KAPANIŞTA BEKLENEN", tr.ExpectedLabel);
            // 2026-11-24: EĞİTİM bölümü kaldırıldı; kapanışta EĞİTİM satırı yok
            Rows(tr.Expected, "kapanışta", ("", "ERZAK", "24,0 → 21,9 / 60", Minus + "2,1"),
                ("", "ÜRETİM", "iş emri yok · çıktı yok", ""), ("", "KAPI", "aday yok", ""));
            Assert.IsEmpty(tr.Warnings);
            Assert.AreEqual("plan 001/1 · tek kullanımlık · yeniden basılmaz", tr.Footer);
            Assert.AreEqual("fişi EVRAK’a koy", tr.Action, "kesme işareti ’ (D3, token ile aynı)");

            UseEnglish();
            var en = HandoverSlipFormat.Format(V001());
            Assert.AreEqual("KYGA · SHELTER 07", en.HeaderLeft);
            Assert.AreEqual("No. 001/1", en.HeaderRight);
            Assert.AreEqual("SHIFT 001 · HANDOVER SLIP", en.Title);
            Assert.AreEqual("8/10 people · rations 24/60", en.Subtitle);
            Assert.AreEqual("FIELD · RAID TEAM", en.FieldLabel, "D3: pano ile aynı anahtar (tasarımın RAID SQUAD'ı değil)");
            Rows(en.Field, "EN saha", ("T", "Cem", "kit · armour", ""), ("E", "İpek", "kit · armour", ""), ("A", "Zeynep", "kit", ""));
            Assert.AreEqual("SHELTER", en.ShelterLabel);
            Rows(en.Shelter, "EN sığınak", ("", "Suna", "PRODUCTION", ""), ("", "Deniz", "KITCHEN", ""), ("", "Selin", "TRAINING · TECHNICIAN", ""),
                ("", "Eren", "IDLE", ""), ("", "Nermin", "IDLE", ""));
            Assert.AreEqual("EXPECTED AT CLOSING", en.ExpectedLabel);
            Rows(en.Expected, "EN kapanışta", ("", "RATIONS", "24.0 → 21.9 / 60", Minus + "2.1"),
                ("", "PRODUCTION", "no work order · no output", ""), ("", "DOOR", "no candidates", ""));
            Assert.AreEqual("plan 001/1 · single use · cannot be reprinted", en.Footer);
            Assert.AreEqual("file the slip in PAPERS", en.Action, "D3-A: EN'de EVRAK yerine PAPERS (plaka ile tek anahtar)");
        }

        [Test]
        public void Format_V031_TrEn_MatchesR19c()
        {
            var tr = HandoverSlipFormat.Format(V031());
            Assert.AreEqual("No. 031/1", tr.HeaderRight);
            Assert.AreEqual("VARDİYA 031 · DEVİR FİŞİ", tr.Title);
            Assert.AreEqual("9/10 kişi · erzak 17/60", tr.Subtitle);
            Rows(tr.Field, "saha", ("T", "Deniz", "kit · zırh", ""), ("E", "Cem", "kit · zırh · EMP hücresi", ""), ("A", "Zeynep", "kit · bakım kiti", ""));
            Rows(tr.Shelter, "sığınak", ("", "Rauf", "ÜRETİM", ""), ("", "Nermin", "MUTFAK", ""), ("", "Suna", "EĞİTİM · EMP", ""),
                ("", "Selin", "BOŞTA", "yaralı"), ("", "İpek", "BOŞTA", "yatıyor 2"), ("", "Aylin", "BOŞTA", "kısıtlı 2"));
            Rows(tr.Expected, "kapanışta", ("", "ERZAK", "17,0 → 15,6 / 60", Minus + "1,4"),
                ("", "ÜRETİM", "Zırh plakası 0/1 → 1/1 · Rauf", ""), ("", "KAPI", "1 aday bekliyor", ""));
            Assert.AreEqual(2, tr.Warnings.Count);
            Box(tr.Warnings[0], SlipWarningKind.DoorLastShift, "KAPI ·", "Son vardiya: karar verilmezse adaylar gider.");
            Box(tr.Warnings[1], SlipWarningKind.Exposure, "İFŞA ·", "eşikte · kapanışta konum çözülür");
            Assert.AreEqual("plan 031/1 · tek kullanımlık · yeniden basılmaz", tr.Footer);

            UseEnglish();
            var en = HandoverSlipFormat.Format(V031());
            Assert.AreEqual("SHIFT 031 · HANDOVER SLIP", en.Title);
            Assert.AreEqual("9/10 people · rations 17/60", en.Subtitle);
            Rows(en.Field, "EN saha", ("T", "Deniz", "kit · armour", ""), ("E", "Cem", "kit · armour · EMP cell", ""), ("A", "Zeynep", "kit · care kit", ""));
            Rows(en.Shelter, "EN sığınak", ("", "Rauf", "PRODUCTION", ""), ("", "Nermin", "KITCHEN", ""), ("", "Suna", "TRAINING · EMP", ""),
                ("", "Selin", "IDLE", "injured"), ("", "İpek", "IDLE", "laid up 2"), ("", "Aylin", "IDLE", "restricted 2"));
            Rows(en.Expected, "EN kapanışta", ("", "RATIONS", "17.0 → 15.6 / 60", Minus + "1.4"),
                ("", "PRODUCTION", "Armour plate 0/1 → 1/1 · Rauf", ""), ("", "DOOR", "1 candidates waiting", ""));
            Box(en.Warnings[0], SlipWarningKind.DoorLastShift, "DOOR ·", "Last shift: the candidates leave if no decision is made.");
            Box(en.Warnings[1], SlipWarningKind.Exposure, "EXPOSURE ·", "at threshold · compromised at closing");
            Assert.AreEqual("plan 031/1 · single use · cannot be reprinted", en.Footer);
        }

        [Test]
        public void Format_WarningsOneBoxPerKind_FirstSeenOrder_RetiredKindsSkipped()
        {
            var f = V001();
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.DoorLastShift });
            f.warnings.Add(new SlipWarning { kind = (SlipWarningKind)1, name = "Rauf" });   // aşama 7'de sökülen tür (eski yan dosya)
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.ShelterShort });
            f.warnings.Add(new SlipWarning { kind = (SlipWarningKind)0 });
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.DoorLastShift });
            f.shelterShort = 1; f.shelterShortMax = 3;
            var l = HandoverSlipFormat.Format(f);
            CollectionAssert.AreEqual(new[] { SlipWarningKind.DoorLastShift, SlipWarningKind.ShelterShort }, l.Warnings.Select(b => b.Kind).ToArray(),
                "tür başına bir kutu, verideki ilk görünüş sırası; eski yan dosyanın sökülmüş türleri (0–3) yazılmaz");
            Box(l.Warnings[0], SlipWarningKind.DoorLastShift, "KAPI ·", "Son vardiya: karar verilmezse adaylar gider.");
            Box(l.Warnings[1], SlipWarningKind.ShelterShort, "SIĞINAK EKSİK ·", "1/3");
            Assert.IsFalse(l.Warnings.Any(b => b.Prefix == "DOSYA ·"), "DOSYA kutusu yok");
        }

        [Test]
        public void Format_EdgeCases_EmptyPocketNoTrainingNoOutputAndStates()
        {
            Assert.AreEqual("", HandoverSlipFormat.Format(null).Title, "null veri → boş satırlar");
            Assert.IsEmpty(HandoverSlipFormat.Format(null).Field);

            var f = V001();
            f.field[2] = new SlipFieldRow { role = "ana", name = "" };   // JsonUtility null → "" (boş cep)
            f.training = new SlipTraining { active = false };
            f.production = new SlipProduction { kind = SlipProductionKind.NoOutput, itemType = "kit_tek" };
            f.shelter.Add(new SlipShelterRow { name = "Kerem", job = "rest", state = SlipPersonState.RestrictionEnded });
            // Tesis aşama 5: GÖZETİM satırını kurucu yalnız cebi açıkken yazar; emekli görevler (contact) BOŞTA
            f.shelter.Add(new SlipShelterRow { name = "Orhan", job = "watch" });
            f.shelter.Add(new SlipShelterRow { name = "Nil", job = "contact" });
            f.shelter.Add(new SlipShelterRow { name = "Banu", job = "cook", state = SlipPersonState.Hurt });
            var l = HandoverSlipFormat.Format(f);
            Assert.AreEqual("—", l.Field[2].Name, "boş cep");
            Assert.AreEqual("", l.Field[2].Value);
            Assert.AreEqual("A", l.Field[2].Disc);
            Assert.AreEqual("Teknisyen kiti · çıktı yok", l.Expected[1].Value, "EĞİTİM satırı yok (2026-11-24)");
            Assert.AreEqual("kısıtlı · bitti", l.Shelter[5].Suffix);
            Assert.AreEqual("GÖZETİM", l.Shelter[6].Value);
            Assert.AreEqual("BOŞTA", l.Shelter[7].Value);
            Assert.AreEqual("MUTFAK", l.Shelter[8].Value);
            Assert.AreEqual("yaralı", l.Shelter[8].Suffix);

            f.suppliesAfter = 26.35;   // artış: "+" işaretli fark; yuvarlanmış değerlerden
            var up = HandoverSlipFormat.Format(f).Expected[0];
            Assert.AreEqual("24,0 → 26,4 / 60", up.Value);
            Assert.AreEqual("+2,4", up.Suffix);

            // Saf: aynı veri iki kez aynı satırlar; satırlarda zengin metin etiketi yok (kalınlığı yüz ekler)
            var a = HandoverSlipFormat.Format(V031());
            var b = HandoverSlipFormat.Format(V031());
            Assert.AreEqual(Flatten(a), Flatten(b));
            Assert.IsFalse(Flatten(a).Contains("<"), "biçimleyici düz metin üretir");
        }

        // ================================================================== ölçüler (E14, prefab gerekmez)

        [Test]
        public void Metrics_MatchR10Template_Mm()
        {
            var m = new HandoverSlipMetrics();
            Near(DesignW * Px, m.sizeMm.x, 1e-3f, "tasarım genişliği 832 px (173,33 mm)");
            Near(150, m.sizeMm.y, 1e-4f, "yükseklik");
            // Tek kâğıt akışı: dünya ölçüsü = tasarım × 1,5 = teleks kâğıdı (token shiftClose.slip.sizeMm, faceScale)
            Near(1.5f, m.worldScale, 1e-6f, "dünya ölçeği");
            Near(new Vector2(260, 225), m.WorldSizeMm, 0.01f, "dünyada 260 × 225 mm");
            var theme = SiginakTheme.Active;
            Assert.IsNotNull(theme);
            Near(new Vector2(theme.NumberOf("shiftClose.slip.sizeMm[0]"), theme.NumberOf("shiftClose.slip.sizeMm[1]")), m.WorldSizeMm, 0.01f, "token sizeMm = tasarım × ölçek");
            Near(theme.NumberOf("shiftClose.slip.faceScale"), m.worldScale, 1e-6f, "token faceScale");
            Near(0.0015f, HandoverSlipFace.CanvasScale(m), 1e-7f, "tuval ölçeği 0,001 × 1,5");
            Near(new Vector3(-0.13f, 0.1125f, 0), HandoverSlipFace.CanvasCorner(m), 1e-6f, "tuvalin sol üst köşesi");
            Near(2.50f, m.padTopMm, 0.005f, "üst boşluk 12 px");
            Near(5.42f, m.padRightMm, 0.005f, "sağ boşluk 26 px");
            Near(2.92f, m.padBottomMm, 0.005f, "alt boşluk 14 px");
            Near(5.42f, m.padLeftMm, 0.005f, "sol boşluk 26 px");
            Near(5.63f, m.ContentLeftMm, 0.01f, "içerik x 27 px");
            Near(ContentW * Px, m.ContentWidthMm, 0.01f, "içerik genişliği 778 px (162,08 mm; eskisi 426 px)");
            Near(3.75f, m.ContentTopMm, 0.01f, "içerik üstü 18 px");
            Near(146.875f, m.ContentBottomMm, 0.01f, "içerik altı 705 px");
            Near(2.92f, m.headerEmMm, 0.005f, "başlık satırı 14 px");
            Near(4.79f, m.titleEmMm, 0.005f, "başlık 23 px");
            Near(3.125f, m.subtitleEmMm, 0.005f, "alt başlık 15 px");
            Near(0.42f, m.redRuleMm, 0.005f, "kırmızı çizgi 2 px");
            Near(2.92f, m.sectionEmMm, 0.005f, "bölüm etiketi 14 px");
            Near(5.0f, m.rowMm, 0.001f, "satır adımı 24 px");
            Near(4.58f, m.discMm, 0.005f, "disk 22 px");
            Near(0.31f, m.discRingMm, 0.005f, "disk halkası 1,5 px");
            Near(3.75f, m.nameEmMm, 0.005f, "ad 18 px");
            Near(3.125f, m.valueEmMm, 0.005f, "değer 15 px");
            Near(2.92f, m.jobEmMm, 0.005f, "görev 14 px");
            Near(21.67f, m.labelColumnMm, 0.005f, "KAPANIŞTA etiket sütunu 104 px");
            Near(3.75f, m.deltaEmMm, 0.005f, "ERZAK farkı 18 px");
            Near(0.31f, m.boxBorderMm, 0.005f, "kutu çerçevesi 1,5 px");
            Near(1.04f, m.boxBarMm, 0.005f, "kutu sol çubuğu 5 px");
            Near(3.33f, m.boxIconMm, 0.005f, "uyarı simgesi 16 px");
            Near(2.92f, m.boxPrefixEmMm, 0.005f, "önek 14 px");
            Near(3.125f, m.boxTextEmMm, 0.005f, "kutu metni 15 px");
            Near(4.17f, m.boxTextLineMm, 0.005f, "kutu satırı 20 px");
            Near(2.92f, m.footerEmMm, 0.005f, "alt satır 14 px");
            Near(3.96f, m.actionEmMm, 0.005f, "eylem 19 px");
            Near(4.17f, m.actionIconMm, 0.005f, "ok simgesi 20 px");
            Near(0.625f, m.actionRuleMm, 0.005f, "çift çizgi 3 px");

            // Kıvrım: modelin z = −0,004 · ((y + 0,075) / 0,15)²'si + 0,4 mm
            Near(-4.4f, HandoverSlipFace.CurlZ(0, m), 1e-4f, "üst kenar");
            Near(-0.4f, HandoverSlipFace.CurlZ(150, m), 1e-4f, "alt kenar");
            Near(-1.4f, HandoverSlipFace.CurlZ(75, m), 1e-4f, "orta");
            Near(-(4f * 0.64f + 0.4f), HandoverSlipFace.CurlZ(30, m), 1e-4f, "y 30");
        }

        // ================================================================== prefab · yerleşim (E14)

        [Test]
        public void Layout_FollowsR10Template_Mm()
        {
            var face = Spawn();
            face.Bind(V001());
            Assert.IsFalse(face.Overflowing);
            var m = face.Layout;

            // CSS akışı (px → mm); "normal" satır yüksekliği fontun satır yüksekliği
            float headerTop = 18 * Px;
            NearBaseline(face.HeaderLeft, headerTop, 18 * Px, 14 * Px, "başlık satırı sol");
            NearBaseline(face.HeaderRight, headerTop, 18 * Px, 14 * Px, "başlık satırı sağ");
            Near(27 * Px, face.HeaderLeft.rectTransform.anchoredPosition.x, Tol, "başlık sol x");
            Near((27 + ContentW) * Px, face.HeaderRight.rectTransform.anchoredPosition.x, Tol, "başlık sağ kenarı x");
            float titleTop = headerTop + 18 * Px + 4 * Px;
            NearBaseline(face.Title, titleTop, 23 * 1.15f * Px, 23 * Px, "başlık");
            float subTop = titleTop + 23 * 1.15f * Px + 2 * Px;
            NearBaseline(face.Subtitle, subTop, 15 * 1.3f * Px, 15 * Px, "alt başlık");
            float ruleTop = subTop + 15 * 1.3f * Px + 8 * Px;
            NearBox(face.RedRule.rectTransform, 27 * Px, ruleTop, ContentW * Px, 2 * Px, "kırmızı çizgi");

            float labelLine = NormalLine(face.FieldLabel, 14 * Px);
            float fieldLabelTop = ruleTop + 2 * Px + 8 * Px;
            NearBaseline(face.FieldLabel, fieldLabelTop, labelLine, 14 * Px, "SAHA etiketi");
            float rowsTop = fieldLabelTop + labelLine + 1 * Px;
            for (int i = 0; i < 3; i++) NearBox(face.FieldRows[i].Rect, 27 * Px, rowsTop + i * 24 * Px, ContentW * Px, 24 * Px, "saha satırı " + i);
            var first = face.FieldRows[0];
            NearBox(first.DiscFill.rectTransform, 0, 1 * Px, 22 * Px, 22 * Px, "disk (satır yereli)");
            Near(8 * Px + 22 * Px, first.NameText.rectTransform.anchoredPosition.x, Tol, "ad x (disk + 8 px)");
            NearBaselineLocal(first.NameText, 24 * Px, 18 * Px, "ad taban çizgisi");
            NearBaselineLocal(first.ValueText, 24 * Px, 15 * Px, "eşya taban çizgisi");
            Near(ContentW * Px, first.ValueText.rectTransform.anchoredPosition.x, Tol, "eşyalar sağa yaslı");
            Assert.IsTrue(first.Leader.gameObject.activeSelf, "nokta çizgisi");
            Near(24 * Px - 6 * Px - 3 * Px, Top(first.Leader.rectTransform), Tol, "nokta çizgisi altı satırın 6 px üstünde");

            float shelterLabelTop = rowsTop + 3 * 24 * Px + 7 * Px;
            NearBaseline(face.ShelterLabel, shelterLabelTop, labelLine, 14 * Px, "SIĞINAK etiketi");
            float shelterTop = shelterLabelTop + labelLine + 1 * Px;
            for (int i = 0; i < 5; i++) NearBox(face.ShelterRows[i].Rect, 27 * Px, shelterTop + i * 24 * Px, ContentW * Px, 24 * Px, "sığınak satırı " + i);
            float expectedLabelTop = shelterTop + 5 * 24 * Px + 7 * Px;
            NearBaseline(face.ExpectedLabel, expectedLabelTop, labelLine, 14 * Px, "KAPANIŞTA etiketi");
            float expectedTop = expectedLabelTop + labelLine + 1 * Px;
            for (int i = 0; i < 3; i++) NearBox(face.ExpectedRows[i].Rect, 27 * Px, expectedTop + i * 24 * Px, ContentW * Px, 24 * Px, "kapanışta satırı " + i);
            var rations = face.ExpectedRows[0];
            Near(104 * Px, rations.ValueText.rectTransform.anchoredPosition.x, Tol, "değer etiket sütunundan sonra");
            Assert.IsTrue(rations.SuffixText.gameObject.activeSelf, "ERZAK farkı");
            Near(ContentW * Px, rations.SuffixText.rectTransform.anchoredPosition.x, Tol, "fark en sağda");
            Near(rations.ValueText.rectTransform.anchoredPosition.y, rations.SuffixText.rectTransform.anchoredPosition.y, 1e-4f, "ortak taban çizgisi");
            Assert.IsFalse(face.ExpectedRows[1].SuffixText.gameObject.activeSelf, "farksız satır");

            // Alt blok alttan: içerik altı 705 px; eylem içeriği max(20 px simge, 19 px yazının normal satırı)
            float actionLine = NormalLine(face.Action, 19 * Px);
            float inner = Mathf.Max(20 * Px, actionLine);
            float doubleTop = 705 * Px - 2 * Px - inner - 6 * Px - 3 * Px;
            Near(140.2f, doubleTop, 0.15f, "çift çizgi C §7.2 (140,2–140,8)");
            NearBox(face.ActionRuleTop.rectTransform, 27 * Px, doubleTop, ContentW * Px, 1 * Px, "çift çizgi üst");
            NearBox(face.ActionRuleBottom.rectTransform, 27 * Px, doubleTop + 2 * Px, ContentW * Px, 1 * Px, "çift çizgi alt");
            float footerTextTop = doubleTop - 6 * Px - 14 * 1.3f * Px;
            NearBaseline(face.Footer, footerTextTop, 14 * 1.3f * Px, 14 * Px, "alt satır");
            float footerRuleTop = footerTextTop - 5 * Px - 1 * Px;
            Near(134f, footerRuleTop, 0.3f, "alt çizgi C §7.2 (≈ 643 px)");
            NearBox(face.FooterRule.rectTransform, 27 * Px, footerRuleTop, ContentW * Px, 1 * Px, "alt çizgi");
            Near(footerRuleTop - 8 * Px, face.FooterTopMm, Tol, "alt bloğun üstü (8 px boşluk)");
            NearBox(face.ActionIcon.rectTransform, Left(face.ActionIcon.rectTransform), doubleTop + 3 * Px + 6 * Px + (inner - 20 * Px) / 2, 20 * Px, 20 * Px, "ok simgesi");
            float actionCentre = (Left(face.ActionIcon.rectTransform) + face.Action.rectTransform.anchoredPosition.x + face.Action.rectTransform.sizeDelta.x) / 2;
            Near((27 + ContentW / 2) * Px, actionCentre, Tol, "eylem satırı ortada");

            // Yazı kuralları: otomatik punto yok, ışın hedefi yok, puntolar tasarımın
            foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.IsFalse(t.enableAutoSizing, t.name + ": otomatik punto");
                Assert.IsFalse(t.raycastTarget, t.name + ": raycastTarget");
            }
            Near(m.nameEmMm, first.NameText.fontSize, 1e-4f, "ad puntosu");
            Near(m.titleEmMm, face.Title.fontSize, 1e-4f, "başlık puntosu");
        }

        [Test]
        public void Layout_V031_WarningBoxesAndSpareSpace()
        {
            var face = Spawn();
            face.Bind(V031());
            Assert.IsFalse(face.Overflowing);
            Assert.AreEqual(2, face.Warnings.Count);
            Assert.AreEqual(-1, face.CutNames, "kesim yok");
            var door = face.Warnings[0];
            Assert.LessOrEqual(door.LineCount, 2, "KAPI uyarısı en çok iki satır");
            Assert.AreEqual(1, face.Warnings[1].LineCount, "İFŞA uyarısı tek satır");
            float boxTop = Top(door.Rect);
            Near(Bottom(face.ExpectedRows[2].Rect) + 10 * Px, boxTop, Tol, "ilk kutu 10 px aşağıda");
            Near(Bottom(door.Rect) + 6 * Px, Top(face.Warnings[1].Rect), Tol, "ikinci kutu 6 px aşağıda");
            Near((1.5f + 5 + door.LineCount * 20 + 6 + 1.5f) * Px, door.HeightMm, Tol, "kutu yüksekliği satır sayısından (iki satır 54 px)");
            NearBox(door.FrameLeft.rectTransform, 0, 0, 5 * Px, door.HeightMm, "sol çubuk 5 px");
            Near((5 + 6 + 18 + 7) * Px, door.Text.rectTransform.anchoredPosition.x, Tol, "metin simge sütunundan sonra");
            float spare = face.FooterTopMm - face.ContentBottomMm;
            TestContext.WriteLine("V.031 boş yer: " + (spare / Px).ToString("0.0") + " px (tasarım 47 px, iki satırlık eski KAPI uyarısıyla)");
            Assert.That(spare / Px, Is.InRange(64f, 79f + 20f * (2 - door.LineCount)), "V.031'in boş yeri tasarımınkine yakın (47 px; tek satırlık kutuda +20 px; 2026-11-24 EĞİTİM satırı kalktı: +24 px)");
        }

        [Test]
        public void Layout_WorstCase_TrEn_NoOverflow()
        {
            var failures = new List<string>();
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                string lang = english ? "EN" : "TR";
                var face = Spawn();
                face.Bind(Worst());
                var m = face.Layout;
                if (face.Overflowing) failures.Add(lang + ": taşma, içerik altı " + face.ContentBottomMm.ToString("0.00") + " > " + face.FooterTopMm.ToString("0.00"));
                TestContext.WriteLine(lang + " en kötü fiş: içerik altı " + face.ContentBottomMm.ToString("0.00") + " mm, alt blok " +
                                      face.FooterTopMm.ToString("0.00") + " mm, kesim " + face.CutNames);
                Assert.AreEqual(3, face.FieldRows.Count);
                Assert.AreEqual(7, face.ShelterRows.Count);
                Assert.AreEqual(2, face.Warnings.Count, "KAPI + SIĞINAK EKSİK");
                // Tek kâğıt akışı: tam genişlikte (162 mm) iki kutu da tek satır
                foreach (var w in face.Warnings)
                    if (w.LineCount != 1) failures.Add(lang + " " + w.name + ": uyarı " + w.LineCount + " satır (tek satır olmalı) '" + w.Text.text + "'");
                foreach (var row in face.FieldRows.Concat(face.ShelterRows).Concat(face.ExpectedRows))
                {
                    string at = lang + " " + row.name + " '" + row.NameText.text + "'";
                    if (row.HeightMm > m.rowMm + 0.01f) failures.Add(at + ": satır sardı (" + row.HeightMm.ToString("0.00") + " mm) '" + row.ValueText.text + "'");
                    row.NameText.ForceMeshUpdate(true, true);
                    if (row.NameText.textInfo.lineCount != 1) failures.Add(at + ": ad tek satır değil");
                    if (row.Kind != HandoverSlipRowKind.Expected && !row.Leader.gameObject.activeSelf) failures.Add(at + ": nokta çizgisine yer kalmadı");
                    if (!Mathf.Approximately(row.NameText.fontSize, row.Kind == HandoverSlipRowKind.Expected ? m.labelEmMm : m.nameEmMm))
                        failures.Add(at + ": ad puntosu değişti");
                }
                foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (t.enableAutoSizing) failures.Add(lang + " " + t.name + ": otomatik punto");
                    if (t.fontSize < m.headerEmMm - 1e-4f) failures.Add(lang + " " + t.name + ": punto " + t.fontSize + " < 14 px sınıfı");
                }
                foreach (var g in face.Content.GetComponentsInChildren<Graphic>(false))
                {
                    var c = new Vector3[4];
                    g.rectTransform.GetLocalCorners(c);
                    // tuval yereline (tasarım mm'si) çevir: köşeler fişin 173,33 × 150 mm'si içinde
                    for (int i = 0; i < 4; i++)
                    {
                        var p = face.FaceRect.InverseTransformPoint(g.rectTransform.TransformPoint(c[i]));
                        if (p.x < -0.01f || p.x > DesignW * Px + 0.01f || p.y > 0.01f || p.y < -150.01f)
                            failures.Add(lang + " " + g.name + ": fişin dışında (" + p.x.ToString("0.0") + "; " + (-p.y).ToString("0.0") + ")");
                    }
                }
                Object.DestroyImmediate(face.gameObject);
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>
        /// KOS aşama 4: olaylar açıkken en kötü fiş: KAPI uyarısı ve dört parçalı ERZAK kutusu (aç vardiya, ayrılacak kişi, yarım tayın,
        /// karartma). ERZAK kesilmez ("… +n" yok, ayrılacak kişinin adı yazılı); TR ve EN'de fiş taşmaz, punto küçülmez.
        /// </summary>
        [Test]
        public void Layout_WorstCase_WithSiege_TrEn_NoOverflow()
        {
            // Tesis aşama 11 (§8.5): KARŞI SALDIRI kutusu (ilk kutu) en kötü fişe eklenir: tek satır, taşma yok, punto küçülmez
            var failures = new List<string>();
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                string lang = english ? "EN" : "TR";
                var face = Spawn();
                var f = Worst();
                f.siegeTrust = 1;
                f.warnings.Insert(0, new SlipWarning { kind = SlipWarningKind.Siege });
                face.Bind(f);
                TestContext.WriteLine(lang + " KARŞI SALDIRI + en kötü: içerik altı " + face.ContentBottomMm.ToString("0.00") + " mm, alt blok " +
                                      face.FooterTopMm.ToString("0.00") + " mm");
                Assert.AreEqual(3, face.Warnings.Count, lang + ": KARŞI SALDIRI + KAPI + SIĞINAK EKSİK");
                var siege = face.Warnings[0];
                StringAssert.Contains(HandoverSlipFormat.SiegeText(f), siege.Text.text, lang);
                if (siege.LineCount != 1) failures.Add(lang + ": KARŞI SALDIRI kutusu " + siege.LineCount + " satır '" + siege.Text.text + "'");
                if (face.Overflowing) failures.Add(lang + ": taşma, içerik altı " + face.ContentBottomMm.ToString("0.00") + " > " + face.FooterTopMm.ToString("0.00"));
                foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(false))
                    if (t.enableAutoSizing || t.fontSize < face.Layout.headerEmMm - 1e-4f) failures.Add(lang + " " + t.name + ": punto " + t.fontSize);
                Object.DestroyImmediate(face.gameObject);
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void Layout_WorstCase_WithFourPartRations_TrEn_NoOverflow()
        {
            var failures = new List<string>();
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                string lang = english ? "EN" : "TR";
                var face = Spawn();
                var f = Worst();
                f.hungryStreak = 12;
                f.halfRationLeft = 3;
                f.blackout = true;
                f.nextDeserter = "Feride 21";
                f.warnings.RemoveAll(w => w.kind != SlipWarningKind.DoorLastShift);
                f.warnings.Add(new SlipWarning { kind = SlipWarningKind.Rations, name = f.nextDeserter });
                face.Bind(f);
                TestContext.WriteLine(lang + " KAPI + ERZAK: içerik altı " + face.ContentBottomMm.ToString("0.00") + " mm, alt blok " +
                                      face.FooterTopMm.ToString("0.00") + " mm");
                Assert.AreEqual(2, face.Warnings.Count, lang + ": KAPI + ERZAK");
                string rations = face.Warnings[1].Text.text;
                TestContext.WriteLine(lang + " ERZAK kutusu: " + rations);
                StringAssert.Contains("Feride 21", rations, lang + ": ayrılacak kişinin adı kesilmez");
                StringAssert.DoesNotContain("… +", rations, lang + ": ERZAK kutusu kesilmez");
                if (face.Overflowing) failures.Add(lang + ": taşma, içerik altı " + face.ContentBottomMm.ToString("0.00") + " > " + face.FooterTopMm.ToString("0.00"));
                foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (t.enableAutoSizing) failures.Add(lang + " " + t.name + ": otomatik punto");
                    if (t.fontSize < face.Layout.headerEmMm - 1e-4f) failures.Add(lang + " " + t.name + ": punto " + t.fontSize + " < 14 px sınıfı");
                }
                Object.DestroyImmediate(face.gameObject);
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // ================================================================== kıvrım

        [Test]
        public void Face_RowsFollowCurl()
        {
            var face = Spawn();
            face.Bind(V031());
            var m = face.Layout;

            // Tuval modelin düz düzleminde (z 0), model kimlik yerelinde; modelin üst kenarı yüz yönüne 4 mm kıvrık
            Near(0, face.FaceRect.localPosition.z, 1e-6f, "tuval z");
            var model = face.GetComponentInChildren<Rev3Model>();
            Assert.IsNotNull(model);
            Assert.AreEqual(Quaternion.identity, model.transform.localRotation);
            Near(Vector3.zero, model.transform.localPosition, 1e-6f, "model kimlik yerelinde");
            var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
            var top = mesh.vertices.OrderByDescending(v => v.y).First();
            var bottom = mesh.vertices.OrderBy(v => v.y).First();
            Near(-0.006f, top.z, 2e-4f, "modelin üst kenarı (yüz −z; dünyada 6 mm = 4 tasarım mm'si × 1,5)");
            Near(0f, bottom.z, 2e-4f, "modelin alt kenarı");

            int checkedCount = 0;
            foreach (Transform child in face.Content)
            {
                if (!child.gameObject.activeSelf) continue;
                var rt = (RectTransform)child;
                float z = rt.anchoredPosition3D.z;
                var tmp = child.GetComponent<TMP_Text>();
                if (tmp == null)
                {
                    float topMm = Top(rt);
                    Near(HandoverSlipFace.CurlZ(topMm, m), z, 1e-4f, child.name + " z = kıvrım(üst kenar)");
                }
                else
                {
                    float baseline = -rt.anchoredPosition.y;
                    Assert.That(z, Is.InRange(HandoverSlipFace.CurlZ(baseline - 7f, m) - 1e-4f, HandoverSlipFace.CurlZ(baseline, m) + 1e-4f),
                        child.name + ": z satırının üstündeki kâğıda göre");
                }
                // Kâğıdın önünde: öğenin üst kenarındaki kâğıt z'si − 0,4 mm (kıvrım üstte en büyük)
                float paperTop = -m.curlMm * Mathf.Pow((150f - Mathf.Max(0, Top(rt))) / 150f, 2);
                Assert.LessOrEqual(z, paperTop - m.liftMm + 1e-3f, child.name + ": kâğıdın 0,4 mm önünde");
                foreach (Transform grand in child)
                    Near(0, ((RectTransform)grand).anchoredPosition3D.z, 1e-6f, child.name + "/" + grand.name + " satır içinde düz");
                checkedCount++;
            }
            Assert.Greater(checkedCount, 20, "yerleşen öğe sayısı");
            Assert.Less(face.FieldRows[0].Rect.anchoredPosition3D.z, face.ExpectedRows[2].Rect.anchoredPosition3D.z,
                "üstteki satır kâğıtla birlikte yüze daha yakın");
        }

        // ================================================================== dil

        [Test]
        public void Face_LanguageChange_WordsOnly()
        {
            var face = Spawn();
            var facts = V001();
            face.Bind(facts);
            var names = face.FieldRows.Concat(face.ShelterRows).Select(r => r.NameText.text).ToArray();
            int rows = face.FieldRows.Count + face.ShelterRows.Count + face.ExpectedRows.Count;
            int before = HandoverSlipFace.LayoutCount;
            face.Bind(facts);
            Assert.AreEqual(before, HandoverSlipFace.LayoutCount, "aynı veri ve dil: yeniden yerleşim yok");

            var onEnable = typeof(HandoverSlipFace).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            var onDisable = typeof(HandoverSlipFace).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(face, null);   // Edit modda OnEnable çağrılmaz: DeskLoc.Changed aboneliği elle
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                Assert.Greater(HandoverSlipFace.LayoutCount, before, "DeskLoc.Changed yeniden yazdı");
                Assert.AreSame(facts, face.Facts, "veri aynı");
                Assert.AreEqual("SHIFT 001 · HANDOVER SLIP", face.Title.text);
                Assert.AreEqual("FIELD · RAID TEAM", face.FieldLabel.text);
                Assert.AreEqual("24.0 → 21.9 / 60", face.ExpectedRows[0].ValueText.text, "ondalık nokta");
                Assert.AreEqual(Minus + "2.1", face.ExpectedRows[0].SuffixText.text);
                CollectionAssert.AreEqual(names, face.FieldRows.Concat(face.ShelterRows).Select(r => r.NameText.text).ToArray(), "adlar çevrilmez");
                Assert.AreEqual(rows, face.FieldRows.Count + face.ShelterRows.Count + face.ExpectedRows.Count, "satır sayısı aynı");
                Assert.AreEqual("T", face.FieldRows[0].DiscLetter.text, "disk harfi");
            }
            finally
            {
                DeskLoc.Instance.SetLanguage(DeskLang.Tr);
                onDisable.Invoke(face, null);
            }
            Assert.AreEqual("VARDİYA 001 · DEVİR FİŞİ", face.Title.text, "TR'ye dönüş");
            Assert.AreEqual("24,0 → 21,9 / 60", face.ExpectedRows[0].ValueText.text);

            face.Bind(null);
            Assert.IsFalse(face.Content.gameObject.activeSelf, "null → boş yüz");
            Assert.IsNull(face.Facts);
        }

        // ================================================================== prefab ve malzeme

        [Test]
        public void Prefab_HasNoColliderOrRaycaster()
        {
            var prefab = LoadPrefab();
            var face = prefab.GetComponent<HandoverSlipFace>();
            Assert.IsNotNull(face, "kökte HandoverSlipFace");
            Assert.AreEqual("HandoverSlipView", prefab.name);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), "çarpıştırıcı yok (tek fiş çarpıştırıcısı ShiftReceiptPaper'da)");
            Assert.IsEmpty(prefab.GetComponentsInChildren<GraphicRaycaster>(true), "GraphicRaycaster yok");
            Assert.IsEmpty(prefab.GetComponentsInChildren<Mask>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<RectMask2D>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<CanvasScaler>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<DeskLocText>(true), "yüz yazıları dinamik (DeskLocText yok)");
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(b => b == null), "eksik script yok");

            // Model: iç içe HandoverSlip prefab'ı, kimlik yereli; Body malzemesi fişin malzemesi, gölge açık
            var model = prefab.GetComponentInChildren<Rev3Model>(true);
            Assert.IsNotNull(model);
            Assert.AreEqual("HandoverSlip", model.ModelName);
            var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
            Assert.IsNotNull(source, "model iç içe prefab");
            Assert.AreEqual(ModelGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)));
            var body = model.GetComponentInChildren<MeshRenderer>(true);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.IsNotNull(mat, MaterialPath);
            Assert.AreSame(mat, body.sharedMaterial, "Body → Handover slip.mat (telexPaper paylaşılır, değişmez)");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.On, body.shadowCastingMode);

            // Tuval: World Space, 173,33 × 150 birim (tasarım mm'si), ölçek 0,0015 (× 1,5 = teleks kâğıdının 260 × 225 mm'si), sol üst
            // pivot FACE yuvasının köşesinde, z 0
            var canvasRect = face.FaceRect;
            Assert.IsNotNull(canvasRect);
            var canvas = canvasRect.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.AreNotEqual(0, (int)(canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.Normal), "ışıklı yazı için normal kanalı");
            Near(face.Layout.sizeMm, canvasRect.sizeDelta, 1e-4f, "tuval = tasarım ölçüsü");
            Near(new Vector2(DesignW * Px, 150), canvasRect.sizeDelta, 1e-3f, "tuval 173,33 × 150 tasarım mm'si");
            Near(new Vector2(0, 1), canvasRect.pivot, 1e-6f, "sol üst pivot");
            Near(Vector3.one * HandoverSlipFace.CanvasScale(face.Layout), canvasRect.localScale, 1e-7f, "1 birim = 1,5 mm (tasarım × 1,5)");
            Near(Vector3.one * 0.0015f, canvasRect.localScale, 1e-7f, "ölçek 0,0015");
            Assert.AreEqual(Quaternion.identity, canvasRect.localRotation, "ön yüz −z (fişin yüzü)");
            Near(new Vector3(-0.13f, 0.1125f, 0f), canvasRect.localPosition, 1e-5f, "FACE yuvasının sol üst köşesi, modelin düz düzlemi");
            var faceSlot = model.GetComponentInChildren<Rev3TextSlot>(true);
            if (faceSlot != null) Near(new Vector2(0.26f, 0.225f), faceSlot.Size, 1e-4f, "modelin FACE yuvası 260 × 225 mm");
            Assert.IsFalse(canvasRect.IsChildOf(model.transform), "tuval modelin kardeşi (iç içe modele nesne eklenmez)");
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var inner = contents.GetComponentInChildren<Rev3Model>(true).gameObject;
                Assert.IsEmpty(PrefabUtility.GetAddedGameObjects(inner), "iç içe modelde eklenen nesne yok");
                Assert.IsEmpty(PrefabUtility.GetAddedComponents(inner), "iç içe modelde eklenen bileşen yok");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }

            // Şablonlar pasif kökün altında, bağlı
            Assert.IsNotNull(face.FieldTemplate);
            Assert.IsNotNull(face.ShelterTemplate);
            Assert.IsNotNull(face.ExpectedTemplate);
            Assert.IsNotNull(face.WarningTemplate);
            Assert.IsFalse(face.FieldTemplate.transform.parent.gameObject.activeSelf, "şablon kökü pasif");
            Assert.AreEqual(HandoverSlipRowKind.Field, face.FieldTemplate.Kind);
            Assert.AreEqual(HandoverSlipRowKind.Shelter, face.ShelterTemplate.Kind);
            Assert.AreEqual(HandoverSlipRowKind.Expected, face.ExpectedTemplate.Kind);
            Assert.AreEqual(Image.Type.Tiled, face.FieldTemplate.Leader.type, "nokta çizgisi döşenir");

            // Yazılar: Courier Prime (CP-Regular / CP-Bold), fontun kendi (ışıksız) malzemesi, otomatik punto yok. Işıklı TMP
            // gölgelendiricisi World Space uGUI tuvalinde mürekkebi soldurur (Aşama 4 kabulü: K6 elde ad satırı #1E1B16 yerine
            // ~(175, 166, 151), kâğıt (209, 209, 198)); ışıksız mürekkep R19b_elde gibi koyu kalır
            var desk = DeskTheme.Active;
            var fonts = new[] { desk.Font(DeskFont.CpRegular), desk.Font(DeskFont.CpBold) };
            foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.IsInstanceOf<TextMeshProUGUI>(t, t.name);
                CollectionAssert.Contains(fonts, t.font, t.name + ": Courier Prime");
                Assert.AreSame(t.font.material, t.fontSharedMaterial, t.name + ": fontun kendi (ışıksız) malzemesi");
                Assert.AreSame(t.font.atlasTexture, t.fontSharedMaterial.GetTexture("_MainTex"), t.name + ": malzemenin atlası fontun");
                Assert.IsFalse(t.enableAutoSizing, t.name);
                Assert.IsFalse(t.raycastTarget, t.name);
            }
            foreach (var g in prefab.GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(g.raycastTarget, g.name + ": raycastTarget");

        }

        [Test]
        public void Material_And_Texture_FollowPlan()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) Assert.Ignore("Handover slip.mat yok (Aşama 4'ün tek seferlik kurulumu; script silindi): " + MaterialPath);
            Assert.AreEqual("Universal Render Pipeline/Lit", mat.shader.name);
            Assert.AreEqual(0f, mat.GetFloat("_Cull"), "Render Face: Both");
            Assert.AreEqual(1f, mat.GetFloat("_AlphaClip"), "alfa kırpma");
            Assert.AreEqual(0.3f, mat.GetFloat("_Cutoff"), 1e-4f);
            Assert.IsTrue(mat.IsKeywordEnabled("_ALPHATEST_ON"));
            Assert.AreEqual(0.08f, mat.GetFloat("_Smoothness"), 1e-4f, "telexPaper ile aynı");
            Assert.AreEqual(0f, mat.GetFloat("_Surface"), "opak");
            Assert.AreEqual(Color.white, mat.GetColor("_BaseColor"));
            string texPath = Rev3TextureBaker.Dir + "/" + Rev3TextureBaker.HandoverSlipTexture + ".png";
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Texture2D>(texPath), mat.GetTexture("_BaseMap"), "taban dokusu");

            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            Assert.AreEqual(TextureImporterNPOTScale.None, ti.npotScale, "NPOT küçültme yok");
            Assert.IsTrue(ti.mipmapEnabled, "mip");
            Assert.IsTrue(ti.sRGBTexture, "sRGB");

            var tex = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(texPath)));
                Assert.AreEqual(832, tex.width, "teleks kâğıdının 260 mm'si / 1,5 (4,8 px/mm)");
                Assert.AreEqual(720, tex.height);
                // satırlar alttan: CSS y → doku satırı 719 − y
                Color At(int x, int y) => tex.GetPixel(x, 719 - y);
                for (int y = 7; y < 720; y += 37)
                    for (int x = 0; x < 832; x += 23)
                        Assert.AreEqual(1f, At(x, y).a, 1e-3f, "alfa yalnız kesik kenarda (" + x + "; " + y + ")");
                Assert.Less(At(8, 1).a, 0.1f, "dişin dibi (x 8k, y 1) saydam");
                Assert.Greater(At(4, 1).a, 0.9f, "dişin tepesi (x 4 + 8k) kâğıt");
                var paper = At(240, 360);
                Assert.AreEqual(0xEF / 255f, paper.r, 0.01f, "kâğıt #EFE4BC");
                Assert.AreEqual(0xE4 / 255f, paper.g, 0.01f);
                Assert.AreEqual(0xBC / 255f, paper.b, 0.01f);
                var edge = At(0, 400);
                Assert.AreEqual(0xC9 / 255f, edge.r, 0.01f, "sol kenar #C9B98A");
                Assert.AreEqual(0xB9 / 255f, At(831, 400).g, 0.01f, "sağ kenar");
                Assert.AreEqual(0x8A / 255f, At(240, 719).b, 0.01f, "alt kenar");
                Assert.Less(At(5, 11).r, paper.r - 0.05f, "delik sırası (x 5,5; y 11)");
                Assert.AreEqual(paper.r, At(10, 11).r, 0.01f, "delikler arası kâğıt");
            }
            finally { Object.DestroyImmediate(tex); }
        }

        // ================================================================== örnek veri (R19c)

        static SlipFieldRow Field(string role, string name, params string[] items) =>
            new SlipFieldRow { role = role, name = name, items = items.ToList() };

        static SlipShelterRow Shelter(string name, string job, string subject = null, SlipPersonState state = SlipPersonState.None, int shifts = 0) =>
            new SlipShelterRow { name = name, job = job, subject = subject, state = state, stateShifts = shifts };

        /// <summary>R19c V.001 (ilk vardiya): <c>devir_fisi_v001_tr.html</c>.</summary>
        static HandoverSlipFacts V001()
        {
            var f = new HandoverSlipFacts
            {
                receiptId = "vardiya-0", shift = 1, copy = 1, alive = 8, capacity = 10,
                supplies = 24.0, suppliesCapacity = 60, suppliesAfter = 21.9,
                training = new SlipTraining { active = true, name = "Selin", subject = "tek", xpBefore = 2, xpAfter = 4, tierBefore = 1, tierAfter = 1 },
                production = new SlipProduction { kind = SlipProductionKind.None },
                openApplications = 0,
            };
            f.field.Add(Field("tek", "Cem", "kit_tek", "armor"));
            f.field.Add(Field("emp", "İpek", "kit_emp", "armor"));
            f.field.Add(Field("ana", "Zeynep", "kit_ana"));
            f.shelter.Add(Shelter("Suna", "craft"));
            f.shelter.Add(Shelter("Deniz", "cook"));
            f.shelter.Add(Shelter("Selin", "train", "tek"));
            f.shelter.Add(Shelter("Eren", "rest"));
            f.shelter.Add(Shelter("Nermin", "rest"));
            return f;
        }

        /// <summary>R19c V.031 (geç harekât): <c>devir_fisi_v031_tr.html</c>.</summary>
        static HandoverSlipFacts V031()
        {
            var f = new HandoverSlipFacts
            {
                receiptId = "vardiya-30", shift = 31, copy = 1, alive = 9, capacity = 10,
                supplies = 17.0, suppliesCapacity = 60, suppliesAfter = 15.6,
                training = new SlipTraining { active = true, name = "Suna", subject = "emp", xpBefore = 0, xpAfter = 2, tierBefore = 0, tierAfter = 1 },
                production = new SlipProduction { kind = SlipProductionKind.Progress, itemType = "armor", maker = "Rauf", done = 0, after = 1, turns = 1 },
                openApplications = 1,
            };
            f.field.Add(Field("tek", "Deniz", "kit_tek", "armor"));
            f.field.Add(Field("emp", "Cem", "kit_emp", "armor", "emp_cell"));
            f.field.Add(Field("ana", "Zeynep", "kit_ana", "medkit"));
            f.shelter.Add(Shelter("Rauf", "craft"));
            f.shelter.Add(Shelter("Nermin", "cook"));
            f.shelter.Add(Shelter("Suna", "train", "emp"));
            f.shelter.Add(Shelter("Selin", "rest", null, SlipPersonState.Hurt));
            f.shelter.Add(Shelter("İpek", "rest", null, SlipPersonState.Down, 2));
            f.shelter.Add(Shelter("Aylin", "rest", null, SlipPersonState.Restricted, 2));
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.DoorLastShift });
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.Exposure });
            return f;
        }

        /// <summary>
        /// En kötü gerçekçi fiş (E13): 10 canlı (3 saha + 7 sığınak), en uzun adlar (aday havuzu + sayı eki), her görev × olası
        /// durum (Core: yatan yalnız dinlenir, kısıtlı gözetmensiz yalnız dinlenir, yaralı her göreve girebilir), eğitim + üretim +
        /// kapı, KAPI (son vardiya) ve SIĞINAK EKSİK kutuları (dört parçalı ERZAK ayrı testte).
        /// </summary>
        static HandoverSlipFacts Worst()
        {
            var f = new HandoverSlipFacts
            {
                receiptId = "vardiya-118", shift = 119, copy = 1, alive = 10, capacity = 10,
                supplies = 59.9, suppliesCapacity = 60, suppliesAfter = 60.0,
                training = new SlipTraining { active = true, name = "Levent 31", subject = "craft", xpBefore = 1, xpAfter = 3, tierBefore = 0, tierAfter = 1 },
                production = new SlipProduction { kind = SlipProductionKind.Progress, itemType = "armor", maker = "Nermin 18", done = 0, after = 1, turns = 1 },
                openApplications = 1,
            };
            f.field.Add(Field("tek", "Levent 30", "kit_tek", "armor", "emp_cell"));
            f.field.Add(Field("emp", "Yalcin 12", "kit_emp", "armor", "medkit"));
            f.field.Add(Field("ana", "Feride 21", "kit_ana", "armor", "emp_cell"));
            f.shelter.Add(Shelter("Nermin 18", "craft", null, SlipPersonState.Hurt));
            f.shelter.Add(Shelter("Zeynep 27", "cook", null, SlipPersonState.Hurt));
            f.shelter.Add(Shelter("Levent 31", "train", "craft", SlipPersonState.Hurt));
            f.shelter.Add(Shelter("Gülizar", "rest", null, SlipPersonState.Down, 12));
            f.shelter.Add(Shelter("Mehmet 19", "rest", null, SlipPersonState.Restricted, 12));
            f.shelter.Add(Shelter("Kerem 22", "rest", null, SlipPersonState.RestrictionEnded));
            f.shelter.Add(Shelter("Orhan 25", "rest", null, SlipPersonState.Hurt));
            f.shelterShort = 2; f.shelterShortMax = 3;
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.DoorLastShift });
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.ShelterShort });
            return f;
        }

        // ================================================================== İngilizce tablo

        /// <summary>
        /// Fişin yeni anahtarlarının EN'i (PLAN §4) ve kullandığı var olan anahtarlar. Tablo (<c>strings.txt</c>) önce okunur;
        /// tabloda henüz yoksa (W6 çeviri güncellemesinden önce) buradaki değer kullanılır.
        /// </summary>
        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "KYGA · SIĞINAK 07", "KYGA · SHELTER 07" },
            { "No. {0:000}/{1}", "No. {0:000}/{1}" },
            { "VARDİYA {0:000} · DEVİR FİŞİ", "SHIFT {0:000} · HANDOVER SLIP" },
            { "{0}/{1} kişi · erzak {2}/{3}", "{0}/{1} people · rations {2}/{3}" },
            { "SAHA · BASKIN KADROSU", "FIELD · RAID TEAM" },
            { "SIĞINAK", "SHELTER" },
            { "KAPANIŞTA BEKLENEN", "EXPECTED AT CLOSING" },
            { "T E A Ü M B İ", "T E A P K C F" },
            { "kit", "kit" },
            { "zırh", "armour" },
            { "bakım kiti", "care kit" },
            { "EMP hücresi", "EMP cell" },
            { "ÜRETİM", "PRODUCTION" },
            { "MUTFAK", "KITCHEN" },
            { "EĞİTİM", "TRAINING" },
            { "EĞİTİM · {0}", "TRAINING · {0}" },
            { "Boşta", "Idle" },
            { "Teknisyen", "Technician" },
            { "EMP", "EMP" },
            { "Üretim", "Production" },
            { "yaralı", "injured" },
            { "yatıyor {0}", "laid up {0}" },
            { "kısıtlı {0}", "restricted {0}" },
            { "kısıtlı · bitti", "restricted · ended" },
            { "ERZAK", "RATIONS" },
            { "{0} → {1} / {2}", "{0} → {1} / {2}" },
            { "{0} · {1} +{2} ({3}→{4})", "{0} · {1} +{2} ({3}→{4})" },
            { "{0} · {1} +{2} → {3}", "{0} · {1} +{2} → {3}" },
            { "eğitimli", "trained" },
            { "eğitim yok", "no training" },
            { "iş emri yok · çıktı yok", "no work order · no output" },
            { "{0} · çıktı yok", "{0} · no output" },
            { "{0} {1}/{2} → {3}/{2} · {4}", "{0} {1}/{2} → {3}/{2} · {4}" },
            { "Zırh plakası", "Armour plate" },
            { "KAPI", "DOOR" },
            { "aday yok", "no candidates" },
            { "{0} aday bekliyor", "{0} candidates waiting" },
            { "KAPI ·", "DOOR ·" },
            { "{0} … +{1}", "{0} … +{1}" },
            { "plan {0:000}/{1} · tek kullanımlık · yeniden basılmaz", "plan {0:000}/{1} · single use · cannot be reprinted" },
            { "fişi EVRAK’a koy", "file the slip in PAPERS" },
        };

        static void UseEnglish()
        {
            var table = new Dictionary<string, string>();
            if (File.Exists(TablePath) && Loc.ParseTable(File.ReadAllText(TablePath)).TryGetValue(Loc.English, out var en))
                foreach (var kv in en) table[kv.Key] = kv.Value;
            foreach (var kv in English)
                if (!table.TryGetValue(kv.Key, out var v) || string.IsNullOrEmpty(v)) table[kv.Key] = kv.Value;
            Loc.SetLanguage(Loc.English, table);
        }

        // ================================================================== yardımcılar

        HandoverSlipFace Spawn()
        {
            var go = (GameObject)Object.Instantiate(LoadPrefab());
            spawned.Add(go);
            var face = go.GetComponent<HandoverSlipFace>();
            Assert.IsNotNull(face);
            return face;
        }

        static GameObject LoadPrefab()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (p == null) Assert.Ignore("Fiş prefab'ı yok (Aşama 4'ün tek seferlik kurulumu; script silindi): " + PrefabPath);
            return p;
        }

        static void Rows(List<HandoverSlipLines.Row> rows, string what, params (string disc, string name, string value, string suffix)[] expected)
        {
            Assert.AreEqual(expected.Length, rows.Count, what + " satır sayısı");
            for (int i = 0; i < expected.Length; i++)
            {
                string at = what + " " + i + ": ";
                Assert.AreEqual(expected[i].disc, rows[i].Disc, at + "disk");
                Assert.AreEqual(expected[i].name, rows[i].Name, at + "ad");
                Assert.AreEqual(expected[i].value, rows[i].Value, at + "değer");
                Assert.AreEqual(expected[i].suffix, rows[i].Suffix, at + "ek");
            }
        }

        static void Box(HandoverSlipLines.Box box, SlipWarningKind kind, string prefix, string text)
        {
            Assert.AreEqual(kind, box.Kind);
            Assert.AreEqual(prefix, box.Prefix, kind + " öneki");
            Assert.AreEqual(text, box.Text, kind + " metni");
        }

        static string Flatten(HandoverSlipLines l)
        {
            var parts = new List<string> { l.HeaderLeft, l.HeaderRight, l.Title, l.Subtitle, l.FieldLabel, l.ShelterLabel, l.ExpectedLabel, l.Footer, l.Action };
            foreach (var r in l.Field.Concat(l.Shelter).Concat(l.Expected)) parts.AddRange(new[] { r.Disc, r.Name, r.Value, r.Suffix, r.Strong });
            foreach (var b in l.Warnings) parts.AddRange(new[] { b.Prefix, b.Text });
            return string.Join("|", parts);
        }

        /// <summary>CSS "normal" satır yüksekliği: fontun satır yüksekliği / punto.</summary>
        static float NormalLine(TMP_Text t, float em) => t.font.faceInfo.lineHeight / t.font.faceInfo.pointSize * em;

        /// <summary>CSS satır kutusunda taban çizgisi: üst + (satır − (yükselen − inen) · em) / 2 + yükselen · em.</summary>
        static float CssBaseline(TMP_Text t, float top, float line, float em)
        {
            var fi = t.font.faceInfo;
            float asc = fi.ascentLine / fi.pointSize, content = (fi.ascentLine - fi.descentLine) / fi.pointSize;
            return top + (line - content * em) / 2 + asc * em;
        }

        static void NearBaseline(TMP_Text t, float top, float line, float em, string what)
        {
            var rt = t.rectTransform;
            Near(0.5f, rt.pivot.y, 1e-6f, what + ": pivot taban çizgisinde");
            Near(CssBaseline(t, top, line, em), -rt.anchoredPosition.y, Tol, what + " taban çizgisi");
            Near(em, t.fontSize, 1e-3f, what + " puntosu");
        }

        static void NearBaselineLocal(TMP_Text t, float line, float em, string what) => NearBaseline(t, 0, line, em, what);

        static float Top(RectTransform rt) => -rt.anchoredPosition.y - (1 - rt.pivot.y) * rt.sizeDelta.y;
        static float Bottom(RectTransform rt) => Top(rt) + rt.sizeDelta.y;
        static float Left(RectTransform rt) => rt.anchoredPosition.x - rt.pivot.x * rt.sizeDelta.x;

        static void NearBox(RectTransform rt, float x, float y, float w, float h, string what)
        {
            Near(x, Left(rt), Tol, what + " x");
            Near(y, Top(rt), Tol, what + " y");
            Near(w, rt.sizeDelta.x, Tol, what + " genişlik");
            Near(h, rt.sizeDelta.y, Tol, what + " yükseklik");
        }

        static void Near(float expected, float actual, float tol, string what) =>
            Assert.AreEqual(expected, actual, tol, what + ": " + actual.ToString("0.####") + " (beklenen " + expected.ToString("0.####") + ")");

        static void Near(Vector2 expected, Vector2 actual, float tol, string what) =>
            Assert.IsTrue((expected - actual).magnitude <= tol, what + ": " + actual.ToString("F5") + " ≠ " + expected.ToString("F5"));

        static void Near(Vector3 expected, Vector3 actual, float tol, string what) =>
            Assert.IsTrue((expected - actual).magnitude <= tol, what + ": " + actual.ToString("F5") + " ≠ " + expected.ToString("F5"));
    }
}
