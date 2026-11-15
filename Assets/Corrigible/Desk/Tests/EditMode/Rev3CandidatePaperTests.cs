using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 2 · aday kartının (148 × 105 mm) ve kapı fişinin (210 × 74 mm) yüzleri (tasarım §7, §8): gerçek grup verisiyle TR/EN
    /// yerleşim, metin taşması taraması (her kademe × her şüphe satırı × en dolu özellik takımları × iki dil; en uzun adlar), KABUL izi
    /// kartın içinde, fişte on satır / seçili satır / satır kutuları / GERİ ÇEVİR izi. Prefab'lar tek seferlik kurulumla üretilir
    /// (<c>CandidateCard.prefab</c>, <c>CandidateGroupSlip.prefab</c>); yoksa testler atlanır. Açık sahneye dokunmaz.
    /// </summary>
    public sealed class Rev3CandidatePaperTests
    {
        const float Px = 0.25f;
        /// <summary>Havuzun en uzun adı, sayılı hâliyle (ad tükenince "Ad n" verilir).</summary>
        const string LongName = "Yasemin 128";

        readonly List<Object> spawned = new List<Object>();

        /// <summary>Kâğıdın uyarı kırmızısı (<c>admission.paper.alarm</c>): BEDEL etiketi, YER YOK, SON VARDİYA, seçili satır.</summary>
        static Color Alarm => SiginakTheme.Active.ColorOf("admission.paper.alarm");

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.DestroyImmediate(o);
            spawned.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ---------------------------------------------------------------- kurulum

        T Spawn<T>(string path) where T : AdmissionPaperFace
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Assert.Ignore("Kâğıt prefab'ı yok (KOS aşama 2 kurulumu çalışmamış): " + path);
            var go = Object.Instantiate(prefab);
            spawned.Add(go);
            if (!go.activeSelf) go.SetActive(true);
            var face = go.GetComponent<T>();
            Assert.IsNotNull(face, path);
            return face;
        }

        static void Use(bool english)
        {
            if (english) Rev3AdmissionEnglish.Use(); else Loc.SetLanguage(Loc.Turkish);
        }

        static Simulation Game(uint seed, int capacity = 0, int emergencyBelow = -1)
        {
            var b = new Balance { shelterEventsEnabled = false, planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1 };
            if (capacity > 0) b.boardResidentCapacity = capacity;
            if (emergencyBelow >= 0) b.candidateEmergencyBelow = emergencyBelow;
            var sim = Simulation.NewGame(seed, b);
            ToGroup(sim);
            return sim;
        }

        static void ToGroup(Simulation sim)
        {
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++)
            {
                Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            }
            Assert.Greater(sim.Admission.Group.Count, 0, "kurulum: kapıda aday grubu");
        }

        static CandidateGroupData Group(Simulation sim, string replaceId = null) => AdmissionSnapshotBuilder.Build(sim,
            new AdmissionUi { ReplaceForGroup = AdmissionSnapshotBuilder.GroupKey(sim), ReplaceId = replaceId }).Group;

        static string Describe(CandidateCardData d) =>
            d.Name + " · " + d.TierName + " · " + d.Suspicion + " · " + string.Join(" + ", d.Perks.Select(p => p.Id));

        /// <summary>Kurucunun biçimiyle (aynı Core metinleri) sentetik kart: tarama için.</summary>
        static CandidateCardData Card(Balance b, string name, int tier, string suspicionKey, string subject, params string[] perks)
        {
            var d = new CandidateCardData
            {
                Id = "resident-99", Name = DeskText.Upper(name), PortraitKey = "suna", Arrived = Loc.F("V.{0:000}", 128),
                Mastery = Loc.F("{0} · {1}", PlanningBoardSnapshotBuilder.TopicLabel(subject), PlanningBoardSnapshotBuilder.TierWord(2)),
                Stats = Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", 4, 4, 3, 2),
                Tier = tier, TierName = DeskText.Upper(CandidateData.TierName(tier)),
                Suspicion = CandidateData.SuspicionLine(suspicionKey, "Zeynep 12"),
                CanChoose = true,
            };
            foreach (string perk in perks)
                d.Perks.Add(new PerkRowData
                {
                    Id = perk, Cost = CandidateData.IsCost(perk), Tag = CandidateData.IsCost(perk) ? Loc.T("BEDEL") : Loc.T("GETİRİ"),
                    Name = CandidateData.PerkName(perk, b), Effect = CandidateData.PerkEffect(perk, b, subject),
                });
            return d;
        }

        // ---------------------------------------------------------------- aday kartı

        /// <summary>Gerçek gruplar (tohum 79: iki kart; tohum 3: üç kart; acil grup), TR ve EN: alanlar veriden, puntolar sabit, taşma yok.</summary>
        [Test]
        public void Card_148x105_Layout_TrEn()
        {
            var face = Spawn<CandidateCardFace>(AdmissionFixtures.CardPrefab);
            Assert.AreEqual(new Vector2(148, 105), face.SizeMm);
            foreach (bool en in new[] { false, true })
                foreach (var sim in new[] { Game(79), Game(3), Game(11), Game(79, emergencyBelow: 9) })
                {
                    Use(en);
                    foreach (var d in Group(sim).Cards)
                    {
                        string at = (en ? "EN " : "TR ") + Describe(d);
                        face.Bind(d);
                        Assert.IsFalse(face.Overflowing, at + ": taşma (içerik altı " + face.ContentBottomMm.ToString("0.0") + " mm)");
                        Assert.LessOrEqual(face.ContentBottomMm, CandidateCardFace.ContentBottomLimitMm + 0.01f, at);
                        Assert.AreEqual(148f, face.FaceRect.sizeDelta.x, 1e-3f, "tuval 148 mm");
                        Assert.AreEqual(105f, face.FaceRect.sizeDelta.y, 1e-3f, "tuval 105 mm");
                        Assert.IsTrue(face.Content.gameObject.activeSelf, "veri varken içerik açık");
                        Assert.AreEqual(d.Name, face.NameText.text, at);
                        Assert.AreEqual(d.Arrived, face.Issued.text, at);
                        Assert.AreEqual(d.Mastery, face.Profession.text, at);
                        Assert.AreEqual(d.Suspicion, face.Origin.text, at);
                        Assert.AreEqual(d.Stats, face.Stats.text, at);
                        Assert.AreEqual(d.TierName, face.TierText.text, at);
                        Assert.IsTrue(ColorUtility.TryParseHtmlString(d.TierColor, out var tierColour));
                        Assert.AreEqual(tierColour.r, face.TierText.color.r, 0.01f, at + ": kademe rengi veriden");
                        Assert.AreEqual(tierColour.g, face.TierText.color.g, 0.01f, at);
                        Assert.AreEqual(28 * Px, face.NameText.fontSize, 1e-4f, "ad 7 mm");
                        Assert.AreEqual(17 * Px, face.Origin.fontSize, 1e-4f, "şüphe satırı 4,25 mm");
                        for (int i = 0; i < CandidateCardFace.MaxPerks; i++)
                        {
                            bool used = i < d.Perks.Count;
                            Assert.AreEqual(used, face.PerkName(i).gameObject.activeSelf, at + ": özellik satırı " + i);
                            Assert.AreEqual(used, face.PerkEffect(i).gameObject.activeSelf, at);
                            if (!used) continue;
                            Assert.AreEqual(d.Perks[i].Tag, face.PerkTag(i).text, at);
                            Assert.AreEqual(d.Perks[i].Name, face.PerkName(i).text, at);
                            Assert.AreEqual(d.Perks[i].Effect, face.PerkEffect(i).text, at);
                            Assert.AreEqual(16 * Px, face.PerkEffect(i).fontSize, 1e-4f, "etki 4 mm");
                            var alarm = Alarm;
                            Assert.AreEqual(d.Perks[i].Cost, Mathf.Abs(face.PerkTag(i).color.r - alarm.r) < 0.01f && Mathf.Abs(face.PerkTag(i).color.g - alarm.g) < 0.01f,
                                at + ": BEDEL etiketi uyarı kırmızısı, GETİRİ değil");
                            if (i > 0)
                                Assert.Greater(-face.PerkName(i).rectTransform.anchoredPosition.y, -face.PerkEffect(i - 1).rectTransform.anchoredPosition.y,
                                    at + ": satırlar alt alta");
                        }
                        foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(false)) Assert.IsFalse(t.enableAutoSizing, t.name + ": yazı küçülmez");
                        Assert.IsFalse(face.Impression.gameObject.activeSelf, at + ": iz yalnız basışta");
                        Assert.IsFalse(face.ImpressionVisible);
                    }
                }
            face.Bind(null);
            Assert.IsFalse(face.Content.gameObject.activeSelf, "boş yüz");
        }

        /// <summary>
        /// §8 metin taşması: her kademe × o kademenin her şüphe satırı (kefilde sayılı kurucu adı) × Core'un üretebildiği en dolu özellik
        /// takımları (kademe 0: tek getiri ya da bedel; kademe 1: tek getiri; kademe 2: iki ayrı getiri; acilde hepsine ek <c>kit</c>) ×
        /// her saha konusu × TR/EN, en uzun adla. Hiçbir kart taşmaz.
        /// </summary>
        [Test]
        public void Card_WorstCase_FitsInBothLanguages()
        {
            var face = Spawn<CandidateCardFace>(AdmissionFixtures.CardPrefab);
            var b = new Balance { shelterEventsEnabled = false };
            var over = new List<string>();
            int cases = 0;
            float deepest = 0f;
            void Check(CandidateCardData d, bool en)
            {
                cases++;
                face.Bind(d);
                deepest = Mathf.Max(deepest, face.ContentBottomMm);
                if (face.Overflowing) over.Add((en ? "EN " : "TR ") + Describe(d) + " → " + face.ContentBottomMm.ToString("0.0") + " mm");
            }
            string[] good = CandidateData.Benefits, bad = CandidateData.Costs;
            foreach (bool en in new[] { false, true })
            {
                Use(en);
                // Şüphe satırları: her kademe, en dolu olağan takımıyla (kademe 2: iki getiri)
                for (int tier = 0; tier < 3; tier++)
                    foreach (string key in CandidateData.Suspicions[tier])
                        Check(Card(b, LongName, tier, key, "emp", tier == 2 ? new[] { "devre", "erzak" } : new[] { "devre" }), en);

                foreach (string subject in PlanningBoardData.CandidateSubjects)
                {
                    bool field = PlanningBoardData.FieldJobs.Contains(subject);
                    // Olağan grup: kademe 0 tek özellik (getiri ya da bedel), kademe 1 tek getiri, kademe 2 iki ayrı getiri
                    foreach (string perk in good.Concat(bad))
                    {
                        if (perk == "kit" && !field) continue;
                        Check(Card(b, LongName, 0, "kefil", subject, perk), en);
                    }
                    foreach (string first in good)
                        foreach (string second in good)
                        {
                            if (first == second || (!field && (first == "kit" || second == "kit"))) continue;
                            Check(Card(b, LongName, 2, "gece_gecis", subject, first, second), en);
                        }
                    // Acil grup (yalnız saha konuları): çekilen kit devreye/erzağa çevrilir, sonra bağlı kit en sona eklenir
                    if (!field) continue;
                    foreach (string perk in good.Where(p => p != "kit").Concat(bad))
                        Check(Card(b, LongName, 0, "kefil", subject, perk, "kit"), en);
                    foreach (string first in good.Where(p => p != "kit"))
                        foreach (string second in good.Where(p => p != "kit"))
                            if (first != second) Check(Card(b, LongName, 2, "gece_gecis", subject, first, second, "kit"), en);
                }
            }
            Assert.Greater(cases, 300, "tarama anlamlı");
            Assert.IsEmpty(over, over.Count + "/" + cases + " kart taşıyor (sınır " + CandidateCardFace.ContentBottomLimitMm.ToString("0.0") + " mm):\n" + string.Join("\n", over.Take(30)));
            TestContext.WriteLine(cases + " kart · en derin içerik " + deepest.ToString("0.0") + " mm / " + CandidateCardFace.ContentBottomLimitMm.ToString("0.0") + " mm");
        }

        /// <summary>KABUL izi kartın içinde kalır (TR ve EN), 4° dönük, rengi veriden; basış 0 → 1 <c>stampPressS</c> içinde; izsiz veri iz göstermez.</summary>
        [Test]
        public void Card_Impression_InsideTheCard()
        {
            var face = Spawn<CandidateCardFace>(AdmissionFixtures.CardPrefab);
            var sim = Game(3);
            foreach (bool en in new[] { false, true })
            {
                Use(en);
                var g = Group(sim);
                AdmissionSnapshotBuilder.StampCard(g, g.Cards[0].Id, sim.B);
                var d = g.Cards[0];
                face.Bind(d);
                face.ShowImpression(true, false);
                string at = en ? "EN" : "TR";
                Assert.IsTrue(face.ImpressionVisible, at);
                Assert.IsTrue(face.Impression.gameObject.activeSelf, at);
                Assert.AreEqual(1f, face.ImpressionGroup.alpha, 1e-6f, at + ": animasyonsuz");
                Assert.AreEqual(CandidateCardFace.ImpressionTiltDeg, face.Impression.localEulerAngles.z, 1e-3f, at);
                Assert.AreEqual(d.StampLines[0], face.ImpressionLine1.text, at);
                Assert.AreEqual(d.StampLines[1], face.ImpressionLine2.text, at);
                Assert.AreEqual(en ? "ADMIT" : "KABUL", face.ImpressionLine1.text);
                Assert.IsTrue(ColorUtility.TryParseHtmlString(d.StampColor, out var colour));
                Assert.AreEqual(colour.g, face.ImpressionLine1.color.g, 0.01f, at + ": iz rengi veriden");
                var size = face.ImpressionSizeMm;
                var centre = face.ImpressionCenterMm;
                Assert.Greater(size.x, 20f, at);
                Assert.GreaterOrEqual(centre.x - size.x / 2f, 1.5f, at + ": iz kartın solundan taşmaz");
                Assert.LessOrEqual(centre.x + size.x / 2f, 148 - 1.5f, at + ": iz kartın sağından taşmaz");
                Assert.GreaterOrEqual(centre.y - size.y / 2f, 1.5f, at + ": iz kartın üstünden taşmaz");
                Assert.LessOrEqual(centre.y + size.y / 2f, 105 - 1.5f, at + ": iz kartın altından taşmaz");
                Assert.LessOrEqual(centre.y + size.y / 2f, face.PerksBottomMm, at + ": iz özellik satırlarının altına inmez");
                Assert.IsFalse(face.Overflowing, at + ": iz taşma saymaz");
            }

            Use(false);
            face.ShowImpression(true, true);
            Assert.AreEqual(0f, face.ImpressionGroup.alpha, 1e-6f, "basışın başı");
            face.Advance(SiginakTheme.Active.NumberOf("admission.motion.stampPressS", 0.12f) / 2f);
            Assert.AreEqual(0.5f, face.ImpressionGroup.alpha, 0.02f);
            face.Advance(1f);
            Assert.AreEqual(1f, face.ImpressionGroup.alpha, 1e-6f, "uçta tam");
            face.ShowImpression(false, false);
            Assert.IsFalse(face.Impression.gameObject.activeSelf);
            face.Bind(Group(sim).Cards[1]);
            face.ShowImpression(true, false);
            Assert.IsFalse(face.ImpressionVisible, "iz satırı yoksa iz yok");
        }

        // ---------------------------------------------------------------- kapı fişi

        /// <summary>Kapasite 10'da dolu sığınak: 8 kurucu + alınan iki aday, kapıda üçüncü grup.</summary>
        static Simulation TenAlive(uint seed = 79)
        {
            var sim = Game(seed);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(sim.ChooseCandidate(sim.Admission.Group[0].Resident.Id).Ok);
                ToGroup(sim);
            }
            Assert.AreEqual(10, sim.Board.People.Count(p => p.Alive), "kurulum: 10/10");
            return sim;
        }

        /// <summary>
        /// Dolu sığınakta (10/10) fiş, TR ve EN: on satır iki sütunda, seçili satır ve işareti uyarı kırmızısı, özet satırı, satır kutuları
        /// kendi dizinine eşlenir (<c>RowAt</c>), sol blok damga alanı; taşma yok. En uzun adla da sığar.
        /// </summary>
        [Test]
        public void Slip_210x74_TenRows_Selected_TrEn()
        {
            var face = Spawn<CandidateGroupSlipFace>(AdmissionFixtures.GroupSlipPrefab);
            Assert.AreEqual(new Vector2(210, 74), face.SizeMm);
            var sim = TenAlive();
            var alive = sim.Board.People.Where(p => p.Alive).ToList();
            string marked = alive[6].Id;
            foreach (bool en in new[] { false, true })
            {
                Use(en);
                string at = en ? "EN" : "TR";
                var g = Group(sim, marked);
                Assert.IsTrue(g.Full);
                Assert.AreEqual(10, g.Replace.Count);
                face.Bind(g);
                Assert.IsFalse(face.Overflowing, at + ": taşma (içerik altı " + face.ContentBottomMm.ToString("0.0") + " mm)");
                Assert.AreEqual(210f, face.FaceRect.sizeDelta.x, 1e-3f);
                Assert.AreEqual(74f, face.FaceRect.sizeDelta.y, 1e-3f);
                Assert.AreEqual(10, face.RowCount);
                Assert.AreEqual(g.SlipHeader, face.Header.text);
                Assert.AreEqual(g.SlipShift, face.Shift.text);
                Assert.AreEqual(g.SlipTitle, face.Title.text);
                Assert.AreEqual(g.WaitLine, face.Body.text);
                Assert.AreEqual(g.HintLine, face.Hint.text);
                Assert.AreEqual(g.RoomTitle, face.RoomTitle.text);
                Assert.AreEqual(en ? "NO ROOM · 10/10" : "YER YOK · 10/10", face.RoomTitle.text);
                Assert.AreEqual(g.RoomHint, face.RoomHint.text);
                Assert.AreEqual(g.ReplaceLine, face.ReplaceLine.text);
                Assert.IsTrue(face.ReplaceLine.gameObject.activeSelf);
                Assert.IsFalse(face.Note.gameObject.activeSelf, at + ": acil satırı yok");
                var alarm = Alarm;
                Assert.AreEqual(alarm.r, face.RoomTitle.color.r, 0.01f, at + ": YER YOK uyarı renginde");
                for (int i = 0; i < 10; i++)
                {
                    var row = g.Replace[i];
                    Assert.IsTrue(face.Row(i).gameObject.activeSelf, at + " satır " + i);
                    Assert.AreEqual(row.Label, face.Row(i).text, at);
                    Assert.IsTrue(face.RowMark(i).gameObject.activeSelf, at);
                    bool red = Mathf.Abs(face.Row(i).color.r - alarm.r) < 0.01f && Mathf.Abs(face.Row(i).color.g - alarm.g) < 0.01f;
                    Assert.AreEqual(row.Selected, red, at + " satır " + i + ": yalnız seçili satır kırmızı");
                    var rect = face.RowRect(i);
                    Assert.AreEqual(i, face.RowAt(rect.center), at + ": satır kutusu kendi dizinine eşlenir");
                    Assert.AreEqual(i, face.RowAt(new Vector2(rect.xMin + 0.2f, rect.yMin + 0.2f)), at);
                    Assert.AreEqual(i, face.RowAt(new Vector2(rect.xMax - 0.2f, rect.yMax - 0.2f)), at);
                    Assert.GreaterOrEqual(rect.xMin, CandidateGroupSlipFace.RightX - 0.01f, at + ": satırlar sağ blokta");
                    Assert.LessOrEqual(rect.xMax, CandidateGroupSlipFace.RightRight + 0.01f, at);
                    Assert.LessOrEqual(rect.yMax, CandidateGroupSlipFace.BottomMm + 0.01f, at);
                    Assert.IsFalse(face.InStampArea(rect.center), at + ": satır damga alanında değil");
                    Assert.AreEqual(i < CandidateGroupSlipFace.RowsPerColumn ? 0 : 1, rect.xMin > CandidateGroupSlipFace.RightX + 1f ? 1 : 0, at + ": iki sütun × 5");
                }
                Assert.AreEqual(1, g.Replace.Count(r => r.Selected));
                for (int i = 0; i < 10; i++)
                    for (int k = i + 1; k < 10; k++)
                    {
                        // Komşu kutular kenar paylaşır: 0,01 mm içeri çekilmiş hâlleri örtüşmemeli
                        var a = face.RowRect(i);
                        var inner = Rect.MinMaxRect(a.xMin + 0.01f, a.yMin + 0.01f, a.xMax - 0.01f, a.yMax - 0.01f);
                        Assert.IsFalse(inner.Overlaps(face.RowRect(k)), at + ": satır kutuları örtüşmez " + i + "/" + k);
                    }
                Assert.AreEqual(-1, face.RowAt(new Vector2(20, 30)), "sol blok satır değil");
                Assert.AreEqual(-1, face.RowAt(new Vector2(CandidateGroupSlipFace.RightX + 5, CandidateGroupSlipFace.TopMm + 1)), "yer başlığı satır değil");
                Assert.AreEqual(-1, face.RowAt(new Vector2(CandidateGroupSlipFace.DividerX, 40)), "bölme satır değil");
                Assert.IsTrue(face.InStampArea(face.StampCenterMm), "iz merkezi sol blokta");
                Assert.IsTrue(face.InStampArea(new Vector2(20, 30)));
                Assert.IsFalse(face.InStampArea(new Vector2(CandidateGroupSlipFace.RightX + 5, 30)));
                foreach (var t in face.Content.GetComponentsInChildren<TMP_Text>(false)) Assert.IsFalse(t.enableAutoSizing, t.name);

                // En uzun ad her satırda: yine sığar
                var longest = Group(sim, marked);
                foreach (var row in longest.Replace) row.Label = Loc.F("{0} · {1}", LongName, row.Job);
                longest.ReplaceLine = Loc.F("{0} yollanacak · yolluk {1} erzak · bağlı teçhizatı onunla gider", LongName, "2");
                face.Bind(longest);
                Assert.IsFalse(face.Overflowing, at + ": en uzun adla taşma (" + string.Join(" | ", longest.Replace.Select(r => r.Label)) + ")");
            }

            // On birinci kişi yazılmaz ve taşma bildirilir (kapasite dengede 10)
            Use(false);
            var eleven = Group(sim, marked);
            eleven.Replace.Add(new ReplaceRowData { Id = "resident-x", Name = "Fazla", Job = "Boşta", Label = "Fazla · Boşta", Enabled = true });
            face.Bind(eleven);
            Assert.AreEqual(10, face.RowCount);
            Assert.IsTrue(face.Overflowing, "on birinci satır taşma sayılır");
            Assert.AreEqual(10, new Balance { shelterEventsEnabled = false }.boardResidentCapacity, "kapasite dengede 10: fişin satır sayısı ona göre");
            face.Bind(null);
            Assert.IsFalse(face.Content.gameObject.activeSelf, "boş yüz");
            Assert.AreEqual(0, face.RowCount);
        }

        /// <summary>
        /// Boş yer varken fiş (TR ve EN): satır ve ipucu yok, YER VAR mürekkep renginde; son vardiyada bekleme satırı uyarı renginde; acil
        /// grupta ACİL satırı; GERİ ÇEVİR izi sol blokta, 3° dönük, basış animasyonlu.
        /// </summary>
        [Test]
        public void Slip_RoomWaitEmergency_AndPassStamp_TrEn()
        {
            var face = Spawn<CandidateGroupSlipFace>(AdmissionFixtures.GroupSlipPrefab);
            var alarm = Alarm;
            foreach (bool en in new[] { false, true })
            {
                Use(en);
                string at = en ? "EN" : "TR";
                var sim = Game(3);
                var g = Group(sim);
                face.Bind(g);
                face.ShowStamp(false, false);
                Assert.IsFalse(face.Overflowing, at);
                Assert.AreEqual(0, face.RowCount);
                for (int i = 0; i < CandidateGroupSlipFace.MaxRows; i++)
                {
                    Assert.IsFalse(face.Row(i).gameObject.activeSelf, at + ": satır yok");
                    Assert.IsFalse(face.RowMark(i).gameObject.activeSelf, at);
                }
                Assert.IsFalse(face.RoomHint.gameObject.activeSelf, at);
                Assert.IsFalse(face.ReplaceLine.gameObject.activeSelf, at);
                Assert.AreEqual(-1, face.RowAt(new Vector2(120, 30)), "satır yokken tık satıra düşmez");
                Assert.Greater(Mathf.Abs(face.RoomTitle.color.r - alarm.r) + Mathf.Abs(face.RoomTitle.color.g - alarm.g), 0.05f, at + ": YER VAR kırmızı değil");
                Assert.Greater(Mathf.Abs(face.Body.color.r - alarm.r) + Mathf.Abs(face.Body.color.g - alarm.g), 0.05f, at + ": ilk vardiya kırmızı değil");
                Assert.IsFalse(face.Stamp.gameObject.activeSelf, at + ": iz yok");

                // Son vardiya
                Assert.IsTrue(sim.PrintShift().Ok);
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
                var last = Group(sim);
                Assert.IsTrue(last.LastShift);
                face.Bind(last);
                Assert.IsFalse(face.Overflowing, at + ": son vardiya");
                Assert.AreEqual(last.WaitLine, face.Body.text);
                Assert.AreEqual(alarm.r, face.Body.color.r, 0.01f, at + ": SON VARDİYA uyarı renginde");
                Assert.AreEqual(alarm.g, face.Body.color.g, 0.01f, at);

                // GERİ ÇEVİR izi
                AdmissionSnapshotBuilder.StampPass(last);
                face.Bind(last);
                face.ShowStamp(true, false);
                Assert.IsTrue(face.StampVisible, at);
                Assert.IsTrue(face.Stamp.gameObject.activeSelf, at);
                Assert.AreEqual(en ? "TURNED AWAY" : "GERİ ÇEVRİLDİ", face.StampText.text);
                Assert.AreEqual(CandidateGroupSlipFace.StampTiltDeg, face.Stamp.localEulerAngles.z, 1e-3f, at);
                var c = face.StampCenterMm;
                var size = face.Stamp.sizeDelta;
                Assert.GreaterOrEqual(c.x - size.x / 2f, 1f, at + ": iz fişin solundan taşmaz");
                Assert.LessOrEqual(c.x + size.x / 2f, CandidateGroupSlipFace.DividerX, at + ": iz sol blokta kalır");
                Assert.LessOrEqual(c.y + size.y / 2f, 74 - 1f, at);
                face.ShowStamp(true, true);
                face.Advance(1f);
                Assert.IsTrue(face.StampVisible);
                face.ShowStamp(false, false);
                Assert.IsFalse(face.Stamp.gameObject.activeSelf);

                // Acil grup, dolu sığınak, işaretsiz: ACİL satırı ve sekiz satır birlikte sığar
                var urgent = Game(79, capacity: 8, emergencyBelow: 9);
                var u = Group(urgent);
                Assert.IsTrue(u.Emergency && u.Full);
                face.Bind(u);
                Assert.IsFalse(face.Overflowing, at + ": acil + dolu (içerik altı " + face.ContentBottomMm.ToString("0.0") + " mm)");
                Assert.IsTrue(face.Note.gameObject.activeSelf, at);
                Assert.AreEqual(u.EmergencyLine, face.Note.text);
                Assert.AreEqual(8, face.RowCount);
                Assert.IsFalse(face.ReplaceLine.gameObject.activeSelf, at + ": işaret yokken özet yok");
                Assert.IsTrue(face.RoomHint.gameObject.activeSelf, at);
                // Acil grubun son vardiyası: SON VARDİYA + ACİL + ipucu sol bloğa sığar
                Assert.IsTrue(urgent.PrintShift().Ok);
                Assert.IsTrue(urgent.FileShift(urgent.Board.ReceiptId).Ok);
                face.Bind(Group(urgent));
                Assert.IsFalse(face.Overflowing, at + ": acil son vardiya");
            }
        }
    }
}
