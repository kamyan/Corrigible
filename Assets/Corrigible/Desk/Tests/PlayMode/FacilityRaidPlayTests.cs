using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using K = Corrigible.Desk.Tests.FacilityPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskını oyunda (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §3.3–3.6, §11.5): karargâhın ilk kanadı
    /// baştan sona (kapı ve GEÇ, pafta çevirme aynı Baskin sahnesinde, hedef, dönüş, tesisin girişi, sonuç kartı, operasyon raporu);
    /// baskın ortasında kaydet/çık ve devam (oda baştan); sonuç kartında çık ve devam (kart gösterilmez, sonuç yazılır); giriş
    /// odasında süre sonu (onay notu, geri çekilme). Tesis tek baskın kipidir; masa asset dengesiyle kurulur (söküm asset'in
    /// değişmediğini doğrular); kayıtlar test klasörüne gider.
    /// </summary>
    public sealed class FacilityRaidPlayTests
    {
        [UnityTearDown] public IEnumerator Teardown()
        {
            yield return K.UnloadRaid();
            yield return BoardPlayKit.TearDown();
        }

        /// <summary>Masayı kurar (asset dengesi), harekâtın ilk sektörüne baskın emri verir, tahtayı bekler.</summary>
        static IEnumerator Open()
        {
            yield return BoardPlayKit.LoadDesk();
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            var sim = BoardPlayKit.Sim;
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsNotNull(next, "alınacak sektör yok");
            var started = BoardPlayKit.Game.StartLiberationRaid(next.Id);
            Assert.IsTrue(started.Ok, started.Msg);
            Debug.Log("[FacilityRaidPlayTests] baskın " + next.Id);
            yield return K.Source();
            Assert.IsTrue(FacilityRaidSession.Applies(sim));
            Assert.IsNotNull(K.Live.State.Room, "tesis odası");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator HeadwaterOrder_ShowsFlameRobotOnTheTargetBoard()
        {
            yield return BoardPlayKit.LoadDesk(tune: FacilityRewardPlayKit.Tune);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            var sim = BoardPlayKit.Sim;
            FacilityRegionPlayKit.Reach(sim, "sa");
            var slip = LiberationMapBuilder.Slip(sim, "sa");
            Assert.AreEqual("Alev robotu", slip.Fields.Single(f => f.Label == Loc.T("İMZA")).Value);
            var next = sim.NextLiberationSector("sa");
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(next.Id).Ok);
            Assert.AreEqual("alev", sim.S.Raid.Params.Signature);
            yield return K.Source();
            var src = K.Live;
            yield return K.ThroughGate(src);
            yield return K.ThroughGate(src);
            var unit = src.State.Units.First(u => u.Type == "alev" && Baskin.OnBoard(u));
            Assert.AreEqual("flame", Baskin.Intents(src.State).Single(i => i.Unit == unit.Id).Kind);
            var piece = K.Board.UnitPiece(unit.Id);
            Assert.IsNotNull(piece);
            Assert.AreEqual("alev", piece.Kind);
            Assert.IsTrue(piece.GetComponentsInChildren<Transform>(true).Any(part => part.name == "Alev ağzı"),
                "tahtadaki parça Alev robotunun modelini kullanır");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator SilentAgent_ChangesTeamAtTheConsole_AndIsReportedMissing()
        {
            yield return BoardPlayKit.LoadDesk(seed: 79, tune: b =>
            {
                FacilityRewardPlayKit.Tune(b);
                b.infiltratorFieldEnabled = true;
                b.liberationRegionEvents = false;
                b.admissionKnockMin = b.admissionKnockMax = 1;
                b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 1;
                b.traceDelayMin = b.traceDelayMax = 6;
                b.boardStartSupplies = b.boardSuppliesCapacity = 100;
            });
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            var sim = BoardPlayKit.Sim;
            string region = sim.S.Liberation.CurrentRegion;
            while (sim.NextLiberationSector(region).MissionKind != "server")
            {
                Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(region).Id).Ok);
                FacilityRewardPlayKit.WinRaid(sim);
                FacilityRewardPlayKit.Close(sim);
            }
            // İlk grup güvenceli. Sonraki grubun kimliği yalnız test dengesinden bilinir.
            for (int group = 0; group < 2; group++)
            {
                for (int shift = 0; sim.Admission.Group.Count == 0 && shift < 8; shift++) FacilityRewardPlayKit.Close(sim);
                Assert.IsNotEmpty(sim.Admission.Group);
                if (group == 0) Assert.IsTrue(sim.PassCandidates().Ok);
            }
            string id = sim.Admission.Group[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            var person = sim.Board.Person(id);
            while (person.Down > 0) FacilityRewardPlayKit.Close(sim);
            var own = sim.Board.Equipment.FirstOrDefault(e => e.OwnerId == id && e.Kind.StartsWith("kit_"));
            string role = own == null ? "tek" : own.Kind.Substring(4);
            if (own == null) Assert.IsTrue(sim.EquipResident(sim.Board.Equipment.First(e => e.Kind == "kit_" + role && !e.Bound).Id, id).Ok);
            Assert.IsTrue(sim.AssignResident(id, role).Ok);
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(sim.NextLiberationSector(region).Id).Ok);
            yield return K.Source();
            var src = K.Live;
            yield return K.ThroughGate(src);
            yield return K.ThroughGate(src);
            K.Calm(src);
            var specialist = src.State.Units.Single(u => u.PersonId == id);
            Assert.IsTrue(Baskin.IsSpecialist(specialist));
            yield return K.EndTurnConfirmed(src);
            Assert.IsTrue(Baskin.IsSpecialist(specialist));
            yield return K.EndTurnConfirmed(src);
            Assert.IsTrue(Baskin.IsTurncoat(specialist));
            Assert.IsTrue(src.Current.Team.Any(c => c.Name == person.Name && c.DownText == Loc.T("TARAF DEĞİŞTİRDİ")));
            Assert.IsTrue(src.Current.Log.Any(l => l.Text.Contains(person.Name) && l.Text.Contains("taraf değiştirdi")));
            src.Retreat();
            src.Retreat();
            yield return BoardPlayKit.WaitFor(() => src.Current.Finished, 5, "sonuç kartı");
            yield return BoardPlayKit.WaitSeconds(.8f);
            src.Continue();
            yield return BoardPlayKit.WaitFor(() => K.Live == null, 8, "masa dönüşü");
            Assert.IsFalse(person.Alive);
            Assert.IsTrue(OperationReportBuilder.Build(sim).Results.Any(row => row.Value == Loc.T("Sızmacı tesiste kaldı")));
        }

        static readonly FieldInfo HeadingField = typeof(RaidBlueprintView).GetField("heading", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest, Timeout(180000)]
        public IEnumerator Marksman_IsEarnedInTheRaidReport_AndExtendsTheNextAmbush()
        {
            yield return BoardPlayKit.LoadDesk(tune: b =>
            {
                FacilityRewardPlayKit.Tune(b);
                b.facilityTitlesEnabled = true;
                b.facilityMarksmanAt = 1;
                b.liberationRegionEvents = false;
            });
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            var sim = BoardPlayKit.Sim;
            var first = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(first.Id).Ok);
            yield return K.Source();
            var src = K.Live;
            K.Calm(src); K.Floor(src);
            K.Put(K.Team(src, "tek"), 1, 3);
            K.Put(K.Team(src, "emp"), 1, 4);
            K.Put(K.Team(src, "ana"), 1, 5);
            var machine = K.AddMachine(src, "dron", 4, 3);
            machine.Hp = 1;
            machine.Idle = true;
            src.Select('T');
            yield return K.ArmByKey(src, Baskin.ActAmbush);
            K.ClickTile(2, 3);
            yield return K.EndTurnConfirmed(src);
            Assert.IsTrue(machine.Dead, "pusu isabeti");

            yield return K.PlayWing(src, new List<string>());
            yield return BoardPlayKit.WaitSeconds(.8f);
            src.Continue();
            yield return BoardPlayKit.WaitFor(() => K.Live == null, 8, "baskın kapanmadı");
            Assert.IsTrue(sim.S.RaidLog.Last().Success);
            var winner = sim.S.RaidLog.Last().LiberationReward.Squad.Single(s => s.Type == "tek");
            Assert.AreEqual(FacilityTitle.Marksman, winner.NewTitles);
            Assert.IsTrue(OperationReportBuilder.Build(sim).Results.Any(r => r.Value?.Contains("Yeni unvan · Nişancı") == true));

            FacilityRewardPlayKit.Close(sim);
            var second = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(second.Id).Ok);
            yield return K.Source();
            src = K.Live;
            K.Calm(src); K.Floor(src);
            var holder = K.Team(src, "tek");
            K.Put(holder, 1, 3);
            K.Put(K.Team(src, "emp"), 1, 4);
            K.Put(K.Team(src, "ana"), 1, 5);
            var farMachine = K.AddMachine(src, "dron", 7, 3);
            farMachine.Hp = 1;
            farMachine.Idle = true;
            src.Select('T');
            yield return K.ArmByKey(src, Baskin.ActAmbush);
            K.ClickTile(2, 3);
            Assert.AreEqual(6, Baskin.AmbushLine(src.State, holder).Count);
            StringAssert.Contains("6 kare", FacilityMoveText.Detail(src.State, holder, Baskin.ActAmbush));
            yield return K.EndTurnConfirmed(src);
            Assert.IsTrue(farMachine.Dead, "unvanla altıncı kareye isabet");
        }

        /// <summary>Antet görünüyorsa yazısı; görünmüyorsa (yerleşimde kapalı) yalnız görünüm modeli denetlenir.</summary>
        static void AssertHeading(string expected, bool startsWith = false)
        {
            var view = Object.FindFirstObjectByType<RaidBlueprintView>();
            if (view == null || !view.isActiveAndEnabled || HeadingField == null) { Debug.Log("[FacilityRaidPlayTests] antet görünmüyor; yalnız model"); return; }
            var text = ((TMP_Text)HeadingField.GetValue(view)).text;
            if (startsWith) StringAssert.StartsWith(expected, text, "antet");
            else Assert.AreEqual(expected, text, "antet");
        }

        static int LastIndexOf(Simulation sim, string kind) => sim.Record.commands.FindLastIndex(c => c.kind == kind);

        [UnityTest]
        public IEnumerator Wing01_ThreeSheetsAndBack_ToTheOperationReport()
        {
            yield return Open();
            var src = K.Live; var sim = BoardPlayKit.Sim;
            int start = LastIndexOf(sim, "liberation_raid"), logs = sim.S.RaidLog.Count;
            Assert.GreaterOrEqual(start, 0, "baskın emri kayıtta");
            Assert.AreEqual(1, src.State.Room.Index);
            Assert.AreEqual(1, src.Current.BlueprintWing, "karargâhın ilk kanadı");
            Assert.AreEqual(1, src.Current.BlueprintSheet);
            AssertHeading(Loc.F("KANAT {0:00} · PAFTA {1}/3", 1, 1));
            int handle = K.SceneHandle;

            var visits = new List<string>();
            yield return K.PlayWing(src, visits, s =>
            {
                string at = "pafta " + visits.Last();
                Assert.AreEqual(handle, K.SceneHandle, at + ": Baskin sahnesi yeniden yüklenmez");
                Assert.AreEqual(visits.Count - 1, sim.S.Raid.Reports.Count, at + ": önceki her oda raporlandı");
                Assert.AreEqual("player", s.State.Phase, at);
                Assert.AreEqual(1, s.State.Turn, at + ": oda ilk turdan açılır");
                Assert.AreEqual(0, s.State.Hist.Count, at + ": geri alma geçmişi boş");
                Assert.AreEqual(s.State.Room.Index, s.Current.BlueprintSheet, at + ": antetin paftası");
                Assert.AreEqual(1, s.Current.BlueprintWing, at);
                Assert.AreEqual(FacilityRaidText.IsReturnBoard(s.State), s.Current.BlueprintReturn, at + ": dönüş eki");
                Assert.IsFalse(s.Current.Finished, at);
                string header = FacilityRaidText.IsReturnBoard(s.State)
                    ? Loc.F("— DÖNÜŞ · PAFTA {0}/3 · {1} —", s.State.Room.Index, s.State.Room.Name)
                    : Loc.F("— PAFTA {0}/3 · {1} —", s.State.Room.Index, s.State.Room.Name);
                Assert.IsTrue(s.Current.Log.Any(l => l.Text == header), at + ": gözcü raporunun başlığı (" + header + ")");
            });
            Debug.Log("[FacilityRaidPlayTests] paftalar " + string.Join(" ", visits));

            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, visits.Take(3).ToArray(), "gidişte üç pafta");
            var reps = sim.S.Raid.Reports;
            Assert.AreEqual(visits.Count, reps.Count, "oynanan her pafta bir rapor");
            Assert.IsTrue(sim.RaidRoomsClosed);
            CollectionAssert.AreEqual(Enumerable.Repeat("raid_room", reps.Count).ToArray(),
                sim.Record.commands.Skip(start + 1).Select(c => c.kind).ToArray(), "baskın emrinden sonra yalnız oda raporları");
            Assert.AreEqual(logs, sim.S.RaidLog.Count, "sonuç (raidres) yalnız kartın ardından");
            Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), src.Current.ResultTitle);
            StringAssert.Contains("PAFTA 3/3", src.Current.ResultTurn);
            Assert.AreEqual(Loc.F("TUR {0} · PAFTA {1}/3", reps.Sum(r => r.Turns), 3), src.Current.ResultTurn);

            int turns = reps.Sum(r => r.Turns), rooms = reps.Count;
            yield return BoardPlayKit.WaitSeconds(.8f);   // sonuç kartının girdi kilidi (ResultLockSeconds)
            src.Continue();
            yield return BoardPlayKit.WaitFor(() => !RaidLauncher.IsRaidLoaded, 8, "baskın sahnesi kapanmadı");
            Assert.IsNull(sim.S.Raid);
            // Sonuç oda raporlarının hemen ardından (masaya dönüşte pano kendi komutlarını sonra yazabilir)
            Assert.AreEqual("raidres", sim.Record.commands[start + 1 + rooms].kind);
            Assert.AreEqual(logs + 1, sim.S.RaidLog.Count);
            Assert.IsTrue(sim.S.RaidLog.Last().Success, "sektör alındı");
            Assert.AreEqual(rooms, sim.S.RaidLog.Last().Rooms.Count, "telsiz defterinde odaların raporları");
            var report = OperationReportBuilder.Build(sim);
            Assert.IsNotNull(report, "operasyon raporu");
            Assert.AreEqual(Loc.F("{0} tur", turns), report.Duration, "SÜRE: odaların turlarının toplamı");
        }

        [UnityTest]
        public IEnumerator MidRaidSave_ContinuesAtTheStartOfTheSheet()
        {
            yield return Open();
            var src = K.Live; var sim = BoardPlayKit.Sim;
            yield return K.ThroughGate(src);
            Assert.AreEqual(2, src.State.Room.Index, "ikinci pafta");

            // İkinci paftada bir yürüyüş: tahta hamlesi kayda girmez; devam odayı baştan açar
            int commands = sim.Record.commands.Count;
            var u = K.Standing(src.State).First();
            src.ClearHover();
            src.Select(K.Letter(u));
            Assert.AreEqual(K.Letter(u), src.Selected, "seçilemedi");
            Assert.IsTrue(src.Current.Moves.Any(p => !Baskin.IsGateTile(src.State, p.x, p.y)), "yürünecek kare yok");
            var dest = src.Current.Moves.First(p => !Baskin.IsGateTile(src.State, p.x, p.y));
            src.Move(dest);
            yield return K.Settle(src, "yürüyüş");
            Assert.IsTrue(u.Moved, "uzman yürüdü");
            Assert.AreEqual(commands, sim.Record.commands.Count, "tahta hamlesi kampanya kaydını değiştirmez");
            string expected = Baskin.Signature(sim.OpenRaidRoom());

            BoardPlayKit.Game.Save();
            yield return BoardPlayKit.ContinueDesk();
            yield return K.Source();
            src = K.Live; sim = BoardPlayKit.Sim;
            Assert.IsTrue(FacilityRaidSession.Applies(sim), "devam eden oyunda tesis baskını sürüyor");
            Assert.AreEqual(1, sim.S.Raid.Reports.Count);
            Assert.AreEqual(2, src.State.Room.Index, "içinde bulunulan pafta");
            Assert.AreEqual(expected, Baskin.Signature(src.State), "oda baştan (yürüyüş yok)");
            Assert.IsTrue(src.Current.Log.Any(l => l.Text == Loc.F("Kayıttan devam: PAFTA {0}/3 baştan açıldı.", 2)), "Kayıttan devam satırı");
            Assert.IsTrue(src.Current.Log.Any(l => l.Text.StartsWith("— PAFTA 1/3 kapandı")), "önceki odanın kapanış ayracı");

            // Devam eden zincir sona kadar oynanır
            var visits = new List<string>();
            yield return K.PlayWing(src, visits);
            Assert.AreEqual("2", visits[0]);
            Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), src.Current.ResultTitle);
            Assert.AreEqual(visits.Count + 1, sim.S.Raid.Reports.Count);
        }

        [UnityTest]
        public IEnumerator CloseAtResultCard_ResolvesWithoutReplaying()
        {
            yield return Open();
            var src = K.Live; var sim = BoardPlayKit.Sim;
            int logs = sim.S.RaidLog.Count;
            src.Retreat();
            Assert.IsTrue(src.Current.RetreatArmed, "geri çekilme onayı");
            src.Retreat();
            yield return BoardPlayKit.WaitFor(() => src.Current.Finished, 4, "sonuç kartı gelmedi");
            Assert.AreEqual(Loc.T("GERİ ÇEKİLDİ"), src.Current.ResultTitle);
            Assert.IsTrue(sim.RaidRoomsClosed, "son oda raporlandı");
            Assert.AreEqual(1, sim.S.Raid.Reports.Count);
            Assert.IsTrue(sim.S.Raid.Reports[0].Retreat && sim.S.Raid.Reports[0].Last);
            int raidRoom = LastIndexOf(sim, "raid_room");
            Assert.AreEqual(LastIndexOf(sim, "liberation_raid") + 1, raidRoom, "tek oda raporu, baskın emrinin ardından");
            Assert.AreEqual(raidRoom, sim.Record.commands.Count - 1, "kartta sonuç (raidres) yok");

            // Kartta çıkılır (raidres yok); devamda kart gösterilmez, sonuç yazılır, Baskin kendiliğinden kapanır
            BoardPlayKit.Game.Save();
            yield return BoardPlayKit.ContinueDesk();
            sim = BoardPlayKit.Sim;
            // IsRaidLoaded eşzamansız boşaltma başlar başlamaz false olur; sahnenin nesneleri boşaltma bitince gider
            yield return BoardPlayKit.WaitFor(() => BoardPlayKit.Sim.S.Raid == null && !RaidLauncher.IsRaidLoaded
                && Object.FindFirstObjectByType<LiveRaidDataSource>() == null, 10, "kapalı baskın sonuçlanmadı ya da Baskin kapanmadı");
            Assert.AreEqual("raidres", sim.Record.commands[raidRoom + 1].kind, "devamda sonuç yazıldı, oda yeniden oynanmadı");
            Assert.AreEqual(logs + 1, sim.S.RaidLog.Count);
            Assert.IsFalse(sim.S.RaidLog.Last().Success);
            Assert.AreEqual(1, sim.S.RaidLog.Last().Rooms.Count);
            Assert.IsNull(Object.FindFirstObjectByType<LiveRaidDataSource>(), "Baskin sahnesi kapandı");
        }

        [UnityTest]
        public IEnumerator EntryTimeout_AsksConfirmation_ThenWithdraws()
        {
            yield return Open();
            var src = K.Live; var sim = BoardPlayKit.Sim;
            Assert.AreEqual(RoomType.Entry, src.State.Room.Type);
            for (int guard = 0; guard < 30 && src.State.Turn < src.State.MaxTurns; guard++)
            {
                K.Calm(src);
                yield return K.EndTurnConfirmed(src);
                Assert.AreEqual(1, src.State.Room.Index, "uzmanlar kapıya gitmedi: oda sürer");
                Assert.AreEqual(0, sim.S.Raid.Reports.Count);
            }
            Assert.AreEqual(src.State.MaxTurns, src.State.Turn, "son tur");
            K.Calm(src);
            yield return K.Settle(src, "son tur");
            src.EndTurn();
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(src.Current.EndConfirm, "son turda onay notu");
            Assert.AreEqual(Loc.T("SON TUR · GERİ ÇEKİLME"), src.Current.ConfirmTitle);
            Assert.AreEqual("player", src.State.Phase, "onaysız tur bitmez");
            Assert.AreEqual(0, sim.S.Raid.Reports.Count);

            src.EndTurn();
            yield return BoardPlayKit.WaitFor(() => src.Current.Finished, 10, "süre sonunda sonuç kartı gelmedi");
            Assert.AreEqual(Loc.T("GERİ ÇEKİLDİ"), src.Current.ResultTitle);
            var last = sim.S.Raid.Reports.Last();
            Assert.IsTrue(last.Retreat && last.Last, "giriş odasında süre sonu geri çekilmedir");
        }

        /// <summary>
        /// Telsiz masası (tesis aşama 5, §7.3): EMRİ İLET'le “YAYINDA” yanar (OnAir önce true), gözcünün ilk rapor satırıyla ya da
        /// evre sonunda söner; görünüm en az holdS yanık tutar. GEÇ de lambayı yakar. Teyp baskın boyunca döner, oda dosyalanınca
        /// “KAYIT · PAFTA 1” yazar; kayıttan devamda aynı. Lamba kaynağın meşguliyetini uzatmaz (Settle beklemesi değişmez).
        /// </summary>
        [UnityTest]
        public IEnumerator RadioDesk_LampOnOrders_TapeRecordsTheSheets()
        {
            yield return Open();
            var src = K.Live; var sim = BoardPlayKit.Sim;
            var hud = Object.FindFirstObjectByType<RaidHudView>();
            Assert.IsTrue(hud.RadioDeskShown, "tesis baskınında telsiz masası");
            var lamp = hud.OnAirLamp; var tape = hud.TapeDeck;
            Assert.IsFalse(src.Current.OnAir, "açılışta yayın yok");
            Assert.IsFalse(lamp.Lit);
            Assert.IsTrue(tape.Spinning, "makaralar baskın boyunca döner");
            CollectionAssert.IsEmpty(src.Current.TapeSheets);
            Assert.AreEqual(Loc.T("KAYIT"), tape.Label.text);
            var reel = tape.transform.Find("Reel L");
            float angle = reel.localEulerAngles.z;
            yield return BoardPlayKit.WaitSeconds(.25f);
            Assert.AreNotEqual(angle, reel.localEulerAngles.z, "makara döndü");

            // OnAir akışı Changed'den okunur: önce true, sonra false; söndüren yayın makine evresindeyse gözcünün satırıdır
            var seen = new List<bool>(); bool wasOn = false; string offLine = null; bool offWhileExecuting = false;
            void OnChanged(RaidViewModel vm)
            {
                seen.Add(vm.OnAir);
                if (wasOn && !vm.OnAir && offLine == null) { offLine = vm.EventLine ?? "-"; offWhileExecuting = src.Executing; }
                wasOn |= vm.OnAir;
            }
            src.Changed += OnChanged;
            try
            {
                yield return K.Settle(src, "tur öncesi");
                src.EndTurn();
                if (src.Current.EndConfirm) src.EndTurn();
                float from = Time.realtimeSinceStartup;
                Assert.IsTrue(seen.Contains(true), "emir iletilince OnAir true");
                Assert.IsTrue(lamp.Lit, "lamba yandı");
                float to = -1;
                for (float until = from + 12; Time.realtimeSinceStartup < until && (to < 0 || src.Executing || src.Busy); )
                {
                    if (to < 0 && !lamp.Lit) to = Time.realtimeSinceStartup;
                    yield return null;
                }
                Assert.Greater(to, 0, "lamba sönmedi");
                Assert.GreaterOrEqual(to - from, RaidOnAirLampView.HoldSeconds - .02f, "en az yanık kalma süresi");
                Assert.IsFalse(src.Current.OnAir, "evre bitti: yayın yok");
                Assert.IsNotNull(offLine, "OnAir false'a döndü");
                if (offWhileExecuting) Assert.AreNotEqual(Loc.T("emir iletildi · gözcü bildiriyor"), offLine, "söndüren satır gözcünün raporu");
                Debug.Log("[FacilityRaidPlayTests] yayın: " + string.Join(",", seen.Select(b => b ? 1 : 0)) + " · söndüren: " + offLine + " · yanık " + (to - from).ToString("0.00") + " s");

                // GEÇ de emirdir: lamba yanar; oda dosyalanınca teyp “KAYIT · PAFTA 1” yazar
                yield return K.Settle(src, "makine evresi");
                if (src.State.Phase != "player") Assert.Inconclusive("ilk tur sonunda oda sürmüyor: " + src.State.Phase);
                seen.Clear(); wasOn = false;
                yield return K.ThroughGate(src);
                Assert.IsTrue(seen.Contains(true), "GEÇ'te OnAir true");
                Assert.IsFalse(src.Current.OnAir, "geçiş satırıyla söndü");
            }
            finally { src.Changed -= OnChanged; }
            var rooms = sim.S.Raid.Params.Rooms;
            Assert.IsTrue(sim.FacilityKnown(rooms[0]), "girilen oda bilinir");
            CollectionAssert.AreEqual(new[] { 1 }, src.Current.TapeSheets);
            Assert.AreEqual(Loc.F("KAYIT · PAFTA {0}", 1), tape.Label.text);
            Assert.IsTrue(tape.SheetRecorded(1) && !tape.SheetRecorded(2));
            yield return BoardPlayKit.WaitSeconds(.8f);
            Assert.IsFalse(lamp.Lit, "lamba söndü");

            // Kayıttan devam: teyp raporlardan aynı
            BoardPlayKit.Game.Save();
            yield return BoardPlayKit.ContinueDesk();
            yield return K.Source();
            src = K.Live;
            hud = Object.FindFirstObjectByType<RaidHudView>();
            Assert.IsTrue(hud.RadioDeskShown);
            CollectionAssert.AreEqual(new[] { 1 }, src.Current.TapeSheets, "kayıttan devamda teyp");
            Assert.AreEqual(Loc.F("KAYIT · PAFTA {0}", 1), hud.TapeDeck.Label.text);
            Assert.IsFalse(src.Current.OnAir);
            Assert.IsFalse(hud.OnAirLamp.Lit);
        }
    }
}
