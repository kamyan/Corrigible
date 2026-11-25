using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis hamleleri veri kaynağında, sahnesiz (aşama 7; belge §4, §11.5): E ile hamle kipi (döngü, kapatan komutlar), kart satırları
    /// ve E fişi, tahtaya tık yolu (UseAction), Core durumu, telsiz kaydı ve fiş, geri alma; iki kişilik iş, menfez, yerdeki yük,
    /// gövdeler ve uyarılar. Tahta ve HUD yok, oyun oynamıyor: hamleler eşzamanlı.
    /// Oda geçişi gerçek GEÇ yolundan (EndTurn).
    /// </summary>
    public class FacilityMovesPresentationTests
    {
        GameObject root;
        LiveRaidDataSource source;
        Simulation sim;

        [SetUp]
        public void Open()
        {
            root = new GameObject("Tesis hamleleri sunumu");
            root.SetActive(false);
            source = root.AddComponent<LiveRaidDataSource>();
            sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            source.OpenFacilityForValidation(sim);
            Settle();
        }

        [TearDown] public void Close() => Object.DestroyImmediate(root);

        Baskin.BState B => source.State;
        Baskin.Unit T => Team(B, "tek");
        Baskin.Unit E => Team(B, "emp");
        Baskin.Unit A => Team(B, "ana");

        /// <summary>Makineler kalkar, tahta düz zemin, ekip köşede (kurulum).</summary>
        void Settle() { FacilityDeskKit.Calm(B); Floor(B); Park(B); }

        /// <summary>Sıradaki pafta: ayaktakiler kapıya, sonra GEÇ (EMRİ İLET).</summary>
        void NextRoom()
        {
            int room = B.Room.Index;
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            source.EndTurn();
            Assert.AreEqual(room + 1, B.Room.Index, "GEÇ sıradaki paftayı açmadı");
            Settle();
        }

        void Select(Baskin.Unit u) { source.Select(Letter(u)); Assert.AreEqual(Letter(u), source.Selected, "seçilemedi: " + u.Type); }

        int Index(string kind, int other = 0)
        {
            var i = FacilityMoveText.Options(B, B.Units.First(u => Letter(u) == source.Selected)).FindIndex(o => o.Kind == kind && (other == 0 || o.Other == other));
            Assert.GreaterOrEqual(i, 0, "seçenek yok: " + kind);
            return i;
        }

        void Choose(string kind, int other = 0)
        {
            source.ChooseAction(Index(kind, other));
            Assert.AreEqual(kind, source.ActionKind);
        }

        string LastLog => source.Current.Log.Last().Text;

        /// <summary>Hareket emrinde fare yürüyüş karesindeyken görünüm modeli seçili birimin yolunu taşır (Core'un MovePath'i; duvarın
        /// çevresinden dolanır); ulaşılamaz karede, yetenek kurulunca ve fare çekilince boş (geliştirici, 2026-11-24).</summary>
        [Test]
        public void Hover_WalkTile_PublishesThePath_AroundWalls()
        {
            Put(T, 1, 3); Put(E, 6, 6); Put(A, 6, 7);
            B.Cells[3, 2].T = "wall"; B.Cells[2, 2].T = "wall";   // kurulum: T'nin doğusunda iki karelik duvar
            Select(T);
            source.Hover(new Vector2Int(3, 3));
            var expected = Baskin.MovePath(B, T, 3, 3);
            Assert.IsNotNull(expected, "duvarın arkası ulaşılabilir");
            CollectionAssert.AreEqual(expected.Select(p => new Vector2Int(p.X, p.Y)).ToArray(), source.Current.MovePath);
            Assert.Greater(source.Current.MovePath.Length, 3, "düz değil: duvarı dolanır");
            foreach (var p in source.Current.MovePath) Assert.AreNotEqual("wall", B.Cells[p.y, p.x].T);

            source.Hover(new Vector2Int(7, 0));
            if (!source.Current.Moves.Contains(new Vector2Int(7, 0))) CollectionAssert.IsEmpty(source.Current.MovePath, "ulaşılamaz kare");
            source.Hover(new Vector2Int(1, 4));
            Assert.AreEqual(2, source.Current.MovePath.Length, "komşu kare: iki noktalı yol");
            source.Arm();
            CollectionAssert.IsEmpty(source.Current.MovePath, "yetenek kuruluyken yol yok");
            source.Arm();
            source.ClearHover();
            CollectionAssert.IsEmpty(source.Current.MovePath, "fare çekilince yol yok");
        }

        [Test]
        public void E_CyclesUsable_ThenCloses_QEscSelectUndoClose()
        {
            Put(T, 3, 3); Put(E, 2, 3); MakeBody(E); Put(A, 3, 5);
            B.Carrier = T.Id;
            Select(T);
            // Kullanılabilir: pusu, sürükleme (EMP), fırlatma (Analist hatta); koşu (makine yok) atlanır
            Assert.IsFalse(source.Current.ActionArmed);
            Assert.AreEqual(0, source.Current.ActionKey, "kip kapalıyken E fişi ilk kullanılabilir satırda");
            source.ArmAction();
            Assert.AreEqual(Baskin.ActAmbush, source.ActionKind);
            Assert.IsTrue(source.Current.ActionArmed);
            Assert.IsTrue(source.Current.ActionRows[source.Current.ActionKey].Armed);
            Assert.AreEqual(0, source.Current.Moves.Length, "kipte yürüyüş boyası yok");
            CollectionAssert.AreEquivalent(Baskin.ActionTargets(B, T, Baskin.ActAmbush).Select(p => new Vector2Int(p.X, p.Y)).ToArray(), source.Current.AbilityTargets);
            source.ArmAction();
            Assert.AreEqual(Baskin.ActDrag, source.ActionKind);
            Assert.AreEqual(E.Id, source.ActionOther);
            Assert.AreEqual(Index(Baskin.ActDrag, E.Id), source.Current.ActionKey);
            source.ArmAction();
            Assert.AreEqual(Baskin.ActThrow, source.ActionKind);
            source.ArmAction();
            Assert.IsFalse(source.Current.ActionArmed, "sonuncudan sonra kapanır");
            Assert.IsNull(source.ActionKind);

            // Q hamle kipini kapatır ve yeteneği kurar; E yeteneği kapatır
            source.ArmAction();
            source.Arm();
            Assert.IsFalse(source.Current.ActionArmed);
            Assert.IsTrue(source.Current.AbilityArmed);
            source.ArmAction();
            Assert.IsTrue(source.Current.ActionArmed);
            Assert.IsFalse(source.Current.AbilityArmed);
            // Esc
            source.Cancel();
            Assert.IsFalse(source.Current.ActionArmed);
            Assert.AreEqual(Letter(T), source.Selected, "Esc önce yalnız kipi kapatır");
            // Başka seçim
            source.ArmAction();
            Select(A);
            Assert.IsFalse(source.Current.ActionArmed);
            // Z (geri alınacak hamle varken)
            source.Move(new Vector2Int(4, 5));
            Select(T);
            source.ArmAction();
            Assert.IsTrue(source.Current.ActionArmed);
            source.Undo();
            Assert.IsFalse(source.Current.ActionArmed);
            // Kart satırına tık: kurar; aynı satıra yeniden tık kapatır; kullanılamaz satır bir şey yapmaz
            int rush = Index(Baskin.ActRush);
            source.ChooseAction(rush);
            Assert.IsFalse(source.Current.ActionArmed, "koşu kullanılamaz");
            int thr = Index(Baskin.ActThrow);
            source.ChooseAction(thr);
            Assert.AreEqual(Baskin.ActThrow, source.ActionKind);
            Assert.AreEqual(thr, source.Current.ActionKey);
            source.ChooseAction(thr);
            Assert.IsFalse(source.Current.ActionArmed);
        }

        [Test]
        public void Ambush_Source_LineLogUndo()
        {
            Put(T, 3, 4);
            Select(T);
            source.ArmAction();
            Assert.AreEqual(Baskin.ActAmbush, source.ActionKind);
            CollectionAssert.AreEqual(new[] { Loc.T("Hatta duran ilk makine · −1"), Loc.T("makine evresinde") }, source.Current.Effects);
            var card = Card(source, new Vector2Int(3, 3));
            var line = Baskin.PreviewAction(B, T.Id, Baskin.ActAmbush, 3, 3).Line;
            Assert.AreEqual(Loc.F("Pusu hattı: {0} kare", line.Count), card.Rows[0].Text);
            source.UseAction(new Vector2Int(3, 3));
            Assert.AreEqual(0, T.AmbushDir, "kuzey");
            Assert.IsFalse(source.Current.ActionArmed);
            Assert.AreEqual(Loc.F("{0} pusu kurdu.", Baskin.UnitTypes["tek"].Name), LastLog);
            StringAssert.Contains(" · pusu · " + FacilityRaidText.Cell(1, 3, 3) + "–", source.Current.EventLine);
            // Hattın karesinin hover satırı
            Assert.IsTrue(Card(source, new Vector2Int(3, 1)).Rows.Any(r => r.Text == Loc.F("Pusu hattı · {0}: makine evresinde burada duran ilk makine −1", FacilityMoveText.Who(T))));
            Assert.IsTrue(source.Current.Team.First(t => t.Letter == 'T').Ability == false, "pusu yeteneğin yerine");
            // Tahtasız işaretler (MarksFor): kâğıt notları ve seçim bugünkü kuralla; pusu hattı işareti parça 2
            var marks = source.MarksFor(source.Current);
            Assert.AreEqual(T.Id, marks.Selected);
            CollectionAssert.AreEquivalent(FacilityRaidText.BoardNotes(B), marks.Notes);
            source.Undo();
            Assert.AreEqual(-1, T.AmbushDir);
        }

        [Test]
        public void Rush_Source_PushAndCollision()
        {
            Put(T, 3, 1);
            var m = Machine(B, 3, 5);
            Select(T);
            Choose(Baskin.ActRush);
            CollectionAssert.Contains(source.Current.AbilityTargets, new Vector2Int(3, 5));
            var card = Card(source, new Vector2Int(3, 5));
            Assert.AreEqual(Loc.F("Koşu: {0} kare", 3), card.Rows[0].Text);
            source.UseAction(new Vector2Int(3, 5));
            Assert.AreEqual((3, 4), (T.X, T.Y));
            Assert.Greater(m.Y, 5, "makine itildi");
            Assert.IsTrue(T.Moved && T.Acted);
            Assert.IsTrue(source.Current.Log.Any(l => l.Text == Loc.F("{0} koşu darbesi indirdi.", Baskin.UnitTypes["tek"].Name)));
            StringAssert.Contains(" · koşu darbesi → " + FacilityRaidText.Cell(1, 3, 5), source.Current.EventLine);
        }

        [Test]
        public void Drag_Source_BodyFollows_GateCarries()
        {
            Put(T, 3, 3); Put(E, 2, 3); MakeBody(E);
            Select(T);
            Assert.AreEqual(Loc.F("YERDE · {0}", FacilityRaidText.Cell(1, 2, 3)), source.Current.Team.First(t => t.Letter == 'E').DownText);
            Choose(Baskin.ActDrag, E.Id);
            CollectionAssert.AreEqual(new[] { Loc.T("Gövde yanında gelir"), Loc.T("bu tur yetenek yok") }, source.Current.Effects);
            source.UseAction(new Vector2Int(5, 3));
            Assert.AreEqual((5, 3), (T.X, T.Y));
            Assert.AreEqual((4, 3), (E.X, E.Y), "gövde yolun sondan bir önceki karesinde");
            StringAssert.Contains(" sürükleniyor → ", source.Current.EventLine);
            source.Undo();
            Assert.AreEqual((2, 3), (E.X, E.Y));

            // Kapıya sürükleme: ikisi de kapıda; ekip kartı GÖVDE KAPIDA; GEÇ'ten sonra gövde sıradaki paftada yatar
            var g = Baskin.GateTiles(B)[0];
            int dir = g.X >= 4 ? -1 : 1;
            Put(T, g.X + dir, g.Y); Put(E, g.X + 2 * dir, g.Y);
            FacilityMovesTestKit.Republish(source);
            Select(T);
            Choose(Baskin.ActDrag, E.Id);
            source.UseAction(new Vector2Int(g.X, g.Y));
            Assert.IsTrue(T.AtGate && E.AtGate, "ikisi kapıda");
            Assert.AreEqual(Loc.T("GÖVDE KAPIDA"), source.Current.Team.First(t => t.Letter == 'E').DownText);
            Assert.AreEqual('\0', source.Selected, "kapıya giren seçimden çıkar");
            Assert.IsTrue(Card(source, new Vector2Int(g.X, g.Y)).Rows.All(r => !r.Text.StartsWith("Yerde kalan:")), "kapıdaki gövde yerde değil");
            NextRoom();
            Assert.AreEqual(2, B.Room.Index);
            var body = B.Units.Single(u => u.Side == 'p' && u.Type == "emp");
            Assert.IsTrue(Baskin.LyingBody(body), "gövde sıradaki paftada yatar");
            FacilityMovesTestKit.Republish(source);
            StringAssert.StartsWith(Loc.F("YERDE · {0}", FacilityRaidText.Cell(2, body.X, body.Y)), source.Current.Team.First(t => t.Letter == 'E').DownText);
        }

        [Test]
        public void Throw_Source_CarrierAndExitRow()
        {
            Put(T, 3, 3); Put(A, 3, 5);
            B.Carrier = T.Id;
            Select(T);
            Choose(Baskin.ActThrow);
            Assert.AreEqual(Loc.F("Yükü {0} taşır", FacilityMoveText.Who(A)), Card(source, new Vector2Int(3, 5)).Rows[0].Text);
            source.UseAction(new Vector2Int(3, 5));
            Assert.AreEqual(A.Id, B.Carrier);
            Assert.AreEqual(Loc.F("{0} yükü fırlattı; yükü artık {1} taşıyor.", Baskin.UnitTypes["tek"].Name, Baskin.UnitTypes["ana"].Name), LastLog);
            // Tesisin girişinde (kaçış) çıkış karesinin satırı malzemeyi taşıyanı adlandırır
            B.Escape = true;
            Assert.IsTrue(Baskin.IsFinalExit(B));
            var exit = B.Exits[0];
            var row = Card(source, new Vector2Int(exit.X, exit.Y)).Rows.First(r => r.Good && r.Icon == "ic_eye_s");
            Assert.AreEqual(Loc.F("Buradan çıkan uzman tahliye edilir; malzeme {0} ile çıkar", FacilityMoveText.Who(A)), row.Text);
        }

        [Test]
        public void Joint_Shutter_HoldThenOpen()
        {
            NextRoom();
            var s = B.Room.Shutter.Value;
            var n1 = FreeNeighbor(B, s, false, B.Room.Vents.ToArray()); Put(T, n1.X, n1.Y);
            var n2 = FreeNeighbor(B, s, false, B.Room.Vents.Concat(new[] { n1 }).ToArray()); Put(E, n2.X, n2.Y);
            var wall = Card(source, new Vector2Int(s.X, s.Y));
            Assert.AreEqual(Loc.T("Ağır kepenk"), wall.Title);
            Assert.IsTrue(wall.Rows.Any(r => r.Text == Loc.T("İki uzman aynı tur bitişikten kaldırır (E)")));
            Assert.IsFalse(wall.Rows.Any(r => r.Text == Loc.T("Madenci dinamitle yıkabilir") || r.Text == Loc.T("Geçilmez; atışı durdurur")), "dinamit kepengi hedef almaz");
            Select(T);
            Choose(Baskin.ActJoint);
            Assert.AreEqual(Loc.T("Kepengi kaldır"), source.Current.ActionRows[Index(Baskin.ActJoint)].Text);
            source.UseAction(new Vector2Int(s.X, s.Y));
            Assert.AreEqual(T.Id, B.HoldId);
            Assert.IsTrue(FacilityRaidText.BoardNotes(B).Contains((s.Y * 8 + s.X, Loc.T("TUTULUYOR"))));
            Assert.AreEqual(Loc.F("{0} · kepenk tutuluyor", source.Current.Team.First(t => t.Letter == 'T').Name), source.Current.EventLine);
            Assert.IsTrue(Card(source, new Vector2Int(s.X, s.Y)).Rows.Any(r => r.Text == Loc.F("Tutuluyor: {0} · ikinci uzman bu tur", FacilityMoveText.Who(T))));
            Select(E);
            Assert.AreEqual(Loc.F("yetenek yerine · {0} tutuyor", FacilityMoveText.Who(T)), source.Current.ActionRows[Index(Baskin.ActJoint)].Detail);
            Choose(Baskin.ActJoint);
            Assert.AreEqual(Loc.T("Kepenk kalkar; makineler de geçer"), Card(source, new Vector2Int(s.X, s.Y)).Rows[0].Text);
            source.UseAction(new Vector2Int(s.X, s.Y));
            Assert.AreEqual("floor", Baskin.CellAt(B, s.X, s.Y).T);
            Assert.AreEqual(Loc.T("Kepenk kalktı."), LastLog);
            var up = Card(source, new Vector2Int(s.X, s.Y));
            Assert.AreEqual(Loc.T("Kalkık kepenk"), up.Title);
            Assert.IsTrue(up.Rows.Any(r => r.Text == Loc.T("Açık kalır; makineler de geçer")));
        }

        [Test]
        public void Joint_BigCrate()
        {
            NextRoom();
            NextRoom();
            Assert.IsTrue(B.Room.BigCrate.HasValue, "pafta 3'te büyük sandık yok");
            var c = B.Room.BigCrate.Value;
            var closed = Card(source, new Vector2Int(c.X, c.Y));
            Assert.AreEqual(Loc.T("Büyük sandık"), closed.Title);
            Assert.IsTrue(closed.Rows.Any(r => r.Text == Loc.T("İki uzman aynı tur bitişikten açar (E)")));
            Assert.IsTrue(closed.Rows.Any(r => r.Text == Loc.F("Kazanılırsa +{0} metal +{1} devre", sim.B.facilityBigCrateMetal, sim.B.facilityBigCrateCircuits)));
            var n1 = FreeNeighbor(B, c, false); Put(T, n1.X, n1.Y);
            var n2 = FreeNeighbor(B, c, false, n1); Put(E, n2.X, n2.Y);
            Select(T);
            Choose(Baskin.ActJoint);
            Assert.AreEqual(Loc.T("Büyük sandığı aç"), source.Current.ActionRows[Index(Baskin.ActJoint)].Text);
            CollectionAssert.AreEqual(new[] { Loc.T("Sandık açılır"), Loc.T("iki uzman aynı tur") }, source.Current.Effects);
            source.UseAction(new Vector2Int(c.X, c.Y));
            Select(E);
            Choose(Baskin.ActJoint);
            Assert.AreEqual(Loc.F("Sandık açılır: kazanılırsa +{0} metal +{1} devre", sim.B.facilityBigCrateMetal, sim.B.facilityBigCrateCircuits), Card(source, new Vector2Int(c.X, c.Y)).Rows[0].Text);
            source.UseAction(new Vector2Int(c.X, c.Y));
            Assert.IsTrue(B.CrateOpen);
            Assert.AreEqual(Loc.T("Büyük sandık açıldı."), LastLog);
            Assert.AreEqual(Loc.T("Açılmış büyük sandık"), Card(source, new Vector2Int(c.X, c.Y)).Title);
            Assert.IsFalse(FacilityRaidText.BoardNotes(B).Any(n => n.text == Loc.T("SANDIK")));
        }

        [Test]
        public void Vent_Analyst_AbilityStays()
        {
            NextRoom();
            var v0 = B.Room.Vents[0]; var v1 = B.Room.Vents[1];
            var near = FreeNeighbor(B, v0, true, v1);
            Put(A, near.X, near.Y);
            Select(A);
            Choose(Baskin.ActVent);
            CollectionAssert.Contains(source.Current.AbilityTargets, new Vector2Int(v1.X, v1.Y));
            source.UseAction(new Vector2Int(v1.X, v1.Y));
            Assert.AreEqual((v1.X, v1.Y), (A.X, A.Y));
            Assert.IsTrue(A.Moved);
            Assert.IsFalse(A.Acted);
            Assert.AreEqual(Loc.F("{0} menfezden geçti.", Baskin.UnitTypes["ana"].Name), LastLog);
            Assert.IsTrue(source.Current.UnitCard.Ability, "yetenek kullanılabilir kalır");
            Assert.IsTrue(source.Current.ActionRows[Index(Baskin.ActAmbush)].Used == false, "pusu hâlâ kullanılabilir");
            Assert.IsTrue(source.Current.ActionRows[Index(Baskin.ActVent)].Used);
        }

        [Test]
        public void CargoFloor_StampGoalWalkPickup()
        {
            Put(T, 3, 3);
            B.CargoX = 2; B.CargoY = 3;
            FacilityMovesTestKit.Republish(source);
            Assert.IsTrue(source.Current.CargoOnFloor);
            Assert.AreEqual(Loc.F("Amaç: yükü yerden al ({0})", FacilityRaidText.Cell(1, 2, 3)), source.Current.Goal);
            Assert.IsTrue(Card(source, new Vector2Int(2, 3)).Rows.Any(r => r.Text == Loc.T("Yük yerde: kareye giren ilk ekip üyesi alır")));
            // Eski bir hamlenin efektleri listede kalsa da yük alımı Carrier farkından okunur
            B.FxList.Add(new Baskin.Fx { K = "cargo-pick", X = 0, Y = 0, Unit = A.Id });
            Select(T);
            source.Move(new Vector2Int(2, 3));
            Assert.AreEqual(T.Id, B.Carrier);
            Assert.IsFalse(source.Current.CargoOnFloor);
            Assert.AreEqual(FacilityMoveText.PickupSlip(B, source.Current.Team.First(t => t.Letter == 'T').Name, 2, 3), source.Current.EventLine);
        }

        [Test]
        public void Hover_BodyCrateShutterCargo()
        {
            Put(E, 2, 3); MakeBody(E);
            var lying = Card(source, new Vector2Int(2, 3));
            Assert.AreEqual(source.Current.Team.First(t => t.Letter == 'E').Name, lying.Title);
            Assert.AreEqual(Loc.T("Yerde: taşınmazsa ölür"), lying.Rows[0].Text);
            Assert.AreEqual(Loc.T("Yanındaki ya da üstündeki uzman E ile sürükler"), lying.Rows[1].Text);
            Put(T, 2, 3);
            Assert.IsTrue(Card(source, new Vector2Int(2, 3)).Rows.Any(r => r.Text == Loc.F("Altında: {0} (yerde)", FacilityMoveText.Who(E))));
            // Kapıda yerde kalan uyarısı (GEÇ onaysız)
            var g = Baskin.GateTiles(B)[0];
            Assert.IsTrue(Card(source, new Vector2Int(g.X, g.Y)).Rows.Any(r => r.Text == Loc.F("Yerde kalan: {0}", FacilityMoveText.Who(E))));
        }

        [Test]
        public void Card_ReasonsAndKey()
        {
            Put(T, 3, 3);
            Select(T);
            var rows = source.Current.ActionRows;
            CollectionAssert.AreEqual(new[] { Loc.T("Pusu"), Loc.T("Koşu darbesi") }, rows.Select(r => r.Text).ToArray());
            Assert.IsFalse(rows[0].Used);
            Assert.AreEqual(Loc.F("yetenek yerine · hat {0} kare", B.Moves.AmbushRange), rows[0].Detail);
            Assert.IsTrue(rows[1].Used);
            Assert.AreEqual(Loc.T("hatta makine yok"), rows[1].Detail);
            Assert.AreEqual("ic_bolt_s", rows[0].Icon);
            Assert.AreEqual(0, source.Current.ActionKey);
            Assert.AreEqual(Loc.T("Hareket"), source.Current.UnitRows[0].Text, "Hareket / yetenek / plaka satırları değişmez");
            Assert.AreEqual(source.Current.IsLootMission ? Loc.T("İkmal alma") : Loc.T("Sabotaj"), source.Current.UnitRows[1].Text);
            source.ArmAction();
            Assert.IsTrue(source.Current.ActionRows[0].Armed);
            Assert.IsTrue(source.Current.Team.First(t => t.Letter == 'T').Armed, "ekip kartı kurulu");
        }

        [Test]
        public void Warnings_TimeoutAndFatal_Bodies()
        {
            Put(E, 2, 3); MakeBody(E);
            B.Turn = B.MaxTurns;
            FacilityMovesTestKit.Republish(source);
            Assert.AreEqual(Loc.F("{0} {1}", Loc.T("Süre dolunca ekip geri çekilir ve baskın başarısız biter. Kapıdakiler de döner."), Loc.T("Yerde yatanlar ölür.")), source.Current.FatalWarning);
            // Tesisin girişinde son tur (kaçış): yatan gövdeler de ölüm uyarısında
            B.Escape = true;
            FacilityMovesTestKit.Republish(source);
            StringAssert.Contains(Loc.F("{0} yerde: taşınmazsa ölür.", string.IsNullOrEmpty(E.PersonName) ? source.Current.Team.First(t => t.Letter == 'E').Name : E.PersonName), source.Current.FatalWarning);
            // Geri çekilme: tesiste ayrı gövde metni (görünümde)
            Assert.AreEqual(Loc.T("Ekip binadan çıkar; operasyon başarısız sayılır. Yerde yatanlar ölür."), RaidText.Get("raid.retreat.bodyFacility"));
        }

        /// <summary>Yük yerdeyken (taşıyıcı düştü) kapı ve çıkış karesinin kartı yükün yerde kalacağını söyler (aşama 7 kapanışı); kapı
        /// kartı en çok 4 satır (yer açılır), eski tahtada satır yok.</summary>
        [Test]
        public void CargoFloor_ExitAndGateRows_CargoStays()
        {
            Put(T, 3, 3);
            var g = Baskin.GateTiles(B)[0];
            string stays = Loc.F("Yük yerde kalır · {0}", FacilityRaidText.Cell(1, 2, 3));
            Assert.IsFalse(Card(source, new Vector2Int(g.X, g.Y)).Rows.Any(r => r.Text == stays), "yük yerde değilken satır yok");
            B.CargoX = 2; B.CargoY = 3;
            Assert.AreEqual(stays, Card(source, new Vector2Int(g.X, g.Y)).Rows.Last().Text, "kapı kartının son satırı");
            // En kötü durum: seçili uzman kapı karesinde (4 temel satır), yerde gövde ve yük: iki satır düşer, ikisi de kalır
            Put(E, 5, 5); MakeBody(E);
            Put(T, g.X, g.Y);
            var rows = FacilityRaidText.GateRows(B, T, g.X, g.Y).Select(r => r.Text).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                Loc.F("Kapıda: {0}/{1} · herkes gelince GEÇ (Enter)", 0, 2),
                Loc.T("Uzmana yeniden tıkla: adım harcamadan kapıya girer."),
                Loc.F("Yerde kalan: {0}", FacilityMoveText.Who(E)),
                stays,
            }, rows);
            // Tesisin girişi (kaçış): çıkış karesinde tahliye satırının hemen ardından
            Put(T, 3, 3);
            B.Escape = true;
            Assert.IsTrue(Baskin.IsFinalExit(B));
            var exit = B.Exits[0];
            var exitRows = Card(source, new Vector2Int(exit.X, exit.Y)).Rows.Select(r => r.Text).ToList();
            int evac = exitRows.IndexOf(Loc.T("Buradan çıkan uzman tahliye edilir"));
            Assert.GreaterOrEqual(evac, 0, string.Join(" | ", exitRows));
            Assert.AreEqual(stays, exitRows[evac + 1]);
            // Yük yerden alındı: satır kalkar; eski tahtada hiç yok
            B.CargoX = B.CargoY = -1; B.Carrier = T.Id;
            Assert.IsFalse(Card(source, new Vector2Int(exit.X, exit.Y)).Rows.Any(r => r.Text == stays));
            Assert.IsNull(FacilityRaidText.CargoLeftRow(Baskin.Create(new RaidParams { Seed = 11 })));
        }

        /// <summary>Kalkmış makineyi kimliğiyle ayağa kaldırır ve (x, y)'ye koyar (niyetsiz; planı makine evresinde kurulur).</summary>
        static Baskin.Unit Revive(Baskin.BState b, int id, int x, int y)
        {
            var e = b.Units.First(v => v.Id == id);
            e.Dead = false; e.Out = false; e.Cab = false; e.Frozen = false; e.Left = false; e.Hp = e.Max; e.Intent = null; e.X = x; e.Y = y;
            return e;
        }

        /// <summary>
        /// Son nefes (aşama 6 Core'u, Unit.Breath): Teknisyen ve Analist kapıda, tahtadaki son uzman makine evresinde düşer ve oda
        /// kendiliğinden geçilir. Gövde "YERDE" değil ekiple gösterilir: ekip kartı EKİPLE TAŞINIYOR, hover satırı ekiple gider, kapı kartı
        /// ve GEÇ fişi onu yerde kalan saymaz; tesisin girişinde (Core'un Body 2 kuralı) ÇIKARILDI. Makine evresi Core'dan verilir (EditMode
        /// veri kaynağı evreyi oynatmaz); kurulum Core'un kopyasında seçilir.
        /// </summary>
        [Test]
        public void LastBreath_AutoPass_TextsGoWithTeam()
        {
            Assert.IsTrue(B.Moves.LastBreath, "son nefes kuralı açık (varsayılan denge)");
            foreach (var u in new[] { T, A })
            {
                var g = FacilityDeskKit.FreeGate(B, u).Value;
                Put(u, g.X, g.Y);
                Assert.IsTrue(Baskin.EnterGate(B, u.Id), "kapıya giriş: " + u.Type);
            }
            var e = E;
            Put(e, 3, 3);
            e.Hp = 1; e.Armor = 0; e.Ice = 0;   // kurulum: ilk darbe düşürür
            int machine = 0, turns = 0; Baskin.Pt start = default;
            foreach (var id in B.Units.Where(u => u.Side == 'e').Select(u => u.Id).Take(6).ToList())
            {
                foreach (var d in Baskin.Dirs)
                {
                    int x = e.X + d[0], y = e.Y + d[1];
                    if (Baskin.CellAt(B, x, y).T != "floor" || Baskin.UnitAt(B, x, y) != null || Baskin.IsGateTile(B, x, y)) continue;
                    var copy = B.Clone();
                    Revive(copy, id, x, y);
                    int n = 0;
                    for (int k = 1; k <= 3 && n == 0; k++)
                    {
                        if (!Baskin.EndTurn(copy)) break;
                        if (copy.Phase != "player") n = k;
                    }
                    // Gövdenin karesinde makine durmasın (hover kartı gövdenin kartı olsun)
                    if (n == 0 || copy.Phase != "pass" || !copy.Units.First(u => u.Id == e.Id).Breath || Baskin.UnitAt(copy, e.X, e.Y) != null) continue;
                    machine = id; start = new Baskin.Pt(x, y); turns = n;
                    break;
                }
                if (machine != 0) break;
            }
            Assert.AreNotEqual(0, machine, "son uzmanı düşürüp odayı kendiliğinden geçiren makine kurulumu bulunamadı");
            Revive(B, machine, start.X, start.Y);
            for (int k = 0; k < turns; k++) Assert.IsTrue(Baskin.EndTurn(B), "makine evresi " + (k + 1));
            Assert.AreEqual("pass", B.Phase, "kendiliğinden geçiş");
            Assert.IsTrue(e.Breath && Baskin.LyingBody(e), "son nefes");
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.F("{0} son nefeste düştü; ekip onu da götürdü.", Baskin.UnitTypes["emp"].Name)), "Core'un telsiz satırı");

            FacilityMovesTestKit.Republish(source);
            var card = source.Current.Team.First(t => t.Letter == 'E');
            Assert.AreEqual(Loc.T("EKİPLE TAŞINIYOR"), card.DownText, "ekip kartı yerde demez");
            var hover = Card(source, new Vector2Int(e.X, e.Y));
            Assert.AreEqual(card.Name, hover.Title, "gövdenin karesi kişinin kartı");
            Assert.AreEqual(Loc.T("Son nefeste düştü: ekip onu da götürür"), hover.Rows[0].Text);
            Assert.IsFalse(hover.Rows.Any(r => r.Text == Loc.T("Yerde: taşınmazsa ölür")));
            Assert.AreSame(e, FacilityMoveText.BodyAt(B, e.X, e.Y));
            var gate = Baskin.GateTiles(B)[0];
            Assert.IsFalse(FacilityRaidText.GateRows(B, null, gate.X, gate.Y).Any(r => r.Text == Loc.F("Yerde kalan: {0}", FacilityMoveText.Who(e))), "kapı kartı yerde kalan saymaz");
            Assert.AreEqual(Loc.T("ekip kapıda · GEÇ'e bas"), FacilityMoveText.PassSlip(B));
            Assert.IsFalse(FacilityMoveText.Lying(e));
            Assert.IsTrue(FacilityMoveText.GoesWithTeam(e));
            // Tesisin girişinde son nefes tesisten çıkarılır (Core: Body 2)
            B.Escape = true;
            Assert.IsTrue(Baskin.IsFinalExit(B));
            FacilityMovesTestKit.Republish(source);
            Assert.AreEqual(Loc.T("ÇIKARILDI"), source.Current.Team.First(t => t.Letter == 'E').DownText);
            // Son nefes değilse bugünkü YERDE
            B.Escape = false; e.Breath = false;
            FacilityMovesTestKit.Republish(source);
            Assert.AreEqual(Loc.F("YERDE · {0}", FacilityRaidText.Cell(1, e.X, e.Y)), source.Current.Team.First(t => t.Letter == 'E').DownText);
        }
    }
}
