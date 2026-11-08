using System.IO;
using System.Linq;
using Corrigible.Desk.EditorTools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 1 (ilk tur ve Revizyon 2): token içe aktarımı, font glifleri ve sprite içe aktarma ayarları.</summary>
    public class SiginakFoundationTests
    {
        static string Json => File.ReadAllText(SiginakThemeSetup.TokensPath);
        static string RaidJson => File.ReadAllText(SadeThemeSetup.RaidTokensPath);

        static RaidTheme FreshRaid()
        {
            var theme = ScriptableObject.CreateInstance<RaidTheme>();
            SadeThemeSetup.FillRaid(theme, RaidJson);
            return theme;
        }

        /// <summary>JSON alt ağacında temaya girecek bir değer (sayı ya da renk) var mı; yalnız metin taşıyan dallar atlanır.</summary>
        static bool HasValue(JToken t) => t is JValue v
            ? v.Type == JTokenType.Integer || v.Type == JTokenType.Float || v.Type == JTokenType.Boolean || v.Type == JTokenType.String && ((string)v).StartsWith("#")
            : t.Children().Any(c => c is JProperty p ? !p.Name.StartsWith("_") && HasValue(p.Value) : HasValue(c));

        static SiginakTheme Fresh(out SiginakThemeSetup.Report report)
        {
            var theme = ScriptableObject.CreateInstance<SiginakTheme>();
            report = SiginakThemeSetup.Fill(theme, Json);
            return theme;
        }

        static bool HasKey(SiginakTheme t, string key) =>
            t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key)) || t.strings.Any(s => Matches(s.key, key));

        static bool Matches(string have, string key) => have == key || have.StartsWith(key + ".") || have.StartsWith(key + "[");

        [Test]
        public void Tokens_AllColorsParse()
        {
            var theme = Fresh(out var report);
            try
            {
                Assert.IsEmpty(report.BadHex, "okunamayan renkler");
                int hexInJson = 0;
                void Count(JToken t, bool skip)
                {
                    if (skip) return;
                    if (t is JObject o) foreach (var p in o.Properties()) Count(p.Value, p.Name.StartsWith("_") || p.Name == "meta" || p.Name == "reuse");
                    else if (t is JArray a) foreach (var x in a) Count(x, false);
                    else if (t is JValue v && v.Type == JTokenType.String && ((string)v).StartsWith("#")) hexInJson++;
                }
                Count(JObject.Parse(Json), false);
                // Her renk yazısı bir renk anahtarı ya da gölge katmanı olur (dal dışındaki gölgeler ikisi birden)
                int parsed = theme.colors.Count + theme.shadows.Sum(s => s.layers.Length);
                Assert.GreaterOrEqual(parsed, hexInJson, "JSON'daki renklerin hepsi temaya girmeli");
                Assert.IsFalse(theme.colors.Any(c => c.color == Color.magenta), "eflatun renk");
            }
            finally { Object.DestroyImmediate(theme); }
        }

        [Test]
        public void Tokens_EveryNewPathResolves()
        {
            var theme = Fresh(out _);
            try
            {
                var paths = ((JArray)JObject.Parse(Json)["_new"]).Select(p => (string)p).ToList();
                Assert.Greater(paths.Count, 250);
                var missing = paths.Where(p =>
                    p.StartsWith("colors.") ? !HasKey(theme, p.Substring(7)) :
                    p.StartsWith("type.") ? !theme.type.Any(t => t.role == p.Substring(5)) :
                    p.StartsWith("shadows.") ? !theme.shadows.Any(s => s.key == p.Substring(8)) :
                    !HasKey(theme, p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan yeni tokenlar: " + string.Join(", ", missing));
            }
            finally { Object.DestroyImmediate(theme); }
        }

        [Test]
        public void Tokens_ReusedSadeKeysExist()
        {
            var sade = SadeTheme.Active;
            Assert.IsNotNull(sade);
            var reuse = (JArray)JObject.Parse(Json)["reuse"]["colors"];
            var missing = reuse.Select(r => (string)r).Where(k => k.EndsWith(".*")
                ? !sade.colors.Any(c => c.key.StartsWith(k.Substring(0, k.Length - 1)))
                : !sade.TryGet(k, out _) && !sade.colors.Any(c => c.key.StartsWith(k + "["))).ToList();   // [gövde, kenar] çiftleri
            Assert.IsEmpty(missing, "Sade temasında olmayan ortak anahtarlar: " + string.Join(", ", missing));
            foreach (var shadow in new[] { "card", "hoverCard", "plaque", "eventSlip", "topButton", "action", "actionHover" })
                Assert.IsNotEmpty(sade.Shadow(shadow), "Sade gölgesi: " + shadow);
        }

        [Test]
        public void Tokens_SpotValues()
        {
            var t = Fresh(out _);
            try
            {
                Assert.AreEqual("B8955A", ColorUtility.ToHtmlStringRGB(t.ColorOf("brass.metal")));
                Assert.AreEqual(0.3f, t.ColorOf("crt.scanline").a, 1e-4);
                Assert.AreEqual("6E5A40", ColorUtility.ToHtmlStringRGB(t.ColorOf("bunker.soil[0]")));
                Assert.AreEqual("D9C08B", ColorUtility.ToHtmlStringRGB(t.ColorOf("ui.folder[0]")));
                Assert.AreEqual("round", t.Text("stamp.free.edge"));
                Assert.AreEqual("1A4789", ColorUtility.ToHtmlStringRGB(t.ColorOf("stamp.free")));
                Assert.AreEqual(5, t.Number("mapLiberation.frontContour.width"));
                Assert.AreEqual(-45, t.Number("mapLiberation.occupiedHatch[1].angle"));
                Assert.AreEqual(0.32f, t.ColorOf("mapLiberation.occupiedHatch[1]").a, 1e-4);
                Assert.AreEqual(new Vector3(0, 1.52f, -0.15f), t.Vector("cameras.K1.pos"));
                Assert.AreEqual(56.7f, t.Number("cameras.K1.fovV"), 1e-4);
                Assert.AreEqual(0.36f, t.Number("materials.brass.roughness"), 1e-4);
                Assert.AreEqual("A6E8B8", ColorUtility.ToHtmlStringRGB(t.ColorOf("materials.crtScreen.emissive")));
                Assert.AreEqual(1, t.Number("lights.bulb.shadows"));
                Assert.AreEqual(4, t.Number("motion.coverAutoCloseS"));
                Assert.AreEqual(new Vector2(820, 52), new Vector2(t.Number("space.telexStrip.w"), t.Number("space.telexStrip.h")));
                Assert.IsTrue(t.TryType("plateLabel", out var plate));
                Assert.AreEqual(DeskFont.CpBold, plate.font); Assert.AreEqual(16, plate.size); Assert.AreEqual(0.06f, plate.letterSpacingEm, 1e-4);
                Assert.IsTrue(t.TryType("analystNote", out var note)); Assert.AreEqual(DeskFont.KalamBold, note.font);
                Assert.IsTrue(t.TryType("sectorName", out var sector)); Assert.AreEqual(DeskFont.LfSemiBold, sector.font);
                Assert.IsTrue(t.TryType("wallStencil", out var stencil)); Assert.AreEqual(DeskFont.StencilExtraBold, stencil.font, "Big Shoulders Stencil (Revizyon 3)");
                Assert.IsFalse(t.strings.Any(x => x.key == "type.wallStencil.fallback"), "eşlenen ailede yedek notu yok");
                Assert.IsTrue(t.TryType("hoverTitle", out _), "ortak rol Sade'den okunur");
                var report = t.Shadow("report");
                Assert.AreEqual(1, report.Length); Assert.AreEqual(new Vector2(5, 7), report[0].offset); Assert.AreEqual(0.45f, report[0].color.a, 1e-4);
                Assert.IsNotEmpty(t.Shadow("hoverCard"), "ortak gölge Sade'den okunur");
                Assert.AreEqual("1E1B16", ColorUtility.ToHtmlStringRGB(t.ColorOf("ink")), "ortak renk Sade'den okunur");
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>Resources'taki asset JSON'la güncel olmalı (token değişince “Tokenları içe aktar” çalıştırılmalı).</summary>
        [Test]
        public void Tokens_AssetIsUpToDate()
        {
            var asset = SiginakTheme.Active;
            Assert.IsNotNull(asset, "Resources/SiginakTheme.asset yok");
            var fresh = Fresh(out _);
            try
            {
                CollectionAssert.AreEquivalent(fresh.colors.Select(c => c.key + "=" + c.color), asset.colors.Select(c => c.key + "=" + c.color));
                CollectionAssert.AreEquivalent(fresh.numbers.Select(n => n.key + "=" + n.value), asset.numbers.Select(n => n.key + "=" + n.value));
                CollectionAssert.AreEquivalent(fresh.strings.Select(s => s.key + "=" + s.value), asset.strings.Select(s => s.key + "=" + s.value));
                CollectionAssert.AreEquivalent(fresh.type.Select(s => s.role + s.font + s.size), asset.type.Select(s => s.role + s.font + s.size));
                Assert.AreEqual(fresh.shadows.Count, asset.shadows.Count);
            }
            finally { Object.DestroyImmediate(fresh); }
        }

        /// <summary>Revizyon 2 (token 1.1): “_new_1_1” listesindeki her yol temada karşılık bulur.</summary>
        [Test]
        public void Tokens_Rev2NewPathsResolve()
        {
            var theme = Fresh(out var report);
            try
            {
                Assert.IsEmpty(report.BadHex, "okunamayan renkler");
                var root = JObject.Parse(Json);
                Assert.AreEqual("1.3", (string)root["meta"]["version"], "Revizyon 3 + ek 11-05");
                var paths = ((JArray)root["_new_1_1"]).Select(p => (string)p).ToList();
                Assert.AreEqual(87, paths.Count);
                var missing = paths.Where(p =>
                    p.StartsWith("colors.") ? !HasKey(theme, p.Substring(7)) :
                    p.StartsWith("type.") ? !theme.type.Any(t => t.role == p.Substring(5)) :
                    p.StartsWith("shadows.") ? !theme.shadows.Any(s => s.key == p.Substring(8)) :
                    !HasKey(theme, p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan 1.1 tokenları: " + string.Join(", ", missing));
            }
            finally { Object.DestroyImmediate(theme); }
        }

        /// <summary>
        /// Revizyon 2: “_changed_1_1”in dokuz değeri yenidir, eski değerler (“_was_1_1”) temaya girmez. Listede olmayan iki farkta
        /// projenin değeri korunur (IMPLEMENTATION_MAP › Kararlar 3): taskHead 14 px (punto kuralı), K6a pozu sahnedeki.
        /// </summary>
        [Test]
        public void Tokens_Rev2ChangedValues()
        {
            var t = Fresh(out _);
            try
            {
                var root = JObject.Parse(Json);
                var changed = ((JArray)root["_changed_1_1"]).Select(p => (string)p).ToList();
                CollectionAssert.AreEquivalent(changed, ((JObject)root["_was_1_1"]).Properties().Select(p => p.Name));
                Assert.IsFalse(t.colors.Any(c => c.key.StartsWith("_")) || t.numbers.Any(n => n.key.StartsWith("_")), "eski değerler temaya girmemeli");

                Assert.AreEqual("E9DDBE", ColorUtility.ToHtmlStringRGB(t.ColorOf("counter.tenths")), "onda bir çarkı açık renk");
                Assert.AreEqual("D4B06E", ColorUtility.ToHtmlStringRGB(t.ColorOf("counter.decimalDot")));
                Assert.IsTrue(t.TryType("stamp", out var stamp)); Assert.AreEqual(14, stamp.size); Assert.AreEqual(0.04f, stamp.letterSpacingEm, 1e-4);
                Assert.IsTrue(t.TryType("roomCardNameTex", out var name)); Assert.AreEqual(84, name.size);
                Assert.IsTrue(t.TryType("roomCardLineTex", out var line)); Assert.AreEqual(68, line.size);
                Assert.AreEqual(432, t.Number("space.slipW.tr")); Assert.AreEqual(432, t.Number("space.slipW.en"));
                Assert.AreEqual(new Vector2(72, 28), t.Vector2Of("space.deltaTag"));
                Assert.AreEqual(540, t.Number("space.reportW"));
                Assert.AreEqual(0.25f, t.Number("motion.stringS"), 1e-4);

                Assert.IsTrue(t.TryType("taskHead", out var task)); Assert.AreEqual(14, task.size, "ikincil yazı ≥ 14 px");
                Assert.AreEqual(new Vector3(0.32f, 1.28f, 0.36f), t.Vector("cameras.K6a.pos"));
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>Revizyon 2: baskın tokenları 1.1; yeni grupların sayı ve renkleri temaya girer, on bir değişen grup yeni değerini verir.</summary>
        [Test]
        public void RaidTokens_Rev2()
        {
            var t = FreshRaid();
            try
            {
                var root = JObject.Parse(RaidJson);
                Assert.AreEqual("1.3", (string)root["meta"]["version"], "Revizyon 3 + ek 11-05");
                bool Has(string key) => t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key));
                JToken At(string path) => path.Split('.').Aggregate((JToken)root, (n, k) => n?[k]);
                var paths = ((JArray)root["_new_1_1"]).Select(p => (string)p).ToList();
                Assert.AreEqual(13, paths.Count);
                var missing = paths.Where(p => At(p) == null || HasValue(At(p)) && !Has(p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan 1.1 tokenları: " + string.Join(", ", missing));
                var changed = ((JArray)root["_changed_1_1"]).Select(p => (string)p).ToList();
                Assert.AreEqual(11, changed.Count);
                Assert.IsEmpty(changed.Where(p => !Has(p)), "değişen gruplar temada olmalı");
                Assert.IsFalse(t.colors.Any(c => c.key.StartsWith("_")) || t.numbers.Any(n => n.key.StartsWith("_")), "eski değerler temaya girmemeli");

                Assert.AreEqual(24, t.Number("labels.hpTag.heightPx"));
                Assert.AreEqual(11, t.Number("labels.hpTag.pip.sizePx"));
                Assert.AreEqual(28, t.Number("labels.orderFlag.heightPx"));
                Assert.AreEqual(3.4f, t.Number("intents.fire.strokePx"), 1e-4);
                Assert.AreEqual(14, t.Number("intents.melee.arcRisePx[0]")); Assert.AreEqual(26, t.Number("intents.melee.arcRisePx[1]"));
                Assert.AreEqual(237, t.Number("layout1080.teamCards[0][2]")); Assert.AreEqual(976, t.Number("layout1080.teamCards[2][1]"));
                Assert.AreEqual(6, t.Number("layout1080.teamCardSelectedLiftPx"));
                Assert.AreEqual(592, t.Number("layout1080.targetForm[2]"));
                Assert.AreEqual(318, t.Number("layout1080.endTurnButton[2]"));
                Assert.AreEqual(136, t.Number("layout1080.undoButton[2]"));
                Assert.AreEqual(1724, t.Number("layout1080.retreatButton[0]"));
                Assert.AreEqual(1540, t.Number("layout1080.rulesCard.button[0]"));
                Assert.AreEqual(34, t.Number("turnStrip.cellPx"));
                Assert.AreEqual("C9A36B", ColorUtility.ToHtmlStringRGB(t.Get("panel.plywood.top")));
                Assert.AreEqual(1560, t.Number("trainingNote.rect[0]"));
                // Kamera token'ı oyunla uyuşmaz ve okunmaz; oyunun kamerası tokens_sade.json → raid.camera (132,58 px/birim)
                Assert.AreEqual(88.388f, t.Number("camera.pixelsPerUnitAt1080"), 1e-3);
                Assert.AreEqual(132.58f, SadeTheme.Active.Number("raid.camera.pixelsPerUnitAt1080"), 1e-3);
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>1.2 eki yalnız ekleme: üç makine, dört niyet, kopyalama halkası, etiketteki zırh, bölge özelliği işaretleri.</summary>
        [Test]
        public void RaidTokens_Rev2Addendum12()
        {
            var t = FreshRaid();
            try
            {
                var root = JObject.Parse(RaidJson);
                bool Has(string key) => t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key));
                JToken At(string path) => path.Split('.').Aggregate((JToken)root, (n, k) => n?[k]);
                var paths = ((JArray)root["_new_1_2"]).Select(p => (string)p).ToList();
                Assert.AreEqual(21, paths.Count);
                Assert.IsEmpty(paths.Where(p => At(p) == null), "1.2 listesinde olup dosyada olmayan yollar");
                var missing = paths.Where(p => HasValue(At(p)) && !Has(p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan 1.2 tokenları: " + string.Join(", ", missing));

                Assert.AreEqual(0.53f, t.Number("materials3d.armorDrone.topZ"), 1e-4);
                Assert.AreEqual("9A9C98", ColorUtility.ToHtmlStringRGB(t.Get("materials3d.armorDrone.armourBand.color")));
                Assert.AreEqual(7, t.Number("materials3d.bomber.horns.count"));
                Assert.AreEqual(0.45f, t.Number("materials3d.replicator.miniDrone.scale"), 1e-4);
                Assert.AreEqual(0.4f, t.Number("intents.lob.arcRise.ratioOfChord"), 1e-4);
                Assert.AreEqual(90, t.Number("intents.lob.arcRise.minPx"));
                Assert.AreEqual(3.2f, t.Number("intents.shield.gapPx"), 1e-4);
                Assert.AreEqual(4, t.Number("intents.clone.dashPx[1]"));
                Assert.AreEqual(12, t.Number("marks.ghostWake.dashes"));
                Assert.AreEqual(12.5f, t.Number("labels.hpTag.armorIcon.sizePx[1]"), 1e-4);
                // Zarar vermeyen niyetler kırmızı değil (S9 kuralı); onarım 1.1'deki gibi kaldı
                foreach (var key in new[] { "intents.shield.color", "intents.clone.color", "marks.ghostWake.color", "intents.nonHarmful.color" })
                    Assert.AreEqual("854005", ColorUtility.ToHtmlStringRGB(t.Get(key)), key);
                Assert.AreEqual("B8241A", ColorUtility.ToHtmlStringRGB(t.Get("intents.lob.color")));
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>
        /// Revizyon 3 + ek 11-05 (token 1.3): “_new_1_3”teki her yol temada karşılık bulur. Kartlı oyundan kaldırılan 26 düğüm
        /// eski değerleriyle (“_was_1_3”) temada kalır, çünkü eski yol onları okur (Rev3 IMPLEMENTATION_MAP › Soru 5);
        /// kaldırılmamış değişiklikler (K4 kamerası) yeni değerini verir.
        /// </summary>
        [Test]
        public void Tokens_Rev3()
        {
            var t = Fresh(out var report);
            try
            {
                Assert.IsEmpty(report.BadHex, "okunamayan renkler");
                var root = JObject.Parse(Json);
                JToken At(string path) => path.Split('.').Aggregate((JToken)root, (n, k) => n?[k]);
                var paths = ((JArray)root["_new_1_3"]).Select(p => (string)p).ToList();
                Assert.AreEqual(117, paths.Count);
                var missing = paths.Where(p => At(p) == null ||
                    (p.StartsWith("colors.") ? !HasKey(t, p.Substring(7)) :
                     p.StartsWith("type.") ? !t.type.Any(x => x.role == p.Substring(5)) :
                     HasValue(At(p)) && !HasKey(t, p))).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan 1.3 tokenları: " + string.Join(", ", missing));

                Assert.AreEqual(0.55f, t.Number("lights.roomBulb.k4Scale"), 1e-4, "kaldırılan düğüm eski değeriyle");
                Assert.AreEqual("C0281C", ColorUtility.ToHtmlStringRGB(t.ColorOf("door.fatalRoomLight")), "eski yolun ölümcül kapı ışığı");
                Assert.AreEqual(0.12f, t.Number("motion.summaryStaggerS"), 1e-4);
                Assert.IsFalse(t.numbers.Any(n => n.key.EndsWith(".removed")), "kaldırma işaretleri temaya girmemeli");

                Assert.AreEqual(new Vector3(0, 1.293f, -0.98f), t.Vector("cameras.K4.localPos"), "K4 panoya bağlı (yeni değer)");
                Assert.AreEqual(42.5f, t.Number("cameras.K4.fovV"), 1e-4);
                Assert.AreEqual(10, t.Number("board.limits.people"));
                Assert.AreEqual(60, t.Number("board.limits.rationCap"));
                Assert.AreEqual(80, t.Number("cards.sizesMm.person[0]")); Assert.AreEqual(120, t.Number("cards.sizesMm.person[1]"));
                Assert.AreEqual(0.35f, t.Number("shiftClose.slip.exitS"), 1e-4);
                Assert.AreEqual("PLAN KİLİTLİ", t.Text("board.plates.tr.flapB"));
                Assert.AreEqual("PLAN LOCKED", t.Text("board.plates.en.flapB"));
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>Revizyon 3: baskın tokenları 1.3 yalnız ekleme (kit modelleri, kit ve parça malzemeleri).</summary>
        [Test]
        public void RaidTokens_Rev3()
        {
            var t = FreshRaid();
            try
            {
                var root = JObject.Parse(RaidJson);
                bool Has(string key) => t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key));
                JToken At(string path) => path.Split('.').Aggregate((JToken)root, (n, k) => n?[k]);
                var paths = ((JArray)root["_new_1_3"]).Select(p => (string)p).ToList();
                Assert.AreEqual(72, paths.Count);
                Assert.IsEmpty(paths.Where(p => At(p) == null), "1.3 listesinde olup dosyada olmayan yollar");
                var missing = paths.Where(p => HasValue(At(p)) && !Has(p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan 1.3 tokenları: " + string.Join(", ", missing));
                Assert.AreEqual(0, ((JArray)root["_changed_1_3"]).Count, "baskında değişen yol yok");
                Assert.AreEqual("A8A194", ColorUtility.ToHtmlStringRGB(t.Get("kits.weu.wallTop")));
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>
        /// Oyundaki her bölge özelliğinin ve makine türünün işareti var; <see cref="SiginakGlyphs"/> tablosu raid_tokens.json'la
        /// aynı sprite'ı verir, sprite (düz ve "_frame") içe aktarılmış ve kayıtta.
        /// </summary>
        [Test]
        public void Glyphs_CoverGameTraitsAndMachines()
        {
            var root = JObject.Parse(RaidJson);
            CollectionAssert.AreEquivalent(Corrigible.Core.Baskin.RegionTraits.Keys, SiginakGlyphs.Traits.Select(r => r.Id));
            CollectionAssert.AreEquivalent(Corrigible.Core.Baskin.UnitTypes.Values.Where(u => u.Side == 'e').Select(u => u.Id),
                SiginakGlyphs.Machines.Select(r => r.Id));
            var traitKeys = ((JObject)root["regionTraits"]).Properties().Where(p => p.Value is JObject).Select(p => p.Name);
            CollectionAssert.AreEquivalent(traitKeys, SiginakGlyphs.Traits.Select(r => r.TokenKey), "regionTraits anahtarları");
            var machineKeys = ((JObject)root["machineTypes"]["glyph"]).Properties().Where(p => !p.Name.StartsWith("_")).Select(p => p.Name);
            CollectionAssert.AreEquivalent(machineKeys, SiginakGlyphs.Machines.Select(r => r.TokenKey), "machineTypes.glyph anahtarları");

            foreach (var r in SiginakGlyphs.Traits)
                Assert.AreEqual((string)root["regionTraits"][r.TokenKey]["glyph"], r.Sprite, r.Id);
            foreach (var r in SiginakGlyphs.Machines)
                Assert.AreEqual((string)root["machineTypes"]["glyph"][r.TokenKey], r.Sprite, r.Id);

            foreach (var r in SiginakGlyphs.Traits.Concat(SiginakGlyphs.Machines))
                foreach (var name in new[] { r.Sprite, r.Sprite + "_frame" })
                {
                    Assert.IsNotNull(SiginakSpriteManifest.Get(name), name + " sprite tablosunda yok");
                    Assert.IsTrue(DeskSprites.Instance.TryGet(name, out var e) && e.sprite != null, name + " kayıtta yok");
                }
            Assert.AreEqual("glyph_crowd_frame", SiginakGlyphs.Trait("kalabalik", framed: true));
            Assert.AreEqual("glyph_armordrone", SiginakGlyphs.Machine("zdron"));
            Assert.IsNull(SiginakGlyphs.Trait("kopya"));
        }

        /// <summary>Resources'taki baskın teması JSON'la güncel olmalı (“Sade UI/Tokenları içe aktar”).</summary>
        [Test]
        public void RaidTokens_AssetIsUpToDate()
        {
            var asset = RaidTheme.Active;
            Assert.IsNotNull(asset, "Resources/RaidTheme.asset yok");
            var fresh = FreshRaid();
            try
            {
                CollectionAssert.AreEquivalent(fresh.colors.Select(c => c.key + "=" + c.color), asset.colors.Select(c => c.key + "=" + c.color));
                CollectionAssert.AreEquivalent(fresh.numbers.Select(n => n.key + "=" + n.value), asset.numbers.Select(n => n.key + "=" + n.value));
            }
            finally { Object.DestroyImmediate(fresh); }
        }

        /// <summary>Sığınak kodunda geçen her tema anahtarı (ColorOf / Number / NumberOf / Vector / Shadow / ApplyType) çözülür.</summary>
        [Test]
        public void ThemeKeys_UsedInSiginakCode_Resolve()
        {
            var theme = SiginakTheme.Active;
            var rx = new System.Text.RegularExpressions.Regex(@"\b(ColorOf|Number|NumberOf|Vector|Vector2Of)\(""([A-Za-z0-9_.\[\]]+)""");
            var files = Directory.GetFiles(DeskSpriteManifest.Root + "/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(f => f.Replace('\\', '/').Contains("/Siginak/") || Path.GetFileName(f).StartsWith("Siginak")).ToList();
            Assert.Greater(files.Count, 20);
            var missing = files.SelectMany(f => rx.Matches(File.ReadAllText(f)).Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => (kind: m.Groups[1].Value, key: m.Groups[2].Value, file: Path.GetFileName(f))))
                .Where(k => !k.key.EndsWith(".") && !Resolves(theme, k.kind, k.key))
                .Select(k => k.file + ": " + k.key).Distinct().ToList();
            Assert.IsEmpty(missing, "temada olmayan anahtarlar: " + string.Join(", ", missing));
        }

        static bool Resolves(SiginakTheme t, string kind, string key)
        {
            if (kind == "ColorOf") return t.TryGet(key, out _) || SadeTheme.Active.TryGet(key, out _);
            if (kind == "Vector") return t.TryNumber(key + "[0]", out _) && t.TryNumber(key + "[2]", out _);
            if (kind == "Vector2Of") return t.TryNumber(key + "[0]", out _);
            return t.TryNumber(key, out _) || SadeTheme.Active.TryNumber(key, out _);
        }

        /// <summary>PROMPT §2: zorunlu glifler her yüzde statik atlas zincirinde (kendi atlası ya da DejaVu yedeği) bulunur.</summary>
        [Test]
        public void Fonts_CoverRequiredGlyphs()
        {
            var theme = SiginakTheme.Active;
            var desk = DeskTheme.Active;
            var faces = theme.type.Select(t => t.font).Distinct().ToList();
            Assert.That(faces, Is.SupersetOf(new[] { DeskFont.CpRegular, DeskFont.CpBold, DeskFont.LfRegular, DeskFont.LfSemiBold, DeskFont.LfBold, DeskFont.KalamBold }));
            foreach (var face in faces)
            {
                var font = desk.Font(face);
                Assert.IsNotNull(font, face.ToString());
                Assert.AreEqual("", SiginakFontPreview.MissingStatic(font, SiginakFontPreview.RequiredGlyphs), face + " eksik glif");
            }
        }

        [Test]
        public void Sprites_ImportedWithManifestSettings()
        {
            foreach (var dir in new[] { SiginakSpriteManifest.SourceDir, SiginakSpriteManifest.Rev2SourceDir })
            {
                if (!Directory.Exists(dir)) continue;
                var packaged = Directory.GetFiles(dir, "*.png").Select(Path.GetFileNameWithoutExtension);
                Assert.IsEmpty(packaged.Where(n => SiginakSpriteManifest.Get(n) == null), "tabloda olmayan paket sprite'ı: " + dir);
            }
            foreach (var e in SiginakSpriteManifest.All)
            {
                var ti = AssetImporter.GetAtPath(e.AssetPath) as TextureImporter;
                Assert.IsNotNull(ti, e.AssetPath);
                Assert.AreEqual(e.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, ti.wrapMode, e.Name);
                Assert.AreEqual(e.Filter, ti.filterMode, e.Name);
                Assert.AreEqual(e.Mip, ti.mipmapEnabled, e.Name);
                if (e.Kind == SiginakSpriteManifest.Kind.Texture)
                {
                    Assert.AreEqual(TextureImporterType.Default, ti.textureType, e.Name);
                    Assert.IsTrue(ti.mipmapEnabled, e.Name);
                    continue;
                }
                Assert.AreEqual(TextureImporterType.Sprite, ti.textureType, e.Name);
                Assert.AreEqual(SiginakSpriteManifest.PixelsPerUnit, ti.spritePixelsPerUnit, e.Name);
                Assert.AreEqual(e.SpriteBorder, ti.spriteBorder, e.Name);
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                Assert.AreEqual(SpriteMeshType.FullRect, s.spriteMeshType, e.Name);
            }
        }
    }
}
