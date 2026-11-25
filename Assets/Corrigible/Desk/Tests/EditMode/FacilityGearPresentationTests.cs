using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;
using static Corrigible.Desk.Tests.FacilityGearTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Ekipman ve uzman olmayan birimler veri kaynağında, sahnesiz (aşama 9; tasarım §2, §3, §11.3): R ile ekipman kipi (döngü,
    /// kapatan komutlar, kart satırı ve R fişi, tahtaya tıkla eylem), köpeğin seçimi (kimlikle, 4 ile sıradaki köpek) ve E komutları,
    /// tutsağın tahtada tıkla seçilmesi, aygıtın seçilmemesi, harf aramasının tutsağı bulmaması; sınıf süzgeçleri (onay, kaçış
    /// uyarısı, KAPIDA paydası); dördüncü ekip kartı, eşya satırı, kurallar satırı, hover kartı, kart katlaması. Tahta ve HUD yok;
    /// eylemler eşzamanlı.
    /// </summary>
    public class FacilityGearPresentationTests
    {
        GameObject root;
        LiveRaidDataSource source;
        DeskLang lang;

        [SetUp]
        public void Open()
        {
            lang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            root = new GameObject("Tesis ekipmanı sunumu");
            root.SetActive(false);
            source = root.AddComponent<LiveRaidDataSource>();
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            source.OpenFacilityForValidation(sim);
            FacilityDeskKit.Calm(B); Floor(B); Park(B);
            Clear(B, T);
            Republish(source);
        }

        [TearDown] public void Close() { Object.DestroyImmediate(root); DeskLoc.Instance.SetLanguage(lang); }

        Baskin.BState B => source.State;
        Baskin.Unit T => Team(B, "tek");
        Baskin.Unit E => Team(B, "emp");
        Baskin.Unit A => Team(B, "ana");

        void Click(int x, int y) => typeof(RaidDataSource).GetMethod("Click", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, new object[] { x, y });
        void Select(Baskin.Unit u) { source.Select(Letter(u)); Assert.AreEqual(Letter(u), source.Selected); }

        [Test]
        public void R_CyclesWireKit_ThenCloses_QEEscUndoClose()
        {
            Gear(T, FacilityGear.Wire, 2);
            Select(T);
            var vm = source.Current;
            Assert.AreEqual(1, vm.GearRows.Length, "tek ekipman satırı");
            Assert.AreEqual(0, vm.GearKey, "kullanılabilirken R fişi satırda");
            Assert.AreEqual(PlanningBoardData.EquipmentName(FacilityGear.Wire), vm.GearRows[0].Text);
            Assert.IsTrue(vm.ActionRows.All(r => r.Text != Loc.T("Tel ör")), "E satırlarında ekipman yok");
            source.ArmGear();
            Assert.AreEqual(Baskin.ActWire, source.GearKind);
            Assert.IsTrue(source.Current.GearArmed);
            Assert.IsTrue(source.Current.GearRows[0].Armed);
            Assert.AreEqual(0, source.Current.Moves.Length, "kipte yürüyüş boyası yok");
            CollectionAssert.AreEquivalent(Baskin.ActionTargets(B, T, Baskin.ActWire).Select(p => new Vector2Int(p.X, p.Y)).ToArray(), source.Current.AbilityTargets);
            CollectionAssert.AreEqual(FacilityGearText.Effects(B, T, Baskin.ActWire).Texts, source.Current.Effects);
            source.ArmGear();
            Assert.AreEqual(Baskin.ActPlank, source.GearKind);
            source.ArmGear();
            Assert.IsFalse(source.GearArmed, "sonuncudan sonra kapanır");
            // Kart satırına tık R ile aynı
            source.ChooseGear(0);
            Assert.AreEqual(Baskin.ActWire, source.GearKind);
            // Q kapatır (yeteneği kurar); E kapatır (hamle kurar); R hamle kipini kapatır
            source.Arm();
            Assert.IsFalse(source.GearArmed);
            Assert.IsTrue(source.Current.AbilityArmed);
            source.ArmGear();
            Assert.IsTrue(source.GearArmed);
            Assert.IsFalse(source.Current.AbilityArmed);
            source.ArmAction();
            Assert.IsTrue(source.ActionArmed);
            Assert.IsFalse(source.GearArmed, "E ekipman kipini kapatır");
            source.ArmGear();
            Assert.IsFalse(source.ActionArmed, "R hamle kipini kapatır");
            Assert.IsTrue(source.GearArmed);
            // Esc yalnız kipi kapatır
            source.Cancel();
            Assert.IsFalse(source.GearArmed);
            Assert.AreEqual(Letter(T), source.Selected);
            // Tahtaya tık eylemi verir: tel kurulur, kip kapanır, telsiz fişi, Z geri alır
            source.ArmGear();
            var at = Baskin.ActionTargets(B, T, Baskin.ActWire)[0];
            int hist = B.Hist.Count;
            Click(at.X, at.Y);
            Assert.AreEqual(hist + 1, B.Hist.Count);
            Assert.AreEqual("tel", Baskin.CellAt(B, at.X, at.Y).O?.K);
            Assert.IsFalse(source.GearArmed);
            Assert.AreEqual(1, T.GearLeft);
            Assert.AreEqual(FacilityGearText.Slip(B, Baskin.ActWire, T.Id, at.X, at.Y, id => source.Current.Team.First(t => t.Letter == 'T').Name), source.Current.EventLine);
            Assert.IsTrue(source.Current.Log.Last().Text.Contains(Loc.F("{0} tel ördü.", Baskin.UnitTypes["tek"].Name)));
            source.Undo();
            Assert.IsNull(Baskin.CellAt(B, at.X, at.Y).O);
            Assert.AreEqual(2, T.GearLeft);
            // Z kipi kapatır
            Select(T);
            source.ArmGear();
            source.Move(new Vector2Int(T.X, T.Y - 1));
            Select(T);
            source.ArmGear();
            source.Undo();
            Assert.IsFalse(source.GearArmed);
        }

        [Test]
        public void R_IgnoredForDogCaptiveAndGearless_AndRowWhenUnusable()
        {
            Select(T);
            source.ArmGear();
            Assert.IsFalse(source.GearArmed, "ekipmansız uzmanda R etkisiz");
            Assert.AreEqual(0, source.Current.GearRows.Length);
            Assert.AreEqual(-1, source.Current.GearKey);
            var dog = AddDog(B, T.Slot, 6, 1);
            source.SelectId(dog.Id);
            source.ArmGear();
            Assert.IsFalse(source.GearArmed, "köpekte R etkisiz");
            // Hak bitmiş: satır soluk, R fişi yok, R etkisiz
            Gear(T, FacilityGear.Turret, 0);
            Select(T);
            Assert.IsTrue(source.Current.GearRows[0].Used);
            Assert.AreEqual(-1, source.Current.GearKey);
            Assert.AreEqual(Loc.T("hak kalmadı"), source.Current.GearRows[0].Detail);
            source.ArmGear();
            Assert.IsFalse(source.GearArmed);
            // Aygıt kurulur ve seçilmez (tık hover'a kalır, yürüyüşe düşmez)
            Gear(T, FacilityGear.Turret, 1);
            Republish(source);
            source.ArmGear();
            var at = Baskin.ActionTargets(B, T, Baskin.ActTurret)[0];
            Click(at.X, at.Y);
            var g = B.Units.Single(Baskin.IsGadget);
            Assert.AreEqual(FacilityGearView.DirTo(T.X, T.Y, at.X, at.Y), g.Dir, "yön uzmandan kareye");
            Select(A);
            int ax = A.X, ay = A.Y;
            Click(g.X, g.Y);
            Assert.AreEqual(Letter(A), source.Selected, "aygıta tık seçimi değiştirmez");
            Assert.AreEqual(0, source.SelectedOther);
            Assert.AreEqual((ax, ay), (A.X, A.Y), "aygıta tık yürüyüş değil");
        }

        [Test]
        public void Dog_SelectById_FourCycles_CommandsByE()
        {
            var d1 = AddDog(B, T.Slot, 5, 1);
            var d2 = AddDog(B, E.Slot, 6, 2);
            Republish(source);
            var vm = source.Current;
            Assert.AreEqual(4, vm.Team.Length, "dördüncü kart köpeğin");
            var card = vm.Team[3];
            Assert.IsTrue(card.Dog);
            Assert.AreEqual(d1.Id, card.UnitId, "seçili yokken ayaktaki ilk köpek (sahip sırası)");
            Assert.AreEqual(Loc.T("KÖPEK"), card.Name);
            Assert.AreEqual(Loc.F("Sahibi: {0} · +{1} köpek", FacilityMoveText.Who(T), 1), card.Equipment);
            // 4 (kartın kimliği): seçer; seçiliyken sıradaki köpek
            source.SelectId(card.UnitId, true);
            Assert.AreEqual(d1.Id, source.SelectedOther);
            Assert.AreEqual('\0', source.Selected);
            Assert.IsTrue(source.Current.Team[3].Selected);
            source.SelectId(source.Current.Team[3].UnitId, true);
            Assert.AreEqual(d2.Id, source.SelectedOther, "4 sıradaki köpeğe geçer");
            Assert.AreEqual(d2.Id, source.Current.Team[3].UnitId, "kart seçili köpeği gösterir");
            source.SelectId(source.Current.Team[3].UnitId, true);
            Assert.AreEqual(d1.Id, source.SelectedOther, "döner");
            // Köpeğin kartı: başlık sahibiyle, yalnız Hareket satırı, Q yok; E satırları havla (+ kokla)
            vm = source.Current;
            Assert.AreEqual(FacilityGearText.DogName(B, d1), vm.UnitCard.Name);
            Assert.AreEqual(1, vm.UnitRows.Length, "yalnız Hareket");
            Assert.AreEqual(B.Moves.DogMove, vm.UnitCard.MoveRange);
            Assert.AreEqual(0, vm.Effects.Length, "kip kapalıyken alt düğme yok");
            Assert.AreEqual(Loc.T("Havla"), vm.ActionRows[0].Text);
            source.Arm();
            Assert.IsFalse(source.Current.AbilityArmed, "köpekte Q yok");
            // Yürür (yürüyüşü 5), E ile havlar: tahtaya kendi karesine tık
            Assert.IsTrue(vm.Moves.Length > 0);
            source.ArmAction();
            Assert.AreEqual(Baskin.ActBark, source.ActionKind);
            int hist = B.Hist.Count;
            Click(d1.X, d1.Y);
            Assert.AreEqual(hist + 1, B.Hist.Count);
            Assert.IsTrue(B.Units.Single(u => u.Id == d1.Id).Bark);
            Assert.AreEqual(Loc.T("Köpek · havladı"), source.Current.EventLine);
            // Uzmanı harfle seçmek köpek seçimini kaldırır; Esc seçimi kaldırır
            Select(T);
            Assert.AreEqual(0, source.SelectedOther);
            source.SelectId(d2.Id);
            source.Cancel();
            Assert.AreEqual(0, source.SelectedOther);
            // Köpek düşer: kartta TAHTADAN ÇEKİLDİ; 4 onu seçmez
            var dead = B.Units.Single(u => u.Id == d2.Id);
            dead.Dead = true; dead.Hp = 0;
            var alive = B.Units.Single(u => u.Id == d1.Id);
            alive.Dead = true; alive.Hp = 0;
            Republish(source);
            Assert.IsTrue(source.Current.Team[3].Down);
            Assert.AreEqual(Loc.T("TAHTADAN ÇEKİLDİ"), source.Current.Team[3].DownText);
            source.SelectId(source.Current.Team[3].UnitId, true);
            Assert.AreEqual(0, source.SelectedOther);
        }

        [Test]
        public void Captive_ClickSelects_LetterDoesNot_CommandByE()
        {
            var m = Machine(B, 4, 4);
            m.Type = "dron";
            Captive(B, m, T.Slot, 2);
            var target = AddMachine(B, "bakim", 4, 1);
            Republish(source);
            Assert.AreEqual(3, source.Current.Team.Length, "tutsak kartsız");
            // Harf araması tutsağı bulmaz
            source.Select(Baskin.UnitTypes["dron"].Short[0]);
            Assert.AreEqual('\0', source.Selected);
            Click(m.X, m.Y);
            Assert.AreEqual(m.Id, source.SelectedOther, "tahtaya tık seçer");
            var vm = source.Current;
            Assert.AreEqual(Loc.F("{0} · {1}", DeskText.Upper(Baskin.UnitTypes["dron"].Name), FacilityGearText.CaptiveText(B, m)), vm.UnitCard.Name);
            Assert.AreEqual(1, vm.UnitRows.Length);
            CollectionAssert.AreEqual(new[] { Loc.T("Komut ver") }, vm.ActionRows.Select(r => r.Text).ToArray());
            Assert.IsFalse(vm.ActionRows[0].Used, "hattaki makineyi vurur");
            source.ArmAction();
            Assert.AreEqual(Baskin.ActCommand, source.ActionKind);
            Click(target.X, target.Y);
            Assert.Less(B.Units.Single(u => u.Id == target.Id).Hp, target.Max, "tutsak ateş etti");
            Assert.AreEqual(Loc.F("Tutsak {0} · {1}", DeskText.Lower(Baskin.UnitTypes["dron"].Name, DeskLang.Tr), FacilityRaidText.Cell(B.Room.Index, target.X, target.Y)), source.Current.EventLine);
            // Kipte ekip üyesine tık onu seçer
            Click(T.X, T.Y);
            Assert.AreEqual(Letter(T), source.Selected);
            Assert.AreEqual(0, source.SelectedOther);
            // Hover: başlık ve kalan tur
            var card = Card(source, new Vector2Int(m.X, m.Y));
            Assert.AreEqual(Loc.F("Tutsak · {0}", Baskin.UnitTypes["dron"].Name), card.Title);
            Assert.IsTrue(card.Rows.Any(r => r.Text == Loc.F("Kalan: {0} tur", FacilityGearText.CaptiveLeft(B, m))));
            Assert.AreEqual("thumb_drone", card.Thumb);
        }

        [Test]
        public void ClassFilters_ConfirmFatalGateCount()
        {
            AddGadget(B, "taret", 5, 1, 1, 3, T.Slot);
            var dog = AddDog(B, T.Slot, 6, 1);
            Republish(source);
            // Uzmanlar hamlesini bitirdi: aygıt ve köpek onay istemez (eski: aygıt yüzünden her tur onay)
            foreach (var u in B.Units.Where(Baskin.IsSpecialist)) u.Moved = u.Acted = true;
            source.EndTurn();
            Assert.IsFalse(source.Current.EndConfirm, "uzman olmayanların hakkı onay istemez");
            Assert.AreEqual("enemy", B.Phase);
            B.Phase = "player";
            foreach (var u in B.Units.Where(Baskin.IsSpecialist)) u.Moved = u.Acted = false;
            source.EndTurn();
            Assert.IsTrue(source.Current.EndConfirm);
            Assert.IsTrue(source.Current.Unused.All(x => "TEA".Contains(x.Letter)), "kullanılmayan listesi uzmanlar");
            Assert.IsFalse(source.Current.Team[3].Warn);
            source.Cancel();
            // KAPIDA paydası uzmanlar (köpek ve aygıt kapıya girmez)
            foreach (var u in B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).ToList())
            {
                var g = FacilityDeskKit.FreeGate(B, u).Value;
                u.X = g.X; u.Y = g.Y;
                Assert.IsTrue(Baskin.EnterGate(B, u.Id), "EnterGate");
            }
            Republish(source);
            var notes = FacilityRaidText.BoardNotes(B);
            Assert.IsTrue(notes.Any(n => n.Item2 == Loc.F("KAPIDA {0}/{1}", 3, 3)), string.Join(", ", notes.Select(n => n.Item2)));
        }

        [Test]
        public void FatalWarning_SpecialistsOnly_DogCargoLine()
        {
            var dog = AddDog(B, T.Slot, 6, 1);
            AddGadget(B, "yem", 5, 1, 1, 3, T.Slot);
            B.Escape = true; B.Turn = B.MaxTurns;
            Republish(source);
            var w = source.Current.FatalWarning;
            Assert.IsFalse(string.IsNullOrEmpty(w));
            string dies = Loc.F("{0} geride kalırsa kalıcı olarak ölür.", string.Empty).Trim();
            Assert.AreEqual(3, w.Split('\n').Count(l => l.Contains(dies)), "üç uzman (köpek ve aygıt değil): " + w);
            Assert.AreNotEqual(Loc.T("GERİDE KALIRSA ÖLÜR"), source.Current.Team[3].Threat, "köpeğe ölüm uyarısı yok");
            B.Carrier = dog.Id;
            Republish(source);
            StringAssert.Contains(Loc.T("Köpek yükle kalırsa yük kaybedilir"), source.Current.FatalWarning);
        }

        [Test]
        public void EquipmentLine_Charges_StruckAtZero_RulesLine()
        {
            Gear(T, FacilityGear.Wire, 2);
            Republish(source);
            var card = source.Current.Team.First(t => t.Letter == 'T');
            StringAssert.Contains(Loc.F("{0} · {1} hak", PlanningBoardData.EquipmentName(FacilityGear.Wire), 2), card.Equipment);
            T.GearLeft = 0;
            Republish(source);
            StringAssert.Contains(Loc.F("<s>{0}</s>", Loc.F("{0} · {1} hak", PlanningBoardData.EquipmentName(FacilityGear.Wire), 0)), source.Current.Team.First(t => t.Letter == 'T').Equipment);
            Gear(E, FacilityGear.Dog, -1);
            Republish(source);
            StringAssert.Contains(PlanningBoardData.EquipmentName(FacilityGear.Dog), source.Current.Team.First(t => t.Letter == 'E').Equipment);
            StringAssert.DoesNotContain(Loc.F("{0} · {1} hak", PlanningBoardData.EquipmentName(FacilityGear.Dog), -1), source.Current.Team.First(t => t.Letter == 'E').Equipment);
            // Kurallar satırı: ekipmanlı kadroda R; köpekte 1–4
            source.Panel("rules");
            Assert.AreEqual(Loc.T("1 / 2 / 3: seç · Q: yetenek · E: hamle · R: ekipman · Z: geri al · Enter: emri ilet · Esc: vazgeç"), source.Current.Status);
            AddDog(B, E.Slot, 6, 1);
            Republish(source);
            Assert.AreEqual(Loc.T("1–4: seç · Q: yetenek · E: hamle · R: ekipman · Z: geri al · Enter: emri ilet · Esc: vazgeç"), source.Current.Status);
        }

        [Test]
        public void Hover_GadgetDogBarrier()
        {
            var g = AddGadget(B, "vinc", 5, 1, 2, 2, T.Slot);
            var card = Card(source, new Vector2Int(g.X, g.Y));
            Assert.AreEqual(Baskin.UnitTypes["vinc"].Name, card.Title);
            Assert.AreEqual("thumb_winch", card.Thumb);
            CollectionAssert.AreEqual(FacilityGearText.UnitRows(B, g).Select(r => r.Text).Take(5).ToArray(), card.Rows.Select(r => r.Text).ToArray());
            Assert.IsTrue(card.Rows.Any(r => r.Text == Loc.F("Yön: {0}", Loc.T("güney"))));
            var dog = AddDog(B, T.Slot, 6, 1);
            card = Card(source, new Vector2Int(dog.X, dog.Y));
            Assert.AreEqual(Loc.T("Köpek"), card.Title);
            Assert.AreEqual(Loc.F("Sahibi: {0}", FacilityMoveText.Who(T)), card.Rows[0].Text);
            Baskin.CellAt(B, 3, 1).O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 };
            card = Card(source, new Vector2Int(3, 1));
            Assert.AreEqual(Loc.T("Kalas duvar"), card.Title);
            Assert.AreEqual("thumb_plank", card.Thumb);
            Assert.AreEqual(Loc.T("Yürüyüşü ve atışı keser"), card.Rows[0].Text);
        }

        /// <summary>
        /// Niyetsiz makine gerçek Core yolundan (inceleme bulguları 1–2): Teknisyen kilitleyiciyi kelepçeler (<c>DoAction</c> kelepce), tur
        /// biter, tutsak hattındaki makineyi kilitler (komuta). Kilitli makine bu turun niyetini yine uygular: hover "Sonraki planında
        /// niyetsiz", tahta notu KİLİTLİ (niyet okuyla yan yana NİYETSİZ yazılmaz). Tur bitince plan onu atlar (Intent yok, Idle değil):
        /// hover "Bu planı niyetsiz geçiriyor", not NİYETSİZ. Elle kurulan Idle + Intent yok durumu Core'da oluşmaz.
        /// </summary>
        [Test]
        public void Idle_LockedThenSkipped_HoverAndNote_FromCore()
        {
            var lockNote = Loc.T("KİLİTLİ");
            var idleNote = Loc.T("NİYETSİZ");
            var c = AddMachine(B, "kilit", T.X + 1, T.Y);
            Gear(T, FacilityGear.Clamp, 1);
            Assert.IsTrue(Baskin.DoAction(B, T.Id, Baskin.ActClamp, c.X, c.Y), "kelepçe");
            Assert.IsTrue(Baskin.IsCaptive(c));
            // Kilitleyicinin hattında (dört kare) makine: tutsağın komutu onu hedef alır
            Baskin.Unit m = null;
            foreach (var d in Baskin.Dirs)
            {
                int x1 = c.X + d[0], y1 = c.Y + d[1], x2 = c.X + 2 * d[0], y2 = c.Y + 2 * d[1];
                if (FacilityGearTestKit.Open(B, x1, y1) && FacilityGearTestKit.Open(B, x2, y2)) { m = AddMachine(B, "dron", x2, y2); break; }
            }
            Assert.IsNotNull(m, "kilitleyicinin hattında boş kare yok");
            B.StillTurns = B.Turn + 5;   // kurulum: plan yürüyüşsüz (makine hatta kalsın)
            Assert.IsTrue(Baskin.EndTurn(B), "tur");
            Assert.IsNotNull(m.Intent, "plan makineye niyet verdi");
            CollectionAssert.Contains(Baskin.ActionTargets(B, c, Baskin.ActCommand).Select(p => (p.X, p.Y)).ToList(), (m.X, m.Y), "kilit hedefi");
            Assert.IsTrue(Baskin.DoAction(B, c.Id, Baskin.ActCommand, m.X, m.Y), "kilit komutu");
            Assert.IsTrue(m.Idle && m.Intent != null, "Core: kilitli makine niyetini korur, sonraki planı atlar");
            Assert.IsTrue(Baskin.Intents(B).Any(i => i.Unit == m.Id), "bu turun niyet oku yerinde");
            Republish(source);
            int key = m.Y * 8 + m.X;
            var rows = Card(source, new Vector2Int(m.X, m.Y)).Rows.Select(r => r.Text).ToList();
            CollectionAssert.Contains(rows, Loc.T("Sonraki planında niyetsiz"), string.Join(" | ", rows));
            CollectionAssert.DoesNotContain(rows, Loc.T("Bu planı niyetsiz geçiriyor"));
            var notes = source.MarksFor(source.Current).Notes;
            Assert.IsTrue(notes.Contains((key, lockNote)), "kilitli makinenin notu KİLİTLİ");
            Assert.IsFalse(notes.Contains((key, idleNote)), "niyet okuyla yan yana NİYETSİZ yok");

            Assert.IsTrue(Baskin.EndTurn(B), "tur");
            Assert.IsTrue(!m.Idle && m.Intent == null && !m.Dead, "Core: plan kilitli makineyi atladı");
            Republish(source);
            key = m.Y * 8 + m.X;
            rows = Card(source, new Vector2Int(m.X, m.Y)).Rows.Select(r => r.Text).ToList();
            CollectionAssert.Contains(rows, Loc.T("Bu planı niyetsiz geçiriyor"), string.Join(" | ", rows));
            CollectionAssert.DoesNotContain(rows, Loc.T("Sonraki planında niyetsiz"));
            notes = source.MarksFor(source.Current).Notes;
            Assert.IsTrue(notes.Contains((key, idleNote)), "planı atlanan makinenin notu NİYETSİZ");
            Assert.IsFalse(notes.Contains((key, lockNote)));
        }

        /// <summary>Tek kip (tasarım §2.3; inceleme bulgusu 8): kullanılabilir eylem yokken de R hamle kipini, E ekipman kipini kapatır;
        /// ekipmansız uzmanda R etkisiz.</summary>
        [Test]
        public void E_And_R_CloseTheOtherMode_WithoutUsableOptions()
        {
            Gear(T, FacilityGear.Turret, 0);
            Select(T);
            source.ArmAction();
            Assert.IsTrue(source.ActionArmed, "E hamle kurar");
            source.ArmGear();
            Assert.IsFalse(source.ActionArmed, "hakkı biten ekipmanda R hamle kipini kapatır");
            Assert.IsFalse(source.GearArmed);

            Gear(T, null, 0);
            Select(T);
            source.ArmAction();
            Assert.IsTrue(source.ActionArmed);
            source.ArmGear();
            Assert.IsTrue(source.ActionArmed, "ekipmansız uzmanda R etkisiz");
            source.Cancel();

            Gear(T, FacilityGear.Wire, 2);
            Select(T);
            source.ArmGear();
            Assert.IsTrue(source.GearArmed);
            T.Moved = T.Acted = true;   // kurulum: hiçbir hamle kullanılamaz
            Assert.IsFalse(FacilityMoveText.Options(B, T).Any(o => o.Usable), "kullanılabilir hamle yok");
            source.ArmAction();
            Assert.IsFalse(source.ActionArmed);
            Assert.IsFalse(source.GearArmed, "kullanılabilir hamle yokken E ekipman kipini kapatır");
        }

        /// <summary>
        /// Tesisin girişi, kaçış, ikmal (inceleme bulgusu 9): son uzmanın çıkış karesinde yükü köpek taşıyorsa zafer işareti yok, uyarı
        /// satırı "Köpek yükle kalırsa yük kaybedilir" (Core <c>FinishEscape</c>: köpek yükle tahtada kalırsa malzeme kaybedilir); yükü
        /// uzman taşıyorsa zafer işareti, uyarı yok.
        /// </summary>
        [Test]
        public void ExitHover_LastSpecialist_DogCarriesCargo_Warns()
        {
            B.Params.MissionKind = "loot";
            B.Escape = true;
            Assert.IsTrue(Baskin.IsFinalExit(B));
            foreach (var u in B.Units.Where(v => Baskin.IsSpecialist(v) && v != T)) u.Out = true;
            var exit = B.Exits[0];
            var near = FreeNeighbor(B, exit, true);
            Put(T, near.X, near.Y);
            var spot = FreeNeighbor(B, near, true, exit);
            var dog = AddDog(B, T.Slot, spot.X, spot.Y);
            B.Carrier = dog.Id;
            Select(T);
            var warn = Loc.T("Köpek yükle kalırsa yük kaybedilir");
            var card = Card(source, new Vector2Int(exit.X, exit.Y));
            Assert.IsFalse(card.Win, "yük köpekteyken son çıkış zafer değil");
            Assert.IsTrue(card.Rows.Any(r => r.Text == warn), string.Join(" | ", card.Rows.Select(r => r.Text)));
            B.Carrier = T.Id;
            card = Card(source, new Vector2Int(exit.X, exit.Y));
            Assert.IsTrue(card.Win, "yükü taşıyan son uzman çıkınca kazanılır");
            Assert.IsFalse(card.Rows.Any(r => r.Text == warn));
        }

        [Test]
        public void Preview_GearRowsFirst_InHover()
        {
            Gear(T, FacilityGear.Turret, 1);
            Select(T);
            source.ArmGear();
            var at = Baskin.ActionTargets(B, T, Baskin.ActTurret)[0];
            var card = Card(source, new Vector2Int(at.X, at.Y));
            Assert.AreEqual(Loc.T("Her makine evresinin sonunda hattaki ilk makine −1"), card.Rows[0].Text);
            var line = FacilityGearView.TurretLine(B, at.X, at.Y, FacilityGearView.DirTo(T.X, T.Y, at.X, at.Y)).Line;
            Assert.AreEqual(Loc.F("Hat: {0} kare", line.Count), card.Rows[1].Text);
        }

        [Test]
        public void Card_FoldsBeyondTenRows()
        {
            // Pafta 2 (menfez ve kepenk): Analist taşıyıcı, plakalı, örme setli, menfezin yanında, yanında iki gövde: 3 + 1 + 7 > 10
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            source.EndTurn();
            Assert.AreEqual(2, B.Room.Index, "GEÇ pafta 2'yi açmadı");
            FacilityDeskKit.Calm(B); Floor(B); Park(B);
            var vent = B.Room.Vents[0];
            var near = FreeNeighbor(B, vent, true, B.Room.Vents[1]);
            Put(A, near.X, near.Y);
            var s1 = FreeNeighbor(B, near, true, vent, B.Room.Vents[1]);
            Put(E, s1.X, s1.Y); MakeBody(E);
            var s2 = FreeNeighbor(B, near, true, vent, B.Room.Vents[1], s1);
            Put(T, s2.X, s2.Y); MakeBody(T);
            B.Carrier = A.Id; A.Armor = 1;
            Gear(A, FacilityGear.Wire, 2);
            Select(A);
            var vm = source.Current;
            int rows = vm.UnitRows.Length + vm.GearRows.Length + vm.ActionRows.Length;
            var options = FacilityMoveText.Options(B, A);
            Assert.Greater(vm.UnitRows.Length + vm.GearRows.Length + options.Count, RaidDataSource.MaxCardRows, "kurulum katlama ister");
            TestContext.WriteLine("seçenek " + options.Count + ", satır " + rows);
            Assert.LessOrEqual(rows, RaidDataSource.MaxCardRows);
            if (vm.UnitRows.Length + vm.GearRows.Length + options.Count > RaidDataSource.MaxCardRows)
            {
                // Katlanmış kartta satıra tık doğru seçeneği kurar; "Diğer" bir şey yapmaz
                for (int i = 0; i < vm.ActionRows.Length; i++)
                {
                    if (source.ActionArmed) source.Cancel();
                    source.ChooseAction(i);
                    if (vm.ActionRows[i].Used) { Assert.IsFalse(source.ActionArmed, "Diğer"); continue; }
                    Assert.AreEqual(vm.ActionRows[i].Text, FacilityMoveText.Name(B, A, source.ActionKind, source.ActionOther));
                }
            }
        }
    }
}
