using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 9, parça 3 (sığınak): depo ikinci sırası, defterin ödül ve kapalı sayfaları, üç girdi ve erzak satırı, eşya kartının
    /// kullanım satırı ve YENİ etiketi, dinlenen köpek, köpeğin hurdaya bırakılamaması, erzak durma nedeni (K23) ve eksik kit önce (K19)
    /// tahmininin Core kapanışıyla eşliği, Kulak kazanımının gözetim puanı. Kurulum <see cref="FacilityDeskKit"/>; ödüller gerçek
    /// <c>server_reward</c> komutuyla (bekleyen ödül elle kurulur: kayda girmeyen kurulum).
    /// </summary>
    public sealed class FacilityGearBoardTests
    {
        const string PagePrefabPath = "Assets/Corrigible/Desk/Prefabs/Rev3Board/RecipeBookPage.prefab";
        const string CardPrefabPath = "Assets/Corrigible/Desk/Prefabs/Rev3Board/RecipeCard.prefab";
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void Clean()
        {
            Loc.SetLanguage(Loc.Turkish);
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        static void UseEnglish()
        {
            var table = Loc.ParseTable(File.ReadAllText(TablePath));
            table.TryGetValue(Loc.English, out var en);
            Loc.SetLanguage(Loc.English, en);
        }

        static BoardSnapshot Snap(Simulation sim, bool english = false) => PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), english, false, false);

        /// <summary>Yedi temel tarif sırayla ödülden açılır (gerçek server_reward).</summary>
        static Simulation AllSeven()
        {
            var sim = FacilityDeskKit.New();
            foreach (var region in FacilityDeskKit.GearRegions) FacilityDeskKit.Reward(sim, region);
            CollectionAssert.AreEqual(FacilityGear.Kinds, sim.Facility.Recipes, "yedi tarif açıldı");
            return sim;
        }

        // ================================================================== defter

        [Test]
        public void Book_LockedPagesAfterOpen_OpeningDoesNotShift_SevenRecipes()
        {
            var sim = FacilityDeskKit.New();
            var s = Snap(sim);
            Assert.AreEqual(13, s.Recipes.Count, "6 açık + 7 kapalı sayfa");
            CollectionAssert.AreEqual(FacilityGear.Kinds, s.Recipes.Skip(6).Select(r => r.Id).ToArray(), "kapalı sayfalar FacilityGear.Kinds sırasıyla");
            Assert.IsTrue(s.Recipes.Skip(6).All(r => r.Locked), "açılmamış temel tarifler kapalı");
            Assert.AreEqual("KAPALI · VERITY sunucusundan", s.Recipes[6].LockedStamp);
            Assert.AreEqual("6 tarif · 7 kapalı", s.BookLine);
            Assert.AreEqual(BookZone.Page, RecipeBookView.RecipeZone(BookZone.RecipeL, s.Recipes[6]), "kapalı sayfanın kutusu sürüklenmez");
            Assert.AreEqual(BookZone.RecipeR, RecipeBookView.RecipeZone(BookZone.RecipeR, s.Recipes[1]), "açık sayfa sürüklenir");

            var before = s.Recipes.Take(6).Select(r => r.Id).ToArray();
            // Ödülle açılan sayfa kapalı kuyruktan açık listenin sonuna geçer; eski açık sayfaların dizini kaymaz (K17)
            FacilityDeskKit.Reward(sim, "sa");   // taret
            s = Snap(sim);
            CollectionAssert.AreEqual(before, s.Recipes.Take(6).Select(r => r.Id).ToArray());
            Assert.AreEqual("turret", s.Recipes[6].Id);
            Assert.IsFalse(s.Recipes[6].Locked);
            Assert.IsTrue(s.Recipes[6].Reward);
            Assert.AreEqual("PARAGON", sim.RegionName("cn"), "denetim: YZ adları");
            Assert.AreEqual(sim.RegionName("sa"), s.Recipes[6].Source);
            StringAssert.StartsWith(sim.RegionName("sa") + " sunucusundan", s.Recipes[6].Note, "ödül sayfasının el notu");
            Assert.AreEqual(13, s.Recipes.Count);
            Assert.IsFalse(s.Recipes.Skip(7).Any(r => r.Id == "turret"));
            Assert.AreEqual("7 tarif · 6 kapalı", s.BookLine);
            FacilityDeskKit.Reward(sim, "weu");
            s = Snap(sim);
            Assert.AreEqual("turret", s.Recipes[6].Id, "açık sayfa yerinde kalır");
            Assert.AreEqual("wire_kit", s.Recipes[7].Id);

            s = Snap(AllSeven());
            Assert.AreEqual(13, s.Recipes.Count);
            Assert.AreEqual(7, (s.Recipes.Count + 1) / 2, "SpreadCount 7");
            Assert.IsFalse(s.Recipes.Any(r => r.Locked));
            Assert.AreEqual("13 tarif", s.BookLine);
            for (int i = 0; i < s.Recipes.Count; i++) Assert.AreEqual(i, s.Recipes[i].Index);
        }

        [Test]
        public void Book_DecoyThreeInputs_DogSupplies_PagesFit_TrEn()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PagePrefabPath);
            Assert.IsNotNull(prefab, PagePrefabPath);
            var go = Object.Instantiate(prefab);
            created.Add(go);
            var page = go.GetComponent<RecipeBookPage>();
            Assert.AreEqual(3, page.InputIcons.Count, "üçüncü girdi satırı prefab'da");
            var sim = AllSeven();
            // Tezgâh notu da sayfaya sığmalı (en uzun el notu)
            sim.Facility.Perks.Add(FacilityGear.Workbench);
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                var s = Snap(sim, english);
                var decoy = s.Recipes.Single(r => r.Id == "decoy");
                Assert.AreEqual(3, decoy.Inputs.Count, "yem üç girdi");
                var dog = s.Recipes.Single(r => r.Id == "dog");
                Assert.AreEqual(2, dog.Inputs.Count, "köpek: kumaş + erzak");
                Assert.IsTrue(dog.Inputs[1].Supplies, "son satır erzak");
                Assert.AreEqual(4, dog.Supplies);
                Assert.AreEqual(english ? "rations" : "erzak", dog.Inputs[1].Name);
                for (int i = 0; i < s.Recipes.Count; i++)
                {
                    int spread = i / 2;
                    page.Bind(s.Recipes[i], s.Book, i % 2 == 1, spread > 0, spread < 6, true);
                    string where = (english ? "EN " : "TR ") + s.Recipes[i].Id;
                    Assert.IsFalse(page.Overflowing, where + ": " + string.Join(", ", page.OverflowFields));
                    if (s.Recipes[i].Id == "decoy")
                    {
                        Assert.IsTrue(page.InputIcons[2].gameObject.activeSelf, "yemde üçüncü satır");
                        Assert.IsNotNull(page.OutputIcon.sprite, "yem piktogramı (DeskSprites picto_decoy)");
                    }
                    if (s.Recipes[i].Id == "dog") Assert.IsNotNull(page.InputIcons[1].sprite, "erzak simgesi (icon_erzak)");
                    if (s.Recipes[i].Id == "armor") Assert.IsFalse(page.InputIcons[2].gameObject.activeSelf, "iki girdili sayfada üçüncü satır yok");
                }
            }
            // Kapalı sayfa: GİRDİ ve SÜRE yok, damga el notu yerinde, İŞ EMRİ sekmesi yok
            Loc.SetLanguage(Loc.Turkish);
            var locked = Snap(FacilityDeskKit.New()).Recipes[6];
            page.Bind(locked, Snap(FacilityDeskKit.New()).Book, false, true, false, true);
            Assert.IsFalse(page.ShowsTab, "kapalı sayfada İŞ EMRİ sekmesi yok");
            Assert.IsFalse(page.InputLabel.gameObject.activeSelf, "GİRDİ yok");
            Assert.IsFalse(page.TimeText.gameObject.activeSelf, "SÜRE yok");
            Assert.AreEqual(locked.LockedStamp, page.Note.text);
            Assert.IsFalse(page.Overflowing, string.Join(", ", page.OverflowFields));
        }

        /// <summary>
        /// Tesis aşama 10 (K20, K45): beş cevap gerçek server_reward ile seçilir; temelin sayfası aynı dizinde, aynı kimlikle cevabın adı,
        /// +1 devre ve el notuyla durur (Çift zemberek üç aygıtı, Söküm Teknisyen kitini dönüştürür); girdi türü en çok üç, sayfa taşmaz
        /// (TR ve EN); defterin satırı cevap sayısını söyler.
        /// </summary>
        [Test]
        public void AnswerPage_ReplacesBase_SameIndex_ThreeInputsFit()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PagePrefabPath);
            var go = Object.Instantiate(prefab);
            created.Add(go);
            var page = go.GetComponent<RecipeBookPage>();
            var sim = AllSeven();
            sim.Facility.Counter.Clear();
            sim.Facility.Counter.AddRange(new[] { 2, 2, 2, 2, 2, 2 });
            var before = Snap(sim).Recipes.Select(r => r.Id).ToArray();
            foreach (var region in new[] { "na", "cn", "eeu", "ru", "oc" }) FacilityDeskKit.Reward(sim, region);
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                var s = Snap(sim, english);
                CollectionAssert.AreEqual(before, s.Recipes.Select(r => r.Id).ToArray(), "dizinler ve kimlikler kaymaz");
                var answers = s.Recipes.Where(r => r.Answer != null).ToList();
                CollectionAssert.AreEquivalent(new[] { "kit_tek", "clamp", "charge", "wire_kit", "turret", "decoy", "winch" }, answers.Select(r => r.Id));
                foreach (var r in answers)
                {
                    Assert.AreEqual(FacilityGear.RecipeName(r.Answer), r.Name, r.Id);
                    Assert.LessOrEqual(r.Inputs.Count, 3, r.Id + ": en çok üç girdi türü");
                    Assert.IsTrue(r.Inputs.Any(i => i.Kind == "devre"), r.Id + ": devre");
                    Assert.IsFalse(string.IsNullOrEmpty(r.Note), r.Id + ": cevap notu");
                    page.Bind(r, s.Book, r.Index % 2 == 1, r.Index / 2 > 0, r.Index / 2 < 6, true);
                    Assert.IsFalse(page.Overflowing, (english ? "EN " : "TR ") + r.Id + ": " + string.Join(", ", page.OverflowFields));
                }
                Assert.AreEqual(english ? "13 recipes · 7 answers" : "13 tarif · 7 cevap", s.BookLine);
            }
        }

        [Test]
        public void RecipeCard_ThreeColumnsNarrow_DogBoxesAndSupplies()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            var go = Object.Instantiate(prefab);
            created.Add(go);
            var card = go.GetComponent<BoardCardView>();
            var face = (RecipeCardFace)card.Face;
            var sim = AllSeven();
            var s = Snap(sim);
            var decoy = s.Recipes.Single(r => r.Id == "decoy");
            var d = new RecipeCardData { Id = "decoy", RecipeId = "decoy", Name = decoy.Name, IconKey = "decoy", TurnsLine = decoy.TurnsLine, OutLine = "→ 1 ürün", NeedsLabel = "GİRDİ", Turns = decoy.Turns, Done = -1 };
            foreach (var i in decoy.Inputs) d.Inputs.Add(i);
            card.Bind(d);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(face.InputIcons[i].gameObject.activeSelf, "simge " + i);
                Assert.IsFalse(face.InputNames[i].gameObject.activeSelf, "üç girdide ad yok " + i);
                Assert.AreEqual(6f + 34f * i, face.InputIcons[i].rectTransform.anchoredPosition.x, 0.01f, "dar adım " + i);
            }
            Assert.IsNotNull(face.Icon.sprite, "çıktı simgesi adla (picto_decoy)");
            // İki girdiye dönünce adım 50 ve adlar geri gelir
            var armor = s.Recipes.Single(r => r.Id == "armor");
            var a = new RecipeCardData { Id = "armor", RecipeId = "armor", Name = armor.Name, IconKey = "armor", TurnsLine = armor.TurnsLine, OutLine = "→ 1 ürün", NeedsLabel = "GİRDİ", Turns = armor.Turns, Done = -1 };
            foreach (var i in armor.Inputs) a.Inputs.Add(i);
            card.Bind(a);
            Assert.AreEqual(56f, face.InputIcons[1].rectTransform.anchoredPosition.x, 0.01f);
            Assert.IsTrue(face.InputNames[1].gameObject.activeSelf);
            Assert.IsFalse(face.InputIcons[2].gameObject.activeSelf);
            // Köpek: kumaş + erzak, üç vardiya üç kutu
            var dog = s.Recipes.Single(r => r.Id == "dog");
            var g = new RecipeCardData { Id = "is-9", RecipeId = "dog", Name = dog.Name, IconKey = "dog", TurnsLine = dog.TurnsLine, OutLine = "→ 1 ürün", NeedsLabel = "GİRDİ", Turns = dog.Turns, Done = 1 };
            foreach (var i in dog.Inputs) g.Inputs.Add(i);
            card.Bind(g);
            Assert.AreEqual(3, dog.Turns);
            Assert.AreEqual(3, face.VisibleBoxes, "üç vardiya üç kutu");
            Assert.AreEqual(1, face.FilledBoxes);
            Assert.IsNotNull(face.InputIcons[1].sprite, "erzak sütununun simgesi");
            Assert.AreEqual("×4", face.InputQty[1].text);
        }

        // ================================================================== depo

        [Test]
        public void Store_GearRow_SevenKinds_CountOnFace_RestingDogBelow()
        {
            var sim = FacilityDeskKit.New();
            Assert.IsTrue(Snap(sim).Gear.All(g => g == null), "depoda ekipman yokken sıra boş (gölge yok)");
            foreach (var kind in FacilityGear.Kinds) sim.AddBoardEquipment(kind);
            var resting = sim.AddBoardEquipment(FacilityGear.Dog);
            resting.Rest = 3;
            // Dinlenen köpek önce eklense de alta düşer: ikinci köpek eklenir
            var s = Snap(sim);
            for (int i = 0; i < FacilityGear.Kinds.Length; i++)
            {
                Assert.IsNotNull(s.Gear[i], FacilityGear.Kinds[i]);
                Assert.AreEqual(FacilityGear.Kinds[i], s.Gear[i].Kind);
            }
            var dogs = s.Gear[6];
            Assert.AreEqual(2, dogs.Count);
            var top = s.Item(dogs.TopIds.Last());
            Assert.AreEqual(0, top.Rest, "üstteki kart dinlenmeyen köpek");
            Assert.AreEqual(2, top.Count);
            StringAssert.EndsWith("×2", top.SlotLabel, "yığın sayısı kart yüzünde");
            var rest = s.Item(resting.Id);
            Assert.AreEqual(3, rest.Rest);
            Assert.AreEqual("DİNLENİYOR · 3 V", rest.UseLine, "dinlenen köpeğin damgası");
            var turret = s.Item(s.Gear[2].TopIds.Last());
            Assert.AreEqual(0, turret.Count);
            Assert.AreEqual("YARDIMCI", turret.SlotLabel, "tek kartta sayı yok");
            Assert.AreEqual("baskında 1 hak · 3 tur çalışır", turret.UseLine);
            Assert.AreEqual("baskında 2 hak · tel ya da kalas", s.Item(s.Gear[0].TopIds.Last()).UseLine);
            Assert.AreEqual("dördüncü birim · havla, getir, kokla", top.UseLine);
            Assert.AreEqual("1 hak · kullanılınca tükenir", s.Item(s.Gear[5].TopIds.Last()).UseLine);
            // Yedek çanta örme setine +1 hak
            sim.Facility.Perks.Add(FacilityGear.SpareBag);
            Assert.AreEqual("baskında 3 hak · tel ya da kalas", Snap(sim).Item(s.Gear[0].TopIds.Last()).UseLine);
            // Kişiye takılan ekipman sıradan çıkar
            var person = sim.Board.People.First(p => p.Alive);
            Assert.IsTrue(sim.EquipResident(s.Gear[2].TopIds.Last(), person.Id).Ok);
            Assert.IsNull(Snap(sim).Gear[2], "taret kişide: yığın boş");
        }

        [Test]
        public void GearRow_InsideBoard_NoOverlap()
        {
            var inner = new Rect(0, 0, BoardLayout.InnerWidthMm, BoardLayout.InnerHeightMm);
            Rect Card(Vector2 c, float w = 80, float h = 50) => new Rect(c.x - w / 2, c.y - h / 2, w, h);
            var others = new List<(Rect r, string what)>();
            for (int i = 0; i < BoardLayout.MaterialKinds.Length; i++) others.Add((Card(BoardLayout.Material(i), 80, 56), "hammadde " + i));
            for (int i = 0; i < BoardLayout.KitKinds.Length; i++)
            {
                // Kit yığınının çizilen üç kartı (0,9 mm kayık) ve ×n etiketi
                others.Add((Card(BoardLayout.Kit(i)), "kit " + i));
                others.Add((new Rect(BoardLayout.KitCount(i).x - 10, BoardLayout.KitCount(i).y - 4, 20, 8), "kit sayacı " + i));
            }
            others.Add((BoardLayout.Hurda, "hurda"));
            Assert.AreEqual(7, BoardLayout.GearKinds.Length);
            for (int i = 0; i < BoardLayout.GearKinds.Length; i++)
            {
                // Üst kart (çapada) öbür öğelerle kesişmez; yığının alttaki kartları (0,9 mm kayık) pano iç alanında kalır. Kit yığınının
                // son kartı da hurda kutusuna aynı 1,8 mm sokulur (PlanningBoardSceneTests üst kartı ölçer)
                var r = Card(BoardLayout.GearCard(i, 0));
                Assert.IsTrue(BoardLayout.Depot.Contains(r.center), "DEPO bandında");
                foreach (var o in others) Assert.IsFalse(r.Overlaps(o.r), "ekipman " + i + " ↔ " + o.what + ": " + r + " / " + o.r);
                for (int k = 0; k < i; k++) Assert.IsFalse(r.Overlaps(Card(BoardLayout.Gear(k))), "ekipman " + i + " ↔ " + k);
                foreach (int j in new[] { 0, 2 })
                {
                    var rj = Card(BoardLayout.GearCard(i, j));
                    Assert.IsTrue(inner.Contains(rj.min) && inner.Contains(rj.max), "ekipman " + i + " pano iç alanında: " + rj);
                }
            }
            // Token'lar temada (içe aktarıldı) ve kod yedeğiyle aynı
            var theme = Resources.Load<SiginakTheme>("SiginakTheme");
            Assert.IsNotNull(theme);
            Assert.AreEqual(571f, theme.NumberOf("board.layoutMm.gearY", -1));
            Assert.AreEqual(459f, theme.NumberOf("board.layoutMm.gearX[0]", -1));
            Assert.AreEqual(951f, theme.NumberOf("board.layoutMm.gearX[6]", -1));
        }

        [Test]
        public void NewItem_Gear_SlidesToGearStack()
        {
            var sim = FacilityDeskKit.New();
            var e = sim.AddBoardEquipment("winch");
            sim.Board.LastCompleted.Add("winch");
            var n = PlanningBoardSnapshotBuilder.NewItemOf(sim, new BoardUiState());
            Assert.IsNotNull(n, "ekipman da YENİ alır");
            Assert.IsTrue(n.Gear);
            Assert.AreEqual(System.Array.IndexOf(BoardLayout.GearKinds, "winch"), n.Stack);
            Assert.AreEqual(e.Id, n.ItemId);
        }

        // ================================================================== takma, hurda, önizleme

        [Test]
        public void Scrap_DogRejected_OtherGearAllowed_RestNote()
        {
            var sim = FacilityDeskKit.New();
            var dog = sim.AddBoardEquipment(FacilityGear.Dog);
            var turret = sim.AddBoardEquipment(FacilityGear.Turret);
            Assert.AreEqual("Köpek hurdaya atılmaz.", BoardDropRules.Reason(BoardCardKind.Item, dog.Id, BoardTarget.Hurda(), sim));
            Assert.IsNull(BoardDropRules.Reason(BoardCardKind.Item, turret.Id, BoardTarget.Hurda(), sim), "öbür ekipman hurdaya gider");
            var preview = BoardPreviewText.For(BoardCardKind.Item, dog.Id, BoardTarget.Hurda(), sim);
            Assert.IsFalse(preview.Valid, "önizleme reddeder");
            Assert.AreEqual("Köpek hurdaya atılmaz.", preview.Body);
            dog.Rest = 2;
            var person = sim.Board.People.First(p => p.Alive && p.Job == "tek");
            var p2 = BoardPreviewText.For(BoardCardKind.Item, dog.Id, BoardTarget.Person(person.Id), sim);
            Assert.IsTrue(p2.Valid && p2.Warning, "takılır, uyarıyla");
            StringAssert.Contains("Köpek dinleniyor: 2 vardiya baskına gitmez", p2.Body);
            Assert.IsTrue(sim.EquipResident(dog.Id, person.Id).Ok);
            var slot = sim.PlanningSquad().Single(x => x.PersonId == person.Id);
            Assert.IsNull(slot.Gear, "dinlenen köpek baskına gitmez (Core)");
            var members = RaidPaperFacts.Members(sim);
            Assert.AreEqual("Köpek (dinleniyor · 2 V) · baskına gitmez", members.Single(m => m.PersonId == person.Id).Slots[2]);
            var folder = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = person.Id }, false, false, false).Folder;
            StringAssert.Contains("(dinleniyor)", folder.Gear[2]);
            // Dinlenmeyen ekipman hakkıyla
            dog.Rest = 0;
            Assert.AreEqual("Köpek", RaidPaperFacts.Members(sim).Single(m => m.PersonId == person.Id).Slots[2]);
            var other = sim.Board.People.First(p => p.Alive && p.Job == "emp");
            Assert.IsTrue(sim.EquipResident(turret.Id, other.Id).Ok);
            Assert.AreEqual("Yaylı taret · 1 hak", RaidPaperFacts.Members(sim).Single(m => m.PersonId == other.Id).Slots[2]);
        }

        // ================================================================== iş emri ve tahmin

        [Test]
        public void Forecast_NoSupplies_DogOrderWaits_MatchesCoreClose()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.Reward(sim, "jk");   // köpek
            Assert.IsTrue(sim.QueueProduction(FacilityGear.Dog).Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("kumas", 1).Ok);
            Craft(sim);
            // Kayda girmeyen kurulum: kapanışın erzak değişiminden sonra 3 erzak (açlık yok, bedel 4)
            sim.Shelter.Supplies = 3 - sim.BoardFoodPreview;
            Assert.AreEqual(3, ProductionForecast.SuppliesAtProduction(sim), 1e-6);
            var o = ProductionForecast.Of(sim);
            Assert.AreEqual(ProductionStop.NoSupplies, o.Stop, "erzak yetmiyor");
            Assert.IsFalse(o.Works);
            var s = Snap(sim);
            Assert.AreEqual(OrderState.Stopped, s.Order.State);
            Assert.AreEqual("erzak yetmiyor · emir bekliyor", s.Order.StopReason);
            Assert.AreEqual(2, s.Order.Card.Inputs.Count, "kart: kumaş + erzak");
            Assert.IsTrue(s.Order.Card.Inputs[1].Supplies);
            Close(sim);
            Assert.IsFalse(sim.Board.Orders.Single().Started, "Core: emir başlamadı (tahminle aynı)");

            sim.Shelter.Supplies = 40;
            o = ProductionForecast.Of(sim);
            Assert.AreEqual(ProductionStop.None, o.Stop);
            Assert.IsTrue(o.Works);
            Close(sim);
            Assert.IsTrue(sim.Board.Orders.Single().Started, "Core: erzak yetince emir başladı");
        }

        [Test]
        public void Forecast_KitFirst_K19_MatchesCoreClose()
        {
            var sim = FacilityDeskKit.New();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            foreach (var m in sim.BoardRecipes.Single(r => r.Id == "armor").Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(m.Kind, m.Quantity).Ok);
            Craft(sim);
            // Analist kiti kaybolmuş (kayda girmeyen kurulum): K19 eksik kiti önce yapar
            sim.Board.Equipment.RemoveAll(e => e.Kind == "kit_ana");
            Assert.IsTrue(sim.BoardKitMissing("kit_ana"));
            var o = ProductionForecast.Of(sim);
            Assert.IsTrue(o.KitFirst, "tahmin: bu kapanış kit");
            Assert.AreEqual("kit_ana", o.Output);
            Assert.AreEqual("armor", o.WaitingRecipeId);
            var s = Snap(sim);
            StringAssert.Contains("bu kapanış analist kiti yapılır · emir beklemede", s.Order.Status);
            int remaining = sim.Board.Orders.Single().Remaining;
            Close(sim);
            Assert.IsFalse(sim.BoardKitMissing("kit_ana"), "Core: kit yapıldı");
            Assert.AreEqual(remaining, sim.Board.Orders.Single().Remaining, "Core: emir ilerlemedi");
            Assert.IsFalse(sim.Board.Orders.Single().Started, "Core: girdiler ödenmedi");
            Assert.IsFalse(ProductionForecast.Of(sim).KitFirst, "kit tamam: emir sırası");
        }

        [Test]
        public void WatchGain_Ear_PlusOne()
        {
            var sim = FacilityDeskKit.New();
            Assert.AreEqual(sim.B.facilityWatchIntel, FacilityWatchText.Gain(sim));
            FacilityDeskKit.Reward(sim, "weu", FacilityGear.ChoicePerk);   // Kulak
            Assert.AreEqual(sim.B.facilityWatchIntel + sim.B.facilityEarIntel, FacilityWatchText.Gain(sim), "Kulak: Core'un AdvanceFacilityIntel'iyle aynı");
        }

        // ================================================================== yardımcılar

        /// <summary>Üretim cebinde biri olsun (yeni oyunda "craft" kurucu).</summary>
        static void Craft(Simulation sim)
        {
            if (sim.Board.People.Any(p => p.Alive && p.Job == "craft" && p.Down <= 0)) return;
            var p0 = sim.Board.People.First(p => p.Alive && p.Down <= 0 && !PlanningBoardData.FieldJobs.Contains(p.Job));
            Assert.IsTrue(sim.AssignResident(p0.Id, "craft").Ok);
        }

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }
    }
}
