using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 12, parça P4b (aşama 12 tasarımı §11.2, §11.3, §11.6): Katar ve Yalpa veri kaynağında, sahnesiz. Katar: giriş vagonunda
    /// kenar yok, ara vagonda kenar sayısı Core'un <c>brink</c> sayısı; açık kenar hover'ı; EMP önizlemesinin düşme satırları (makine ve
    /// ekip), gerçek Q ve kareye kullanma, telsiz fişi, geri alma; ekip kartında VAGONDAN DÜŞTÜ ve sonraki odada PAFTA'lı hâli; sonuç kartı
    /// eki; dönüşte AYIR (Q ve Teknisyen'in kartı yalnız <c>CanPass &amp;&amp; CanDecouple</c>'da çevirir), EMRİ İLET AYIR, takipçi etiketi
    /// ve hover'ı, durum satırı, rapor; takipli geçişte gözcü ve kapanış satırı. Yalpa: tur şeridi okları, önceki turun soluk okları ve
    /// "YALPA 2", yatış turunda kayma hatları (Core <c>Slides</c> ile aynı, çarpan Hit), "YALPA 1", hover satırları (kayan, çarpan,
    /// cıvatalı, sabit). Kurulum (ışınlama, makineler) test kurulumudur; komutlar veri kaynağının gerçek yolundan.
    /// </summary>
    public sealed class FacilityConditionPresentationTests
    {
        GameObject root;
        LiveRaidDataSource source;
        Simulation sim;
        DeskLang lang;

        void Open(string region, System.Action<Simulation> before = null)
        {
            lang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            root = new GameObject("Tesis şartları sunumu");
            root.SetActive(false);
            source = root.AddComponent<LiveRaidDataSource>();
            sim = FacilityDeskKit.Reach(79, region);
            before?.Invoke(sim);
            FacilityDeskKit.StartFirst(sim);
            source.OpenFacilityForValidation(sim);
        }

        [TearDown]
        public void Close()
        {
            if (root != null) Object.DestroyImmediate(root);
            root = null;
            DeskLoc.Instance.SetLanguage(lang);
        }

        Baskin.BState B => source.State;
        Baskin.Unit T => Team(B, "tek");
        Baskin.Unit E => Team(B, "emp");
        Baskin.Unit A => Team(B, "ana");

        void Settle() { FacilityDeskKit.Calm(B); Floor(B); Park(B); Republish(source); }

        /// <summary>Sıradaki pafta: makineler kalkar, hedef odasında kaçış başlar, ayaktakiler kapıya, EMRİ İLET (GEÇ).
        /// <paramref name="keep"/>: odada geçitlerden uzak bir makine ayakta kalır (oda temiz sayılmaz; dönüşte yürünmez, oynanır).</summary>
        void Next(bool keep = false)
        {
            int filed = sim.S.Raid.Reports.Count;
            FacilityDeskKit.Calm(B);
            if (keep) { Floor(B); Machine(B, 3, 6); }
            if (B.Room.Type == RoomType.Target && !B.Escape) FacilityDeskKit.StartEscape(B);
            FacilityDeskKit.ToGateCore(B);
            Republish(source);
            source.EndTurn();
            Assert.AreEqual(filed + 1, sim.S.Raid.Reports.Count, "GEÇ odanın raporunu dosyalar");
        }

        /// <summary>Hedeften dönüşte Katar'ın ara vagonuna kadar (dönüş, kaçış).</summary>
        void ToReturnCar()
        {
            Next();
            Next(true);
            Next();
            Assert.AreEqual(2, B.Room.Index, "dönüşte ara vagon");
            Assert.IsTrue(B.Escape && FacilityConditionText.IsKatarReturn(B), "Katar'ın dönüşü");
        }

        /// <summary>Dönüş vagonunda herkes kapıda, batı geçidine bir karede (bağlantıya 1) bir makine.</summary>
        Baskin.Unit GateWithFollower()
        {
            Settle();
            FacilityDeskKit.ToGateCore(B);
            var w = B.Room.West[0];
            var m = Machine(B, w.X + 1, w.Y);
            Republish(source);
            CollectionAssert.AreEqual(new[] { m.Id }, Baskin.FollowerIds(B), "bağlantıya yakın makine takipçi");
            Assert.IsTrue(source.Current.CanPass, "herkes kapıda");
            return m;
        }

        static bool HasRow(RaidInspectorModel card, string text, string detail = null) =>
            card.Rows.Any(r => r.Text == text && (detail == null || r.Detail == detail));

        [Test]
        public void Katar_Edges_FallPreview_Card_Undo_ResultSuffix()
        {
            Open("na");
            Assert.IsNull(B.Ev, "giriş vagonu şartsız");
            Assert.IsNull(source.MarksFor(source.Current).Hazards, "giriş vagonunda kenar çizgisi yok");
            Next();
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual("katar", B.Ev?.Id, "ara vagon Katar");
            Settle();
            int core = Baskin.Hazards(B).Edges.Count(e => e.Kind == "brink");
            Assert.Greater(core, 0);
            Assert.AreEqual(core, source.MarksFor(source.Current).Hazards.Edges.Count(e => e.Kind == "brink"), "kenar sayısı Core'unki");

            // Hover: açık kenar karesi (makine yok olur, ekip üyesi 5 vardiya yatar)
            var m = Machine(B, 3, 0);
            int mid = m.Id;
            Put(E, 3, 3); Put(A, 5, 0); Put(T, 6, 5);
            Republish(source);
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(3, 0)), "Açık kenar: buradan itilen düşer", "yok olur"), "makinenin karesi");
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(5, 0)), "Açık kenar: buradan itilen düşer", Loc.F("{0} vardiya yatar", 5)), "ekip üyesinin karesi");
            Assert.IsFalse(Card(source, new Vector2Int(3, 3)).Rows.Any(r => r.Text == "Açık kenar: buradan itilen düşer"), "iç kare kenar değil");

            // Q + hedef: önizleme düşmeyi yazar, kullanınca makine düşer (telsiz fişi), Z geri getirir
            source.Select('E');
            source.Arm();
            Assert.IsTrue(source.Armed, "EMP kuruldu");
            var preview = Card(source, new Vector2Int(3, 1));
            Assert.IsTrue(preview.Rows.Any(r => r.Text == "Kenardan düşer · yok olur" && r.Good), "makine düşme satırı (iyi)");
            source.Use(new Vector2Int(3, 1));
            Assert.IsTrue(B.Units.First(u => u.Id == mid).Dead, "makine kenardan düştü");
            string edge = FacilityRaidText.Cell(2, 3, 0);
            StringAssert.Contains(Loc.F("kenardan düştü · {0}", edge), source.Current.EventLine, "telsiz fişi kareyle");
            source.Undo();
            Assert.IsFalse(B.Units.First(u => u.Id == mid).Dead, "Z makineyi geri getirir");

            // Ekip üyesi: EMP kendi Analist'ini düşürür; kart VAGONDAN DÜŞTÜ
            var parked = B.Units.First(u => u.Id == mid); parked.X = 6; parked.Y = 6;
            Put(A, 3, 0);
            Republish(source);
            source.Select('E');
            source.Arm();
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(3, 1)), Loc.F("Vagondan düşer · {0} vardiya yatar", 5)), "ekip düşme satırı");
            source.Use(new Vector2Int(3, 1));
            Assert.IsTrue(A.Overboard && A.Dead, "Analist kenardan düştü");
            Assert.IsFalse(Baskin.IsBody(A), "gövde değil");
            var card = source.Current.Team.Single(t => t.Letter == Letter(A));
            Assert.IsTrue(card.Down);
            Assert.AreEqual("VAGONDAN DÜŞTÜ", card.DownText);
            StringAssert.Contains(Loc.F("vagondan düştü · {0}", edge), source.Current.EventLine);
            int aSlot = A.Slot;

            // Sonraki oda: kart PAFTA'lı; zincirin sonunda sonuç kartının eki
            Next();
            Assert.AreEqual(3, B.Room.Index);
            Assert.AreEqual(Loc.F("VAGONDAN DÜŞTÜ · PAFTA {0}", 2), source.Current.Team.Single(t => t.Letter == Short(Baskin.UnitTypes["ana"])).DownText);
            Next();
            Assert.IsTrue(source.Current.Finished, "zincir kapandı (temiz dönüş vagonları yürünür)");
            var squad = source.Current.ResultRows.Last();
            StringAssert.EndsWith(Loc.F("· {0} vagondan düştü", 1), squad.Value, "ekip satırının eki");
            Assert.IsTrue(source.Current.Log.Any(l => l.Text == Loc.F("Vagondan düştü: {0} · {1} · {2} vardiya", FacilityRaidText.PersonName(B.Params, aSlot), edge, 5)), "kapanış satırı");
        }

        static char Short(Baskin.UnitType t) => t.Short[0];

        [Test]
        public void Katar_Return_Detach_QAndCard_OnlyWhenDecouplable()
        {
            Open("na");
            Next();
            // Gidişte AYIR yok: herkes kapıdayken Q bir şey yapmaz
            Settle();
            Machine(B, 3, 6);
            FacilityDeskKit.ToGateCore(B);
            Republish(source);
            Assert.IsTrue(source.Current.CanPass);
            source.Arm();
            Assert.IsFalse(source.Detach, "gidişte AYIR yok");
            Assert.IsNull(source.Current.Status, "gidişte durum satırı yok");
            Assert.IsNull(source.Current.PassNote);
            source.EndTurn();
            ToReturnCarFromTarget();

            var m = GateWithFollower();
            var vm = source.Current;
            Assert.IsNull(vm.PassLabel, "AYIR kapalıyken GEÇ");
            Assert.AreEqual(Loc.F("{0} makine takip eder", 1), vm.PassNote);
            Assert.AreEqual("Enter: GEÇ · Q: AYIR (Teknisyen)", vm.Status);
            Assert.AreEqual(("TAKİP EDER", true), source.MarksFor(vm).Tags[m.Id], "takipçi etiketi (acil)");
            Assert.IsTrue(vm.Team.Single(t => t.Letter == 'T').Detach, "Teknisyen'in kartı AYIR verir");
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(m.X, m.Y)), Loc.F("Bağlantıya {0} kare içinde: GEÇ'te sonraki vagona gelir, AYIR'da kalır", 2)), "takipçi hover'ı");

            source.Arm();
            Assert.IsTrue(source.Detach, "Q AYIR'ı açar");
            Assert.AreEqual("AYIR", source.Current.PassLabel);
            Assert.AreEqual("takip yok", source.Current.PassNote);
            Assert.IsFalse(source.MarksFor(source.Current).Tags.ContainsKey(m.Id), "AYIR açılınca etiket kalkar");
            source.Arm();
            Assert.IsFalse(source.Detach, "Q kapatır");
            source.Select('T');
            Assert.IsTrue(source.Detach, "Teknisyen'in kartı açar");
            source.Select('T');
            Assert.IsFalse(source.Detach, "kart kapatır");

            // Yeteneğini kullanan Teknisyen AYIR veremez: Q çevirmez, durum satırı nedeni yazar
            T.Acted = true;
            Republish(source);
            source.Arm();
            Assert.IsFalse(source.Detach);
            Assert.AreEqual("AYIR yok: Teknisyen bu tur yeteneğini kullandı", source.Current.Status);
            Assert.IsFalse(source.Current.Team.Single(t => t.Letter == 'T').Detach);
            T.Acted = false;
            Republish(source);

            // EN
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            source.Arm();
            Assert.AreEqual("DETACH", source.Current.PassLabel);
            Assert.AreEqual("no pursuit", source.Current.PassNote);
            // Aşama 12 kapanışı: GEÇ'in yerleşik karşılığı "GO THROUGH", kapı "door" (düğme ve öbür kapı fişleriyle aynı terim)
            Assert.AreEqual("Enter: GO THROUGH · Q: DETACH (Technician)", source.Current.Status);
            Assert.AreEqual("team at the door · GO THROUGH or DETACH (Q)", FacilityConditionText.PassSlip(B));
            Assert.AreEqual("Within 2 squares of the coupling: follows into the next car on GO THROUGH, stays on DETACH",
                FacilityConditionText.FollowRow(B, m).Text);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Republish(source);

            // Enter: AYIR (rapor Decouple, takip yok)
            Assert.IsTrue(source.Detach);
            int filed = sim.S.Raid.Reports.Count;
            source.EndTurn();
            Assert.AreEqual(filed + 1, sim.S.Raid.Reports.Count);
            var rep = sim.S.Raid.Reports.Last();
            Assert.IsTrue(rep.Decouple, "rapor AYIR");
            Assert.IsEmpty(rep.Follow, "AYIR'da takip yok");
            Assert.IsTrue(source.Current.Log.Any(l => l.Text == Loc.F("Vagon ayrıldı: PAFTA {0}", 2)), "kapanış satırı");
            Assert.IsTrue(source.Current.Finished || !B.Units.Any(u => u.Side == 'e' && !u.Dead), "ayrılan vagonun makinesi gelmez");
        }

        /// <summary>Hedef odasından (kaçış başlar) dönüşteki ara vagona.</summary>
        void ToReturnCarFromTarget()
        {
            Next();
            Assert.AreEqual(2, B.Room.Index, "dönüşte ara vagon");
            Assert.IsTrue(B.Escape && FacilityConditionText.IsKatarReturn(B), "Katar'ın dönüşü");
        }

        [Test]
        public void Katar_Return_Pass_FollowerEntersNextCar_Spotter()
        {
            Open("na");
            ToReturnCar();
            var m = GateWithFollower();
            int filed = sim.S.Raid.Reports.Count;
            source.EndTurn();
            Assert.AreEqual(filed + 1, sim.S.Raid.Reports.Count);
            var rep = sim.S.Raid.Reports.Last();
            Assert.IsFalse(rep.Decouple);
            Assert.AreEqual(1, rep.Follow.Count, "rapor takipçiyi yazar");
            Assert.AreEqual(1, B.Room.Index, "takip vagonu oynanır (temiz sayılmaz)");
            Assert.IsTrue(B.Escape);
            var foes = B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();
            Assert.AreEqual(1, foes.Count, "takipçi giriş vagonunda");
            var f = foes[0];
            Assert.AreEqual(m.Type, f.Type);
            var cell = FacilityRaidText.Cell(1, f.X, f.Y);
            CollectionAssert.Contains(source.Current.Log.Select(l => l.Text).ToList(), Loc.F("Gözcü · takip: {0} makine doğu geçidinde: {1}", 1, cell), "gözcü");
            CollectionAssert.Contains(source.Current.Log.Select(l => l.Text).ToList(), Loc.F("Takip: {0} makine · PAFTA {1}", 1, 1), "kapanış satırı");
            Assert.LessOrEqual(B.Room.Forward.Min(g => System.Math.Abs(g.X - f.X) + System.Math.Abs(g.Y - f.Y)), 2, "doğu geçidinin yanında");
            Assert.IsNull(source.Current.PassNote, "giriş vagonunda AYIR yok");
        }

        [Test]
        public void Yalpa_Strip_TiltNextAndLines_Hover()
        {
            Open("oc", s => { s.Facility.Counter.Clear(); s.Facility.Counter.AddRange(new[] { 1, 0, 0, 0, 0, 0 }); });
            Assert.IsNull(source.Current.TurnEvents, "giriş odası şartsız: şerit çipsiz");
            Next();
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual("yalpa", B.Ev?.Id);
            Settle();
            var vm = source.Current;
            Assert.AreEqual(6, vm.Turns);
            Assert.AreEqual("←", vm.TurnEvents[2]);
            Assert.AreEqual("→", vm.TurnEvents[4]);
            Assert.IsTrue(vm.TurnEvents.Length <= 6 || vm.TurnEvents[6] == null, "6 turluk odada 6. tur son tur: yatış yok");

            // Tur 1: soluk oklar (tilt_next) ve "YALPA 2"; hat yok
            var hv = source.MarksFor(vm).Hazards;
            int col = Enumerable.Range(0, 8).Count(y => Baskin.CellAt(B, 0, y).T != "wall");
            Assert.AreEqual(col, hv.Edges.Count(e => e.Kind == "tilt_next" && e.X == 0 && e.X2 == -1), "batı kenarında soluk oklar");
            Assert.AreEqual("YALPA 2", hv.Marks.Single(h => h.Kind == "tilt_next").Label);
            Assert.IsFalse(hv.Lines.Any(l => l.Kind == "slide"), "önceki turda hat yok");

            // Tur 2 (yatış turu): kurulum
            Put(A, 0, 3);
            var e = Machine(B, 4, 3);
            var bolted = FacilityGearTestKit.AddMachine(B, "zdron", 5, 5);
            Put(T, 3, 6); MakeBody(T);
            Baskin.CellAt(B, 2, 2).O = new Baskin.Obj { K = "hurda", Hp = 2, Max = 2 };
            Baskin.CellAt(B, 6, 6).O = new Baskin.Obj { K = "tel", Hp = 1, Max = 1 };
            B.CargoX = 6; B.CargoY = 1;
            Put(E, 7, 0);
            B.Turn = 2;
            Assert.IsTrue(Baskin.Bolted(B, bolted), "imza makinesi cıvatalı (kademe 1)");
            Republish(source);
            hv = source.MarksFor(source.Current).Hazards;
            var lines = hv.Lines.Where(l => l.Kind == "slide").Select(l => (l.X, l.Y, l.X2, l.Y2, l.Hit)).ToList();
            CollectionAssert.AreEquivalent(Baskin.Slides(B).Select(l => (l.X, l.Y, l.X2, l.Y2, l.Hit)).ToList(), lines, "hatlar Core'un Slides'ı");
            Assert.Contains((0, 3, -1, 3, true), lines, "duvara (kenara) bitişik uzman çarpar");
            Assert.Contains((4, 3, 3, 3, false), lines, "açıktaki makine kayar");
            Assert.Contains((2, 2, 1, 2, false), lines, "hurda kayar");
            Assert.Contains((3, 6, 2, 6, false), lines, "gövde kayar");
            Assert.Contains((6, 1, 5, 1, false), lines, "yük kayar");
            Assert.IsFalse(lines.Any(l => l.Item1 == 5 && l.Item2 == 5), "cıvatalı hatsız");
            Assert.IsFalse(lines.Any(l => l.Item1 == 6 && l.Item2 == 6), "sabit nesne hatsız");
            Assert.AreEqual(col, hv.Edges.Count(x => x.Kind == "tilt"), "koyu oklar");
            Assert.AreEqual("YALPA 1", hv.Marks.Single(h => h.Kind == "tilt").Label);

            Assert.IsTrue(HasRow(Card(source, new Vector2Int(4, 3)), "Yatışta 1 kare kayar", "batı"), "kayan makine");
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(0, 3)), "Yatışta çarpar", "−1"), "çarpan uzman");
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(5, 5)), "Cıvatalı: yatışta kaymaz"), "cıvatalı");
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(6, 6)), "Sabit: kaymaz"), "sabit nesne");
            Assert.IsTrue(HasRow(Card(source, new Vector2Int(2, 2)), "Yatışta 1 kare kayar", "batı"), "hurda");
        }
    }
}
