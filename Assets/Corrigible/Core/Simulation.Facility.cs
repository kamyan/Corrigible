using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    public sealed partial class Simulation
    {
        /// <summary>Kişisel unvanlar işler: alt bayrak (<see cref="Balance.facilityTitlesEnabled"/>).</summary>
        public bool FacilityTitlesOn => B.facilityTitlesEnabled;
        /// <summary>Harekâtın 108 tesis odası: harekât tohumundan ayrı akışla, önbellekten, bölge bölge tembel. S.Rng'ye dokunmaz, kayda yazılmaz (kayıtta yalnız üretici sürümü, <see cref="FacilityFingerprint"/>).</summary>
        public FacilityPlan FacilityPlan => FacilityMaps.For(S.Seed, B.facilityKatar, B.facilityYalpa);

        /// <summary>Odanın baskın değerleri (dengeden) ve oda tohumu.</summary>
        public RoomSetup FacilityRoomSetup(FacilityRoom room, int seed) => FacilityMaps.Setup(B, room, seed);

        /// <summary>
        /// Odanın ilk ziyaret tur bütçesi: bugün verilecek baskın emrinin o odada açacağı tahtanın <see cref="Baskin.BState.MaxTurns"/>'ü
        /// (<see cref="RaidChain.Setup"/>: oda türünün süresi ve Eşik; <see cref="Baskin.FirstVisitTurns"/>: hedef odasında bölgenin "kisa"
        /// özelliği). Bina planının yatış takvimi bunu okur; sunum kuralı kopyalamaz (aşama 12 kapanışı). Durum yazmaz, RNG çekmez; oda null
        /// ise 0.
        /// </summary>
        public int FacilityRoomTurns(FacilityRoom room)
        {
            if (room == null) return 0;
            var def = GameData.Regions.FirstOrDefault(r => r.Id == room.Region);
            var p = new RaidParams { Traits = def == null ? new List<string>() : new List<string> { def.Trait }, Perks = RaidPerks() };
            return Baskin.FirstVisitTurns(RaidChain.Setup(B, room, p, Array.Empty<RoomReport>(), false).Turns, room, p.Traits);
        }

        /// <summary>Tesis odalarının üretici izi (<see cref="FacilityMaps.FingerprintFor"/>): saha kuralına göre f8s10 veya f10.</summary>
        public string FacilityFingerprint => FacilityMaps.FingerprintFor(B);

        // ---------- Bölgenin tesis şartı ve notu (aşama 12; okuyucular: durum yazmaz, RNG çekmez) ----------
        /// <summary>
        /// Bölgenin baskın emrine yazılan olayı (fiş, kimlik ve sunum bunu okur; <c>OrderRaid</c>'in Event'iyle aynı kural):
        /// <see cref="FacilityMaps.EventFor"/> (na Katar, oc Yalpa; öbür bölgelerde ve alt bayraklar kapalıyken <see cref="RegionDef.Event"/>).
        /// Bölge olayları kapalıysa (<see cref="Balance.liberationRegionEvents"/>) ya da bölge bilinmiyorsa null.
        /// </summary>
        public string RegionEvent(string regionId)
        {
            var def = GameData.Regions.FirstOrDefault(r => r.Id == regionId);
            if (def == null || !B.liberationRegionEvents) return null;
            return FacilityMaps.EventFor(def, B.facilityKatar, B.facilityYalpa);
        }

        /// <summary>Binanın notu: oynanan şart bugünkü olaydan farklıysa ve tesis notu varsa <see cref="RegionDef.FacilityNote"/>, değilse
        /// <see cref="RegionDef.BuildingNote"/> (çevrilmiş). Bölge bilinmiyorsa null.</summary>
        public string RegionNote(string regionId)
        {
            var def = GameData.Regions.FirstOrDefault(r => r.Id == regionId);
            if (def == null) return null;
            var ev = RegionEvent(regionId);
            return ev != null && ev != def.Event && def.FacilityNote != null ? def.FacilityNote : def.BuildingNote;
        }

        // ---------- Oda zinciri (okuyucular: durum yazmaz, RNG çekmez) ----------
        /// <summary>Süren tesis baskınında oynanacak oda; baskın yokken, emrin odası yokken ya da son oda raporlandıysa null.</summary>
        public RoomStep? RaidRoom => RaidChain.IsFacility(S.Raid?.Params) ? RaidChain.Next(S.Raid.Params, S.Raid.Reports) : null;

        /// <summary>Tesis baskınının son odası raporlandı: sonuç (<see cref="ResolveRaid"/>) bekleniyor, tahta açılmaz.</summary>
        public bool RaidRoomsClosed => RaidChain.IsFacility(S.Raid?.Params) && S.Raid.Reports.Count > 0 && S.Raid.Reports[S.Raid.Reports.Count - 1].Last;

        /// <summary>Sıradaki odanın tahtası, raporlardan (canlı oyun, kayıttan devam ve yeniden oynatma aynı tahtayı açar).</summary>
        public Baskin.BState OpenRaidRoom() => RaidChain.Open(B, FacilityPlan, S.Raid.Params, S.Raid.Reports);

        /// <summary>Biten oda tahtasının raporu (<see cref="AdvanceRaidRoom"/>'a verilir).</summary>
        public RoomReport RaidRoomReport(Baskin.BState board) => RaidChain.Report(S.Raid.Params, S.Raid.Reports, board);

        /// <summary>Tesis baskınının sonucu, yalnız oda raporlarından (<see cref="RaidChain.Result"/>).</summary>
        public RaidResult FacilityRaidResult() => RaidChain.Result(S.Raid.Params, S.Raid.Reports);

        // ---------- Komut ----------
        /// <summary>
        /// Oda raporunun yapısal denetimi (sıra, yön, kadro boyu, bitiş, son oda bilgisi); geçerse null. Tahta yeniden oynatılmaz,
        /// canlar ve makineler doğrulanmaz (belge §11.3).
        /// </summary>
        public string AdvanceRaidRoomCheck(RoomReport r)
        {
            var raid = S.Raid;
            if (raid == null) return Loc.T("Süren baskın yok.");
            if (!RaidChain.IsFacility(raid.Params)) return Loc.T("Baskının odaları yok.");
            var next = RaidChain.Next(raid.Params, raid.Reports);
            if (next == null) return Loc.T("Baskının son odası raporlandı.");
            if (r == null) return Loc.T("Oda raporu eksik.");
            if (r.Room != next.Value.Room || r.Return != next.Value.Return) return Loc.T("Rapor bu odanın değil.");
            if (r.Team == null || r.Team.Count != RaidChain.SquadCount(raid.Params)) return Loc.T("Rapor kadroyla uyuşmuyor.");
            if (r.End != "pass" && r.End != "won" && r.End != "lost") return Loc.T("Raporun bitişi tanınmıyor.");
            if (r.Last != RaidChain.EndsHere(raid.Params, raid.Reports, r)) return Loc.T("Raporun son oda bilgisi tutarsız.");
            for (int i = 0; i < r.Team.Count; i++)
            {
                var member = r.Team[i];
                var previous = raid.Reports.LastOrDefault()?.Team[i];
                if (member.FieldTurned || previous?.FieldTurned == true)
                {
                    // Yalnız gözlenebilir zaman ve biçim doğrulanır. Komut önkontrolü kimliği ele veremez.
                    bool plausible = raid.Params.FieldTurn > 0 && !RaidChain.IsDefense(raid.Params) && raid.Params.MissionKind == "server"
                        && member.Hp == 0 && member.Body == 0 && member.Fell == 3
                        && (previous?.FieldTurned == true ? previous.State == member.State :
                            r.Room == 3 && !r.Return && r.Turns >= raid.Params.FieldTurn && previous?.State == "a");
                    if (!plausible) return Loc.T("Saha raporunda taraf değişimi bu evreyle uyuşmuyor.");
                }
                if (member.Work == FacilityTitle.None && member.Dragged == 0) continue;
                int otherMembers = (int.MaxValue >> Math.Max(0, 31 - r.Team.Count)) & ~(1 << i);
                if (!FacilityTitlesOn || (member.Work & ~FacilityTitles.RoomWorks) != 0 || (member.Dragged & ~otherMembers) != 0)
                    return Loc.T("Oda raporunun unvan işleri geçersiz.");
            }
            return KatarReportCheck(raid.Params, r);
        }

        /// <summary>
        /// Aşama 12 (Katar, K32): raporun Katar alanlarının yapısal denetimi; geçerse null. Takip dizinleri <see cref="RoomReport.Machines"/>
        /// içinde; takip ya da AYIR yalnız geçişle biten, kaçış ya da dönüş yönündeki (hedef odası ya da dönüş ziyareti) Katar odasında ve
        /// ikisi birden değil; kenardan düşen ("k") yalnız düştüğü oda Katar'sa. Savunmada hiçbiri olmaz (şartlar kapalı, K21).
        /// </summary>
        string KatarReportCheck(RaidParams p, RoomReport r)
        {
            var follow = r.Follow ?? new List<int>();
            bool defense = RaidChain.IsDefense(p);
            string EventOf(int room) => room >= 1 && room <= p.Rooms.Count ? FacilityPlan?.Room(p.Rooms[room - 1])?.Event : null;
            if (follow.Any(i => i < 0 || i >= (r.Machines?.Count ?? 0)) || follow.Distinct().Count() != follow.Count)
                return Loc.T("Raporun takip listesi geçersiz.");
            if (follow.Count > 0 || r.Decouple)
            {
                if (follow.Count > 0 && r.Decouple) return Loc.T("Rapor hem takip hem AYIR taşıyor.");
                if (defense || r.End != "pass" || !(r.Return || r.Room == 3) || EventOf(r.Room) != "katar")
                    return Loc.T("Takip ve AYIR yalnız Katar vagonunun dönüş geçişinde olur.");
            }
            if (r.Team.Any(m => m.State == "k" && (defense || EventOf(m.Fell) != "katar")))
                return Loc.T("Kenardan düşme yalnız Katar vagonunda olur.");
            return null;
        }

        /// <summary>
        /// Tesis odası bitti: raporu baskına eklenir (raid_room; a = <see cref="RoomReport.Encode"/>). Simülasyon raporun
        /// Decode(Encode) kopyasını saklar: çağıranın nesnesi saklanmaz, yeniden oynatmayla aynı nesne. Ekibin girdiği oda (teyp) ve
        /// köpeğin kokladığı oda (aşama 8, <see cref="RoomReport.Sniff"/>) bilinir. Fon, RNG ve olay değişmez.
        /// </summary>
        public ActResult AdvanceRaidRoom(RoomReport r) => RunCommand(() =>
        {
            var why = AdvanceRaidRoomCheck(r);
            if (why != null) return ActResult.Fail(why);
            S.Raid.Reports.Add(RoomReport.Decode(r.Encode()));
            // Teyp: ekibin girdiği oda kayda geçer, planı harekât boyunca bilinir (dönüş ziyaretinde zaten bilinir)
            LearnFacilityRoom(S.Raid.Params.Rooms[r.Room - 1]);
            // Kokla (aşama 8, K30): köpeğin kokladığı sıradaki odanın planı da bilinir (kayıttan devamda aynı)
            if (r.Sniff >= 1 && r.Sniff <= S.Raid.Params.Rooms.Count) LearnFacilityRoom(S.Raid.Params.Rooms[r.Sniff - 1]);
            return ActResult.Success();
        }, "raid_room", r?.Encode());

        // ---------- Sunucu ödülü (aşama 8; belge §6.2, §11.3) ----------
        /// <summary>Bölgenin ödül seçenekleri: ödül tablosunda olan her bölgede "tarif" ve "kazanim" (cevap paketlerinde tarif cevap
        /// tarifidir, aşama 10: ekip düzeyinde etkili, defterde temelin sayfası); bilinmeyen bölgede boş. Durum yazmaz.</summary>
        public IReadOnlyList<string> ServerRewardOptions(string regionId) => FacilityGear.Reward(regionId).Perk == null
            ? new string[0] : new[] { FacilityGear.ChoiceRecipe, FacilityGear.ChoicePerk };

        public string ChooseServerRewardCheck(string regionId, string choice)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (Facility == null) return Loc.T("Bu bölgenin bekleyen sunucu ödülü yok.");
            if (S.Raid != null) return Loc.T("Önce süren baskını bitir.");
            if (string.IsNullOrEmpty(regionId) || Facility.PendingReward != regionId) return Loc.T("Bu bölgenin bekleyen sunucu ödülü yok.");
            if (!ServerRewardOptions(regionId).Contains(choice)) return Loc.T("Tarif ya da kazanım seç.");
            return null;
        }

        /// <summary>
        /// Sunucu ödülü (server_reward; a: bölge, b: "tarif" | "kazanim"): tarif <see cref="FacilityState.Recipes"/>'e, kazanım
        /// <see cref="FacilityState.Perks"/>'e eklenir; bekleyen ödül boşalır. Seçilmeyen seçenek o harekâtta hiçbir yolla gelmez
        /// (bölge başına tek ödül, <see cref="LiberationRegionState.Rewarded"/>). Fon ve RNG değişmez. Aşama 10: önlem defteri açıkken cevap
        /// tarifi seçilince temellerinden birinin başlamamış taslak iş emri iptal edilir (ileti bunu söyler).
        /// </summary>
        public ActResult ChooseServerReward(string regionId, string choice) => RunCommand(() =>
        {
            var why = ChooseServerRewardCheck(regionId, choice);
            if (why != null) return ActResult.Fail(why);
            var (recipe, perk) = FacilityGear.Reward(regionId);
            Facility.PendingReward = "";
            if (choice == FacilityGear.ChoiceRecipe)
            {
                if (!Facility.Recipes.Contains(recipe)) Facility.Recipes.Add(recipe);
                // Aşama 10 (K22): cevap tarifi temelin sayfasını değiştirir (+ devre); temelin başlamamış taslak emri ayrılmış girdisiyle
                // tarife uymaz (BoardPlanCheck fişi ve baskın emrini kilitlerdi): silinir, ayrılan hammadde serbest kalır. Başlamış emir sürer
                var draft = CountermeasuresOn && Board != null
                    ? Board.Orders.FirstOrDefault(o => !o.Started && FacilityGear.AnswerBases(recipe).Contains(o.RecipeId)) : null;
                if (draft != null)
                {
                    Board.Orders.Remove(draft);
                    return ActResult.Success(Loc.F("Tarif açıldı: {0}. Taslak iş emri iptal edildi: {1} tarifi değişti.",
                        FacilityGear.RecipeName(recipe), PlanningBoardData.EquipmentName(draft.RecipeId)));
                }
                return ActResult.Success(Loc.F("Tarif açıldı: {0}.", FacilityGear.RecipeName(recipe)));
            }
            if (!Facility.Perks.Contains(perk)) Facility.Perks.Add(perk);
            return ActResult.Success(Loc.F("Kazanım: {0}.", FacilityGear.PerkName(perk)));
        }, "server_reward", regionId, choice);

        // ---------- Önlem defteri (aşama 10; belge §6.3, §11.2) ----------
        /// <summary>Önlem defteri işler: alt bayrak (<see cref="Balance.facilityCountermeasures"/>). Kapalıyken sayım, öğrenme, önlemler, cevap etkileri ve cevap sayfaları yoktur.</summary>
        public bool CountermeasuresOn => B.facilityCountermeasures && Facility != null;

        /// <summary>Sınıfın (<see cref="FacilityCounters.Classes"/>) bugünkü önlem kademesi 0..2; kapalıyken ya da bilinmeyende 0.</summary>
        public int CounterLevel(string cls) => CounterAt(Facility?.Counter, cls);

        /// <summary>Sınıfın birikmiş sayacı (YZ'nin gözlemi); kapalıyken ya da bilinmeyende 0.</summary>
        public int CounterTally(string cls) => CounterAt(Facility?.Tally, cls);

        int CounterAt(List<int> list, string cls)
        {
            int i = FacilityCounters.Index(cls);
            return !CountermeasuresOn || i < 0 || list == null || i >= list.Count ? 0 : list[i];
        }

        /// <summary>Yürürlükteki önlemler (kademe ≥ 1), tablo sırasıyla; kapalıyken boş. Durum yazmaz.</summary>
        public IReadOnlyList<(string Class, int Level)> Countermeasures =>
            FacilityCounters.Classes.Select(c => (Class: c, Level: CounterLevel(c))).Where(x => x.Level >= 1).ToList();

        /// <summary>Emre kopyalanan önlem kademeleri (K23 kalıbı): defter açıkken altı öğe (öğrenme yoksa sıfırlar; tahtada sayımı açar),
        /// kapalıyken boş. Durum yazmaz.</summary>
        List<int> RaidCounters() => CountermeasuresOn
            ? FacilityCounters.Classes.Select(c => CounterLevel(c)).ToList() : new List<int>();

        /// <summary>Emre kopyalanan cevap tarifleri (ekip düzeyinde, K20): defter açıkken <see cref="FacilityState.Recipes"/>'teki cevaplar
        /// açılış sırasıyla, kapalıyken boş.</summary>
        List<string> RaidAnswers() => CountermeasuresOn
            ? Facility.Recipes.Where(FacilityGear.IsAnswer).ToList() : new List<string>();

        /// <summary>Sayaç ve kademe listelerini altı öğeye doldurur (yalnız ilk yazışta; alt bayrak kapalıyken JSON boş kalır).</summary>
        void EnsureCounters()
        {
            int n = FacilityCounters.Classes.Length;
            while (Facility.Tally.Count < n) Facility.Tally.Add(0);
            while (Facility.Counter.Count < n) Facility.Counter.Add(0);
        }

        /// <summary>
        /// Baskının sayımı (raidres; başarısız baskın da sayar): oda raporlarının sınıf sayaçları baskın başına
        /// <see cref="Balance.facilityTallyCap"/> ile kesilir (<see cref="FacilityCounters.RaidTally"/>) ve 2. kademede olmayan sınıflara
        /// eklenir. RNG çekmez.
        /// </summary>
        void AddRaidTally(IList<RoomReport> reports)
        {
            EnsureCounters();
            var t = FacilityCounters.RaidTally(reports, B.facilityTallyCap);
            for (int i = 0; i < t.Length; i++)
                if (Facility.Counter[i] < 2) Facility.Tally[i] += t[i];
        }

        /// <summary>
        /// Öğrenme (bölge ilk kez tamamlandığında, oyun bitmediyse): eşiği geçen en büyük sayacın sınıfı bir kademe alır
        /// (<see cref="FacilityCounters.Learn"/>; eşitlikte en uzun süredir önlem alınmamış, sonra tablo sırası), sayacı sıfırlanır, öbür
        /// sayaçlar kalır; günlüğe yazılır ve sunum için <see cref="LiberationRaidReward.Countermeasure"/> dolar. Aday yoksa önlem yok
        /// (bölgenin hakkı yine harcanmıştır). RNG çekmez.
        /// </summary>
        void LearnCountermeasure(string regionId, LiberationRaidReward reward)
        {
            EnsureCounters();
            int c = FacilityCounters.Learn(Facility.Tally, Facility.Counter, Facility.Learned, B.facilityCounterThreshold);
            if (c < 0) return;
            Facility.Counter[c]++;
            Facility.Tally[c] = 0;
            string cls = FacilityCounters.Classes[c];
            Facility.Learned.Add(new FacilityCounterEntry { Day = S.Day, Region = regionId, Class = cls, Level = Facility.Counter[c] });
            if (reward != null) reward.Countermeasure = cls + ":" + Facility.Counter[c].ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Cevap tarifinin sayfası (K20, K21): defter açıkken temel tarifin açılmış bir cevabı varsa (<see cref="FacilityGear.AnswerFor"/>)
        /// sayfa yerinde dönüşür: ad cevabın, girdiye <see cref="Balance.facilityAnswerCircuits"/> devre eklenir (devre satırı varsa adedi
        /// artar), <see cref="BoardRecipe.Answer"/> dolar. Kimlik, çıktı, süre ve ödül bayrağı temelinki kalır (iş emri, sayfa dizini ve
        /// otomatik yol değişmez). Verilen yeni nesneyi değiştirir ve döndürür.
        /// </summary>
        BoardRecipe AnswerPage(BoardRecipe r)
        {
            if (r == null || !CountermeasuresOn) return r;
            string answer = FacilityGear.AnswerFor(r.Id, Facility.Recipes);
            if (answer == null) return r;
            r.Name = FacilityGear.RecipeName(answer);
            r.Answer = answer;
            int extra = Math.Max(0, B.facilityAnswerCircuits);
            if (extra > 0)
            {
                var circuits = r.Inputs.FirstOrDefault(m => m.Kind == "devre");
                if (circuits != null) circuits.Quantity += extra;
                else r.Inputs.Add(new BoardMaterial { Kind = "devre", Quantity = extra });
            }
            return r;
        }

        // ---------- Bina planı (aşama 5; belge §7.1–7.2) ----------
        /// <summary>Tesis baskınlarının harekât durumu (bina planı bilinen odalar, dinleme puanı, kazanımlar…); harekât yoksa null.</summary>
        public FacilityState Facility => S.Liberation?.Facility;

        /// <summary>Odanın bina planı biliniyor mu (teyp, evrak ya da dinleme nöbeti).</summary>
        public bool FacilityKnown(string roomId) => roomId != null && Facility != null && Facility.Known.Contains(roomId);

        /// <summary>
        /// Odanın planı (yapı, sabit nesneler ve şart parçaları), bugünkü kademe ve olay ayarıyla, baskın emrinin kuracağı tahtanın
        /// aynısı; robot, niyet ve uyanma karesi yoktur (bunlar baskın tohumundan gelir). Aşama 10: önlem defteri açıkken bugünkü önlem
        /// kademeleriyle yapısal kaplama (çift cidar, koku yayıcı; <see cref="Baskin.ApplyCounterCells(Baskin.Cell[,], FacilityRoom, bool, IList{int}, Balance)"/>)
        /// da uygulanır: plan = emirdeki tahta (kırmızı kalemin verisi). Aşama 11: kalıcı tahta açıkken kanadın bugünkü izleri kaplamadan
        /// sonra aynı fonksiyonla (<see cref="FacilityMarks.Apply"/>) uygulanır. Kopya döner; RNG çekmez, durum yazmaz.
        /// </summary>
        public Baskin.Cell[,] FacilityPlanCells(FacilityRoom room)
        {
            if (room == null) return null;
            bool events = B.liberationRegionEvents && room.Event != null;
            var cells = room.PlanCells(Tier, events);
            if (cells != null && CountermeasuresOn) Baskin.ApplyCounterCells(cells, room, events, Facility.Counter, B);
            if (cells != null && MarksOn) FacilityMarks.Apply(cells, room, Facility.Marks, null, events);
            return cells;
        }

        /// <summary>Dinleme nöbetinin açacağı oda: bulunulan bölgede sıradaki kanadın planı bilinmeyen ilk odası (G, A, H sırası).
        /// Bölge bittiyse ya da kanadın üç odası da biliniyorsa null.</summary>
        public FacilityRoom FacilityIntelTarget
        {
            get
            {
                var plan = FacilityPlan;
                var next = plan == null ? null : NextLiberationSector(S.Liberation?.CurrentRegion);
                return next == null ? null : plan.Wing(next.Id).FirstOrDefault(r => !FacilityKnown(r.Id));
            }
        }

        void LearnFacilityRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || Facility == null || Facility.Known.Contains(roomId)) return;
            Facility.Known.Add(roomId);
        }

        /// <summary>Evrak: kanat alınınca aynı tesisin sıradaki kanadının giriş odası bilinir (üçüncü kanatta yok).</summary>
        void LearnFacilityPapers(string regionId)
        {
            var next = NextLiberationSector(regionId);
            if (next != null) LearnFacilityRoom(FacilityMaps.RoomId(next.Id, 1));
            SpendFacilityIntel();
        }

        /// <summary>
        /// Dinleme nöbeti (vardiya kapanışı, çekimsiz): bu vardiya çalışan (yatmayan, canlı) Gözetim görevlisi
        /// <see cref="Balance.facilityWatchIntel"/> puan üretir; puan <see cref="Balance.facilityIntelCost"/>'a varınca
        /// <see cref="FacilityIntelTarget"/> açılır. Gözetim tek kişiliktir (atama takas eder).
        /// </summary>
        void AdvanceFacilityIntel(HashSet<string> working)
        {
            if (Board == null || Facility == null) return;
            if (!Board.People.Any(p => working.Contains(p.Id) && p.Job == "watch")) return;
            // Kulak (aşama 8): Gözetim görevlisinin ek puanı
            Facility.Intel += Math.Max(0, B.facilityWatchIntel) + (Facility.Perks.Contains(FacilityGear.Ear) ? Math.Max(0, B.facilityEarIntel) : 0);
            SpendFacilityIntel();
        }

        /// <summary>Biriken puanla açılacak odaları açar; açılacak oda yoksa puan bir oda bedelinde tavanlanır (geliştirici kararı).</summary>
        void SpendFacilityIntel()
        {
            if (Facility == null) return;
            int cost = Math.Max(1, B.facilityIntelCost);
            for (var target = FacilityIntelTarget; target != null && Facility.Intel >= cost; target = FacilityIntelTarget)
            {
                Facility.Known.Add(target.Id);
                Facility.Intel -= cost;
            }
            if (FacilityIntelTarget == null) Facility.Intel = Math.Min(Facility.Intel, cost);
        }
    }
}
