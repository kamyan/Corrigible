using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Baskın HUD'unun komut davranışı: geri alma, önizleme, iptal sırası, tur onayı, seçim kartı, geri çekilme.</summary>
    public class SadeRaidHudTests
    {
        GameObject owner;
        LiveRaidDataSource source;

        [SetUp]
        public void Setup()
        {
            owner = new GameObject("Baskın veri sınırı");
            owner.SetActive(false);
            source = owner.AddComponent<LiveRaidDataSource>();
            var so = new SerializedObject(source);
            so.FindProperty("theme").objectReferenceValue = SadeTheme.Active;
            so.ApplyModifiedPropertiesWithoutUndo();
            source.OpenForValidation(Baskin.Create(new RaidParams { Seed = 11, Compute = 3 }));
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(owner);

        static Baskin.Unit Technician(Baskin.BState b) => b.Units.First(u => u.Type == "tek");

        /// <summary>Teknisyenin bitişiğine bir robot koyar (sabotaj hedefi) ve tahtayı yeniden açar; robotun karesini döner.</summary>
        Vector2Int RobotNextToTechnician()
        {
            var b = Baskin.Create(new RaidParams { Seed = 11, Compute = 3 });
            var tech = Technician(b);
            var robot = b.Units.First(u => u.Side == 'e' && !u.Dead);
            foreach (var d in Baskin.Dirs)
            {
                int x = tech.X + d[0], y = tech.Y + d[1];
                if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                var c = Baskin.CellAt(b, x, y);
                if (c.T != "floor" || c.O != null || b.Units.Any(u => !u.Dead && u.X == x && u.Y == y)) continue;
                robot.X = x; robot.Y = y;
                source.OpenForValidation(b);
                return new Vector2Int(x, y);
            }
            Assert.Fail("Teknisyenin yanında boş kare yok");
            return default;
        }

        /// <summary>Kareye gelinmiş ve gecikme dolmuş gibi üzerine gelme kartı (EditMode'da Update çalışmaz).</summary>
        RaidInspectorModel Card(Vector2Int tile)
        {
            source.Hover(tile);
            typeof(RaidDataSource).GetField("hoverVisible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, true);
            typeof(RaidDataSource).GetMethod("Publish", BindingFlags.Instance | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null)
                .Invoke(source, null);
            Assert.IsNotNull(source.Current.HoverCard);
            return source.Current.HoverCard;
        }

        /// <summary>Üstünde birim ve nesne olmayan, çıkış olmayan zemin kareleri (tahta sırasıyla).</summary>
        static Vector2Int[] EmptyFloor(Baskin.BState b) =>
            Enumerable.Range(0, Baskin.N * Baskin.N).Select(i => new Vector2Int(i % Baskin.N, i / Baskin.N))
                .Where(t => Baskin.CellAt(b, t.x, t.y).T == "floor" && Baskin.CellAt(b, t.x, t.y).O == null && Baskin.UnitAt(b, t.x, t.y) == null
                    && !b.Exits.Any(p => p.X == t.x && p.Y == t.y))
                .ToArray();

        /// <summary>
        /// Elektrikli kanalın kartı (BASKIN-MEKANLARI Katman 6): başlık “Elektrikli kanal”, kural satırı (üstünden yürünür, itilen ya da
        /// çekilen 1 hasar alır). Üstünde birim duran kanal birimin kartını gösterir. Bölgesiz baskında alt satır yok.
        /// </summary>
        [Test]
        public void HoverCard_CableTileShowsTitleAndRule()
        {
            var tile = EmptyFloor(source.State)[0];
            source.State.Cells[tile.y, tile.x].T = "cable";
            var card = Card(tile);
            Assert.AreEqual(Loc.T("Elektrikli kanal"), card.Title);
            Assert.IsTrue(string.IsNullOrEmpty(card.Subtitle), "varsayılan görünüşte alt satır yok");
            CollectionAssert.AreEqual(new[] { Loc.T("Üstünden yürünür; üstüne itilen ya da çekilen birim 1 hasar alır.") }, card.Rows.Select(r => r.Text).ToArray());
            Assert.IsFalse(card.Rows[0].Good, "hasar satırı kırmızı");

            var robot = source.State.Units.First(u => u.Side == 'e' && Baskin.OnBoard(u));
            source.State.Cells[robot.Y, robot.X].T = "cable";
            var onIt = Card(new Vector2Int(robot.X, robot.Y));
            Assert.AreNotEqual(Loc.T("Elektrikli kanal"), onIt.Title, "kanaldaki robotun kartı robotun");
            Assert.IsFalse(onIt.Rows.Any(r => r.Text == Loc.T("Üstünden yürünür; üstüne itilen ya da çekilen birim 1 hasar alır.")), "kanal kuralı robotun kartında yok");

            var floor = EmptyFloor(source.State)[0];
            Assert.AreEqual(Loc.T("Boş kare"), Card(floor).Title, "zemin karesi değişmedi");
        }

        /// <summary>
        /// Şebeke faz değişimi (Japonya-Kore): kanal kartının kuralı yalnız canlı fazı söyler (Baskin.CableHurt: sönük kanal güvenli);
        /// olay satırı canlı/sönük durumunu verir.
        /// </summary>
        [Test]
        public void HoverCard_CableRuleInPhaseEventNamesOnlyTheLivePhase()
        {
            var b = Baskin.Create(new RaidParams { Seed = 11, Compute = 3 });
            var free = EmptyFloor(b);
            Vector2Int live = free[0], dead = free[1];
            b.Cells[live.y, live.x].T = "cable"; b.Cells[live.y, live.x].Phase = 1;
            b.Cells[dead.y, dead.x].T = "cable"; b.Cells[dead.y, dead.x].Phase = 2;
            b.Ev = new Baskin.EvState { Id = "faz" };
            source.OpenForValidation(b);
            Assert.AreEqual(1, b.Turn, "1. turda A fazı canlı");
            string rule = Loc.T("Yalnız canlı fazda üstüne itilen ya da çekilen birim 1 hasar alır.");
            string plain = Loc.T("Üstünden yürünür; üstüne itilen ya da çekilen birim 1 hasar alır.");

            var a = Card(live);
            Assert.AreEqual(Loc.T("Elektrikli kanal"), a.Title);
            var rows = a.Rows.Select(r => r.Text).ToArray();
            CollectionAssert.Contains(rows, Loc.T("Canlı kanal: evre sonunda 1 hasar"));
            CollectionAssert.Contains(rows, rule);
            CollectionAssert.DoesNotContain(rows, plain);

            rows = Card(dead).Rows.Select(r => r.Text).ToArray();
            CollectionAssert.Contains(rows, Loc.T("Sönük kanal: güvenli"));
            CollectionAssert.Contains(rows, rule);
            CollectionAssert.DoesNotContain(rows, plain);
        }

        /// <summary>Bölgenin baskını: boş zemin karelerinden bir duvar, bir kayar raf duvarı, bir kanal, bir gedik (Madenci'nin yıktığı
        /// duvarın yeri) ve bir sade zemin hazırlar. MapSeed ≠ 0 harekât baskını (bölgenin kiti), 0 eğitim ya da örnek baskın.</summary>
        static Baskin.BState KitBoard(string region, uint mapSeed, out Vector2Int[] tiles)
        {
            var b = Baskin.Create(new RaidParams { Seed = 11, Compute = 3, Region = region, MapSeed = mapSeed, MapSlot = mapSeed != 0 ? 3 : 0 });
            tiles = EmptyFloor(b).Take(5).ToArray();
            Assert.AreEqual(5, tiles.Length);
            Vector2Int wall = tiles[0], shelf = tiles[1], cable = tiles[2], breach = tiles[3];
            b.Cells[wall.y, wall.x].T = "wall";
            b.Cells[shelf.y, shelf.x].T = "wall"; b.Cells[shelf.y, shelf.x].Shelf = true;
            b.Cells[cable.y, cable.x].T = "cable";
            b.Rubble.Add(new Baskin.Pt(breach.x, breach.y));
            return b;
        }

        /// <summary>
        /// Bina kitinin adı (BASKIN-MEKANLARI Katman 6) kartta kural adının altında, gri ikinci satırda: duvar (kayar raf duvarı olayın
        /// adını korur: "Kayar arşiv rafı"), kanal, gedik (başlık "Boş kare" kalır). Varsayılan kitte (MapSeed 0: eğitim, örnek baskın)
        /// hiçbir karede alt satır yok. Üstünde birim duran karede mekân adı yok. Kurallar kartının lejantı kanalın adını alır.
        /// </summary>
        [Test]
        public void HoverCard_KitNameIsTheGreySubtitle_DefaultKitShowsNone()
        {
            // Varsayılan kit: bölge Batı Avrupa ama MapSeed 0 (eğitim baskını gibi)
            source.OpenForValidation(KitBoard("weu", 0, out var t));
            Assert.AreEqual(RaidKitText.Default, source.KitId);
            Assert.IsNull(source.Current.CableName, "lejant bugünkü gibi");
            foreach (var tile in t) Assert.IsTrue(string.IsNullOrEmpty(Card(tile).Subtitle), "varsayılan kitte alt satır yok: " + tile);
            Assert.AreEqual(Loc.T("Duvar"), Card(t[0]).Title);

            // Doğu Avrupa harekât baskını: arşiv kiti
            source.OpenForValidation(KitBoard("eeu", 4242, out t));
            Assert.AreEqual("eeu", source.KitId);
            var card = Card(t[0]);
            Assert.AreEqual(Loc.T("Duvar"), card.Title, "kural adı başlıkta kalır");
            Assert.AreEqual(Loc.T("Arşiv rafı"), card.Subtitle);
            card = Card(t[1]);
            Assert.AreEqual(Loc.T("Duvar"), card.Title);
            Assert.AreEqual(Loc.T("Kayar arşiv rafı"), card.Subtitle, "kayar raf duvarı olayın adını korur");
            card = Card(t[2]);
            Assert.AreEqual(Loc.T("Elektrikli kanal"), card.Title);
            Assert.AreEqual(Loc.T("Döküm ızgaralı kanal"), card.Subtitle);
            card = Card(t[3]);
            Assert.AreEqual(Loc.T("Boş kare"), card.Title, "gedik boş karedir");
            Assert.AreEqual(Loc.T("Dağılmış dosyalar"), card.Subtitle);
            Assert.IsTrue(string.IsNullOrEmpty(Card(t[4]).Subtitle), "sade zeminde alt satır yok");
            Assert.AreEqual(Loc.T("Döküm ızgaralı kanal"), source.Current.CableName);
            var robot = source.State.Units.First(u => u.Side == 'e' && Baskin.OnBoard(u));
            source.State.Cells[robot.Y, robot.X].T = "cable";
            Assert.IsTrue(string.IsNullOrEmpty(Card(new Vector2Int(robot.X, robot.Y)).Subtitle), "kanaldaki robotun kartında mekân adı yok");

            // Japonya-Kore: kanal "Bara kanalı"
            source.OpenForValidation(KitBoard("jk", 4242, out t));
            card = Card(t[2]);
            Assert.AreEqual(Loc.T("Elektrikli kanal"), card.Title);
            Assert.AreEqual(Loc.T("Bara kanalı"), card.Subtitle);
            Assert.AreEqual(Loc.T("Bara kanalı"), source.Current.CableName);

            // Editörün kitOverride'ı MapSeed 0'da da kiti zorlar (yalnız editörde)
            var so = new SerializedObject(source);
            so.FindProperty("kitOverride").stringValue = "sea";
            so.ApplyModifiedPropertiesWithoutUndo();
            source.OpenForValidation(KitBoard("weu", 0, out t));
            Assert.AreEqual("sea", source.KitId);
            Assert.AreEqual(Loc.T("Kum torbası barikatı"), Card(t[0]).Subtitle);
        }

        /// <summary>
        /// Kurallar kartının lejantı (KURALLAR, legend.channel): bina kitinde “Elektrikli kanal · bara kanalı” (ad küçük harfle, bölünmez).
        /// On iki kitin hepsi TR ve EN'de 1080p'de kesilmeden sığar: satır en çok iki satıra iner (ikinci satır adın tamamı), alttaki
        /// satırlar işaretleriyle aynı miktar kayar ve lejant uzar; varsayılan kitte lejant prefab'daki gibi.
        /// </summary>
        [Test]
        public void RulesLegend_ChannelRowNamesTheKitsChannelAndFits()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidPanels.prefab");
            try
            {
                // TMP (UGUI) metni yalnız bir Canvas altında kurar; prefab içeriğinin kökü sahnede bir Canvas'ın çocuğu
                if (root.GetComponentInParent<Canvas>() == null) root.AddComponent<Canvas>();
                var view = root.GetComponentInChildren<RaidPanelsView>(true);
                var so = new SerializedObject(view);
                var rowsProp = so.FindProperty("legendRows"); var marksProp = so.FindProperty("legendMarks");
                var rows = Enumerable.Range(0, rowsProp.arraySize).Select(i => (TMPro.TMP_Text)rowsProp.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var marks = Enumerable.Range(0, marksProp.arraySize).Select(i => (RectTransform)marksProp.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var legend = (RectTransform)so.FindProperty("legendRoot").objectReferenceValue;
                Assert.AreEqual(8, rows.Length);
                Assert.AreEqual(8, marks.Length, "her satırın işareti bağlı");
                CollectionAssert.AllItemsAreNotNull(marks);
                var vm = source.Current;
                vm.OpenPanel = "rules";
                vm.CableName = null;
                view.Render(vm);
                Assert.AreEqual(Loc.T("Elektrikli kanal"), rows[3].text, "varsayılan kitte lejant bugünkü gibi");
                var rowRest = rows.Select(r => r.rectTransform.anchoredPosition).ToArray();
                var markRest = marks.Select(m => m.anchoredPosition).ToArray();
                float restHeight = legend.sizeDelta.y;
                var english = Loc.ParseTable(System.IO.File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"))[Loc.English];
                var turkishLetters = new System.Text.RegularExpressions.Regex("[ğüşıöçĞÜŞİÖÇ]");
                int twoLines = 0;
                try
                {
                    foreach (var lang in new[] { Loc.Turkish, Loc.English })
                    {
                        Loc.SetLanguage(lang, lang == Loc.English ? english : null);
                        foreach (var kit in RaidKitText.KitIds)
                        {
                            vm.CableName = RaidKitText.Cable(kit);
                            view.Render(vm);
                            var ch = rows[3];
                            ch.ForceMeshUpdate(true, true);
                            string name = DeskText.Lower(vm.CableName, DeskFormat.Lang), at = lang + " " + kit + ": " + ch.GetParsedText();
                            Assert.AreEqual(RaidText.Get("legend.channel") + " · " + name, ch.GetParsedText(), at);
                            if (lang == Loc.English)
                                foreach (var n in new[] { RaidKitText.Wall(kit), RaidKitText.Cable(kit), RaidKitText.Rubble(kit) })
                                    Assert.IsFalse(turkishLetters.IsMatch(n), "İngilizcede Türkçe harf: " + n);
                            Assert.IsFalse(ch.isTextOverflowing, at + ": kesilmeden sığar");
                            Assert.LessOrEqual(ch.textInfo.lineCount, 2, at);
                            Assert.LessOrEqual(ch.GetPreferredValues(ch.text, ch.rectTransform.rect.width, float.PositiveInfinity).y, ch.rectTransform.rect.height + 0.5f, at);
                            float extra = legend.sizeDelta.y - restHeight;
                            if (ch.textInfo.lineCount == 2)
                            {
                                twoLines++;
                                var line = ch.textInfo.lineInfo[1];
                                string second = new string(Enumerable.Range(line.firstCharacterIndex, line.lastCharacterIndex - line.firstCharacterIndex + 1)
                                    .Select(i => ch.textInfo.characterInfo[i].character).ToArray()).Trim();
                                Assert.AreEqual(name, second, at + ": ad bölünmez, ikinci satırda bütün");
                                Assert.Greater(extra, 10, at + ": lejant bir satır uzar");
                            }
                            else Assert.AreEqual(0, extra, 0.01f, at + ": tek satırda lejant aynı");
                            for (int i = 0; i < rows.Length; i++)
                            {
                                var shift = new Vector2(0, i / 2 > 1 ? extra : 0);
                                Assert.AreEqual(rowRest[i] - shift, rows[i].rectTransform.anchoredPosition, at + " satır " + i);
                                Assert.AreEqual(markRest[i] - shift, marks[i].anchoredPosition, at + " işaret " + i);
                            }
                        }
                    }
                }
                finally { Loc.SetLanguage(Loc.Turkish); }
                Assert.Greater(twoLines, 0, "uzun adlar iki satıra iner");
                vm.CableName = null;
                view.Render(vm);
                CollectionAssert.AreEqual(rowRest, rows.Select(r => r.rectTransform.anchoredPosition).ToArray(), "varsayılana dönünce yerler geri gelir");
                Assert.AreEqual(restHeight, legend.sizeDelta.y, 0.01f);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void MoveUndo_RestoresPositionRightsAndDoesNotConsumeRandom()
        {
            source.Select('T');
            var tech = Technician(source.State);
            var before = (tech.X, tech.Y);
            int rng = source.State.Rs;
            var destination = source.Current.Moves.First();
            source.Move(destination);
            Assert.IsFalse(source.Current.Team[0].Move);
            Assert.IsTrue(source.Current.CanUndo);
            Assert.AreEqual(destination.x, Technician(source.State).X);
            source.Undo();
            Assert.AreEqual(before, (Technician(source.State).X, Technician(source.State).Y));
            Assert.IsTrue(source.Current.Team[0].Move);
            Assert.AreEqual(rng, source.State.Rs);
        }

        [Test]
        public void Preview_IsReadOnlyAndCancelUnwindsArmedThenSelectionThenPanel()
        {
            var robot = RobotNextToTechnician();
            source.Select('T');
            source.Arm();
            Assert.IsTrue(source.Current.AbilityArmed);
            var positions = source.State.Units.Select(u => (u.X, u.Y, u.Hp)).ToArray();
            source.Hover(robot);
            CollectionAssert.AreEqual(positions, source.State.Units.Select(u => (u.X, u.Y, u.Hp)).ToArray());
            source.Cancel();
            Assert.IsFalse(source.Current.AbilityArmed);
            Assert.IsNotNull(source.Current.UnitCard);
            source.Cancel();
            Assert.IsNull(source.Current.UnitCard);
            source.Panel("log");
            source.Cancel();
            Assert.IsNull(source.Current.OpenPanel);
            Assert.IsFalse(source.Current.Paused);
        }

        [Test]
        public void AbilityUndo_RestoresVictimAndActionRights()
        {
            var robot = RobotNextToTechnician();
            var before = source.State.Units.Select(u => (u.Id, u.Hp, u.X, u.Y)).ToArray();
            source.Select('T');
            source.Arm();
            source.Use(robot);
            Assert.IsFalse(source.Current.Team[0].Ability);
            Assert.IsTrue(source.Current.CanUndo);
            source.Undo();
            CollectionAssert.AreEqual(before, source.State.Units.Select(u => (u.Id, u.Hp, u.X, u.Y)).ToArray());
            Assert.IsTrue(source.Current.Team[0].Ability);
        }

        [Test]
        public void TurnRequiresConfirmationThenLocksAllCommands()
        {
            source.EndTurn();
            Assert.IsTrue(source.Current.EndConfirm);
            Assert.IsFalse(source.Current.Locked);
            source.Cancel();
            Assert.IsFalse(source.Current.EndConfirm);
            source.EndTurn(); source.EndTurn();
            Assert.IsTrue(source.Current.Locked);
            source.Select('T');
            Assert.IsNull(source.Current.UnitCard);
            source.Arm();
            Assert.IsFalse(source.Current.AbilityArmed);
            source.Speed(true);
            Assert.IsTrue(source.Current.FastMachines);
            source.Speed(false);
            Assert.IsFalse(source.Current.FastMachines);
        }

        [TestCase('T', 2)]
        [TestCase('E', 2)]
        [TestCase('A', 2)]
        public void Selection_UsesUnitSpecificRows(char unit, int count)
        {
            source.Select(unit);
            Assert.AreEqual(count, source.Current.UnitRows.Length);
            Assert.AreEqual(unit, source.Current.UnitCard.Letter);
        }

        [Test]
        public void BadDestinationsCannotMoveOrDamageAndRetreatRequiresTwoRequests()
        {
            source.Select('T');
            source.Move(new Vector2Int(-1, -1));
            source.Arm();
            source.Use(new Vector2Int(99, 99));
            Assert.IsFalse(source.Current.CanUndo);
            source.Retreat();
            Assert.IsTrue(source.Current.RetreatArmed);
            source.Cancel();
            Assert.IsFalse(source.Current.RetreatArmed);
            source.Retreat(); source.Retreat();
            Assert.IsTrue(source.State.Retreated);
        }
    }
}
