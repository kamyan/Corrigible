using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 5: kurtuluş haritasının durumları, fiş içerikleri, figür kuralları ve sahne bağlantıları.</summary>
    public class SiginakMapTests
    {
        static Simulation NewLiberation(double fon = 100) => Simulation.NewGame(731, new Balance { startFon = fon });
        /// <summary>Beş uzman baştan (atölye kilidi açık).</summary>
        static Simulation NewOpen() =>
            Simulation.NewGame(731, new Balance { startFon = 100, liberationMinerWorkshopLevel = 0, liberationSmokeWorkshopLevel = 0 });

        /// <summary>V.014'ün ilk yarısı: Batı Avrupa'nın üç sektörü alınır, yeni cephe henüz seçilmez.</summary>
        static Simulation WeuFreed()
        {
            var sim = NewLiberation();
            foreach (var id in new[] { "weu_1", "weu_2", "weu_3" })
            {
                while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
                Assert.IsTrue(sim.StartLiberationRaid(id).Ok, id);
                Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok, id);
            }
            return sim;
        }

        [Test]
        public void MapState_NewGame_FrontIsShelterRegionAndTeamStandsThere()
        {
            var sim = NewLiberation();
            var s = LiberationMapBuilder.Build(sim);
            Assert.AreEqual(Simulation.HQ, s.Front);
            Assert.IsTrue(s.FrontOpen);
            Assert.AreEqual(MapRegionKind.Front, s.Find(Simulation.HQ).Kind);
            Assert.AreEqual(100, s.Find(Simulation.HQ).Occupation, 1e-9);
            Assert.IsTrue(s.Regions.Where(r => r.Id != Simulation.HQ).All(r => r.Kind == MapRegionKind.Occupied), "cephe açıkken aday yok");
            Assert.AreEqual(GameData.Regions.Length, s.Regions.Count);
            Assert.IsTrue(s.Team.Ready);
            Assert.AreEqual(Simulation.HQ, s.Team.Region, "açılışta S-07 ile cephe aynı yer");
            Assert.IsFalse(s.Team.AtShelter);
        }

        [Test]
        public void MapState_FreedRegion_StampsAndDrawsFrontierCandidates()
        {
            var sim = WeuFreed();
            var s = LiberationMapBuilder.Build(sim, pendingCandidate: "eeu");
            Assert.IsFalse(s.FrontOpen);
            var weu = s.Find("weu");
            Assert.AreEqual(MapRegionKind.Free, weu.Kind);
            Assert.AreEqual(sim.S.Day + 1, weu.FreeShift, "damga: özgürleştiği vardiya");
            Assert.IsTrue(weu.FreshStamp);
            var candidates = s.Regions.Where(r => r.Kind == MapRegionKind.Candidate).Select(r => r.Id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(sim.LiberationFrontier.OrderBy(x => x).ToArray(), candidates, "adaylar = simülasyonun sınırı");
            Assert.IsTrue(s.Find("eeu").Selected, "seçili aday sarı");
            Assert.AreEqual("weu", s.Team.Region, "bölge özgür: figür özgürleşen bölgede bekler");
        }

        [Test]
        public void MapState_RaidLiftsFigure_FailureSendsItToShelterWithPawnsDown()
        {
            var sim = WeuFreed();
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid("eeu_1").Ok);
            Assert.IsTrue(LiberationMapBuilder.Build(sim).Team.Hidden, "baskında figür haritadan kalkar");
            // Teknisyen düşer ve yerine çıkacak yedek yok: ekip sığınakta bekler
            sim.Shelter.Reserves.Clear();
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Down = 1 }).Ok);
            var team = LiberationMapBuilder.Build(sim).Team;
            Assert.IsTrue(team.AtShelter);
            Assert.AreEqual(Simulation.HQ, team.Region);
            Assert.AreEqual(1, team.Down, "yatan uzman kadar piyon yatık");
            Assert.AreEqual(sim.B.liberationInjuryDays, team.Days);
            StringAssert.Contains(sim.B.liberationInjuryDays.ToString(), TelexReasons.Pick(sim), "Teknisyensiz ekip alınamaz: gün sayısıyla");
        }

        [Test]
        public void Slip_Front_MarksSectorsAndKeepsReadyWithoutFunds()
        {
            var sim = NewLiberation(5);
            var d = LiberationMapBuilder.Slip(sim, Simulation.HQ);
            Assert.IsTrue(d.HasAction);
            CollectionAssert.AreEqual(new[] { SectorMark.Ready, SectorMark.Locked, SectorMark.Locked }, d.Sectors.Select(x => x.Mark).ToArray(),
                "Karar 3: fon eksik olsa da ilk sektör BASKINA HAZIR");
            Assert.IsTrue(d.Sectors[0].Highlight);
            Assert.IsFalse(d.ActionEnabled);
            StringAssert.Contains(DeskFormat.Number(sim.B.liberationRaidCost, 0), d.Reason, "gerekçe sayı içerir");
            Assert.AreEqual(Loc.F("HAREKÂT FORMU · OP-{0:00}", 1), d.Code);
            Assert.AreEqual(Loc.F("{0} fon", DeskFormat.Signed(-sim.B.liberationRaidCost, 0)), d.ActionCost);
            // Karar 7 · S2: eksik yalnız fon → düğme açık kalır (yetersiz kaynak), altında eksik
            Assert.AreEqual(SlipActionState.Shortfall, d.ActionState);
            Assert.IsTrue(TelexReasons.RaidFundsOnly(sim, "weu_1"));
            StringAssert.Contains(DeskFormat.Number(sim.B.liberationRaidCost, 0), d.Shortfall);

            var rich = NewLiberation();
            var ready = LiberationMapBuilder.Slip(rich, Simulation.HQ);
            Assert.IsTrue(ready.ActionEnabled);
            Assert.IsNull(ready.Shortfall);
        }

        /// <summary>S2: SAHA satırları (bölge özelliği, olay, imza, tehdit), ödül 2 × 2, ekip diskleri.</summary>
        [Test]
        public void Slip_Front_FieldsRewardsAndDiscs()
        {
            var sim = NewLiberation();
            var d = LiberationMapBuilder.Slip(sim, Simulation.HQ);
            var def = GameData.Region(Simulation.HQ);
            Assert.AreEqual(4, d.Fields.Count);
            Assert.AreEqual(SiginakGlyphs.Trait(def.Trait, framed: true), d.Fields[0].Glyph);
            Assert.AreEqual(Loc.T(Baskin.RegionTraits[def.Trait].Name), d.Fields[0].Value);
            Assert.AreEqual(Loc.T(Baskin.RegionTraits[def.Trait].Desc), d.Fields[0].HoverLine, "üzerine gelince oyundaki açıklama");
            Assert.AreEqual(Loc.T(Baskin.RegionEvents[def.Event].Name), d.Fields[1].Value, "özelliğin altında bölge olayı");
            Assert.AreEqual(Loc.T(Baskin.RegionEvents[def.Event].Desc), d.Fields[1].HoverLine);
            Assert.AreEqual(SiginakGlyphs.Machine(def.Enemy, framed: true), d.Fields[2].Glyph);
            Assert.AreEqual(Loc.T(Baskin.UnitTypes[def.Enemy].Name), d.Fields[2].Value);
            Assert.AreEqual(sim.Tier, d.Fields[3].Tier);
            Assert.AreEqual(Loc.F("{0}/4", sim.Tier), d.Fields[3].Value);
            Assert.IsFalse(d.Task.Contains("\n"), "kimlik satırı görevden SAHA'ya taşındı");

            Assert.AreEqual(sim.NextLiberationSector(Simulation.HQ).MissionKind == "loot" ? 4 : 2, d.Rewards.Count);
            Assert.AreEqual(DeskFormat.Signed(sim.B.liberationLootFunds, 0), d.Rewards[0].Amount);
            Assert.AreEqual(Loc.T("fon"), d.Rewards[0].Unit);

            CollectionAssert.AreEqual(new[] { "T", "E", "A" }, d.Members.Select(m => m.Letter).ToArray(), "baskın piyonlarıyla aynı harf");
            Assert.AreEqual(Loc.F("{0}/{1}", Simulation.MaxHp("tek"), Simulation.MaxHp("tek")), d.Members[0].Hp);
            Assert.IsTrue(d.Members.All(m => !m.Missing && !m.Rookie));
        }

        /// <summary>KADRO: beş uzman, Teknisyen sabit; seçim ekip disklerine ve düğmeye yansır; eğitim baskınında kilitli.</summary>
        [Test]
        public void Slip_SquadPicker_TechnicianFixed_PickDrivesDiscsAndAction()
        {
            var sim = NewOpen();
            var d = LiberationMapBuilder.Slip(sim, Simulation.HQ);
            CollectionAssert.AreEqual(new[] { "T", "E", "A", "M", "S" }, d.Pick.Select(p => p.Letter).ToArray());
            CollectionAssert.AreEqual(new[] { true, true, true, false, false }, d.Pick.Select(p => p.Picked).ToArray(), "varsayılan: EMP uzmanı ve Analist");
            Assert.IsTrue(d.Pick[0].Locked && !d.Pick[3].Locked);
            Assert.AreEqual(Loc.T("SABİT"), d.Pick[0].Status);
            StringAssert.Contains(Loc.T(Baskin.Abilities["dinamit"].Name), d.Pick[3].HoverLine, "üstüne gelince yetenek");

            var chosen = LiberationMapBuilder.Slip(sim, Simulation.HQ, new[] { "maden", "duman" });
            CollectionAssert.AreEqual(new[] { "T", "M", "S" }, chosen.Members.Select(m => m.Letter).ToArray(), "ekip diskleri seçimi gösterir");
            Assert.IsTrue(chosen.ActionEnabled);

            var partial = LiberationMapBuilder.Slip(sim, Simulation.HQ, new[] { "maden" });
            Assert.AreEqual(SlipActionState.Disabled, partial.ActionState, "iki uzman seçilmeden emir verilmez");
            Assert.AreEqual(Loc.T("KADRO EKSİK"), partial.ActionNote);
            CollectionAssert.AreEqual(new[] { "T", "M" }, partial.Members.Select(m => m.Letter).ToArray());

            var tutorial = LiberationMapBuilder.Slip(sim, Simulation.HQ, new[] { "maden", "duman" }, fixedSquad: true);
            Assert.IsTrue(tutorial.Pick.All(p => p.Locked), "eğitim baskınında kadro sabit");
            Assert.IsFalse(tutorial.Fields.Any(f => f.Label == Loc.T("OLAY")), "eğitim baskını olaysız: fişte OLAY satırı yok");
            CollectionAssert.AreEqual(new[] { "T", "E", "A" }, tutorial.Members.Select(m => m.Letter).ToArray());
        }

        /// <summary>
        /// Kadro raporu (BASKIN EMRİ VER'den sonra, emirden önce): emrin çıkaracağı kadro, takılacak plakasıyla; sağlam yeşil,
        /// yaralı, yedek ve boş rol kırmızı. Rapor durumu değiştirmez; emirden sonra baskının kadrosu raporla aynı.
        /// </summary>
        [Test]
        public void SquadReport_ShowsTheSquadTheOrderSends_WithoutIssuingIt()
        {
            var sim = NewOpen();
            var team = sim.S.Teams[0];
            team.Members.First(m => m.Role == "ana").Wounds = 1;
            team.Members.First(m => m.Role == "emp").Down = 2;
            sim.Shelter.Reserves.Clear();
            sim.Shelter.Plates = 1;
            int commands = sim.Record.commands.Count;
            double fon = sim.S.Fon;

            var d = LiberationMapBuilder.SquadReport(sim, Simulation.HQ, new[] { "emp", "ana" });
            Assert.AreEqual(Loc.T("KADRO RAPORU"), d.Head);
            Assert.AreEqual(Loc.F("{0:00} {1} · {2}", 1, DeskText.Upper(sim.LiberationSector("weu_1").Name), Loc.T("YAĞMA")), d.Sector);
            CollectionAssert.AreEqual(new[] { "T", "E", "A" }, d.Rows.Select(r => r.Letter).ToArray());
            Assert.AreEqual(Loc.F("sağlam · can {0}/{1}", 3, 3), d.Rows[0].Status);
            Assert.IsTrue(d.Rows[0].Good);
            Assert.AreEqual(Loc.F("{0} · yedek yok, rol boş kalır", Loc.F("yatıyor · {0} gün", 2)), d.Rows[1].Status);
            Assert.IsTrue(d.Rows[1].Missing && !d.Rows[1].Good);
            Assert.AreEqual(Loc.F("{0} · zırh plakası", Loc.F("tam iyileşmedi · can {0}/{1}", 1, 2)), d.Rows[2].Status, "plaka en düşük canlıya");
            Assert.IsFalse(d.Rows[2].Good);

            // Boşta yedek varsa yatanın yerine çıkar (eksik canla)
            sim.Shelter.Reserves.Add(new SpecialistState());
            var reserve = LiberationMapBuilder.SquadReport(sim, Simulation.HQ, new[] { "emp", "ana" });
            Assert.AreEqual(Loc.F("{0} · yerine yedek uzman, can {1}/{2}", Loc.F("yatıyor · {0} gün", 2), 3 - sim.B.liberationRookieHpPenalty, 3), reserve.Rows[1].Status);
            Assert.IsFalse(reserve.Rows[1].Missing || reserve.Rows[1].Good);
            Assert.AreEqual(commands, sim.Record.commands.Count, "rapor emir vermez");
            Assert.AreEqual(fon, sim.S.Fon);
            Assert.IsNull(sim.S.Raid);

            var preview = Baskin.SquadReport(sim.LiberationSquadPreview("emp,ana"));
            Assert.IsTrue(sim.StartLiberationRaid("weu_1", "emp,ana").Ok);
            var raid = Baskin.SquadReport(sim.S.Raid.Params);
            CollectionAssert.AreEqual(raid.Select(e => (e.Type, e.Hp, e.Armor, e.Rookie, e.Missing)).ToArray(),
                preview.Select(e => (e.Type, e.Hp, e.Armor, e.Rookie, e.Missing)).ToArray(), "baskının kadrosu raporla aynı");

            // Eğitim baskını: kadro sabit, emir varsayılan seçimi gönderir
            var tutorial = LiberationMapBuilder.SquadReport(NewOpen(), Simulation.HQ, new[] { "maden", "duman" }, fixedSquad: true);
            CollectionAssert.AreEqual(new[] { "T", "E", "A" }, tutorial.Rows.Select(r => r.Letter).ToArray());
            Assert.IsTrue(tutorial.Rows.All(r => r.Good));
        }

        /// <summary>KADRO: katılmamış uzman kilitli çip (“ATÖLYE 2”), seçilemez; seçenek yokken not sıradakini söyler.</summary>
        [Test]
        public void Slip_SquadPicker_LockedSpecialistsWaitForTheWorkshop()
        {
            var sim = NewLiberation();
            var d = LiberationMapBuilder.Slip(sim, Simulation.HQ);
            CollectionAssert.AreEqual(new[] { "T", "E", "A", "M", "S" }, d.Pick.Select(p => p.Letter).ToArray(), "kilitli uzmanlar da görünür");
            CollectionAssert.AreEqual(new[] { true, false, false, true, true }, d.Pick.Select(p => p.Locked).ToArray(), "T sabit, M ve S kilitli");
            Assert.AreEqual(Loc.F("SV {0}", 2), d.Pick[3].Hp);
            Assert.AreEqual(Loc.F("SV {0}", 3), d.Pick[4].Hp);
            Assert.AreEqual(Loc.F("{0} · KİLİTLİ", DeskText.Upper(Baskin.UnitTypes["maden"].Name)), d.Pick[3].HoverTitle);
            StringAssert.Contains(Loc.T(Baskin.Abilities["duman"].Name), d.Pick[4].HoverLine, "kilitliyken de yetenek");
            Assert.AreEqual(Loc.F("{0} atölye seviye {1} olunca kadroya katılır.", Baskin.UnitTypes["maden"].Name, 2), d.PickNote);
            Assert.IsTrue(d.ActionEnabled, "varsayılan kadro (T + E + A) emre hazır");
        }

        /// <summary>S2: baskından dönen ekip aynı vardiyada çıkamaz → düğme devre dışı, sağda kısa neden.</summary>
        [Test]
        public void Slip_Front_DisabledWhenTeamIsBack()
        {
            var sim = NewLiberation();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            var d = LiberationMapBuilder.Slip(sim, Simulation.HQ);
            Assert.AreEqual(SlipActionState.Disabled, d.ActionState);
            Assert.AreEqual(Loc.T("EKİP HAZIR DEĞİL"), d.ActionNote);
            Assert.IsNotNull(d.Reason, "uzun gerekçe telekse ve düğmenin etiketine");
            Assert.IsFalse(TelexReasons.RaidFundsOnly(sim, "weu_2"));
        }

        /// <summary>Karşı saldırının aldığı sektör geri alınana kadar haritada ve fişte işaretli (mark_sector_reocc).</summary>
        [Test]
        public void MapState_Counterattack_MarksReoccupiedSector()
        {
            var sim = NewLiberation(60);
            SiginakFixtures.ApplyV014(sim);
            SiginakFixtures.ApplyCounterattack(sim);
            Assert.AreEqual("eeu_1", sim.S.LastCounterattack);
            var s = LiberationMapBuilder.Build(sim);
            Assert.AreEqual(0, s.Find("eeu").Reoccupied);
            Assert.AreEqual(0, s.Find("eeu").Captured);
            var d = LiberationMapBuilder.Slip(sim, "eeu");
            Assert.IsTrue(d.Sectors[0].Reoccupied);
            Assert.AreEqual(SectorMark.Ready, d.Sectors[0].Mark, "yeniden işgal edilen sektör yine sıradaki hedef");
            Assert.IsTrue(sim.StartLiberationRaid("eeu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.AreEqual(-1, LiberationMapBuilder.Build(sim).Find("eeu").Reoccupied, "geri alınınca işaret kalkar");
        }

        [Test]
        public void Slip_OtherRegions_AreInformationOnly()
        {
            var sim = NewLiberation();
            var info = LiberationMapBuilder.Slip(sim, "cn");
            Assert.IsFalse(info.HasAction);
            Assert.AreEqual(Loc.T("Cephe açmak için komşusunu özgürleştir."), info.Info);
            Assert.AreEqual(4, info.Fields.Count, "henüz özgür olmayan bölgenin özelliği, olayı ve imza makinesi SAHA satırlarında");
            Assert.AreEqual(SiginakGlyphs.Machine(GameData.Region("cn").Enemy, framed: true), info.Fields[2].Glyph);
            Assert.IsTrue(info.Sectors.All(x => x.Mark == SectorMark.Locked));

            var freed = WeuFreed();
            Assert.IsTrue(freed.SelectLiberationRegion("eeu").Ok);
            var neighbor = LiberationMapBuilder.Slip(freed, "na");
            StringAssert.Contains(Loc.T(GameData.Region("eeu").Name), neighbor.Info, "cephe açıkken sınırdaki bölge: tek cephe kuralı");
            var free = LiberationMapBuilder.Slip(freed, "weu");
            Assert.IsFalse(free.HasAction);
            Assert.IsTrue(free.Sectors.All(x => x.Mark == SectorMark.Retaken));
        }

        [Test]
        public void FrontSlip_EnabledOnlyForCandidates()
        {
            var sim = WeuFreed();
            var ok = LiberationMapBuilder.Front(sim, "eeu");
            Assert.IsTrue(ok.ActionEnabled);
            StringAssert.Contains(Loc.T(GameData.Region("weu").Name), ok.Neighbor);
            Assert.AreEqual(3, ok.Sectors.Count);
            var far = LiberationMapBuilder.Front(sim, "oc");
            Assert.IsFalse(far.ActionEnabled);
            Assert.IsNotNull(far.Reason);

            // K3 notu: cephe açıkken aday seçimi teleks hatası verir (“cephe zaten açık (Doğu Avrupa 0/3)”)
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.AreEqual(Loc.F("{0}: cephe zaten açık ({1} {2}/{3})", Loc.T(GameData.Region("na").Name), Loc.T(GameData.Region("eeu").Name), 0, 3),
                TelexReasons.Front(sim, "na"));
        }

        [Test]
        public void Drop_FollowsFrontRules()
        {
            var sim = NewLiberation();
            Assert.IsNull(TelexReasons.Drop(sim, Simulation.HQ), "cephe açıkken kendi cephesi geçerli");
            Assert.AreEqual(Loc.F("Cephe açık: {0} {1}/{2}", Loc.T(GameData.Region(Simulation.HQ).Name), 0, 3), TelexReasons.Drop(sim, "na"));
            Assert.IsNotNull(TelexReasons.Drop(sim, null), "deniz: geçersiz");

            var freed = WeuFreed();
            Assert.IsNull(TelexReasons.Drop(freed, "eeu"), "aday geçerli");
            Assert.IsNull(TelexReasons.Drop(freed, "weu"), "kendi yerine bırakmak geçerli");
            Assert.AreEqual(Loc.F("{0}: özgür bölgeye komşu değil", Loc.T(GameData.Region("oc").Name)), TelexReasons.Drop(freed, "oc"));
            Assert.IsNull(TelexReasons.Pick(freed));
        }

        [Test]
        public void Fixture_V014_MatchesReferenceState()
        {
            var sim = NewLiberation(60);
            SiginakFixtures.ApplyV014(sim);
            var s = LiberationMapBuilder.Build(sim);
            Assert.AreEqual(13, sim.S.Day, "vardiya 014");
            Assert.AreEqual("eeu", s.Front);
            Assert.AreEqual(1, s.Find("eeu").Captured);
            Assert.AreEqual(MapRegionKind.Free, s.Find("weu").Kind);
            Assert.AreEqual(1, s.Regions.Count(r => r.Kind == MapRegionKind.Free));
            Assert.IsTrue(s.Team.Ready);
            // Cephe bloğunun saha işaretleri ve işaretler kutusundaki tehdit
            Assert.AreEqual(GameData.Region("eeu").Trait, s.Find("eeu").Trait);
            Assert.AreEqual(GameData.Region("eeu").Enemy, s.Find("eeu").Signature);
            Assert.AreEqual(sim.Tier, s.Tier);
            Assert.AreEqual(-1, s.Find("eeu").Reoccupied);
        }

        [Test]
        public void Fixture_V031_FiveRegionsFreeFrontIndia()
        {
            var sim = NewLiberation(60);
            SiginakFixtures.ApplyV031(sim);
            var s = LiberationMapBuilder.Build(sim);
            Assert.AreEqual(30, sim.S.Day, "vardiya 031");
            Assert.AreEqual("in", s.Front);
            Assert.AreEqual(1, s.Find("in").Captured);
            CollectionAssert.AreEquivalent(new[] { "weu", "eeu", "me", "af", "ru" }, s.Regions.Where(r => r.Kind == MapRegionKind.Free).Select(r => r.Id));
            Assert.IsNull(sim.S.Ending);
            Assert.IsTrue(s.Team.Ready, "referanstaki gibi ALFA · HAZIR");
        }

        [Test]
        public void DeskGameScene_LiberationMapIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var map = roots.SelectMany(g => g.GetComponentsInChildren<SiginakMap>(true)).Single();
                var so = new SerializedObject(map);
                foreach (var f in new[] { "game", "surface", "rig", "telex", "sadeMap", "layer", "slip", "frontSlip", "squadReport", "figure", "drag", "figureTarget", "counters" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var desk = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDesk>(true)).Single();
                Assert.AreSame(map, new SerializedObject(desk).FindProperty("map").objectReferenceValue, "Enter/Esc önce harita fişine");
                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                Assert.AreSame(map, new SerializedObject(surface).FindProperty("siginakMap").objectReferenceValue);

                var layer = roots.SelectMany(g => g.GetComponentsInChildren<LiberationMapLayer>(true)).Single();
                Assert.AreEqual(GameData.Regions.Length, layer.Regions.Count);
                Assert.IsNotNull(new SerializedObject(layer).FindProperty("legendTier").objectReferenceValue, "işaretler kutusunda TEHDİT");
                foreach (var m in layer.Regions)
                {
                    Assert.IsNotNull(m.Hit, m.RegionId);
                    // Revizyon 2 · S2: cephe bloğu, ADAY ve YENİDEN İŞGAL damgaları; ÖZGÜR damgası sabit yerde
                    var ms = new SerializedObject(m);
                    foreach (var f in new[] { "fieldBox", "traitGlyph", "signatureGlyph", "candidateStamp", "candidateWord", "reoccStamp", "reoccWord", "markReoccupied", "pin", "stamp" })
                        Assert.IsNotNull(ms.FindProperty(f).objectReferenceValue, m.RegionId + "." + f);
                    Assert.AreNotEqual(Vector2.zero, ms.FindProperty("stampPoint").vector2Value, m.RegionId + " damga yeri");
                    Assert.AreEqual(3, ms.FindProperty("marks").arraySize, m.RegionId);
                    Assert.IsTrue(m.Hit.ContainsMapPoint(layer.TeamAnchor(m.RegionId)), m.RegionId + " ekip noktası kendi bölgesinde olmalı");
                }

                Assert.IsTrue(layer.gameObject.activeSelf, "kurtuluş katmanı sahnede açık (tek sunum)");
                Assert.IsTrue(map.transform.root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Liberation fills" && t.gameObject.activeSelf));
                var figure = map.transform.root.GetComponentsInChildren<TeamFigure>(true).Single();
                Assert.Greater(figure.GetComponent<BoxCollider>().size.x, 0.1f, "tık hedefi K1'de de ≥ 44 px");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>
        /// KADRO çipi: durum (SABİT, YEDEK, n GÜN) çipin bütün genişliğinde kendi satırında, ≥ 14 px; her durum iki dilde de çipin
        /// iç genişliğine sığar (önceden canın altındaki dar sütundan yandaki çipe taşıyordu).
        /// </summary>
        [Test]
        public void DeskGameScene_PickChipStatusFitsTheChip()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            var probe = new GameObject("Ölçü", typeof(RectTransform));
            try
            {
                var slip = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<OperationSlip>(true)).Single();
                var chips = new SerializedObject(slip).FindProperty("pickChips");
                Assert.AreEqual(5, chips.arraySize);
                TMPro.TMP_Text status = null;
                float inner = float.MaxValue;
                for (int i = 0; i < chips.arraySize; i++)
                {
                    var e = chips.GetArrayElementAtIndex(i);
                    status = (TMPro.TMP_Text)e.FindPropertyRelative("status").objectReferenceValue;
                    Assert.IsNotNull(status, "çip " + i);
                    Assert.GreaterOrEqual(status.fontSize, 14f, "çip " + i);
                    var root = (GameObject)e.FindPropertyRelative("root").objectReferenceValue;
                    Assert.AreSame(root.transform, status.transform.parent, "durum çipin kendi satırında, can sütununda değil");
                    var pad = root.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding;
                    inner = Mathf.Min(inner, root.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth - pad.left - pad.right);
                }
                var text = probe.AddComponent<TMPro.TextMeshProUGUI>();
                text.font = status.font;
                text.fontSize = status.fontSize;
                text.characterSpacing = status.characterSpacing;
                text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                var table = Loc.ParseTable(System.IO.File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
                foreach (var lang in Loc.Languages)
                {
                    Loc.SetLanguage(lang, lang == Loc.Turkish ? null : table[lang]);
                    var words = new[] { Loc.T("SABİT"), Loc.T("YEDEK") }.Concat(Enumerable.Range(1, 12).Select(n => Loc.F("{0} GÜN", n)));
                    foreach (var w in words)
                        Assert.LessOrEqual(text.GetPreferredValues(w).x, inner + 0.5f, lang + ": " + w);
                }
            }
            finally
            {
                Loc.SetLanguage(Loc.Turkish);
                Object.DestroyImmediate(probe);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Kadro raporu ekran katmanında görev fişinin hemen üstünde (üzerine gelme etiketinin altında), kapalı; alanları bağlı.</summary>
        [Test]
        public void DeskGameScene_SquadReportSitsOnTheSlip()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var map = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakMap>(true)).Single();
                var ms = new SerializedObject(map);
                var report = (SquadReportSlip)ms.FindProperty("squadReport").objectReferenceValue;
                var slip = (OperationSlip)ms.FindProperty("slip").objectReferenceValue;
                var so = new SerializedObject(report);
                Assert.AreSame(slip, so.FindProperty("slip").objectReferenceValue, "raporun yanına oturduğu fiş");
                var it = so.GetIterator();
                it.NextVisible(true);
                while (it.NextVisible(false))
                    if (it.propertyType == SerializedPropertyType.ObjectReference) Assert.IsNotNull(it.objectReferenceValue, it.name);
                var rows = so.FindProperty("rows");
                Assert.AreEqual(3, rows.arraySize, "Teknisyen ve iki uzman");
                for (int i = 0; i < rows.arraySize; i++)
                    foreach (var f in new[] { "root", "disc", "letter", "name", "status" })
                        Assert.IsNotNull(rows.GetArrayElementAtIndex(i).FindPropertyRelative(f).objectReferenceValue, i + "." + f);
                Assert.IsFalse(report.gameObject.activeSelf, "BASKIN EMRİ VER'e kadar kapalı");
                var hud = slip.transform.parent;
                Assert.AreSame(hud, report.transform.parent);
                Assert.AreEqual(slip.transform.GetSiblingIndex() + 1, report.transform.GetSiblingIndex(), "fişin üstünde çizilir");
                Assert.Less(report.transform.GetSiblingIndex(), hud.Find("Hover tag").GetSiblingIndex(), "üzerine gelme etiketi raporun üstünde");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void DeskGameScene_InjuredTeamStandsOnLandInShelterRegion()
        {
            var sim = NewLiberation();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            sim.Shelter.Reserves.Clear();
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Down = 2 }).Ok);
            var team = LiberationMapBuilder.Build(sim).Team;
            Assert.IsTrue(team.AtShelter);
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var layer = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LiberationMapLayer>(true)).Single();
                var shelter = layer.Regions.Single(m => m.RegionId == Simulation.HQ);
                // Baskından yaralı dönen figür S-07 fişinin altında okyanusta duruyordu
                Assert.IsTrue(shelter.Hit.ContainsMapPoint(layer.TeamPoint(team)), "yaralı ekip sığınak bölgesinde, karada");
                Assert.AreEqual(layer.TeamAnchor(Simulation.HQ), layer.TeamPoint(team), "ilk cephede S-07 ile cephe aynı yer");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void DeskGameScene_MapIsClippedToPaper()
        {
            // Bölge çokgenleri tasarım dikdörtgeninden 12 px'e kadar taşar (Grönland, Rusya'nın kuzeyi, Çukotka, Yeni Zelanda);
            // kâğıt (MapBase) dikdörtgende biter. Görünür alanın RectMask2D'si taşanı keser.
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var fitter = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MapCoverFitter>(true)).Single();
                var viewport = (RectTransform)new SerializedObject(fitter).FindProperty("viewport").objectReferenceValue;
                var mask = viewport.GetComponent<UnityEngine.UI.RectMask2D>();
                Assert.IsNotNull(mask, "haritanın görünür alanında RectMask2D");
                Assert.IsTrue(mask.enabled);
                Assert.AreEqual(Vector4.zero, mask.padding, "kesim kâğıdın kenarında");
                Assert.AreEqual(1, viewport.GetComponentsInChildren<Canvas>(true).Length, "iç tuval kesimi atlatır");
                foreach (var g in viewport.GetComponentsInChildren<UnityEngine.UI.MaskableGraphic>(true))
                    Assert.IsTrue(g.maskable, g.name + " kesilmeli");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
