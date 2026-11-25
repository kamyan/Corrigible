using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using K = Corrigible.Desk.Tests.FacilityPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis ekipmanı oyunda (aşama 9; tasarım §2, §5, §6, §11.6): yedi ekipmanın her eylemi gerçek giriş yolundan verilir (uzman seçilir,
    /// R tuşu → ekipman kipi, tahtaya tık → <c>Baskin.DoAction</c>), kart satırı ve R fişi, hedef çerçevesi, geri alma görüntüsü, telsiz
    /// fişi ve tahtadaki parça doğrulanır, Z ile geri alınır ve yeniden verilir; aygıtlar makine evresinde canlanır (taret vuruşu, yem
    /// adımı, vinç çekişi) ve süresi bitince hurdaya döner; köpek dördüncü kartta, 4 tuşu ve kart tıkıyla seçilir, E ile havlar, getirir
    /// ve koklar, düşünce karttan TAHTADAN ÇEKİLDİ olur ve tahtadan kalkar; kelepçelenen makine tahtada tıkla seçilir, E ile komut verir,
    /// süresi bitince serbest kalır ve NİYETSİZ notu taşır. Masa asset dengesiyle kurulur (asset değişmez). Ekipman, köpek, makineler ve ışınlamalar test kurulumudur (kayda girmez); eylemler gerçek yoldan.
    /// </summary>
    public sealed class FacilityGearPlayTests
    {
        [UnityTearDown] public IEnumerator Teardown()
        {
            yield return K.UnloadRaid();
            yield return BoardPlayKit.TearDown();
        }

        static LiveRaidDataSource Src => K.Live;
        static Baskin.Unit U(string type) => K.Team(Src, type);
        static Baskin.Unit ById(int id) => Src.State.Units.FirstOrDefault(u => u.Id == id);
        static string At(string what) => what + " · pafta " + Src.State.Room?.Index + " · tur " + Src.State.Turn + " · evre " + Src.State.Phase;

        static IEnumerator Ready()
        {
            yield return K.Settle(Src, "kurulum öncesi");
            K.Calm(Src); K.Floor(Src); K.Park(Src);
            Src.State.Ev = null;
            Src.State.MaxTurns = Src.State.Turn + 10;   // kurulum: uzun senaryoda süre dolmasın
            yield return K.Settle(Src, "kurulum");
        }

        /// <summary>Son eylemin tıktan hemen sonraki telsiz fişi.</summary>
        static string lastSlip;

        /// <summary>
        /// Ekipman eylemini oyuncunun yolundan verir: uzman harfle seçilir, R'ye eylem kurulana kadar basılır (kartın ekipman satırı kurulu
        /// ve R fişi onda, hedef çerçeveli), tahtadaki hedefe tıklanır; geri alma görüntüsü alındı, kip kapandı, fiş yazıldı.
        /// </summary>
        static IEnumerator Give(Baskin.Unit u, string kind, int x, int y)
        {
            yield return K.Settle(Src, kind + " öncesi");
            Src.ClearHover();
            Src.Select(K.Letter(u));
            Assert.AreEqual(K.Letter(u), Src.Selected, At("seçilemedi: " + u.Type));
            yield return K.ArmGearByKey(Src, kind);
            var vm = Src.Current;
            Assert.IsTrue(vm.GearArmed, At(kind + ": ekipman kipi açık"));
            Assert.AreEqual(1, vm.GearRows.Length, At(kind + ": kartta tek ekipman satırı"));
            Assert.IsTrue(vm.GearRows[0].Armed, At(kind + ": satır kurulu"));
            Assert.AreEqual(0, vm.GearKey, At(kind + ": R fişi ekipman satırında"));
            Assert.IsFalse(string.IsNullOrEmpty(vm.GearRows[0].Text), At(kind + ": satırda ekipmanın adı"));
            Assert.IsFalse(vm.ActionArmed, At(kind + ": hamle kipi kapalı (tek kip)"));
            CollectionAssert.Contains(vm.AbilityTargets, new Vector2Int(x, y), At(kind + ": hedef çerçeveli"));
            int hist = Src.State.Hist.Count;
            K.ClickTile(x, y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, At(kind + ": tahtaya tık eylemi vermedi"));
            Assert.IsFalse(Src.GearArmed, At(kind + ": başarılı eylem kipi kapatır"));
            lastSlip = Src.Current.EventLine;
            Assert.IsFalse(string.IsNullOrEmpty(lastSlip), At(kind + ": telsiz fişi"));
            yield return K.Settle(Src, kind);
        }

        static IEnumerator UndoByKey(string what)
        {
            yield return K.Settle(Src, what + " geri alma öncesi");
            int hist = Src.State.Hist.Count;
            Assert.Greater(hist, 0, At(what + ": geri alınacak eylem yok"));
            yield return BoardPlayKit.PressKey(Key.Z);
            Assert.AreEqual(hist - 1, Src.State.Hist.Count, At(what + ": Z geri almadı"));
            yield return K.Settle(Src, what + " geri alma");
        }

        /// <summary>EMRİ İLET (onay sorulursa bir kez daha) ve makine evresi biter; evrede oynayan efektler toplanır.</summary>
        static IEnumerator EndTurnCollect(List<Baskin.Fx> seen)
        {
            yield return K.Settle(Src, "tur öncesi");
            var board = K.Board;
            void On(Baskin.Fx f, int i, int n) => seen.Add(f);
            board.EffectPlaying += On;
            try
            {
                int turn = Src.State.Turn;
                Src.EndTurn();
                if (Src.Current.EndConfirm) Src.EndTurn();
                yield return BoardPlayKit.WaitFor(() => Src.State.Turn != turn || Src.State.Phase != "player" || Src.Current.Finished, 15, At("tur bitmedi"));
                yield return K.Settle(Src, "makine evresi", 15);
            }
            finally { board.EffectPlaying -= On; }
        }

        static RaidHudView Hud => (RaidHudView)typeof(RaidDataSource).GetField("hud", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Src);
        static RaidTeamCardView[] TeamCards => (RaidTeamCardView[])typeof(RaidHudView).GetField("team", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Hud);

        static bool Near(float yaw, float want) => Mathf.Abs(Mathf.DeltaAngle(yaw, want)) < 1f;

        [UnityTest]
        public IEnumerator Gear_AllSeven_GivenInPlay()
        {
            yield return K.OpenRaid();
            var board = K.Board;
            var given = new HashSet<string>();
            yield return Ready();
            var B = Src.State;

            // ---------- Örme seti: tel (1 hak), sonra kalas (2 hak); R döngüsü tel → kalas
            var t = K.GearUp(Src, "tek", FacilityGear.Wire, 3);
            K.Put(t, 3, 4); Src.ClearHover();
            yield return Give(t, Baskin.ActWire, 3, 3);
            Assert.AreEqual("tel", Baskin.CellAt(Src.State, 3, 3).O?.K, "tel kuruldu");
            StringAssert.Contains(" · tel · ", lastSlip);
            Assert.IsNotNull(board.ObjectPiece(3, 3)?.transform.Find("Visual/Strand"), "tahtada tel örgü parçası");
            yield return UndoByKey("tel");
            Assert.IsNull(Baskin.CellAt(Src.State, 3, 3).O, "geri alınan tel");
            Assert.IsNull(board.ObjectPiece(3, 3), "tel parçası kalktı");
            Assert.IsFalse(board.RubbleAt(3, 3), "geri alınan telin yerine enkaz konmaz");
            t = U("tek");
            yield return Give(t, Baskin.ActWire, 3, 3);
            given.Add(Baskin.ActWire);
            t = U("tek"); t.Acted = false; Src.ClearHover();
            yield return Give(t, Baskin.ActPlank, 4, 4);
            Assert.AreEqual("kalas", Baskin.CellAt(Src.State, 4, 4).O?.K, "kalas kuruldu");
            Assert.IsNotNull(board.ObjectPiece(4, 4)?.transform.Find("Visual/Board"), "tahtada kalas parçası");
            Assert.AreEqual(0, U("tek").GearLeft, "tel 1 + kalas 2 hak");
            yield return UndoByKey("kalas");
            yield return Give(U("tek"), Baskin.ActPlank, 4, 4);
            given.Add(Baskin.ActPlank);

            // ---------- Dinamit lokumu: EMP uzmanının yanındaki duvar kalıcı zemine döner
            var e = K.GearUp(Src, "emp", FacilityGear.Charge, 1);
            Baskin.CellAt(Src.State, 6, 2).T = "wall";
            K.Put(e, 6, 3); Src.ClearHover();
            yield return K.Settle(Src, "duvar");
            yield return Give(e, Baskin.ActCharge, 6, 2);
            Assert.AreEqual("floor", Baskin.CellAt(Src.State, 6, 2).T, "duvar yıkıldı");
            StringAssert.Contains(" · lokum · ", lastSlip);
            yield return UndoByKey("lokum");
            Assert.AreEqual("wall", Baskin.CellAt(Src.State, 6, 2).T, "geri alınan lokum");
            yield return Give(U("emp"), Baskin.ActCharge, 6, 2);
            given.Add(Baskin.ActCharge);

            // ---------- Aygıtlar: Analist taret, yem ve vinç kurar (her biri bir hak; yetenek yerine)
            foreach (var (gear, kind, x, y, part) in new[]
            {
                (FacilityGear.Turret, Baskin.ActTurret, 2, 1, "Visual/Barrel"),
                (FacilityGear.Decoy, Baskin.ActDecoy, 1, 2, "Visual/KeyWing"),
                (FacilityGear.Winch, Baskin.ActWinch, 3, 2, "Visual/Drum"),
            })
            {
                var a = K.GearUp(Src, "ana", gear, 1);
                K.Put(a, 2, 2); Src.ClearHover();
                yield return Give(a, kind, x, y);
                var g = Src.State.Units.Last(v => Baskin.IsGadget(v));
                Assert.AreEqual((kind, x, y), (g.Type, g.X, g.Y), kind + " kuruldu");
                Assert.AreEqual((x, y), board.PieceTile(g.Id), kind + ": parça karesinde");
                Assert.IsNotNull(board.UnitPiece(g.Id).transform.Find(part), kind + ": kendi parçası (dron değil)");
                Assert.IsTrue(Near(board.UnitPiece(g.Id).transform.eulerAngles.y, RaidBoard3D.GadgetYaw(g.Dir)), kind + ": Dir'e bakar");
                StringAssert.Contains(" kuruldu · ", lastSlip);
                yield return UndoByKey(kind);
                Assert.IsFalse(Src.State.Units.Any(v => Baskin.IsGadget(v) && v.X == x && v.Y == y), kind + ": geri alınan aygıt");
                Assert.IsNull(board.PieceTile(g.Id), kind + ": geri alınan aygıtın parçası yok");
                yield return Give(U("ana"), kind, x, y);
                given.Add(kind);
            }

            // ---------- Kumanda kelepçesi: Teknisyen'in yanındaki makine ele geçer, ekip yönüne döner
            var c = K.GearUp(Src, "tek", FacilityGear.Clamp, 1);
            K.Put(c, 5, 5);
            var m = K.Machine(Src.State, 6, 5);
            Src.ClearHover();
            yield return K.Settle(Src, "makine");
            yield return Give(c, Baskin.ActClamp, 6, 5);
            Assert.IsTrue(Baskin.IsCaptive(ById(m.Id)), "makine tutsak");
            StringAssert.Contains(" ele geçirildi · ", lastSlip);
            Assert.IsTrue(Near(board.UnitPiece(m.Id).transform.eulerAngles.y, 45), "tutsak ekip yönünde");
            yield return UndoByKey("kelepçe");
            Assert.IsFalse(Baskin.IsCaptive(ById(m.Id)), "geri alınan kelepçe");
            Assert.IsTrue(Near(board.UnitPiece(m.Id).transform.eulerAngles.y, -135), "geri alınan tutsak makine yönünde");
            yield return Give(U("tek"), Baskin.ActClamp, 6, 5);
            given.Add(Baskin.ActClamp);

            CollectionAssert.AreEquivalent(Baskin.GearActionKinds, given, "yedi ekipman eyleminin hepsi oyunda verildi");
        }

        [UnityTest]
        public IEnumerator Devices_RunInMachinePhase_ThenScrap()
        {
            yield return K.OpenRaid();
            var board = K.Board;
            yield return Ready();
            var B = Src.State;
            // Taret doğuya (hattında makine), vinç güneye (hattında makine), yem doğuya; uzmanlar kurar
            var a = K.GearUp(Src, "ana", FacilityGear.Turret, 1);
            K.Put(a, 1, 1); Src.ClearHover();
            yield return Give(a, Baskin.ActTurret, 2, 1);
            var m1 = K.Machine(Src.State, 5, 1);
            var e = K.GearUp(Src, "emp", FacilityGear.Winch, 1);
            K.Put(e, 6, 3); Src.ClearHover();
            yield return Give(e, Baskin.ActWinch, 6, 4);
            var m2 = K.AddMachine(Src, "dron", 6, 7);
            var t = K.GearUp(Src, "tek", FacilityGear.Decoy, 1);
            K.Put(t, 2, 4); Src.ClearHover();
            yield return Give(t, Baskin.ActDecoy, 3, 4);
            var turret = Src.State.Units.First(v => v.Type == "taret");
            var winch = Src.State.Units.First(v => v.Type == "vinc");
            var decoy = Src.State.Units.First(v => v.Type == "yem");
            Src.ClearHover();
            yield return K.Settle(Src, "aygıtlar");
            Assert.AreEqual(2, board.DeviceLineCount, "taret ve vinç hattı çizili");
            Assert.Greater(board.DecoyPathSegments, 0, "yemin yolu çizili");
            int hp = m1.Hp;

            var seen = new List<Baskin.Fx>();
            yield return EndTurnCollect(seen);
            Assert.IsTrue(seen.Any(f => f.K == "turret-hit" && f.Unit == m1.Id), "taret vuruşu canlandı");
            Assert.Less(ById(m1.Id).Hp, hp, "makine hasar aldı");
            Assert.IsTrue(seen.Any(f => f.K == "decoy-step" && f.Unit == decoy.Id), "yem adımı canlandı");
            Assert.AreEqual((ById(decoy.Id).X, ById(decoy.Id).Y), board.PieceTile(decoy.Id), "yemin parçası yeni karesinde");
            Assert.IsTrue(seen.Any(f => f.K == "winch-pull" && f.Unit == m2.Id), "vinç çekişi canlandı");
            if (!ById(m2.Id).Dead) Assert.AreEqual((ById(m2.Id).X, ById(m2.Id).Y), board.PieceTile(m2.Id), "çekilen makinenin parçası durumdaki karesinde");
            Assert.AreEqual(2, ById(turret.Id).Turns, "aygıtın çalışması bir düştü");
            // Kurulum: makineler kalkar (ekip sonraki evrelerde düşmesin)
            K.Calm(Src);
            yield return K.Settle(Src, "makineler kalktı");

            // Üç evre sonra aygıtlar hurdaya döner (karede hurda nesnesi, parça yok)
            for (int i = 0; i < 2; i++) yield return EndTurnCollect(seen);
            Assert.IsTrue(seen.Any(f => (f.K == "scrap" || f.K == "device-break") && f.Unit == turret.Id), "taret durdu");
            Assert.IsTrue(ById(turret.Id).Dead, "taret tahtadan kalktı");
            Assert.IsNull(board.PieceTile(turret.Id), "taretin parçası yok");
            var tx = ById(turret.Id).X; var ty = ById(turret.Id).Y;
            if (Baskin.CellAt(Src.State, tx, ty).O?.K == "hurda")
                Assert.IsNotNull(board.ObjectPiece(tx, ty)?.transform.Find("Visual/Plate1"), "hurda parçası");
            Assert.IsTrue(Src.State.Cells.Cast<Baskin.Cell>().Any(cell => cell.O?.K == "hurda"), "en az bir aygıt hurdaya döndü");
            Assert.AreEqual(0, board.DeviceLineCount, "hurdaya dönen aygıtların hattı yok");
        }

        [UnityTest]
        public IEnumerator Dog_Card4_Commands_Withdraw()
        {
            yield return K.OpenRaid();
            var board = K.Board;
            yield return Ready();
            var B = Src.State;
            var owner = U("tek");
            var dog = K.AddDog(Src, owner.Slot, 3, 3);
            yield return K.Settle(Src, "köpek");
            Assert.AreEqual(4, Src.Current.Team.Length, "dördüncü kart: köpek");
            Assert.IsTrue(Src.Current.Team[3].Dog, "dördüncü kart köpeğin");
            Assert.IsTrue(TeamCards[3].gameObject.activeSelf, "Card 4 görünür");
            Assert.IsNotNull(board.UnitPiece(dog.Id).transform.Find("Visual/Snout"), "tahtada köpek parçası");

            // 4 tuşu ve kart tıkı seçer
            yield return K.SelectDog(Src, dog.Id);
            Src.Select('T');
            Assert.AreEqual(0, Src.SelectedOther, "harf seçimi köpeği bırakır");
            TeamCards[3].Button.onClick.Invoke();
            yield return null;
            Assert.AreEqual(dog.Id, Src.SelectedOther, "kart tıkı köpeği seçer");

            // Havla: E + tık köpeğin karesine
            yield return K.ArmByKey(Src, Baskin.ActBark);
            int hist = Src.State.Hist.Count;
            K.ClickTile(dog.X, dog.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, "havla verildi");
            Assert.IsTrue(ById(dog.Id).Bark, "köpek havladı");
            yield return K.Settle(Src, "havla");
            Assert.Greater(board.BarkTiles, 0, "havlama erimi boyalı");

            // Getir: yük köpeğin yanında (kurulum: komutun hakkı yeniden)
            ById(dog.Id).Acted = false;
            Src.State.CargoX = dog.X + 1; Src.State.CargoY = dog.Y;
            Src.ClearHover();
            yield return K.Settle(Src, "yük");
            yield return K.SelectDog(Src, dog.Id);
            yield return K.ArmByKey(Src, Baskin.ActFetch);
            hist = Src.State.Hist.Count;
            K.ClickTile(dog.X + 1, dog.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, "getir verildi");
            Assert.AreEqual(dog.Id, Src.State.Carrier, "köpek yükü taşıyor");
            yield return K.Settle(Src, "getir");
            Assert.IsFalse(board.CargoShown, "yerdeki yük kalktı");

            // Kokla: köpek ileri geçidin yanında
            var gate = Src.State.Room.Forward[0];
            var near = K.FreeNeighbor(Src.State, gate, true);
            var d = ById(dog.Id); d.Acted = false; K.Put(d, near.X, near.Y);
            Src.ClearHover();
            yield return K.Settle(Src, "geçit");
            yield return K.SelectDog(Src, dog.Id);
            yield return K.ArmByKey(Src, Baskin.ActSniff);
            hist = Src.State.Hist.Count;
            K.ClickTile(gate.X, gate.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, "kokla verildi");
            Assert.Greater(Src.State.Sniffed, 0, "oda koklandı");
            yield return K.Settle(Src, "kokla");
            Assert.IsTrue(Src.MarksFor(Src.Current).Notes.Any(n => n.text == Loc.T("KOKLANDI")), "KOKLANDI notu");

            // Köpek düşer (kurulum: canı 1, karesinde uyanma işareti) → tahtadan ve karttan çekilir
            Src.State.Carrier = 0; Src.State.CargoX = Src.State.CargoY = -1;
            d = ById(dog.Id); d.Hp = 1;
            Src.State.Spawns.Add(new Baskin.Pt(d.X, d.Y));
            Src.ClearHover();
            var seen = new List<Baskin.Fx>();
            yield return EndTurnCollect(seen);
            Assert.IsTrue(seen.Any(f => f.K == "dog-down" && f.Unit == dog.Id), "köpek çekildi");
            Assert.IsNull(board.PieceTile(dog.Id), "köpek tahtadan kalktı");
            var card = Src.Current.Team.FirstOrDefault(x => x.Dog);
            Assert.IsNotNull(card, "köpeğin kartı");
            Assert.IsTrue(card.Down, "kart düşmüş");
            Assert.AreEqual(Loc.T("TAHTADAN ÇEKİLDİ"), card.DownText);
        }

        [UnityTest]
        public IEnumerator Captive_SelectedOnBoard_Command_Released()
        {
            yield return K.OpenRaid();
            var board = K.Board;
            yield return Ready();
            var c = K.GearUp(Src, "tek", FacilityGear.Clamp, 1);
            K.Put(c, 3, 3);
            var m = K.Machine(Src.State, 4, 3);
            m.Type = "dron"; m.Max = m.Hp = 2;
            Src.ClearHover();
            yield return Give(c, Baskin.ActClamp, 4, 3);
            Assert.IsTrue(Baskin.IsCaptive(ById(m.Id)));

            // Tutsak yakalandığı tur oynamaz: tur biter, sonra tahtada tıkla seçilir ve E ile komut verir
            yield return K.EndTurnConfirmed(Src);
            var cap = ById(m.Id);
            Assert.IsTrue(Baskin.IsCaptive(cap), "tutsak bir tur sonra da ekipte");
            var victim = K.AddMachine(Src, "dron", cap.X + 1, cap.Y);
            victim.Hp = 1;
            Src.ClearHover();
            yield return K.Settle(Src, "hedef");
            K.ClickTile(cap.X, cap.Y);
            Assert.AreEqual(cap.Id, Src.SelectedOther, "tutsak tahtada tıkla seçilir");
            Assert.AreEqual('\0', Src.Selected, "harf seçimi yok");
            yield return K.ArmByKey(Src, Baskin.ActCommand);
            Assert.IsTrue(Src.Current.ActionRows.Any(r => r.Armed), "tutsağın kartında komut satırı kurulu");
            int hist = Src.State.Hist.Count;
            K.ClickTile(victim.X, victim.Y);
            Assert.AreEqual(hist + 1, Src.State.Hist.Count, "komut verildi");
            yield return K.Settle(Src, "komut");
            Assert.IsTrue(ById(victim.Id).Dead, "tutsak dron hedefi vurdu");
            Assert.AreEqual((Loc.F("TUTSAK · {0}", 3), false), Src.MarksFor(Src.Current).Tags[cap.Id], "etiket: kalan üç tur");

            // İki tur daha: son tur (kırmızı); bir tur daha: serbest, makine yönüne döner, bu planı niyetsiz geçirir
            yield return K.EndTurnConfirmed(Src);
            yield return K.EndTurnConfirmed(Src);
            Assert.IsTrue(Baskin.IsCaptive(ById(m.Id)), "son turda hâlâ tutsak");
            Assert.AreEqual((Loc.T("SON TUR"), true), Src.MarksFor(Src.Current).Tags[m.Id], "etiket: son tur kırmızı");
            var seen = new List<Baskin.Fx>();
            yield return EndTurnCollect(seen);
            Assert.IsTrue(seen.Any(f => f.K == "release" && f.Unit == m.Id), "serbest kalma canlandı");
            var freed = ById(m.Id);
            Assert.IsFalse(Baskin.IsCaptive(freed), "makine yeniden YZ'nin");
            Assert.AreEqual('e', freed.Side);
            Assert.IsTrue(Near(board.UnitPiece(m.Id).transform.eulerAngles.y, -135), "makine yönüne döndü");
            Assert.IsTrue(Src.MarksFor(Src.Current).Notes.Contains((freed.Y * 8 + freed.X, Loc.T("NİYETSİZ"))), "NİYETSİZ notu");
        }
    }
}
