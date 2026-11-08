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
    /// Revizyon 3 · Aşama 6 kabul kâğıtlarının yüzleri (PLAN W3, §1.6–§1.10, §1.16; E §3.4 T1–T4): kimlik/sevk belgesi 148 × 105, talimat
    /// şeridi 80 × 210 (Core v1/v2/v3 kuralları TR/EN sarar, küçülmez, taşmaz; işaretçi hizası), bulgu fişi 90 × 60 (bütün fiş türleri,
    /// Core'un görüşme metinleri TR/EN; harf harf döküm), DIŞ KAYIT talep formu (alanlar, karbon kopya, gözetmen yok), damga izleri,
    /// prefab kuralları (ışıksız TMP, raycast yok, kâğıt albedosu ≤ 0,8), dosya etiketi (FileTag TR/EN yuvaya sığar), kişi dosyasının
    /// etiketi bağlaması ve buğu dokusunun belirlenimciliği. Kâğıt prefab'ları Aşama 6'nın tek seferlik kurulumuyla (Rev3Stage6Papers)
    /// üretilir; yoksa o testler atlanır. Testler açık sahneye dokunmaz (yalnız örnek oluşturup siler).
    /// </summary>
    public class Rev3AdmissionPaperTests
    {
        const string PrefabDir = DeskSpriteManifest.Root + "/Prefabs/Rev3Admission";
        const string SlipPath = PrefabDir + "/FindingSlip.prefab";
        const string DocumentPath = PrefabDir + "/AdmissionDocument.prefab";
        const string StripPath = PrefabDir + "/OrdersStrip.prefab";
        const string RequestPath = PrefabDir + "/RequestForm.prefab";
        const string FileTagPath = Rev3ModelImporter.PrefabsDir + "/admission/FileTag.prefab";
        const string MaterialDir = Rev3ModelImporter.ArtDir + "/SceneMaterials/admission";
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const float Px = 0.25f;   // tasarımın 1× pikseli (4 px/mm)

        readonly List<Object> spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.DestroyImmediate(o);
            spawned.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ================================================================== T1 · kimlik/sevk belgesi

        [Test]
        public void Document_148x105_Layout()
        {
            foreach (bool en in new[] { false, true })
            {
                var face = Spawn<AdmissionDocumentFace>(DocumentPath);
                face.Bind(Document(en, "SUNA 9", en ? "Southern field station" : "Güney sahra istasyonu"));
                string lang = en ? "EN" : "TR";
                Assert.IsFalse(face.Overflowing, lang + ": taşma yok (sol " + face.MainBottomMm.ToString("0.0") + ", koçan " + face.StubBottomMm.ToString("0.0") + ")");
                Near(new Vector2(148, 105), face.FaceRect.sizeDelta, 1e-4f, "tuval 148 × 105 mm");
                Assert.IsTrue(face.Content.gameObject.activeSelf, "veri varken içerik açık");

                // KK kimlik_sevk_*: ad CP 700 28 px, meslek LF 600 17, etiketler CP 700 13, kodlar CP 700 16, yer LF 600 17
                Assert.AreEqual(28 * Px, face.NameText.fontSize, 1e-4f, "ad 7 mm");
                Assert.AreEqual(17 * Px, face.Profession.fontSize, 1e-4f, "meslek 4,25 mm");
                Assert.AreEqual(13 * Px, face.OriginLabel.fontSize, 1e-4f, "etiket 3,25 mm (okuma pozunda ≥ 14 px, D14)");
                Assert.AreEqual(16 * Px, face.Serial.fontSize, 1e-4f, "belge no 4 mm");
                Assert.AreEqual(17 * Px, face.Origin.fontSize, 1e-4f, "geldiği yer 4,25 mm");
                foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(false)) Assert.IsFalse(t.enableAutoSizing, t.name);

                // Koçan çizgisi x 412 px = 103 mm, üstten/alttan 2,5 mm; koçan sütunu 431 px = 107,75 mm
                var perf = face.Perforation.rectTransform;
                Assert.AreEqual(412 * Px, perf.anchoredPosition.x, 1e-3f, "koçan çizgisi 103 mm");
                Assert.AreEqual(-10 * Px, perf.anchoredPosition.y, 1e-3f);
                Assert.AreEqual(105 - 20 * Px, perf.sizeDelta.y, 1e-3f);
                Assert.AreEqual(Image.Type.Tiled, face.Perforation.type, "noktalar döşenir");
                Assert.AreEqual(431 * Px, AdmissionDocumentFace.StubLeftMm, 1e-4f);
                Assert.AreEqual(431 * Px, face.Referral.rectTransform.anchoredPosition.x, 1e-3f, "SEVK No. koçan sütununda");
                Assert.Less(Right(face.Origin), 412 * Px, lang + ": geldiği yer koçan çizgisinin solunda");

                // Dikey akış: künye (4,75 mm) → ad (10 mm'den, satır 7,7) → meslek → kırmızı çizgi → portre/alanlar
                float nameBase = -face.NameText.rectTransform.anchoredPosition.y;
                Assert.That(nameBase, Is.InRange(10f, 10f + 28 * 1.1f * Px), "adın taban çizgisi kendi satır kutusunda");
                Assert.Greater(-face.Profession.rectTransform.anchoredPosition.y, nameBase, "meslek adın altında");
                Assert.Greater(-face.Origin.rectTransform.anchoredPosition.y, -face.Profession.rectTransform.anchoredPosition.y);
                Assert.LessOrEqual(face.MainBottomMm, AdmissionDocumentFace.ContentBottomLimitMm + 0.01f);
                // "kapıda koparılır" koçanın altına yaslı, koçan içeriği üstünde biter
                float footBase = -face.StubFoot.rectTransform.anchoredPosition.y;
                Assert.That(footBase, Is.InRange(AdmissionDocumentFace.ContentBottomLimitMm - 4f, AdmissionDocumentFace.ContentBottomLimitMm));
                Assert.Less(face.StubBottomMm, footBase - 2f, lang + ": koçan notuyla çakışmaz");
                Assert.AreEqual("SUNA 9", face.NameText.text);
                Assert.AreEqual(en ? "S.005" : "V.005", face.Date.text, "TARİH = düzenleme vardiyası");
                Assert.IsFalse(face.Impression.gameObject.activeSelf, "iz yalnız basışta");
                if (face.Portrait.gameObject.activeSelf) Assert.Less(face.Portrait.uvRect.width, 0f, "sayılı ad aynalı portre (D18, PortraitFlip)");
            }
        }

        [Test]
        public void Document_Impressions_FourChoices_TrEn_InsideTheDocument()
        {
            var cases = new (string choice, string[] tr, string[] en, string hex)[]
            {
                ("admit", new[] { "KABUL", "TAM ERİŞİM" }, new[] { "ADMIT", "FULL ACCESS" }, "#24592E"),
                ("restricted", new[] { "KISITLI KABUL", "3 VARDİYA" }, new[] { "RESTRICTED ADMISSION", "3 SHIFTS" }, "#854005"),
                ("reject", new[] { "REDDET" }, new[] { "REJECT" }, "#9A1E14"),
                ("aid", new[] { "DIŞARIDAN YARDIM", "2 ERZAK" }, new[] { "OUTSIDE AID", "2 RATIONS" }, "#463F34"),
            };
            var face = Spawn<AdmissionDocumentFace>(DocumentPath);
            foreach (var c in cases)
                foreach (bool en in new[] { false, true })
                {
                    var d = Document(en, "AYLİN", "Kuzey tahliye hattı");
                    d.StampChoiceId = c.choice;
                    d.StampLines = en ? c.en : c.tr;
                    d.StampColor = c.hex;
                    face.Bind(d);
                    face.ShowImpression(true, false);
                    string at = c.choice + (en ? " EN" : " TR");
                    Assert.IsTrue(face.ImpressionVisible, at);
                    Assert.IsTrue(face.Impression.gameObject.activeSelf, at);
                    Assert.AreEqual(1f, face.ImpressionGroup.alpha, 1e-6f, at + ": animasyonsuz");
                    Assert.AreEqual(AdmissionDocumentFace.ImpressionTiltDeg, face.Impression.localEulerAngles.z, 1e-3f, at + ": −4° (CSS)");
                    Assert.AreEqual(d.StampLines[0], face.ImpressionLine1.text, at);
                    ColorUtility.TryParseHtmlString(c.hex, out var colour);
                    Assert.AreEqual(colour.r, face.ImpressionLine1.color.r, 0.01f, at + ": iz rengi veriden");
                    Assert.AreEqual(0.93f, face.ImpressionLine1.color.a, 0.01f, at + ": opaklık 0,93 (KK damga_*)");
                    var size = face.ImpressionSizeMm;
                    var centre = face.ImpressionCenterMm;
                    Assert.GreaterOrEqual(centre.x - size.x / 2f, 1.5f, at + ": iz belgenin solunda taşmaz");
                    Assert.LessOrEqual(centre.x + size.x / 2f, 148 - 1.5f, at + ": iz belgenin sağında taşmaz (EN KISITLI 149 mm tasarım genişliği)");
                    Assert.LessOrEqual(centre.y + size.y / 2f, 105 - 1.5f, at + ": iz belgenin altında taşmaz");
                    if (c.choice == "reject")
                    {
                        Assert.IsFalse(face.ImpressionInner.top.gameObject.activeSelf, at + ": REDDET tek kalın kenar");
                        Assert.IsFalse(face.ImpressionDivider.gameObject.activeSelf, at);
                        Assert.IsFalse(face.ImpressionLine2.gameObject.activeSelf, at);
                    }
                    else
                    {
                        Assert.IsTrue(face.ImpressionInner.top.gameObject.activeSelf, at + ": çift kenar");
                        Assert.AreEqual(d.StampLines[1], face.ImpressionLine2.text, at);
                        Assert.AreEqual(c.choice != "aid", face.ImpressionDivider.gameObject.activeSelf, at + ": bölme çizgisi KABUL ve KISITLI'da");
                    }
                    if (c.choice == "admit" && !en) Near(new Vector2(62f, 83.5f), centre, 0.01f, "KABUL izi belge merkezinden (−12; −31) mm");
                }

            // Basış: 0 → 1 stampPressS (0,12) içinde; gizleme hemen
            face.ShowImpression(true, true);
            Assert.AreEqual(0f, face.ImpressionGroup.alpha, 1e-6f);
            face.Advance(0.06f);
            Assert.AreEqual(0.5f, face.ImpressionGroup.alpha, 0.02f);
            face.Advance(0.1f);
            Assert.AreEqual(1f, face.ImpressionGroup.alpha, 1e-6f, "uçta tam");
            face.ShowImpression(false, false);
            Assert.IsFalse(face.Impression.gameObject.activeSelf);
            face.Bind(Document(false, "AYLİN", "Kuzey tahliye hattı"));
            face.ShowImpression(true, false);
            Assert.IsFalse(face.ImpressionVisible, "iz satırı yoksa iz yok");
            face.Bind(null);
            Assert.IsFalse(face.Content.gameObject.activeSelf, "boş yüz");
        }

        // ================================================================== T2 · talimat şeridi

        [Test]
        public void Strip_80x210_TrEn_v1v2v3()
        {
            var protocols = Protocols();
            foreach (bool en in new[] { false, true })
            {
                if (en) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                for (int v = 1; v <= 3; v++)
                {
                    var face = Spawn<OrdersStripFace>(StripPath);
                    var data = Strip(protocols, v, en);
                    face.Bind(data);
                    string at = "v" + v + (en ? " EN" : " TR");
                    Assert.IsFalse(face.Overflowing, at + ": taşma (kurallar " + face.RulesBottomMm.ToString("0.0") + " > alt blok " + face.BottomBlockTopMm.ToString("0.0") + ")");
                    Near(new Vector2(80, 210), face.FaceRect.sizeDelta, 1e-4f, "tuval 80 × 210 mm");
                    Assert.AreEqual(protocols[v].Rules.Count, face.RuleCount, at + ": Core'un kural sayısı");
                    Assert.LessOrEqual(face.RulesBottomMm, OrdersStripFace.ContentBottomLimitMm, at);
                    for (int i = 0; i < face.RuleCount; i++)
                    {
                        var slot = face.Rule(i);
                        Assert.AreEqual(18 * Px, slot.text.fontSize, 1e-4f, at + ": kural metni 4,5 mm (küçülmez)");
                        Assert.IsFalse(slot.text.enableAutoSizing, at);
                        Assert.AreEqual(TextWrappingModes.Normal, slot.text.textWrappingMode, at + ": sarar");
                        Assert.AreEqual(data.Rules[i].Text, slot.text.text, at);
                        Assert.LessOrEqual(Right(slot.text), OrdersStripFace.ContentLeftMm + OrdersStripFace.ContentWidthMm + 0.01f, at + ": kenarı geçmez");
                        Assert.AreEqual(data.Rules[i].IsNew, slot.pill.gameObject.activeSelf, at + ": YENİ kutusu yalnız yeni kuralda");
                        Assert.AreEqual(!data.Rules[i].IsNew && !string.IsNullOrEmpty(data.Rules[i].Tag), slot.tag.gameObject.activeSelf, at + ": 'sürüm n’den' eki");
                    }
                    for (int i = face.RuleCount; i < face.SlotCount; i++) Assert.IsFalse(face.Rule(i).root.gameObject.activeSelf, at + ": boş yuva kapalı");
                    Assert.AreEqual(v > 1, face.ArchiveTitle.gameObject.activeSelf, at + ": arşiv kutusu v2+");
                    Assert.AreEqual(data.Title, face.Title.text);
                    Assert.IsTrue(face.Footer2.gameObject.activeSelf, at + ": alt satır sağ (KYGA · KAPI)");
                    Assert.AreEqual(v > 1, face.Footer.gameObject.activeSelf, at + ": 'sürüm n arşivde' v2+");
                    TestContext.WriteLine(at + ": kurallar " + face.RulesBottomMm.ToString("0.0") + " mm, alt blok " + face.BottomBlockTopMm.ToString("0.0") + " mm");
                }
            }
        }

        [Test]
        public void RuleCenterMm_Monotonic_AndInsideMarkerRange()
        {
            var protocols = Protocols();
            foreach (bool en in new[] { false, true })
            {
                if (en) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                for (int v = 1; v <= 3; v++)
                {
                    var face = Spawn<OrdersStripFace>(StripPath);
                    face.Bind(Strip(protocols, v, en));
                    string at = "v" + v + (en ? " EN" : " TR");
                    // ilk madde: 3,75 + 4,5 + 1 + 8,625 + 0,25 + 5,2 + 2 + 0,5 + 3 = 28,825; kutu merkezi +3 (yeni kuralda +0,375 + 2)
                    float first = face.RuleCenterMm(0);
                    float expected = 28.825f + (protocols[v].Rules.Count > 0 && Strip(protocols, v, en).Rules[0].IsNew ? 0.375f + 2f : 0f) + 3f;
                    Assert.AreEqual(expected, first, 0.05f, at + ": ilk madde merkezi (CSS akışı)");
                    float prev = OrdersStripFace.MarkerZeroMm;
                    for (int i = 0; i < face.RuleCount; i++)
                    {
                        float c = face.RuleCenterMm(i);
                        Assert.Greater(c, prev + 5.9f, at + ": madde " + i + " öncekinin altında (kutu 6 mm)");
                        Assert.LessOrEqual(c, OrdersStripFace.MarkerZeroMm + OrdersStripFace.MarkerTravelMm, at + ": işaretçi aralığında (≤ 188 mm)");
                        prev = c;
                    }
                    Assert.AreEqual(OrdersStripFace.MarkerZeroMm, face.RuleCenterMm(-1), 1e-6f, "madde yok → en üst");
                    Assert.AreEqual(OrdersStripFace.MarkerZeroMm, face.RuleCenterMm(face.RuleCount), 1e-6f);
                }
            }
        }

        [Test]
        public void Strip_HeadOnly_ForThePocket()
        {
            var protocols = Protocols();
            var face = Spawn<OrdersStripFace>(StripPath);
            face.BindHead(Strip(protocols, 2, false));
            Assert.IsTrue(face.HeadOnly);
            Assert.IsTrue(face.Title.gameObject.activeSelf);
            Assert.AreEqual("SÜRÜM 2", face.Title.text);
            Assert.AreEqual(0, face.RuleCount, "ESKİ cebinde kurallar çizilmez");
            for (int i = 0; i < face.SlotCount; i++) Assert.IsFalse(face.Rule(i).root.gameObject.activeSelf);
            Assert.IsFalse(face.ArchiveTitle.gameObject.activeSelf);
            Assert.IsFalse(face.Footer.gameObject.activeSelf);
            face.Bind(Strip(protocols, 2, false));
            Assert.AreEqual(3, face.RuleCount, "tam şerit yeniden");
        }

        // ================================================================== T3 · bulgu fişi

        [Test]
        public void FindingSlip_90x60_AllTexts()
        {
            var interviews = InterviewKeys();
            Assert.AreEqual(3, interviews.Count, "üç hat görüşmesi bulundu");
            foreach (bool en in new[] { false, true })
            {
                if (en) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                var face = Spawn<FindingSlipFace>(SlipPath);
                Near(new Vector2(90, 60), face.FaceRect.sizeDelta, 1e-4f, "tuval 90 × 60 mm");
                var slips = Slips(en, interviews.Select(k => Loc.T(k)).ToList());
                foreach (var s in slips)
                {
                    face.Bind(s);
                    string at = (en ? "EN " : "TR ") + s.Key;
                    Assert.IsFalse(face.Overflowing, at + ": taşma (gövde " + face.ContentBottomMm.ToString("0.0") + " > alt " + face.FooterTopMm.ToString("0.0") + ", " + face.BodyLines + " satır)");
                    Assert.AreEqual(18 * Px, face.Body.fontSize, 1e-4f, at + ": gövde 4,5 mm (küçülmez)");
                    Assert.AreEqual(14 * Px, face.Header.fontSize, 1e-4f, at);
                    Assert.AreEqual(17 * Px, face.Title.fontSize, 1e-4f, at);
                    Assert.AreEqual(s.Body, face.Body.text, at);
                    Assert.AreEqual(s.Title, face.Title.text, at);
                    Assert.AreEqual(s.Shift, face.Shift.text, at);
                    Assert.AreEqual(!string.IsNullOrEmpty(s.Note), face.Note.gameObject.activeSelf, at);
                    Assert.AreEqual(!string.IsNullOrEmpty(s.Verdict), face.Stamp.gameObject.activeSelf, at);
                    if (!string.IsNullOrEmpty(s.Verdict))
                    {
                        Assert.AreEqual(s.Verdict, face.StampText.text, at);
                        Assert.AreEqual(FindingSlipFace.StampTiltDeg, face.Stamp.localEulerAngles.z, 1e-3f, at + ": −3° (CSS)");
                        Assert.AreEqual(s.Dashed, face.StampDashed.top.gameObject.activeSelf, at + ": kesikli kutu");
                        Assert.AreEqual(!s.Dashed, face.StampFrame.top.gameObject.activeSelf, at + ": düz kutu");
                        var expected = VerdictColour(s.Kind);
                        Assert.AreEqual(expected.r, face.StampText.color.r, 0.01f, at + ": damga rengi " + s.Kind);
                        Assert.AreEqual(expected.g, face.StampText.color.g, 0.01f, at);
                        // damga sağ alt köşede, kâğıdın içinde
                        Assert.LessOrEqual(face.Stamp.anchoredPosition.x + face.Stamp.sizeDelta.x / 2f, FindingSlipFace.ContentLeftMm + FindingSlipFace.ContentWidthMm + 0.01f, at);
                    }
                    Assert.IsTrue(face.Icon.gameObject.activeSelf, at + ": simge (araç ya da inceleme fişi)");
                    Assert.AreEqual(6f, face.Icon.rectTransform.sizeDelta.x, 1e-4f, at + ": simge 24 px = 6 mm");
                }
            }

            // Taşma algısı: sığmayacak gövde işaretlenir (sarar, küçülmez)
            var face2 = Spawn<FindingSlipFace>(SlipPath);
            face2.Bind(new SlipData { Tool = AdmissionTool.Interview, Header = "KYGA", Shift = "V.001", Title = "GÖRÜŞME YAP", Body = string.Join(" ", Enumerable.Repeat("uzun uzun anlatılan bir beyan", 20)), Verdict = "GÖRÜŞÜLDÜ", Key = "taşma" });
            Assert.IsTrue(face2.Overflowing, "taşan gövde işaretlenir");
            Assert.AreEqual(18 * Px, face2.Body.fontSize, 1e-4f, "taşınca da küçülmez");
        }

        [Test]
        public void FindingSlip_TypeDropsLettersOverTheGivenTime()
        {
            var face = Spawn<FindingSlipFace>(SlipPath);
            face.Bind(new SlipData { Tool = AdmissionTool.Interview, Header = "KYGA · İNCELEME BULGUSU", Shift = "V.005", Title = "GÖRÜŞME YAP",
                Body = "Tahliye yolunu sordun. Aday Kuzey tahliye hattından geldiğini söyledi.", Verdict = "GÖRÜŞÜLDÜ", Kind = SlipVerdict.Neutral, Key = "cand:resident-9:interview" });
            int n = face.BodyCharacters;
            Assert.Greater(n, 50);
            Assert.AreEqual(n, face.VisibleCharacters, "bağlanınca tam görünür");
            float total = n * 0.03f;   // admission.motion.transcriptPerCharS
            face.Type(total);
            Assert.IsTrue(face.Typing);
            Assert.AreEqual(0, face.VisibleCharacters);
            face.Advance(total / 2f);
            Assert.That(face.VisibleCharacters, Is.InRange(n / 2 - 1, n / 2 + 1), "yarı sürede yarısı");
            face.Bind(face.Data);
            Assert.IsTrue(face.Typing, "aynı fişin yeniden bağlanması dökümü kesmez");
            face.Advance(total);
            Assert.IsFalse(face.Typing);
            Assert.AreEqual(n, face.VisibleCharacters, "uçta tam");
            face.Type(total);
            face.Type(0f);
            Assert.IsFalse(face.Typing, "0 s = hemen tam");
            Assert.AreEqual(n, face.VisibleCharacters);
            face.Type(total);
            face.Bind(new SlipData { Tool = AdmissionTool.Papers, Header = "KYGA", Shift = "V.005", Title = "BELGELERİ KARŞILAŞTIR", Body = "Belge No. T-04/009 · sevk No. T-04/009.", Key = "başka" });
            Assert.IsFalse(face.Typing, "başka fiş: döküm biter");
            Assert.AreEqual(face.BodyCharacters, face.VisibleCharacters);
        }

        // ================================================================== T4 · talep formu

        [Test]
        public void RequestForm_Fields()
        {
            var face = Spawn<RequestFormFace>(RequestPath);
            Near(new Vector2(100, 1120 / 8.4f), face.FaceRect.sizeDelta, 1e-3f, "tuval 100 × 133,3 mm");
            var data = Request(false, "Cem", "Güney sahra istasyonu kaydı");
            face.Bind(data, false);
            Assert.IsFalse(face.Overflowing, "el yazısı satırlara sığar (gerekirse sıkışır)");
            string[] labels = { "AD", "BELGE No.", "SEVK No.", "MÜHÜR", "SORULACAK", "GÖZETMEN" };
            for (int i = 0; i < RequestFormFace.RowCount; i++)
            {
                Assert.AreEqual(labels[i], face.Row(i).label.text, "etiket " + i);
                Assert.AreEqual(i == RequestFormFace.SupervisorRow ? "Cem" : data.Values[i], face.Row(i).value.text, "değer " + i);
                Assert.GreaterOrEqual(face.Row(i).value.fontSize, 46f / 8.4f * RequestFormFace.MinValueScale - 1e-4f, "el yazısı en çok %70'e sıkışır");
                Assert.AreEqual(Signature.r, face.Row(i).value.color.r, 0.01f, "değerler kalem mavisi (pen.signature)");
            }
            Assert.AreEqual("V.005", face.Shift.text, "vardiya Values[ShiftIndex]");
            Assert.AreEqual("KYGA · DIŞ KAYIT TALEBİ", face.Header.text);
            Assert.IsTrue(face.SupervisorNote.gameObject.activeSelf, "(BOŞTA üst kartı)");
            Assert.AreEqual("sonuç sonraki kapanışta", face.FooterText.text);
            Assert.IsFalse(face.CopyMark.gameObject.activeSelf, "asıl yaprakta KOPYA yok");
            if (face.Paper != null) Assert.AreSame(face.SheetMaterial, face.Paper.sharedMaterial);
            // değerler satır çizgisinin üstünde, etiketler değer sütununun solunda
            Assert.Less(Right(face.Row(4).label), 320 / 8.4f, "SORULACAK etiketi değer sütununun solunda");

            face.Bind(data, true);
            Assert.IsTrue(face.CopyMark.gameObject.activeSelf, "karbonda KOPYA");
            Assert.AreEqual("KOPYA", face.CopyMark.text);
            if (face.Paper != null) Assert.AreSame(face.CarbonMaterial, face.Paper.sharedMaterial, "karbon malzemesi");
            var carbonInk = Tok("admission.paper.carbonInk", new Color32(0x2E, 0x3C, 0x6E, 0xFF));
            Assert.AreEqual(carbonInk.b, face.Row(0).value.color.b, 0.01f, "karbon mürekkebi");

            var missing = Request(false, "gözetmen yok · sonuç gelmez", "Güney sahra istasyonu kaydı");
            missing.SupervisorMissing = true;
            face.Bind(missing, true);
            var alarm = Tok("admission.paper.alarm", new Color32(0x9A, 0x1E, 0x14, 0xFF));
            Assert.AreEqual("gözetmen yok · sonuç gelmez", face.Row(RequestFormFace.SupervisorRow).value.text);
            Assert.AreEqual(alarm.r, face.Row(RequestFormFace.SupervisorRow).value.color.r, 0.01f, "gözetmen yok: kırmızı");
            Assert.IsFalse(face.SupervisorNote.gameObject.activeSelf, "üst kart notu gizli");

            // Etiket verilmezse prefab'ın sabit yazısı kalır
            var bare = Request(false, "İpek", "Kuzey tahliye hattı kaydı");
            bare.Labels = null;
            face.Bind(bare, false);
            Assert.AreEqual("AD", face.Row(0).label.text);
            face.Bind(null, false);
            Assert.IsFalse(face.Content.gameObject.activeSelf);

            UseEnglish();
            var en = Request(true, "İpek", "Southern field station record");
            face.Bind(en, false);
            Assert.IsFalse(face.Overflowing, "EN");
            Assert.AreEqual("SUPERVISOR", face.Row(RequestFormFace.SupervisorRow).label.text);
        }

        // ================================================================== prefab ve malzeme kuralları

        [Test]
        public void Faces_UnlitInk_NoRaycast()
        {
            var desk = DeskTheme.Active;
            var fonts = new[] { desk.Font(DeskFont.CpBold), desk.Font(DeskFont.LfRegular), desk.Font(DeskFont.LfSemiBold), desk.Font(DeskFont.KalamBold) };
            var cases = new (string path, System.Type type, Vector2 mm)[]
            {
                (SlipPath, typeof(FindingSlipFace), new Vector2(90, 60)),
                (DocumentPath, typeof(AdmissionDocumentFace), new Vector2(148, 105)),
                (StripPath, typeof(OrdersStripFace), new Vector2(80, 210)),
                (RequestPath, typeof(RequestFormFace), new Vector2(100, 1120 / 8.4f)),
            };
            foreach (var c in cases)
            {
                var prefab = Load(c.path);
                var face = (AdmissionPaperFace)prefab.GetComponent(c.type);
                Assert.IsNotNull(face, c.path + ": kökte yüz");
                Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(b => b == null), c.path + ": eksik script yok");
                Assert.IsEmpty(prefab.GetComponentsInChildren<GraphicRaycaster>(true), c.path + ": GraphicRaycaster yok");
                Assert.IsEmpty(prefab.GetComponentsInChildren<CanvasScaler>(true), c.path);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Mask>(true), c.path);

                // Tuval: World Space, kâğıt ölçüsü, 1 birim = 1 mm, sol üst pivot kâğıdın sol üst köşesinde, 0,4 mm önde (ön yüz −z)
                var canvasRect = face.FaceRect;
                var canvas = canvasRect.GetComponent<Canvas>();
                Assert.IsNotNull(canvas, c.path);
                Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, c.path);
                Near(c.mm, canvasRect.sizeDelta, 1e-3f, c.path + ": tuval");
                Near(new Vector2(0, 1), canvasRect.pivot, 1e-6f, c.path);
                Near(Vector3.one * 0.001f, canvasRect.localScale, 1e-7f, c.path);
                Near(new Vector3(-c.mm.x / 2000f, c.mm.y / 2000f, -0.0004f), canvasRect.localPosition, 1e-5f, c.path + ": sol üst köşe, kâğıdın 0,4 mm önünde");
                Assert.IsFalse(face.Content.gameObject.activeSelf, c.path + ": boş yüz (veri gelene dek)");

                // Kâğıt: Quad, kâğıt ölçüsünde, gölge düşürmez; ince çarpıştırıcı (okuma tıkı)
                var paper = prefab.GetComponentsInChildren<MeshRenderer>(true).Single();
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, paper.shadowCastingMode, c.path);
                Near(new Vector3(c.mm.x / 1000f, c.mm.y / 1000f, 1f), paper.transform.localScale, 1e-6f, c.path + ": Quad ölçüsü");
                var albedo = paper.sharedMaterial.GetColor("_BaseColor");
                Assert.LessOrEqual(Mathf.Max(albedo.r, albedo.g, albedo.b), 0.8f + 1e-4f, c.path + ": kâğıt albedosu ≤ 0,8 (admission.paperAlbedo)");
                var box = prefab.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, c.path);
                Near(new Vector3(c.mm.x / 1000f, c.mm.y / 1000f, 0.002f), box.size, 1e-6f, c.path + ": çarpıştırıcı");

                // Yazılar: uGUI, fontun kendi (ışıksız) malzemesi, raycast yok, otomatik punto yok
                foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.IsInstanceOf<TextMeshProUGUI>(t, c.path + " " + t.name);
                    CollectionAssert.Contains(fonts, t.font, c.path + " " + t.name + ": proje fontu");
                    Assert.AreSame(t.font.material, t.fontSharedMaterial, c.path + " " + t.name + ": fontun kendi (ışıksız) malzemesi");
                    Assert.IsFalse(t.enableAutoSizing, t.name);
                    Assert.IsFalse(t.raycastTarget, t.name);
                }
                foreach (var g in prefab.GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(g.raycastTarget, c.path + " " + g.name + ": raycastTarget");
                // Sabit etiketlerin kaynağı yazının kendisi (Türkçe)
                foreach (var loc in prefab.GetComponentsInChildren<DeskLocText>(true))
                    Assert.AreEqual(loc.Source, loc.GetComponent<TMP_Text>().text, c.path + " " + loc.name + ": DeskLocText kaynağı");
                AssertWired(face, c.path);
            }
        }

        [Test]
        public void Materials_FogIsTransparent_PapersMatchTheirSources()
        {
            var fog = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Cold fog.mat");
            if (fog == null) Assert.Ignore("Aşama 6 kâğıt kurulumu yapılmamış: " + MaterialDir);
            Assert.AreEqual("Universal Render Pipeline/Lit", fog.shader.name);
            Assert.AreEqual(1f, fog.GetFloat("_Surface"), "Transparent (özel shader yok, D9)");
            Assert.IsTrue(fog.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"));
            Assert.AreEqual((int)UnityEngine.Rendering.RenderQueue.Transparent, fog.renderQueue);
            var tex = fog.GetTexture("_BaseMap");
            Assert.IsNotNull(tex);
            Assert.AreEqual(Rev3TextureBaker.FogTexture, tex.name);
            Assert.AreEqual(0.05f, fog.GetFloat("_Smoothness"), 1e-4f);
            foreach (var name in new[] { "Admission paper", "Admission document", "Admission strip", "Admission request", "Admission carbon" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/" + name + ".mat");
                Assert.IsNotNull(m, name);
                var c = m.GetColor("_BaseColor");
                Assert.LessOrEqual(Mathf.Max(c.r, c.g, c.b), 0.8f + 1e-4f, name + ": albedo ≤ 0,8");
                Assert.AreEqual(0f, m.GetFloat("_Surface"), name + ": opak");
            }
        }

        // ================================================================== FileTag

        [Test]
        public void FileTag_TrEnFits()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FileTagPath);
            Assert.IsNotNull(prefab, FileTagPath + " (Aşama 1 model paketi)");
            var go = (GameObject)Object.Instantiate(prefab);
            spawned.Add(go);
            var label = go.GetComponentsInChildren<TMP_Text>(true).Single();
            var view = go.AddComponent<FileTagView>();
            view.Configure(label);
            float design = label.fontSize;
            var box = label.rectTransform.rect.size;
            Near(new Vector2(0.046f, 0.02f), box, 1e-5f, "TXT_Tag yuvası 46 × 20 mm");
            foreach (var text in new[] { "İNCELEME", "REVIEW", "KISITLI · 3 V", "LIMIT · 3 S", "KISITLI · 12 V", "LIMIT · 12 S" })
            {
                view.Bind(text);
                Assert.IsTrue(go.activeSelf, text);
                Assert.AreEqual(text, view.Text);
                Assert.IsFalse(view.Overflowing, text + ": yuvaya sığar (punto %" + (view.Scale * 100).ToString("0") + ")");
                label.ForceMeshUpdate(true, true);
                var b = label.textBounds.size;
                Assert.LessOrEqual(b.x, box.x + 0.0003f, text + ": genişlik (textBounds; preferredWidth 0,01 birime yuvarlanır)");
                Assert.LessOrEqual(b.y, box.y + 0.0003f, text + ": yükseklik");
                Assert.GreaterOrEqual(label.fontSize, design * FileTagView.MinScale - 1e-6f, text);
                var info = label.textInfo;
                for (int l = 1; l < info.lineCount; l++)
                {
                    int first = info.lineInfo[l].firstCharacterIndex;
                    Assert.IsTrue(char.IsWhiteSpace(info.characterInfo[first - 1].character), text + ": sözcük bölünmez");
                    Assert.AreNotEqual('·', info.characterInfo[first].character, text + ": satır noktayla başlamaz");
                }
                TestContext.WriteLine(text + ": punto %" + (view.Scale * 100).ToString("0") + ", " + info.lineCount + " satır, " + (b.x * 1000).ToString("0.0") + " × " + (b.y * 1000).ToString("0.0") + " mm");
            }
            Assert.AreEqual(1, Fit(view, label, "REVIEW"), "kısa sözcük tek satır, yuvanın puntosunda");
            Assert.AreEqual(1f, view.Scale, 1e-6f, "REVIEW küçülmez");
            view.Bind(null);
            Assert.IsFalse(go.activeSelf, "etiket yok: gizli");
            Assert.IsNull(view.Text);
            view.Bind("");
            Assert.IsFalse(go.activeSelf);
            view.Bind("İNCELEME");
            Assert.IsTrue(go.activeSelf, "yeniden takılır");
        }

        [Test]
        public void PersonFolder_TabSublines_BindAndDimWithTheirTab()
        {
            var folderGo = new GameObject("Kişi dosyası (test)");
            spawned.Add(folderGo);
            var folder = folderGo.AddComponent<PersonFolderView>();
            TextMeshPro Sub(string field)
            {
                var go = new GameObject(field);
                go.transform.SetParent(folderGo.transform, false);
                var text = go.AddComponent<TextMeshPro>();
                typeof(PersonFolderView).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(folder, text);
                return text;
            }
            var review = Sub("reviewSub");
            var access = Sub("accessSub");
            var dismiss = Sub("dismissSub");

            folder.Open(new FolderData { PersonId = "resident-10", Name = "Eren", ReviewSub = "dış kayıt", RestrictSub = "kısıtla", DismissSub = "ayır",
                CanReview = false, CanRestrict = true, CanDismiss = true });
            Assert.AreEqual("dış kayıt", review.text);
            Assert.AreEqual("kısıtla", access.text);
            Assert.AreEqual("ayır", dismiss.text);
            Assert.Less(review.alpha, 1f, "kapalı sekmenin alt satırı adıyla birlikte soluk");
            Assert.AreEqual(1f, access.alpha, 1e-5f, "açık sekme tam mürekkep");
            Assert.AreEqual(1f, dismiss.alpha, 1e-5f);
        }

        [Test]
        public void PersonFolder_OpenBindsTheTag_CloseRemovesIt()
        {
            var folderGo = new GameObject("Kişi dosyası (test)");
            spawned.Add(folderGo);
            var folder = folderGo.AddComponent<PersonFolderView>();
            var tagGo = new GameObject("FileTag (test)");
            tagGo.transform.SetParent(folderGo.transform, false);
            var tag = tagGo.AddComponent<FileTagView>();
            typeof(PersonFolderView).GetField("fileTag", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(folder, tag);
            Assert.AreSame(tag, folder.FileTag);

            folder.Open(new FolderData { PersonId = "resident-10", Name = "Eren", TagText = "İNCELEME" });
            Assert.IsTrue(tagGo.activeSelf, "inceleme fişi açık dosyada etiket");
            Assert.AreEqual("İNCELEME", tag.Text);
            folder.Open(new FolderData { PersonId = "resident-10", Name = "Eren", TagText = "KISITLI · 2 V" });
            Assert.AreEqual("KISITLI · 2 V", tag.Text, "açıkken veri yenilenince etiket de");
            folder.Close();
            Assert.IsFalse(tagGo.activeSelf, "kapanınca kalkar (seçim değişince bayat kalmaz)");
            folder.Open(new FolderData { PersonId = "resident-4", Name = "Suna", TagText = null });
            Assert.IsFalse(tagGo.activeSelf, "etiket yok");
            folder.SetTag("İNCELEME");
            Assert.IsTrue(tagGo.activeSelf, "sunucu doğrudan takabilir");
        }

        // ================================================================== buğu dokusu

        [Test]
        public void BakeFog_Deterministic()
        {
            var a = Rev3TextureBaker.FogPng();
            var b = Rev3TextureBaker.FogPng();
            CollectionAssert.AreEqual(a, b, "iki fırın aynı bayt");
            var px = Rev3TextureBaker.FogPixels();
            int n = Rev3TextureBaker.FogSize;
            Assert.AreEqual(512, n);
            Assert.AreEqual(n * n, px.Length);
            Color At(int x, int y) => px[y * n + x];
            Assert.GreaterOrEqual(At(256, 290).a, 0.9f, "degrade merkezi (256; 290) α ≥ 0,94'e yakın");
            Assert.AreEqual(0f, At(0, 0).a, 1e-4f, "köşe saydam");
            Assert.AreEqual(0f, At(511, 511).a, 1e-4f);
            Assert.Less(At(256, 10).a, 0.15f, "dış çembere yakın soluk (damlalar r ≤ 236 px)");
            // degrade monoton: merkezden kenara alfa azalır (damlalar dışında: aynı ışın boyunca ortalama)
            float Ring(float r) { float s = 0; int k = 0; for (int i = 0; i < 64; i++) { float t = i / 64f * Mathf.PI * 2; int x = 256 + Mathf.RoundToInt(Mathf.Cos(t) * r), y = 256 + Mathf.RoundToInt(Mathf.Sin(t) * r); if (x >= 0 && y >= 0 && x < n && y < n) { s += At(x, y).a; k++; } } return s / Mathf.Max(1, k); }
            Assert.Greater(Ring(60), Ring(160));
            Assert.Greater(Ring(160), Ring(240));
            // 420 damla: beyaz (r ≈ g ≈ b ≈ 1'e çekilmiş) pikseller var
            int bright = px.Count(c => c.a > 0.3f && c.r > 0.96f && c.g > 0.96f && c.b > 0.96f);
            Assert.Greater(bright, 500, "damlalar");
            var tex = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(tex.LoadImage(a));
                Assert.AreEqual(512, tex.width);
                Assert.AreEqual(512, tex.height);
            }
            finally { Object.DestroyImmediate(tex); }
        }

        // ================================================================== veri

        static DocumentData Document(bool en, string name, string origin) => new DocumentData
        {
            Name = name,
            Profession = en ? "Warehouse clerk" : "Depo görevlisi",
            Origin = origin,
            Issued = en ? "S.005" : "V.005",
            Serial = "N-17/204",
            Referral = "N-17/240",
            Seal = "KYGA-G",
            Stats = en ? "HP 3 · MOVE 3 · STR 1" : "CAN 3 · HAR 3 · GÜÇ 1",
            PortraitKey = "suna",
            PortraitFlip = true,
        };

        /// <summary>Tohum 79: v1 (V.000), v2 (V.004 kapanışı), v3 (V.008 kapanışı) Core protokolleri.</summary>
        static Dictionary<int, AdmissionProtocol> Protocols()
        {
            var sim = Simulation.NewGame(79, new Balance { liberationAlarmPerShift = 0 });
            var list = new Dictionary<int, AdmissionProtocol> { [sim.Admission.Protocol.Version] = sim.Admission.Protocol };
            for (int day = 1; day <= 8; day++)
            {
                var printed = sim.PrintShift();
                Assert.IsTrue(printed.Ok, printed.Msg);
                var filed = sim.FileShift(sim.Board.ReceiptId);
                Assert.IsTrue(filed.Ok, filed.Msg);
                list[sim.Admission.Protocol.Version] = sim.Admission.Protocol;
            }
            Assert.IsTrue(list.ContainsKey(1) && list.ContainsKey(2) && list.ContainsKey(3), "v1, v2, v3: " + string.Join(",", list.Keys));
            return list;
        }

        /// <summary>
        /// Şerit verisi (W4 kurucusunun biçimi, PLAN §1.9): Core kuralları çevrilir; önceki sürümde aynı anahtarlı kural "sürüm n’den",
        /// değilse (sürüm &gt; 1) YENİ; v2+ arşiv kutusu önceki sürümün ilk kuralı.
        /// </summary>
        static OrdersData Strip(Dictionary<int, AdmissionProtocol> protocols, int v, bool en)
        {
            var p = protocols[v];
            string issued = (p.IssuedShift + 1).ToString("000");
            var d = new OrdersData
            {
                Version = v,
                Header = en ? "KYGA · ADMISSION RULES" : "KYGA · KABUL TALİMATI",
                Number = "No. " + v,
                Title = (en ? "VERSION " : "SÜRÜM ") + v,
                Subtitle = en ? "valid from S." + issued + " · " + p.Rules.Count + " rules" : "V." + issued + "’ten geçerli · " + p.Rules.Count + " kural",
                Footer = v > 1 ? (en ? "version " + (v - 1) + " archived" : "sürüm " + (v - 1) + " arşivde") : null,
                Footer2 = en ? "KYGA · DOOR" : "KYGA · KAPI",
            };
            var prev = v > 1 ? protocols[v - 1] : null;
            for (int i = 0; i < p.Rules.Count; i++)
            {
                bool carried = prev != null && prev.Rules.Contains(p.Rules[i]);
                d.Rules.Add(new RuleData
                {
                    Number = (i + 1).ToString(),
                    Text = Loc.T(p.Rules[i]),
                    IsNew = prev != null && !carried,
                    Tag = carried ? (en ? "from version " : "sürüm ") + (v - 1) + (en ? "" : "’den") : prev != null ? (en ? "NEW · S." : "YENİ · V.") + issued : null,
                });
            }
            if (prev != null)
            {
                d.ArchiveTitle = (en ? "ARCHIVE · VERSION " : "ARŞİV · SÜRÜM ") + (v - 1) + (en ? " · S." : " · V.") + (prev.IssuedShift + 1).ToString("000");
                d.ArchiveRule = Loc.T(prev.Rules[0]);
            }
            return d;
        }

        /// <summary>Core'un üç görüşme metninin anahtarları (40 tohumda üç hat; Loc.N anahtarı, gösterimde Loc.T).</summary>
        static List<string> InterviewKeys()
        {
            var keys = new List<string>();
            for (uint seed = 1; seed <= 60 && keys.Count < 3; seed++)
            {
                var sim = Simulation.NewGame(seed, new Balance
                {
                    planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1, admissionProtocolInterval = 100,
                });
                for (int i = 0; i < 4 && sim.Admission.Candidate == null; i++)
                {
                    var printed = sim.PrintShift();
                    if (!printed.Ok) break;
                    if (!sim.FileShift(sim.Board.ReceiptId).Ok) break;
                }
                var c = sim.Admission.Candidate;
                if (c == null) continue;
                if (!sim.InspectApplicant("interview").Ok) continue;
                if (!string.IsNullOrEmpty(c.Interview) && !keys.Contains(c.Interview)) keys.Add(c.Interview);
            }
            return keys;
        }

        static List<SlipData> Slips(bool en, List<string> interviews)
        {
            string head = en ? "KYGA · CHECK FINDING" : "KYGA · İNCELEME BULGUSU";
            string shift = en ? "S.012" : "V.012";
            var list = new List<SlipData>
            {
                new SlipData { Tool = AdmissionTool.Papers, Header = head, Shift = shift, Title = en ? "COMPARE DOCUMENTS" : "BELGELERİ KARŞILAŞTIR",
                    Body = en ? "Doc No. T-04/009 · transfer No. T-04/009. Seal KYGA-K, issued S.005." : "Belge No. T-04/009 · sevk No. T-04/009. Mühür KYGA-K, düzenleme V.005.",
                    Verdict = en ? "MATCHES" : "EŞLEŞİYOR", Kind = SlipVerdict.Match, Key = "papers-match" },
                new SlipData { Tool = AdmissionTool.Papers, Header = head, Shift = shift, Title = en ? "COMPARE DOCUMENTS" : "BELGELERİ KARŞILAŞTIR",
                    Body = en ? "Doc No. N-17/204 · transfer No. N-17/240. Seal KYGA-G, issued S.012." : "Belge No. N-17/204 · sevk No. N-17/240. Mühür KYGA-G, düzenleme V.012.",
                    Note = en ? "N-17: outside record required (rule 1)" : "N-17: dış kayıt gerekir (madde 1)",
                    Verdict = en ? "MISMATCH" : "UYUŞMUYOR", Kind = SlipVerdict.Mismatch, Key = "papers-mismatch" },
                new SlipData { Tool = AdmissionTool.Breath, Header = head, Shift = shift, Title = en ? "COLD OBSERVATION" : "SOĞUK GÖZLEM",
                    Body = en ? "Breath fogs the cold glass." : "Soğuk camda nefes buğusu görüldü.",
                    Note = en ? "Fog alone does not prove a human." : "Buğu tek başına insanlık garantisi değildir.",
                    Verdict = en ? "FOG SEEN" : "BUĞU VAR", Kind = SlipVerdict.Neutral, Key = "breath-fog" },
                new SlipData { Tool = AdmissionTool.Breath, Header = head, Shift = shift, Title = en ? "COLD OBSERVATION" : "SOĞUK GÖZLEM",
                    Body = en ? "The cold glass stays clear." : "Soğuk camda nefes buğusu görülmedi.",
                    Note = en ? "Fog alone does not prove a human." : "Buğu tek başına insanlık garantisi değildir.",
                    Verdict = en ? "NO FOG" : "BUĞU YOK", Kind = SlipVerdict.Mismatch, Key = "breath-clear" },
                new SlipData { Tool = AdmissionTool.Record, Header = head, Shift = shift, Title = en ? "OUTSIDE RECORD CHECK" : "DIŞ KAYIT TEYİDİ",
                    Body = en ? "Supervisor: Zeynep · Southern field station record requested by radio." : "Gözetmen: Zeynep · Güney sahra istasyonu kaydı telsizle soruldu.",
                    Note = en ? "result at S.013 closing" : "sonuç V.013 kapanışında",
                    Verdict = en ? "PENDING" : "BEKLENİYOR", Kind = SlipVerdict.Pending, Dashed = true, Key = "record-pending" },
                new SlipData { Tool = AdmissionTool.Record, Header = head, Shift = en ? "S.013" : "V.013", Title = en ? "OUTSIDE RECORD CHECK" : "DIŞ KAYIT TEYİDİ",
                    Body = en ? "Southern field station record differs from the paper: Doc No. ≠ transfer No." : "Güney sahra istasyonu kaydı belgeden farklı: Belge No. ≠ sevk No.",
                    Verdict = en ? "RECORD DIFFERS" : "KAYIT FARKLI", Kind = SlipVerdict.Differs, Key = "record-differs" },
                new SlipData { Tool = AdmissionTool.Record, Header = head, Shift = en ? "S.013" : "V.013", Title = en ? "OUTSIDE RECORD CHECK" : "DIŞ KAYIT TEYİDİ",
                    Body = en ? "Northern evacuation route record matches the paper." : "Kuzey tahliye hattı kaydı belgeyle eşleşiyor.",
                    Verdict = en ? "RECORD FOUND" : "KAYIT BULUNDU", Kind = SlipVerdict.Found, Key = "record-found" },
                new SlipData { Tool = null, Review = true, Header = en ? "KYGA · REVIEW SLIP" : "KYGA · İNCELEME FİŞİ", Shift = en ? "S.009" : "V.009",
                    Title = en ? "ORDERS 3 · ITEM 2" : "TALİMAT 3 · MADDE 2",
                    Body = en ? "Matching old file: Suna 9, Eren, Banu +2. REVIEW tab on the file." : "Eşleşen eski dosya: Suna 9, Eren, Banu +2. Dosyasına İNCELE sekmesi.",
                    Verdict = en ? "REVIEW" : "İNCELE", Kind = SlipVerdict.Review, Key = "review:9" },
            };
            for (int i = 0; i < interviews.Count; i++)
                list.Add(new SlipData { Tool = AdmissionTool.Interview, Header = head, Shift = shift, Title = en ? "INTERVIEW" : "GÖRÜŞME YAP", Body = interviews[i],
                    Verdict = en ? "INTERVIEWED" : "GÖRÜŞÜLDÜ", Kind = SlipVerdict.Neutral, Key = "interview-" + i });
            return list;
        }

        static RequestData Request(bool en, string supervisor, string ask) => new RequestData
        {
            Sent = false, Enabled = true,
            Labels = en ? new[] { "NAME", "DOC No.", "TRANSFER No.", "SEAL", "ASK", "SUPERVISOR" } : new[] { "AD", "BELGE No.", "SEVK No.", "MÜHÜR", "SORULACAK", "GÖZETMEN" },
            Values = new[] { "Suna 9", "N-17/009", "N-17/009", "KYGA-K", ask, supervisor, en ? "S.005" : "V.005" },
            Supervisor = supervisor,
        };

        /// <summary>Tema rengi (yüzlerin okuduğu anahtar; yoksa tasarım değeri).</summary>
        static Color Tok(string key, Color fallback)
        {
            var t = SiginakTheme.Active;
            return t != null && t.TryGet(key, out var c) ? c : fallback;
        }

        static Color Signature => Tok("pen.signature", new Color32(0x1A, 0x2F, 0x5A, 0xFF));

        static Color VerdictColour(SlipVerdict k)
        {
            switch (k)
            {
                case SlipVerdict.Match: case SlipVerdict.Found: return Tok("admission.slipVerdict.match", new Color32(0x24, 0x59, 0x2E, 0xFF));
                case SlipVerdict.Mismatch: case SlipVerdict.Differs: return Tok("admission.slipVerdict.mismatch", new Color32(0x85, 0x40, 0x05, 0xFF));
                case SlipVerdict.Neutral: return Tok("admission.slipVerdict.neutral", new Color32(0x46, 0x3F, 0x34, 0xFF));
                default: return Tok("admission.slipVerdict.pending", new Color32(0x67, 0x5F, 0x50, 0xFF));
            }
        }

        // ================================================================== yardımcılar

        T Spawn<T>(string path) where T : AdmissionPaperFace
        {
            var go = (GameObject)Object.Instantiate(Load(path));
            spawned.Add(go);
            var face = go.GetComponent<T>();
            Assert.IsNotNull(face, path);
            return face;
        }

        static GameObject Load(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p == null) Assert.Ignore("Kâğıt prefab'ı yok (Aşama 6'nın tek seferlik kurulumu Rev3Stage6Papers.Apply çalışmamış): " + path);
            return p;
        }

        /// <summary>
        /// Unity'nin her bileşende serileştirdiği iç nesne alanları (<c>Object</c>/<c>Component</c>/<c>MonoBehaviour</c> tabanı).
        /// Prefab olmayan ya da prefab kaynağının kendisi olan nesnede <c>m_CorrespondingSourceObject</c>/<c>m_PrefabInstance</c>
        /// doğal olarak boştur; bunlar yüzün bağları değildir.
        /// </summary>
        static readonly HashSet<string> UnityInternalRefs = new HashSet<string>
        {
            "m_Script", "m_GameObject", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
        };

        /// <summary>Yüzün bütün [SerializeField] nesne bağları dolu (dizi öğeleri ve çerçeveler dahil; Unity iç alanları hariç).</summary>
        static void AssertWired(Object face, string what)
        {
            var so = new SerializedObject(face);
            var p = so.GetIterator();
            var missing = new List<string>();
            bool enter = true;
            while (p.Next(enter))
            {
                enter = true;
                if (p.depth == 0 && UnityInternalRefs.Contains(p.propertyPath)) { enter = false; continue; }
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null) missing.Add(p.propertyPath);
            }
            Assert.IsEmpty(missing, what + ": bağlanmamış alanlar: " + string.Join(", ", missing));
        }

        static int Fit(FileTagView view, TMP_Text label, string text)
        {
            view.Bind(text);
            label.ForceMeshUpdate(true, true);
            return label.textInfo.lineCount;
        }

        /// <summary>
        /// Sola yaslı yazının çizilen sağ kenarı (tuvalin solundan mm). Yüzler sola yaslı yazının pivotunu sol kenara koyar
        /// (<c>PlaceLine</c>, pivot x 0): yazının yereli x 0 = kutunun sol kenarı = <c>anchoredPosition.x</c>.
        /// </summary>
        static float Right(TMP_Text t)
        {
            t.ForceMeshUpdate(true, true);
            Assert.AreEqual(0f, t.rectTransform.pivot.x, 1e-6f, t.name + ": sola yaslı pivot");
            return t.rectTransform.anchoredPosition.x + t.textBounds.max.x;
        }

        static void UseEnglish()
        {
            var table = new Dictionary<string, string>();
            if (File.Exists(TablePath) && Loc.ParseTable(File.ReadAllText(TablePath)).TryGetValue(Loc.English, out var en))
                foreach (var kv in en) table[kv.Key] = kv.Value;
            Loc.SetLanguage(Loc.English, table);
        }

        static void Near(Vector2 expected, Vector2 actual, float tol, string what)
        {
            Assert.AreEqual(expected.x, actual.x, tol, what + " x");
            Assert.AreEqual(expected.y, actual.y, tol, what + " y");
        }

        static void Near(Vector3 expected, Vector3 actual, float tol, string what)
        {
            Assert.AreEqual(expected.x, actual.x, tol, what + " x");
            Assert.AreEqual(expected.y, actual.y, tol, what + " y");
            Assert.AreEqual(expected.z, actual.z, tol, what + " z");
        }
    }
}
