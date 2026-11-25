using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis hamlelerinin saf metinleri ve seçenekleri (aşama 7; Docs/TESIS-BASKINLARI-TASARIMI.md §4, §11.5): kartın hamle satırları
    /// (anlamlılık, sıra, kullanılabilirlik Core'un kararı, nedenler), etkiler, önizleme verisi, telsiz fişleri, tahtanın kâğıt
    /// notları, gözcü ve kapanış satırları, amaç. TR ve EN. Sahnesiz.
    /// </summary>
    public class FacilityMoveTextTests
    {
        static readonly Regex TurkishLetters = new Regex("[ğüşıöçĞÜŞİÖÇ]");
        DeskLang lang;

        [SetUp] public void Remember() => lang = DeskLoc.Instance.Lang;
        [TearDown] public void Restore() => DeskLoc.Instance.SetLanguage(lang);

        static string Cell(Baskin.BState B, int x, int y) => FacilityRaidText.Cell(B.Room.Index, x, y);

        /// <summary>Altı hamlenin hepsinin anlamlı olduğu kurulum (pafta 2): Analist taşıyıcı, yanında yatan EMP uzmanı, kepenk ve menfez.</summary>
        static Baskin.BState AllSix(out Baskin.Unit analyst)
        {
            var (_, _, B) = Room(2);
            Floor(B); Park(B);
            analyst = Team(B, "ana");
            var emp = Team(B, "emp");
            var vent = B.Room.Vents[0];
            var near = FreeNeighbor(B, vent, true, B.Room.Vents[1]);
            Put(analyst, near.X, near.Y);
            var side = FreeNeighbor(B, near, true, vent, B.Room.Vents[1]);
            Put(emp, side.X, side.Y);
            MakeBody(emp);
            B.Carrier = analyst.Id;
            return B;
        }

        [Test]
        public void Names_AllKinds_TrAndEn()
        {
            var B = AllSix(out var a);
            Assert.IsTrue(B.Room.Shutter.HasValue, "pafta 2'de kepenk yok");
            CollectionAssert.AreEqual(Baskin.ActionKinds, FacilityMoveText.Options(B, a).Select(o => o.Kind).Distinct().ToArray(), "altı hamle anlamlı");
            var missing = new HashSet<string>();
            void OnMissing(string s) => missing.Add(s);
            Loc.Missing += OnMissing;
            try
            {
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    foreach (var kind in Baskin.ActionKinds)
                    {
                        var texts = new List<string> { FacilityMoveText.Name(B, a, kind), FacilityMoveText.Detail(B, a, kind), FacilityMoveText.Reason(B, a, kind) };
                        texts.AddRange(FacilityMoveText.Effects(B, a, kind).Texts);
                        foreach (var t in texts)
                        {
                            Assert.IsFalse(string.IsNullOrEmpty(t), l + " " + kind);
                            if (l == DeskLang.En) Assert.IsFalse(TurkishLetters.IsMatch(t), kind + ": " + t);
                        }
                    }
                    // Seçenekler (kişi adı Türkçe olabilir: yalnız boş değil)
                    foreach (var o in FacilityMoveText.Options(B, a)) Assert.IsFalse(string.IsNullOrEmpty(o.Shown), l + " " + o.Kind);
                }
            }
            finally { Loc.Missing -= OnMissing; }
            CollectionAssert.IsEmpty(missing, "çevirisi olmayan: " + string.Join(" | ", missing));
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Assert.AreEqual(Loc.T("Pusu"), FacilityMoveText.Name(B, a, Baskin.ActAmbush));
            Assert.AreEqual(Loc.T("Kepengi kaldır"), FacilityMoveText.Name(B, a, Baskin.ActJoint));
            Assert.AreEqual(Loc.F("yetenek yerine · hat {0} kare", B.Moves.AmbushRange), FacilityMoveText.Detail(B, a, Baskin.ActAmbush));
        }

        [Test]
        public void Options_OrderRelevanceAndUsable()
        {
            var (_, _, B) = Room(1);
            Floor(B); Park(B);
            var t = Team(B, "tek"); var e = Team(B, "emp"); var a = Team(B, "ana");
            Put(t, 3, 3);
            // Pafta 1: menfez ve kepenk yok; yük kimsede değil, yatan yok: yalnız pusu ve koşu
            CollectionAssert.AreEqual(new[] { Baskin.ActAmbush, Baskin.ActRush }, FacilityMoveText.Options(B, t).Select(o => o.Kind).ToArray());
            CollectionAssert.AreEqual(new[] { Baskin.ActAmbush, Baskin.ActRush }, FacilityMoveText.Options(B, a).Select(o => o.Kind).ToArray(), "menfezsiz odada menfez yok");
            // Taşıyıcıda fırlatma; yatan varken sürükleme (sıra ActionKinds)
            B.Carrier = t.Id;
            Put(e, 6, 6); MakeBody(e);
            var kinds = FacilityMoveText.Options(B, t).Select(o => o.Kind).ToArray();
            CollectionAssert.AreEqual(new[] { Baskin.ActAmbush, Baskin.ActRush, Baskin.ActDrag, Baskin.ActThrow }, kinds);
            Assert.IsFalse(FacilityMoveText.Options(B, a).Any(o => o.Kind == Baskin.ActThrow), "fırlatma yalnız taşıyıcıda");
            // Pafta 2: kepenk (iki kişilik iş) ve Analist'te menfez
            var (_, _, B2) = Room(2);
            Floor(B2);
            var a2 = Team(B2, "ana"); var t2 = Team(B2, "tek");
            CollectionAssert.Contains(FacilityMoveText.Options(B2, a2).Select(o => o.Kind).ToArray(), Baskin.ActVent);
            CollectionAssert.DoesNotContain(FacilityMoveText.Options(B2, t2).Select(o => o.Kind).ToArray(), Baskin.ActVent);
            CollectionAssert.Contains(FacilityMoveText.Options(B2, t2).Select(o => o.Kind).ToArray(), Baskin.ActJoint);
            // Kullanılabilirlik (kararı Core verir): beklenenler kurulumdan elle. Pafta 1, makine yok: pusu açık, koşu hedefsiz; yanında
            // yatan gövde sürüklenir; Analist düz hatta 2 kare ötede: fırlatma açık. Analist'in yanında gövde yok: tek, kullanılamaz satır
            Put(a, 3, 5); Put(e, 4, 3);
            string Row(MoveOption o) => o.Kind + ":" + o.Other + ":" + o.Usable;
            CollectionAssert.AreEqual(new[] { Baskin.ActAmbush + ":0:True", Baskin.ActRush + ":0:False", Baskin.ActDrag + ":" + e.Id + ":True", Baskin.ActThrow + ":0:True" },
                FacilityMoveText.Options(B, t).Select(Row).ToArray(), "Teknisyen");
            CollectionAssert.AreEqual(new[] { Baskin.ActAmbush + ":0:True", Baskin.ActRush + ":0:False", Baskin.ActDrag + ":0:False" },
                FacilityMoveText.Options(B, a).Select(Row).ToArray(), "Analist");
            // Yetenek kullanıldı: yeteneğin yerine olan pusu ve fırlatma kapanır
            t.Acted = true;
            var spent = FacilityMoveText.Options(B, t);
            Assert.IsFalse(spent.First(o => o.Kind == Baskin.ActAmbush).Usable);
            Assert.IsFalse(spent.First(o => o.Kind == Baskin.ActThrow).Usable);
            Assert.AreEqual(Loc.T("yetenek kullanıldı"), spent.First(o => o.Kind == Baskin.ActAmbush).Shown);
            t.Acted = false;
        }

        [Test]
        public void Options_DragPerBody()
        {
            var (_, _, B) = Room(1);
            Floor(B); Park(B);
            var t = Team(B, "tek"); var e = Team(B, "emp"); var a = Team(B, "ana");
            Put(t, 3, 3); Put(e, 2, 3); Put(a, 4, 3);
            MakeBody(e); MakeBody(a);
            var drags = FacilityMoveText.Options(B, t).Where(o => o.Kind == Baskin.ActDrag).ToList();
            CollectionAssert.AreEqual(Baskin.DragBodies(B, t), drags.Select(o => o.Other).ToList());
            Assert.AreEqual(2, drags.Count);
            Assert.AreEqual(Loc.F("Sürükle: {0}", FacilityMoveText.Who(e)), drags[0].Name);
            Assert.AreEqual(Loc.F("Sürükle: {0}", FacilityMoveText.Who(a)), drags[1].Name);
            Assert.IsTrue(drags.All(o => o.Usable));
        }

        [Test]
        public void Reasons_FollowCoreState()
        {
            var (_, _, B) = Room(1);
            Floor(B); Park(B);
            var t = Team(B, "tek"); var e = Team(B, "emp"); var a = Team(B, "ana");
            Put(t, 3, 3);
            MoveOption Opt(Baskin.Unit u, string kind) => FacilityMoveText.Options(B, u).First(o => o.Kind == kind);
            // Makine yok: koşunun hedefi yok
            Assert.IsFalse(Opt(t, Baskin.ActRush).Usable);
            Assert.AreEqual(Loc.T("hatta makine yok"), Opt(t, Baskin.ActRush).Shown);
            Assert.IsTrue(Opt(t, Baskin.ActAmbush).Usable);
            Assert.AreEqual(Loc.F("yetenek yerine · hat {0} kare", B.Moves.AmbushRange), Opt(t, Baskin.ActAmbush).Shown);
            // Yürüdü: koşu yürüyüş ister; pusu (yeteneğin yerine) kalır
            t.Moved = true;
            Assert.AreEqual(Loc.T("yürüdü · yürüyüş ister"), Opt(t, Baskin.ActRush).Shown);
            Assert.IsTrue(Opt(t, Baskin.ActAmbush).Usable);
            t.Locked = true;
            Assert.AreEqual(Loc.T("kilitli"), Opt(t, Baskin.ActRush).Shown);
            t.Locked = false; t.Frozen = true;
            Assert.AreEqual(Loc.T("donmuş"), Opt(t, Baskin.ActRush).Shown);
            t.Frozen = false; t.Acted = true;
            Assert.AreEqual(Loc.T("yetenek kullanıldı"), Opt(t, Baskin.ActAmbush).Shown);
            Assert.AreEqual(Loc.T("yetenek kullanıldı"), Opt(t, Baskin.ActRush).Shown);
            t.Moved = t.Acted = false;
            // Gövde bitişik değil: tek, kullanılamaz sürükleme satırı
            Put(e, 6, 6); MakeBody(e);
            var drag = FacilityMoveText.Options(B, t).Single(o => o.Kind == Baskin.ActDrag);
            Assert.IsFalse(drag.Usable);
            Assert.AreEqual(Loc.T("Sürükle"), drag.Name);
            Assert.AreEqual(Loc.T("yanında yatan yok"), drag.Shown);
            // Yürüyüş yetmez: menzil − sürükleme bedeli < 1
            Put(e, 2, 3);
            int walk = t.MoveRange;
            t.MoveRange = B.Moves.DragCost;
            Assert.AreEqual(Loc.T("yürüyüş yetmez"), FacilityMoveText.Options(B, t).Single(o => o.Kind == Baskin.ActDrag).Shown);
            t.MoveRange = walk;
            // Alıcı yok
            B.Carrier = t.Id;
            Assert.AreEqual(Loc.F("{0} kare içinde alıcı yok", B.Moves.ThrowRange), Opt(t, Baskin.ActThrow).Shown);
            // Menfez uzak (pafta 2; yürüyüşü 1: yalnız menfezin üstünde durulursa)
            var (_, _, B2) = Room(2);
            Floor(B2); Park(B2);
            var a2 = Team(B2, "ana");
            a2.MoveRange = 1;
            var sh = B2.Room.Shutter.Value;
            var far = Enumerable.Range(0, 64).Select(i => new Baskin.Pt(i % 8, i / 8)).First(p => B2.Room.Vents.All(v => System.Math.Abs(v.X - p.X) + System.Math.Abs(v.Y - p.Y) > 2)
                && System.Math.Max(System.Math.Abs(sh.X - p.X), System.Math.Abs(sh.Y - p.Y)) > 1 && Baskin.CellAt(B2, p.X, p.Y).T == "floor" && Baskin.UnitAt(B2, p.X, p.Y) == null);
            Put(a2, far.X, far.Y);
            var vent = FacilityMoveText.Options(B2, a2).Single(o => o.Kind == Baskin.ActVent);
            Assert.IsFalse(vent.Usable);
            Assert.AreEqual(Loc.T("menfeze ulaşılamaz"), vent.Shown);
            Assert.AreEqual(Loc.T("bitişikte iş yok"), FacilityMoveText.Options(B2, a2).Single(o => o.Kind == Baskin.ActJoint).Shown);
        }

        [Test]
        public void Effects_EachKind()
        {
            var B = AllSix(out var a);
            var expect = new Dictionary<string, (string[], string[])>
            {
                [Baskin.ActAmbush] = (new[] { Loc.T("Hatta duran ilk makine · −1"), Loc.T("makine evresinde") }, new[] { "bad", "plain" }),
                [Baskin.ActRush] = (new[] { Loc.F("Makineyi {0} kare iter", B.Moves.RushPush), Loc.T("çarparsa −1") }, new[] { "bad", "plain" }),
                [Baskin.ActDrag] = (new[] { Loc.T("Gövde yanında gelir"), Loc.T("bu tur yetenek yok") }, new[] { "good", "plain" }),
                [Baskin.ActThrow] = (new[] { Loc.T("Yük ekip üyesine geçer"), Loc.F("düz hat {0} kare", B.Moves.ThrowRange) }, new[] { "good", "plain" }),
                [Baskin.ActJoint] = (new[] { Loc.T("Kepenk kalkar"), Loc.T("iki uzman aynı tur") }, new[] { "good", "plain" }),
                [Baskin.ActVent] = (new[] { Loc.T("Öbür uçtan çıkar"), Loc.T("yetenek kalır") }, new[] { "good", "plain" }),
            };
            foreach (var kv in expect)
            {
                var fx = FacilityMoveText.Effects(B, a, kv.Key);
                CollectionAssert.AreEqual(kv.Value.Item1, fx.Texts, kv.Key);
                CollectionAssert.AreEqual(kv.Value.Item2, fx.Tones, kv.Key);
            }
        }

        [Test]
        public void PreviewRows_EachKind()
        {
            var (sim, _, B) = Room(1);
            Floor(B); Park(B);
            var t = Team(B, "tek"); var e = Team(B, "emp"); var a = Team(B, "ana");
            MovePreview Prev(Baskin.BState st, Baskin.Unit u, string kind, int x, int y, int other = 0)
            {
                var p = Baskin.PreviewAction(st, u.Id, kind, x, y, other);
                Assert.IsNotNull(p, kind + " önizlemesi");
                return FacilityMoveView.Preview(st, u, kind, other, x, y, p, sim.B);
            }
            // Pusu: kuzeye hat (kurulduğu kare + hattın kareleri), PUSU etiketi
            Put(t, 3, 4);
            var amb = Prev(B, t, Baskin.ActAmbush, 3, 3);
            var line = Baskin.PreviewAction(B, t.Id, Baskin.ActAmbush, 3, 3).Line;
            Assert.AreEqual(line.Count + 1, amb.Strokes.Single().Points.Length);
            Assert.IsTrue(amb.Strokes[0].Dashed);
            Assert.AreEqual(Loc.F("Pusu hattı: {0} kare", line.Count), amb.Rows[0].Text);
            Assert.AreEqual(Loc.T("PUSU"), amb.Labels.Single().Text);
            Assert.AreEqual((3, 3), (amb.Labels[0].X, amb.Labels[0].Y));
            // Koşu: koşucu makinenin önüne, hayaleti orada
            Put(t, 3, 1);
            var m = Machine(B, 3, 5);
            var rush = Prev(B, t, Baskin.ActRush, 3, 5);
            Assert.AreEqual(Loc.F("Koşu: {0} kare", 3), rush.Rows[0].Text);
            Assert.AreEqual(t.Id, rush.Ghosts.Single().Unit);
            Assert.AreEqual((3, 4), (rush.Ghosts[0].X, rush.Ghosts[0].Y));
            m.Dead = true;
            // Sürükleme: yol düz çizgi, iki hayalet (sürükleyen ve gövde), gövdenin bırakıldığı karede BIRAKIR
            Put(t, 3, 3); Put(e, 2, 3); MakeBody(e);
            var drag = Prev(B, t, Baskin.ActDrag, 5, 3, e.Id);
            Assert.AreEqual(3, drag.Strokes.Single().Points.Length);
            Assert.AreEqual(2, drag.Ghosts.Count);
            Assert.AreEqual((4, 3), (drag.Ghosts.Single(g => g.Unit == e.Id).X, drag.Ghosts.Single(g => g.Unit == e.Id).Y));
            Assert.AreEqual(Loc.T("BIRAKIR"), drag.Labels.Single().Text);
            Assert.AreEqual(Loc.F("Bırakılır: {0}", Cell(B, 4, 3)), drag.Rows[0].Text);
            Assert.AreEqual(Loc.T("Bu tur yetenek kullanılamaz"), drag.Rows[1].Text);
            // Fırlatma: kavisli kesik çizgi, alıcının adı
            e.Dead = false; e.Hp = e.Max; Park(B);
            Put(t, 3, 3); Put(a, 3, 5);
            B.Carrier = t.Id;
            var thr = Prev(B, t, Baskin.ActThrow, 3, 5);
            Assert.AreEqual(.5f, thr.Strokes.Single().Arc, 1e-4);
            Assert.IsTrue(thr.Strokes[0].Dashed);
            Assert.AreEqual(Loc.F("Yükü {0} taşır", FacilityMoveText.Who(a)), thr.Rows[0].Text);
            Assert.AreEqual(Loc.T("ALICI"), thr.Labels.Single().Text);

            // Pafta 2: kepenk (tutar, sonra açılır) ve menfez (yürüyüş + kesik kavis)
            var (sim2, _, B2) = Room(2);
            Floor(B2); Park(B2);
            var t2 = Team(B2, "tek"); var e2 = Team(B2, "emp"); var a2 = Team(B2, "ana");
            var s = B2.Room.Shutter.Value;
            var n1 = FreeNeighbor(B2, s, false, B2.Room.Vents.ToArray()); Put(t2, n1.X, n1.Y);
            var n2 = FreeNeighbor(B2, s, false, B2.Room.Vents.Concat(new[] { n1 }).ToArray()); Put(e2, n2.X, n2.Y);
            var hold = FacilityMoveView.Preview(B2, t2, Baskin.ActJoint, 0, s.X, s.Y, Baskin.PreviewAction(B2, t2.Id, Baskin.ActJoint, s.X, s.Y), sim2.B);
            Assert.AreEqual(Loc.T("Tutar: ikinci uzman bu tur gelmeli"), hold.Rows[0].Text);
            Assert.AreEqual(Loc.T("TUTAR"), hold.Labels.Single().Text);
            Assert.AreEqual(s.Y * 8 + s.X, hold.Focus);
            Assert.IsTrue(Baskin.DoAction(B2, t2.Id, Baskin.ActJoint, s.X, s.Y));
            var open = FacilityMoveView.Preview(B2, e2, Baskin.ActJoint, 0, s.X, s.Y, Baskin.PreviewAction(B2, e2.Id, Baskin.ActJoint, s.X, s.Y), sim2.B);
            Assert.AreEqual(Loc.T("Kepenk kalkar; makineler de geçer"), open.Rows[0].Text);
            Assert.AreEqual(Loc.T("AÇILIR"), open.Labels.Single().Text);
            Baskin.Undo(B2);
            a2 = Team(B2, "ana");   // geri alma birim listesini yeniler
            var v0 = B2.Room.Vents[0]; var v1 = B2.Room.Vents[1];
            var near = FreeNeighbor(B2, v0, true, v1);
            Put(a2, near.X, near.Y);
            var vent = FacilityMoveView.Preview(B2, a2, Baskin.ActVent, 0, v1.X, v1.Y, Baskin.PreviewAction(B2, a2.Id, Baskin.ActVent, v1.X, v1.Y), sim2.B);
            Assert.AreEqual(2, vent.Strokes.Count, "yakın uca yürüyüş ve menfez kavisi");
            Assert.IsFalse(vent.Strokes[0].Dashed);
            Assert.IsTrue(vent.Strokes[1].Dashed);
            Assert.AreEqual(.6f, vent.Strokes[1].Arc, 1e-4);
            Assert.AreEqual((v1.X, v1.Y), (vent.Ghosts.Single().X, vent.Ghosts.Single().Y));
            Assert.AreEqual(Loc.F("Öbür uçtan çıkar: {0}", Cell(B2, v1.X, v1.Y)), vent.Rows[0].Text);
            Assert.AreEqual(Loc.T("Yetenek kullanılabilir kalır"), vent.Rows[1].Text);
        }

        [Test]
        public void Slips_PlayerAndMachine()
        {
            var (_, _, B) = Room(1);
            Floor(B); Park(B);
            var t = Team(B, "tek"); var e = Team(B, "emp"); var a = Team(B, "ana");
            Put(t, 3, 4);
            Assert.IsTrue(Baskin.DoAction(B, t.Id, Baskin.ActAmbush, 3, 3));
            var line = Baskin.AmbushLine(B, t);
            Assert.AreEqual(Loc.F("{0} · pusu · {1}–{2}", FacilityMoveText.Who(t), Cell(B, 3, 3), Cell(B, line.Last().X, line.Last().Y)),
                FacilityMoveText.Slip(B, Baskin.ActAmbush, t.Id, 3, 3));
            Baskin.Undo(B);
            t = Team(B, "tek"); e = Team(B, "emp"); a = Team(B, "ana");   // geri alma birim listesini yeniler
            // Fırlatma: alıcının adı (adlandırıcı verilirse onunla)
            Put(a, 3, 6); B.Carrier = t.Id;
            Assert.IsTrue(Baskin.DoAction(B, t.Id, Baskin.ActThrow, 3, 6));
            Assert.AreEqual(Loc.F("{0} · yük → {1}", "T", "A"), FacilityMoveText.Slip(B, Baskin.ActThrow, t.Id, 3, 6, 0, id => id == t.Id ? "T" : id == a.Id ? "A" : "?"));
            // Yük alımı ve GEÇ notu (yerde gövde varsa sayısıyla)
            Assert.AreEqual(Loc.F("{0} · yük alındı · {1}", "T", Cell(B, 2, 2)), FacilityMoveText.PickupSlip(B, "T", 2, 2));
            Assert.AreEqual(Loc.T("ekip kapıda · GEÇ'e bas"), FacilityMoveText.PassSlip(B));
            MakeBody(e);
            Assert.AreEqual(Loc.F("ekip kapıda · yerde {0} gövde · GEÇ'e bas", 1), FacilityMoveText.PassSlip(B));
            // Makine evresi: pusu vuruşu ve yükün düşmesi; öbür efektler fişsiz
            var m = Machine(B, 5, 5);
            Assert.AreEqual(Loc.F("pusu · {0} · {1}", DeskText.Lower(Baskin.UnitTypes[m.Type].Name, DeskLang.Tr), Cell(B, 5, 5)),
                FacilityMoveText.EffectSlip(new Baskin.Fx { K = "ambush-hit", X = 5, Y = 5, Unit = m.Id, N = t.Id }, B));
            Assert.AreEqual(Loc.F("yük yere düştü · {0}", Cell(B, 1, 2)), FacilityMoveText.EffectSlip(new Baskin.Fx { K = "cargo-drop", X = 1, Y = 2, Unit = t.Id }, B));
            Assert.IsNull(FacilityMoveText.EffectSlip(new Baskin.Fx { K = "dmg", X = 1, Y = 2 }, B));
        }

        [Test]
        public void BoardNotes_HoldCrateCargo()
        {
            var (_, _, B2) = Room(2);
            var s = B2.Room.Shutter.Value;
            int sk = s.Y * 8 + s.X;
            Assert.IsTrue(FacilityRaidText.BoardNotes(B2).Contains((sk, Loc.T("KEPENK"))));
            B2.HoldId = Team(B2, "tek").Id; B2.HoldX = s.X; B2.HoldY = s.Y;
            var notes = FacilityRaidText.BoardNotes(B2);
            Assert.IsTrue(notes.Contains((sk, Loc.T("TUTULUYOR"))));
            Assert.IsFalse(notes.Contains((sk, Loc.T("KEPENK"))));
            B2.CargoX = 1; B2.CargoY = 2;
            Assert.IsTrue(FacilityRaidText.BoardNotes(B2).Contains((2 * 8 + 1, Loc.T("YÜK"))));

            var (_, _, B3) = Room(3);
            Assert.IsTrue(B3.Room.BigCrate.HasValue, "pafta 3'te büyük sandık yok");
            var c = B3.Room.BigCrate.Value;
            Assert.IsTrue(FacilityRaidText.BoardNotes(B3).Contains((c.Y * 8 + c.X, Loc.T("SANDIK"))));
            B3.CrateOpen = true;
            Assert.IsFalse(FacilityRaidText.BoardNotes(B3).Any(n => n.text == Loc.T("SANDIK")));
        }

        [Test]
        public void Spotter_Closed_NewLines_OldLinesUnchanged()
        {
            var (_, _, B3) = Room(3);
            var c = B3.Room.BigCrate.Value;
            var spot = FacilityRaidText.Spotter(B3).Select(l => l.Text).ToList();
            CollectionAssert.Contains(spot, Loc.F("Gözcü · büyük sandık: {0} · iki uzman", Cell(B3, c.X, c.Y)));
            var e = Team(B3, "emp");
            MakeBody(e);
            CollectionAssert.Contains(FacilityRaidText.Spotter(B3).Select(l => l.Text).ToList(), Loc.F("Gözcü · yerde: {0} · {1}", FacilityMoveText.Who(e), Cell(B3, e.X, e.Y)));
            B3.CrateOpen = true;
            Assert.IsFalse(FacilityRaidText.Spotter(B3).Any(l => l.Text.StartsWith("Gözcü · büyük sandık")));

            // Kapanış: bugünkü satırlar ve sırası aynı; yeniler sonra
            var p = new RaidParams();
            var r = FacilityDeskKit.Rep(2);
            r.Turns = 4;
            r.Team[1].State = "d"; r.Team[1].Fell = 2; r.Team[1].X = 2; r.Team[1].Y = 5; r.Team[1].Body = 1;
            r.Team[2].State = "d"; r.Team[2].Fell = 1; r.Team[2].Body = 2;
            r.Opened.Add(3 * 8 + 4);
            r.Crate = true;
            var lines = FacilityRaidText.Closed(p, r, new int[0], null, new Baskin.Pt(6, 0)).Select(l => l.Text).ToList();
            Assert.AreEqual("— PAFTA 2/3 kapandı · 4 tur —", lines[0]);
            Assert.AreEqual(Baskin.UnitTypes["emp"].Name + " düştü · 2-C3", lines[1]);
            Assert.AreEqual(Loc.F("{0} ekiple taşınıyor", Baskin.UnitTypes["emp"].Name), lines[2]);
            Assert.AreEqual(Loc.F("{0} tesisten çıkarıldı", Baskin.UnitTypes["ana"].Name), lines[3]);
            Assert.AreEqual(Loc.F("Kepenk kalktı · {0}", FacilityRaidText.Cell(2, 4, 3)), lines[4]);
            Assert.AreEqual(Loc.F("Büyük sandık açıldı · {0}", FacilityRaidText.Cell(2, 6, 0)), lines[5]);
            Assert.AreEqual(6, lines.Count);
            CollectionAssert.Contains(FacilityRaidText.Closed(p, r, null).Select(l => l.Text).ToList(), Loc.T("Büyük sandık açıldı."));
            // Önceki raporda zaten taşınan/çıkarılan yeniden yazılmaz
            Assert.AreEqual(3, FacilityRaidText.Closed(p, r, new int[0], r).Count);
        }

        [Test]
        public void Goal_CargoOnFloor()
        {
            var (_, _, B) = Room(1);
            Assert.AreEqual(Loc.T("Amaç: ekibi geçide götür, sonra GEÇ"), FacilityRaidText.Goal(B, false));
            B.CargoX = 2; B.CargoY = 3;
            Assert.AreEqual(Loc.F("Amaç: yükü yerden al ({0})", Cell(B, 2, 3)), FacilityRaidText.Goal(B, false));
            // Tesisin girişinde de (bugün orada amaç satırı yok)
            B.Escape = true;
            Assert.IsTrue(Baskin.IsFinalExit(B));
            Assert.AreEqual(Loc.F("Amaç: yükü yerden al ({0})", Cell(B, 2, 3)), FacilityRaidText.Goal(B, true));
            B.CargoX = B.CargoY = -1;
            Assert.IsNull(FacilityRaidText.Goal(B, true));
        }
    }
}
