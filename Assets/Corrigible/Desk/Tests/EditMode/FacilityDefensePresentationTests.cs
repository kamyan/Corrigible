using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P4b (aşama 11 tasarımı §8.7, §8.10): üç odalı savunmanın baskın sahnesindeki sunumu, sahnesiz (veri kaynağı,
    /// <see cref="FacilityMovesTestKit"/> kalıbı). Kuşatma Core'da kurulur ve savunma emri verilir (<see cref="FacilitySiegeDeskKit"/>), tahta
    /// <c>LiveRaidDataSource.OpenFacilityForValidation</c> ile açılır. On turluk şerit ve dalga işaretleri (Core <c>DefenseWaves</c>, terk
    /// gecikmesi dahil), antet, amaç, barikat turu, dalga satırı, gözcü, İÇERİ ÇEKİL ve onayı, hedef odada GEÇ yok, son turda geri çekilme
    /// onayı yok (savunmanın kendi onayı), geri çekilme metni, sonuç kartı (KANAT TUTULDU, RÖLE KIRILDI), saha rölesi satırı ve hover'ı;
    /// izli saldırı tahtasında kırık donanım satırı ve iz hover'ı. Sayılar Core okuyucularından (P3'ün olası ayarı testleri bozmasın).
    /// </summary>
    public class FacilityDefensePresentationTests
    {
        GameObject root;
        LiveRaidDataSource source;
        Simulation sim;
        DeskLang lang;

        void OpenDefense()
        {
            lang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            root = new GameObject("Savunma sunumu");
            root.SetActive(false);
            source = root.AddComponent<LiveRaidDataSource>();
            sim = FacilitySiegeDeskKit.NewSieged().sim;
            FacilitySiegeDeskKit.Defend(sim);
            source.OpenFacilityForValidation(sim);
            Assert.IsTrue(Baskin.IsDefense(B), "savunma tahtası");
        }

        [TearDown]
        public void Close()
        {
            if (root != null) Object.DestroyImmediate(root);
            root = null;
            DeskLoc.Instance.SetLanguage(lang);
        }

        Baskin.BState B => source.State;
        RaidViewModel Vm => source.Current;
        int RoomTrust => sim.DefenseRoomTrust;
        int FallTrust => sim.B.liberationCounterattackTrust;

        static void Invoke(RaidDataSource s, string method) =>
            typeof(RaidDataSource).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(s, null);

        /// <summary>Herkes kapıya, EMRİ İLET iki kez (onay, sonra İÇERİ ÇEKİL): sıradaki pafta açılır (EditMode'da eşzamanlı).</summary>
        void FallBack()
        {
            int room = B.Room.Index, filed = sim.S.Raid.Reports.Count;
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            Republish(source);
            Assert.IsTrue(Vm.CanPass, "herkes kapıda: İÇERİ ÇEKİL");
            source.EndTurn();
            Assert.IsTrue(Vm.EndConfirm, "ilk basış onay sorar");
            Assert.AreEqual(filed, sim.S.Raid.Reports.Count, "ilk basış odayı terk etmez");
            source.EndTurn();
            Assert.AreEqual(filed + 1, sim.S.Raid.Reports.Count, "ikinci basış: raid_room");
            Assert.AreEqual(room + 1, B.Room.Index, "sıradaki pafta açıldı");
        }

        /// <summary>Şeridin dalga işaretleri Core'un dalga takviminin geliş turları (bu oda).</summary>
        void AssertWaveMarks()
        {
            var waves = Baskin.DefenseWaves(B);
            Assert.AreEqual(B.MaxTurns + 1, Vm.TurnEvents.Length);
            for (int t = 1; t <= B.MaxTurns; t++)
            {
                bool wave = waves.Any(w => w.Arrive == t);
                Assert.AreEqual(wave ? FacilityDefenseRaidText.WaveGlyph : null, Vm.TurnEvents[t], "tur " + t);
            }
        }

        [Test]
        public void EntryRoom_TenTurns_WaveMarks_Heading_Goal_Barricade_Spotter()
        {
            OpenDefense();
            Assert.AreEqual(sim.B.facilityDefenseTurns, Vm.Turns, "tek sayaç: süre dengeden (10)");
            Assert.AreEqual(10, Vm.Turns);
            Assert.AreEqual(1, Vm.Turn);
            AssertWaveMarks();
            var waves = Baskin.DefenseWaves(B);
            Assert.AreEqual(sim.B.facilityDefenseWaves, waves.Count, "ilk odada bütün dalgalar");
            CollectionAssert.AreEqual(new[] { 2, 5, 8 }, waves.Select(w => w.Arrive).ToArray(), "dalgalar 2/5/8 (varsayılan denge)");
            Assert.AreEqual("D", Vm.TurnEvents[2]);
            Assert.IsTrue(Vm.BlueprintDefense && Vm.Defense, "antet ve görev çipi savunma");
            Assert.AreEqual(1, Vm.BlueprintSheet);
            Assert.AreEqual("Amaç: 10. tura dayan", Vm.Goal);
            // Barikat turu: makine yok, telsiz fişi açık
            Assert.IsTrue(Baskin.BarricadeTurn(B));
            Assert.IsFalse(B.Units.Any(u => u.Side == 'e' && !u.Dead), "barikat turunda makine yok");
            Assert.AreEqual("SAVUNMA · BARİKAT · makine yok", Vm.EventLine);
            Assert.IsTrue(Vm.EventVisible);
            // Görev çipi: ileri geçit ve dalga satırı (Core'un sayısıyla); donanım satırı yok (savunmada etkisi yok)
            var w1 = waves.First();
            Assert.AreEqual(2, Vm.Targets.Length);
            Assert.AreEqual(Loc.T("İleri geçit"), Vm.Targets[0].Name);
            Assert.AreEqual(Loc.F("DALGA {0}/{1} · {2} makine · {3}. tur", 1, waves.Count, w1.Count, w1.Arrive), Vm.Targets[1].Name);
            Assert.AreEqual(sim.DefenseWaveSize(B.Params.Tier), w1.Count, "dalga büyüklüğü Core'dan");
            Assert.AreEqual(0, Vm.Targets[1].Max, "dalga satırı can taşımaz");
            // Gözcü
            var log = Vm.Log.Select(l => l.Text).ToList();
            CollectionAssert.Contains(log, Loc.F("Gözcü · barikat turu · dalga {0}: {1}. tur, {2} makine, giriş kapısı", 1, w1.Arrive, w1.Count));
            // Düğme ve onay metinleri (dengeden)
            Assert.AreEqual("İÇERİ ÇEKİL", Vm.PassLabel);
            Assert.AreEqual(Loc.F("güven −{0}", RoomTrust), Vm.PassNote);
            Assert.AreEqual(Loc.F("Savunma kaybedilir: kanat düşer, güven −{0}. Yerde yatanlar ölür.", FallTrust), Vm.RetreatBody);
            Assert.AreEqual(4, Vm.RulesDefense.Split('\n').Length, "kurallar kartında dört satırlık savunma bölümü");
            StringAssert.Contains("10. tur sonunda", Vm.RulesDefense);
            // Hover: batı girişi (dalganın geliş turu), ileri geçit (kapı sığınak değil)
            var west = B.Room.West[0];
            var card = Card(source, new Vector2Int(west.X, west.Y));
            if (Baskin.UnitAt(B, west.X, west.Y) == null)
            {
                Assert.AreEqual("Giriş kapısı", card.Title);
                Assert.IsTrue(card.Rows.Any(r => r.Text == Loc.F("Dalga buradan girer ({0}. tur)", w1.Arrive)), Rows(card));
            }
            var gate = Baskin.GateTiles(B).First(g => Baskin.UnitAt(B, g.X, g.Y) == null);
            card = Card(source, new Vector2Int(gate.X, gate.Y));
            Assert.IsTrue(card.Rows.Any(r => r.Text.StartsWith("Savunmada kapı sığınak değil")), Rows(card));

            DeskLoc.Instance.SetLanguage(DeskLang.En);
            Republish(source);
            Assert.AreEqual("Goal: hold to turn 10", Vm.Goal);
            Assert.AreEqual("FALL BACK", Vm.PassLabel);
            Assert.AreEqual("W", Vm.TurnEvents[2]);
            Assert.AreEqual("DEFENCE · BARRICADE · no machines", Vm.EventLine);
        }

        static string Rows(RaidInspectorModel card) => string.Join(" | ", card.Rows.Select(r => r.Text));

        [Test]
        public void FallBack_AsksFirst_WithTheCostAndTheBody_ThenNextSheetShiftsTheWaves()
        {
            OpenDefense();
            // Yerde yatan biri (ekip arkadaşı) içeri çekilmede ölür: onay adıyla söyler
            var lying = FacilityDeskKit.Standing(B).Last();
            MakeBody(lying);
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            Republish(source);
            Assert.IsTrue(Vm.CanPass);
            Assert.IsFalse(Vm.EndConfirm);
            source.EndTurn();
            Assert.IsTrue(Vm.EndConfirm, "ilk basış onay");
            Assert.AreEqual("İÇERİ ÇEKİLME", Vm.ConfirmTitle);
            StringAssert.StartsWith(Loc.F("Bu oda terk edilir: güven −{0}, izler silinir.", RoomTrust), Vm.FatalWarning);
            string name = string.IsNullOrEmpty(lying.PersonName) ? null : lying.PersonName;
            StringAssert.Contains("Yerde yatan ", Vm.FatalWarning);
            if (name != null) StringAssert.Contains(name, Vm.FatalWarning);
            // Esc vazgeçer; sonra iki basış
            source.Cancel();
            Assert.IsFalse(Vm.EndConfirm, "Esc vazgeçer");
            int filed = sim.S.Raid.Reports.Count;
            source.EndTurn();
            source.EndTurn();
            Assert.AreEqual(filed + 1, sim.S.Raid.Reports.Count, "ikinci basış içeri çeker");
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual(2, Vm.BlueprintSheet);
            Assert.IsTrue(Vm.BlueprintDefense);
            Assert.AreEqual(sim.S.Raid.Reports.Last().Turns + 1, B.Turn, "tek sayaç: oda değişince sıfırlanmaz");
            Assert.IsTrue(Baskin.BarricadeTurn(B), "iç odanın ilk turu da barikat");
            Assert.AreEqual(1, Baskin.Abandoned(B));
            AssertWaveMarks();
            // Dalga işareti terk başına bir tur kayar (Core okumasıyla)
            Assert.AreEqual(Baskin.DefenseWaves(B).First().Arrive, System.Array.IndexOf(Vm.TurnEvents, "D"));
            Assert.Greater(Baskin.DefenseWaves(B).First().Arrive, 2, "gecikme");
            Assert.AreEqual(Loc.F("Savunma kaybedilir: kanat düşer, güven −{0}. Yerde yatanlar ölür.", FallTrust + RoomTrust), Vm.RetreatBody);
            CollectionAssert.Contains(Vm.Log.Select(l => l.Text).ToList(), Loc.F("Oda terk edildi: PAFTA {0} · güven −{1}", 1, RoomTrust));
        }

        [Test]
        public void TargetRoom_NoFallBack_RelayRow_RelayHover()
        {
            OpenDefense();
            FallBack();
            FallBack();
            Assert.AreEqual(RoomType.Target, B.Room.Type);
            Assert.AreEqual(0, Baskin.GateTiles(B).Count, "hedef odasında geçit yok");
            Assert.IsFalse(Vm.CanPass, "hedef odasında İÇERİ ÇEKİL yok");
            Assert.AreEqual("Amaç: 10. tura dayan · röleyi koru", Vm.Goal);
            var relay = FacilityDefenseRaidText.RelayAt(B).Value;
            var row = Vm.Targets[0];
            Assert.AreEqual("Saha rölesi", row.Name);
            Assert.AreEqual("thumb_relay", row.Thumb);
            Assert.AreEqual(Baskin.RelayHp(B), row.Hp);
            Assert.AreEqual(Baskin.RelayMax(B), row.Max);
            Assert.AreEqual(sim.DefenseRelayHpFor(B.Params.Perks), row.Max, "röle canı Core'dan");
            Assert.IsFalse(row.Armor);
            Assert.IsFalse(row.Broken);
            var card = Card(source, new Vector2Int(relay.X, relay.Y));
            Assert.AreEqual("Saha rölesi", card.Title);
            Assert.AreEqual("thumb_relay", card.Thumb);
            Assert.IsTrue(card.Rows.Any(r => r.Text == Loc.F("Saha rölesi · {0} can · kırılırsa kanat düşer", Baskin.RelayHp(B))), Rows(card));
            CollectionAssert.Contains(Vm.Log.Select(l => l.Text).ToList(), Loc.F("Gözcü · saha rölesi: {0} · {1} can", FacilityRaidText.Cell(3, relay.X, relay.Y), Baskin.RelayHp(B)));
            Assert.AreEqual(Loc.F("Savunma kaybedilir: kanat düşer, güven −{0}. Yerde yatanlar ölür.", FallTrust + 2 * RoomTrust), Vm.RetreatBody);
        }

        /// <summary>Ters sınama hedefi: eski <c>TimeoutEnd</c> savunmayı dışlamazdı (son turda "SON TUR · GERİ ÇEKİLME" onayı).</summary>
        [Test]
        public void LastTurn_NoWithdrawalConfirm_DefenceEndsConfirm()
        {
            OpenDefense();
            FacilityDeskKit.Calm(B);
            B.Turn = B.MaxTurns;
            Republish(source);
            Assert.IsTrue(Vm.LastTurn);
            Assert.AreNotEqual("SON TUR · GERİ ÇEKİLME", Vm.ConfirmTitle, "savunmada süre dolunca geri çekilme yok");
            source.EndTurn();
            Assert.IsTrue(Vm.EndConfirm, "son tur onay sorar");
            Assert.AreEqual("SON TUR · SAVUNMA BİTER", Vm.ConfirmTitle);
            Assert.AreEqual("Ayakta biri kalırsa kanat tutulur.", Vm.FatalWarning);
            source.Cancel();
            // Son turda içeri çekilinmez (Core): herkes kapıda olsa da İÇERİ ÇEKİL yok
            FacilityDeskKit.ToGateCore(B);
            Republish(source);
            Assert.IsFalse(Vm.CanPass, "son turda İÇERİ ÇEKİL yok");
        }

        [Test]
        public void ResultCard_Held_RowsAndTurn()
        {
            OpenDefense();
            FacilityDeskKit.Calm(B);
            for (int t = 0; t < 40 && B.Phase == "player"; t++)
            {
                Assert.IsTrue(Baskin.EndTurn(B), "EndTurn t" + B.Turn);
                FacilityDeskKit.Calm(B);
            }
            Assert.AreEqual("won", B.Phase);
            Invoke(source, "CheckRoomEnd");
            Assert.IsTrue(sim.RaidRoomsClosed, "son oda dosyalandı");
            Assert.IsTrue(Vm.Finished);
            Assert.IsTrue(Vm.Success);
            Assert.AreEqual("KANAT TUTULDU", Vm.ResultTitle);
            Assert.AreEqual(Loc.F("TUR {0}/{1} · PAFTA {2}/3", sim.FacilityRaidResult().Turns, 10, 1), Vm.ResultTurn);
            Assert.AreEqual(4, Vm.ResultRows.Length, "kartta dört satır");
            int max = sim.DefenseRelayHpFor(B.Params.Perks);
            Assert.AreEqual(("Saha rölesi", Loc.F("{0}/{1} · ulaşılmadı", max, max)), (Vm.ResultRows[0].Label, Vm.ResultRows[0].Value));
            Assert.AreEqual(("Terk edilen oda", Loc.F("{0} · güven −{1}", 0, 0)), (Vm.ResultRows[1].Label, Vm.ResultRows[1].Value));
            Assert.AreEqual("Kırılan sivil sunucu", Vm.ResultRows[2].Label);
            Assert.AreEqual(("Ekip", "3/3 ayakta"), (Vm.ResultRows[3].Label, Vm.ResultRows[3].Value));
            CollectionAssert.Contains(Vm.Log.Select(l => l.Text).ToList(), Loc.F("Savunma bitti: kanat tutuldu · {0} tur", sim.S.Raid.Reports.Last().Turns));
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            Republish(source);
            Assert.AreEqual("WING HELD", Vm.ResultTitle);
            Assert.AreEqual("Field relay", Vm.ResultRows[0].Label);
        }

        [Test]
        public void ResultCard_RelayBroken_AfterTwoFallBacks()
        {
            OpenDefense();
            FallBack();
            FallBack();
            var relay = FacilityDefenseRaidText.RelayAt(B).Value;
            var tek = Team(B, "tek");
            for (int hit = 0; hit < 4 && B.Phase == "player"; hit++)
            {
                FacilityDeskKit.Calm(B);
                var spot = FreeNeighbor(B, relay, true);
                Put(tek, spot.X, spot.Y);
                source.Select('T');
                source.Arm();
                Assert.IsTrue(source.Armed, "sabotaj kurulamadı");
                source.Use(new Vector2Int(relay.X, relay.Y));
                if (B.Phase != "player") break;
                FacilityDeskKit.Calm(B);
                Assert.IsTrue(Baskin.EndTurn(B));
            }
            Assert.AreEqual("lost", B.Phase, "röle kırıldı: savunma kaybedildi");
            Assert.IsTrue(sim.RaidRoomsClosed);
            Assert.IsTrue(Vm.Finished);
            Assert.IsFalse(Vm.Success);
            Assert.AreEqual("RÖLE KIRILDI", Vm.ResultTitle);
            Assert.AreEqual(Loc.F("TUR {0}/{1} · PAFTA {2}/3", sim.FacilityRaidResult().Turns, 10, 3), Vm.ResultTurn);
            Assert.AreEqual(("Saha rölesi", "kırıldı"), (Vm.ResultRows[0].Label, Vm.ResultRows[0].Value));
            Assert.AreEqual(Loc.F("{0} · güven −{1}", 2, 2 * RoomTrust), Vm.ResultRows[1].Value);
            Assert.AreEqual("thumb_relay", Vm.Targets[0].Thumb);
            Assert.IsTrue(Vm.Targets[0].Broken);
            Assert.AreEqual("KIRILDI", Vm.Targets[0].Stamp);
            CollectionAssert.Contains(Vm.Log.Select(l => l.Text).ToList(), "Savunma bitti: kanat düştü");
        }

        [Test]
        public void AttackBoard_MarkedDevice_Broken_TraceHover_Spotter_NoDefenceFields()
        {
            lang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            root = new GameObject("İzli saldırı");
            root.SetActive(false);
            source = root.AddComponent<LiveRaidDataSource>();
            sim = FacilityDeskKit.New();
            Assert.IsTrue(sim.MarksOn, "iz alt bayrağı varsayılan açık");
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            var room = sim.FacilityPlan.Wing(next.Id).First(r => r.Index == 1);
            Assert.IsTrue(room.DeviceAt.HasValue, "giriş odasında donanım");
            var d = room.DeviceAt.Value;
            sim.Facility.Marks.Add(new FacilityMark { Room = room.Id, Kind = FacilityMarks.Device, X = d.X, Y = d.Y, Day = 0 });
            // Bir yıkık duvar izi (kepenk ve sandık nişi değil)
            Baskin.Pt? wall = null;
            for (int y = 0; y < Baskin.N && wall == null; y++)
                for (int x = 0; x < Baskin.N && wall == null; x++)
                {
                    var c = room.PlanCells(1, false)[y, x];
                    if (c.T == "wall" && !c.Shutter && !c.Crate) wall = new Baskin.Pt(x, y);
                }
            sim.Facility.Marks.Add(new FacilityMark { Room = room.Id, Kind = FacilityMarks.Wall, X = wall.Value.X, Y = wall.Value.Y, Day = 0 });
            FacilityDeskKit.StartFirst(sim);
            source.OpenFacilityForValidation(sim);
            Assert.IsFalse(Baskin.IsDefense(B));
            Assert.AreEqual(1, B.Room.Index);
            Assert.IsNull(Baskin.CellAt(B, d.X, d.Y).O, "iz donanımı kırık getirdi");
            Assert.AreEqual("floor", Baskin.CellAt(B, wall.Value.X, wall.Value.Y).T, "yıkık duvar zemin");
            var device = Vm.Targets.Single(t => t.Name == Loc.T(Baskin.ObjNames[room.Device]));
            Assert.IsTrue(device.Broken);
            Assert.AreEqual(Loc.T("KIRILDI"), device.Stamp);
            CollectionAssert.Contains(Vm.Log.Select(l => l.Text).ToList(), Loc.F("Gözcü · önceki baskından: {0}", Loc.F("{0} {1}", 1, "kırık donanım") + ", " + Loc.F("{0} {1}", 1, "yıkık duvar")));
            FacilityDeskKit.Calm(B);
            var occupant = Baskin.UnitAt(B, wall.Value.X, wall.Value.Y);
            if (occupant != null) { var free = FreeNeighbor(B, wall.Value, true); Put(occupant, free.X, free.Y); }
            var card = Card(source, new Vector2Int(wall.Value.X, wall.Value.Y));
            Assert.IsTrue(card.Rows.Any(r => r.Text == "Önceki baskından: yıkık duvar"), Rows(card));
            // Saldırıda savunma alanları boş: bugünkü HUD
            Assert.IsFalse(Vm.Defense || Vm.BlueprintDefense);
            Assert.IsNull(Vm.PassLabel);
            Assert.IsNull(Vm.PassNote);
            Assert.IsNull(Vm.RetreatBody);
            Assert.IsNull(Vm.RulesDefense);
            Assert.AreEqual("Amaç: ekibi geçide götür, sonra GEÇ", Vm.Goal);
            Assert.IsTrue(Vm.TurnEvents == null || Vm.TurnEvents.All(e => e != "D"), "saldırıda dalga işareti yok");
        }
    }
}
