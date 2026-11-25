using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Tests;

// KOS harekât botu (aşama 8 denge ölçümü; Tools/DengeBotu/README.md). Gerçek Core, baskınları Tests/EditMode/RaidBot.cs oynar.
// Kullanım: camp8.exe <tohumBaş> <tohumSon> <kapı> <olay> <şalter> <etiket>
//   kapı: hic | kefilli | comert | karisik | alyolla · olay: yoksay | tecrit | yolla | ucuz · şalter: hic | salter | durgun
// Ortam: RETIRE=1 sunumun kuralı (yayındaki panoda yuvası olmayan görevler Boşta; ölçümler BUNUNLA yapılır),
//        BAL="alan=değer;alan=değer" Balance'ı değiştirir, ALT=n RaidBot seçenek sayısı (varsayılan 8),
//        PICK=rastgele sınırdan rastgele cephe (varsayılan sira: paket sırasıyla ilk). Üretilmiş harita: BAL="liberationMapGen=1".
// Her harekât için bir satır yazar ("C\t" + anahtar=değer, sekmeyle); özet ozet.py.
// Tesis baskınları aşama 0 (Docs/TESIS-BASKINLARI-TASARIMI.md §11.6): her baskın için ayrıca bir "R\t" satırı yazar (görev,
// kademe, sonuç, tur, yürüyüş, yetenek, düşen, kurtarılan, ölen, kırılan sivil, yeniden alınan sektör); RAIDS=0 kapatır.
// Özet: python3 -I ozet.py --baskin etiket.
// Tek kip (kayıt 21, TESIS belgesi §12 "18 · Tek kip"): her baskın üç odalı tesis zinciridir (RaidBot.PlayFacility); eski tek tahta
// yolu ve onun ölçümü kaldırıldı. BAL'deki emekli alanlar (ApplyBal): raidFacilityEnabled=true sessizce yok sayılır (tek kipe eşit;
// eski tesisN.txt listeleri ve kilit betikleri değişmeden koşar); raidFacilityEnabled=false, liberationLootExtraTurns ve
// liberationServerExtraTurns "emekli alan" hatasıyla durur (tek tahta listeleri asama8, tesis0 ve yapılandırmaları TARİHSEL).
// Sahadaki sızmacının C# varsayılanı açık; aşama 13–17'nin "saha kapalı" ölçümleri BAL'de infiltratorFieldEnabled=false ister.
// Tesis baskınları aşama 2 (README "Tesis baskınları"): DEV=kir|hic|hep botun donanım politikası (varsayılan kir); REPLAY=1 her
// harekâtın sonunda kaydı yeniden oynatıp durumu karşılaştırır (C satırına replay=ok). R satırına oda başına sayaçlar eklenir.
// Tesis aşama 11 (README "Kuşatma ve savunma"): SAVUN=hep|hic|yok kuşatma kararı (CampaignBot.Savun), CEKIL=hic|hep|baski savunmada
// içeri çekilme (RaidBot.Retreat); savunma baskını başına bir "S\t" satırı (R süzgecine girmez); R ve C satırına iz ve kuşatma
// anahtarları. Savunma ve izler BAL ile kapanır: facilityDefenseEnabled=false, facilityMarks=false.
// Tesis aşama 12 (README "Katar ve Yalpa"): AYIR=hep|hic|takip Katar'ın dönüş geçidinde AYIR (RaidBot.Detach), KENAR=hic|kacis kendi
// üyesini kenardan düşürmenin bedeli (RaidBot.Edge); R satırının sonuna Katar ve Yalpa sayaçları, C satırının sonuna toplamları. Alt
// bayraklar BAL ile kapanır: facilityKatar=false, facilityYalpa=false (anahtarlar yine yazılır, sabit değerle).
public static class Program
{
    static readonly bool OldBot = Environment.GetEnvironmentVariable("OLDBOT") == "1";

    // Camp8r (aşama 8 doğrulaması): RETIRE=1 sunumun RetireJobs'unu taklit eder: yayındaki panoda yuvası olmayan görevler
    // (care, gather, watch, contact) kilitsiz planda Boşta'ya iner, bot onlara kimseyi atamaz.
    static readonly bool Retire = Environment.GetEnvironmentVariable("RETIRE") == "1";
    // ALT=n: RaidBot seçenek sayısı (varsayılan 8; "usta" bot sınaması için büyütülür).
    static readonly int Alt = int.TryParse(Environment.GetEnvironmentVariable("ALT"), out int alt) && alt > 0 ? alt : 8;
    // DEV=kir|hic|hep: tesis baskınında botun donanım politikası (RaidBot.Devices)
    static readonly string Dev = Environment.GetEnvironmentVariable("DEV") is string dv && dv != "" ? dv : "kir";
    // DUSEN=hep|hic: tesis baskınında botun düşen politikası (RaidBot.Fallen, aşama 6; varsayılan hep)
    static readonly string Dusen = Environment.GetEnvironmentVariable("DUSEN") is string du && du != "" ? du : "hep";
    // REPLAY=1: harekâtın sonunda kayıt yeniden oynatılır ve durum karşılaştırılır (tutmazsa istisna)
    static readonly bool ReplayCheck = Environment.GetEnvironmentVariable("REPLAY") == "1";
    // Tesis aşama 8 (README "Tesis ekipmanı"): ODUL=tarif|kazanim sunucu ödülü (varsayılan tarif; cevap paketlerinde de tarif: "hep
    // tarif" kazanımsız kalır), ARAC=yok|tek|donus ekipman üretimi ve takma (varsayılan donus), GEAR=kullan|hic botun ekipman ve birim
    // eylemleri (RaidBot.Gear). Planlama Tests/EditMode/CampaignBot.cs'te (FacilityBotTests ile ortak, K29).
    static readonly string Odul = Environment.GetEnvironmentVariable("ODUL") is string od && od != "" ? od : "tarif";
    static readonly string Arac = Environment.GetEnvironmentVariable("ARAC") is string ar && ar != "" ? ar : "donus";
    static readonly string GearPolicy = Environment.GetEnvironmentVariable("GEAR") is string ge && ge != "" ? ge : "kullan";
    // Tesis aşama 10 (README "Önlem defteri"): ODUL=onlem cevap tarifini yalnız önlemi olan sınıfta seçer (CampaignBot.AnswerChoice);
    // TAK=hep|hic üretilen ekipmanı saha rollerine takar ya da takmaz (üretim yolunun sınaması; R satırında yalnız hic'te tak=hic).
    // Önlem defterinin açık/kapalısı ve pusu isabetinin sayımı BAL ile: facilityCountermeasures=false, facilityTallyAmbush=true.
    static readonly string Tak = Environment.GetEnvironmentVariable("TAK") is string tk && tk != "" ? tk : "hep";
    // Tesis aşama 11: SAVUN=hep|hic|yok kuşatma kararı (varsayılan hep; yok: komut yok, kapanışta kendiliğinden bırakma), CEKIL=hic|hep|baski
    // savunmada içeri çekilme (varsayılan hic). BAL.txt'e yazılmaz; S satırında ve C satırında görünür.
    static readonly string Savun = Environment.GetEnvironmentVariable("SAVUN") is string sv && sv != "" ? sv : "hep";
    static readonly string Cekil = Environment.GetEnvironmentVariable("CEKIL") is string ck && ck != "" ? ck : "hic";
    // Tesis aşama 12: AYIR=hep|hic|takip (varsayılan hep), KENAR=hic|kacis (varsayılan hic). BAL.txt'e yazılmaz; R ve C satırında
    // görünür.
    static readonly string Ayir = Environment.GetEnvironmentVariable("AYIR") is string ay && ay != "" ? ay : "hep";
    static readonly string Kenar = Environment.GetEnvironmentVariable("KENAR") is string ke && ke != "" ? ke : "hic";
    // BOTW="alan=değer;…": RaidBot.Weights'i değiştirir (yalnız ağırlık taraması; ölçüm satırlarında yok, BAL.txt'e yazılmaz)
    static readonly string BotWeights = Environment.GetEnvironmentVariable("BOTW");
    static void ApplyWeights(RaidBot.Weights w, string spec)
    {
        if (string.IsNullOrEmpty(spec)) return;
        foreach (var kv in spec.Split(';'))
        {
            if (kv.Trim() == "") continue;
            var parts = kv.Split('=');
            var fi = typeof(RaidBot.Weights).GetField(parts[0].Trim(), BindingFlags.Public | BindingFlags.Instance);
            if (fi == null) throw new Exception("ağırlık yok: " + parts[0]);
            if (fi.FieldType == typeof(int)) fi.SetValue(w, int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
            else fi.SetValue(w, double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
        }
    }
    static CampaignBot planner;
    static void ChooseReward(Simulation sim) => planner.ChooseReward(sim);
    static void Plan(Simulation sim) => planner.Plan(sim);

    // ---- ölçüm sayaçları (harekât başına sıfırlanır) ----
    static Dictionary<string, double> M;
    static void Add(string k, double v) { M.TryGetValue(k, out double o); M[k] = o + v; }
    static string lastHit;
    // Paket × kademe (üretilmiş harita ölçümü): paketin kaçıncı seçilen bölge olduğu, ilk baskındaki kademesi ve başarısız baskınları
    static List<string> pkgOrder;
    static Dictionary<string, int> pkgTier, pkgFails;
    static readonly string Pick = Environment.GetEnvironmentVariable("PICK") ?? "sira";
    static Rng pickRng;
    // Aşama 0 tabanı: baskın satırları (R) ve yeniden alınan sektör/bölge takibi (karşı saldırıdan sonra)
    static readonly bool RaidLines = Environment.GetEnvironmentVariable("RAIDS") != "0";
    static string runKey;
    static HashSet<string> everCaptured, everCompleted;
    // Aşama 11: kuşatma girdisinin kaynağı (günlük dizini → vardiya, baskin, olay, kapi), kanadın son saldırı baskını başarısız mıydı,
    // −3'lük kaybın (iki oda terk + kayıp) günü
    static Dictionary<int, string> siegeSrc;
    static Dictionary<string, bool> lastFailed;
    static int loss2Day;
    static void NoteSieges(Simulation sim, int before, string src)
    {
        if (sim.Facility == null) return;
        for (int i = before; i < sim.Facility.Sieges.Count; i++) if (!siegeSrc.ContainsKey(i)) siegeSrc[i] = src;
    }
    static int SiegeCount(Simulation sim) => sim.Facility?.Sieges.Count ?? 0;

    static bool Raid(Simulation sim)
    {
        var why = sim.PlanningRaidCheck();
        if (why != null)
        {
            Add("noRaid", 1);
            string k = why.Contains("gerekiyor") ? "nrKit" : why.Contains("üç sağlıklı") ? "nrSquad" : why.Contains("ayrı tutuluyor") ? "nrFeud"
                : why.Contains("Kısıtlı") ? "nrRestrict" : "nrOther";
            Add(k, 1);
            if (Environment.GetEnvironmentVariable("WHY") == "1")
                Console.Error.WriteLine(sim.S.Day + " " + k + " | " + string.Join(" ; ", sim.Board.People.Select(p => p.Name.Split(' ')[0] + ":" + p.Job + (p.Alive ? "" : "+") + " hp" + p.Hp + " d" + p.Down + (p.Restricted ? " R" : "") + (p.Isolated ? " I" : "") + " [" + string.Join(",", sim.Board.Equipment.Where(e => e.OwnerId == p.Id).Select(e => e.Kind + (e.Bound ? "*" : "") + e.Charges)) + "]"))
                    + " | depo " + string.Join(",", sim.Board.Equipment.Where(e => e.OwnerId == null).Select(e => e.Kind + e.Charges)));
            return false;
        }
        var lib = sim.S.Liberation;
        var region = sim.LiberationRegion(lib.CurrentRegion);
        if (region == null || region.Completed)
        {
            // PICK=rastgele: sınırdan bot akışıyla rastgele aday (oyuncunun sırası); varsayılan sira: paket sırasıyla ilk
            var frontier = sim.LiberationFrontier.ToList();
            var f = Pick == "rastgele" && frontier.Count > 0 ? frontier[(int)Math.Floor(pickRng.Next() * frontier.Count)] : frontier.FirstOrDefault();
            if (f == null || !sim.SelectLiberationRegion(f).Ok) return false;
            if (!pkgOrder.Contains(f)) pkgOrder.Add(f);
        }
        var sector = sim.NextLiberationSector(lib.CurrentRegion);
        if (sector == null) return false;
        // Aşama 11: karar vardiyasında baskın emri kilitli (SAVUN=yok), komutla bırakılan kanada o vardiya baskın yok (Core okuyucuları)
        if (sim.DefenseOn && sim.SiegeDecisionDue) { Add("nrSiege", 1); return false; }
        if (sim.DefenseOn && sim.YieldedToday(sector.Id)) { Add("nrYield", 1); return false; }
        if (!sim.StartLiberationRaid(sector.Id).Ok) return false;
        if (!pkgTier.ContainsKey(sector.Region)) pkgTier[sector.Region] = sim.S.Raid.Params.Tier;
        bool again = everCaptured.Contains(sector.Id), regionAgain = everCompleted.Contains(sector.Region);
        // Aşama 0: oyuncu turu sayısı ve uzman hamleleri (yürüyüş, yetenek) RaidBot'un tur kancasından; oyun kodu değişmez
        int plTurns = 0, moves = 0, acts = 0, dogSteps = 0, capSteps = 0, emit = 0;
        var bot = new RaidBot { Alternatives = Alt, Devices = Dev, Fallen = Dusen, Gear = GearPolicy, Detach = Ayir, Edge = Kenar };
        ApplyWeights(bot.W, BotWeights);
        // Aşama 8: köpeğin ve tutsağın adımları uzman hamlesinden ayrı sayılır (birim, plan uygulanmadan önceki durumdan)
        bot.OnTurn = (st, plan) =>
        {
            plTurns++;
            foreach (var s in plan)
            {
                var su = st.Units.FirstOrDefault(x => x.Id == s.Unit);
                int n = (s.Move ? 1 : 0) + (s.Act ? 1 : 0);
                if (Baskin.IsDog(su)) dogSteps += n;
                else if (Baskin.IsCaptive(su)) capSteps += n;
                else { if (s.Move) moves++; if (s.Act) acts++; }
            }
            // Aşama 10: turun başında koku yayıcının susturduğu köpek (köpek-tur; önlemsiz tahtada hep 0)
            foreach (var d in st.Units) if (Baskin.IsDog(d) && !d.Dead && Baskin.OnBoard(d) && Baskin.EmitterBlocks(st, d)) emit++;
        };
        Baskin.BState B;
        RaidResult result;
        // Tesis baskını (aşama 2; tek kip): oda zinciri; oda başına tur, yürüyüş, yetenek ve sayaçlar OnRoom'dan
        var rooms = new List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)>();
        int maxSum = 0;
        // Aşama 8: emirde açık ekipman tarifleri (kullanım payının paydası) ve kadronun yardımcı eşyası; baskından önceki ekipman
        string rec = string.Join("+", CampaignBot.GearRecipes(sim).Select(r => r.Output));
        string carry = string.Join("/", sim.S.Raid.Params.Squad.Select(q => q.Gear ?? "-"));
        var gearBefore = sim.Board.Equipment.Where(e => FacilityGear.IsGear(e.Kind)).Select(e => (e.Id, e.Kind, e.OwnerId)).ToList();
        // Aşama 10: emir (önlem kademeleri, cevaplar, kadro) ve baskın başındaki yarım tayın
        var rp = sim.S.Raid.Params;
        bool half = sim.HalfRationActive;
        Baskin.BState lastBoard = null;
        int m0 = 0, a0 = 0;
        bot.OnRoom = (b, rep, t) => { lastBoard = b; maxSum += b.MaxTurns; rooms.Add((b, rep, t, moves - m0, acts - a0)); m0 = moves; a0 = acts; };
        result = bot.PlayFacility(sim);
        B = lastBoard;
        int t0 = sim.S.Trust, c0 = sim.S.Counterattacks, dead0 = sim.Board.People.Count(p => !p.Alive), tier = sim.Tier;
        var reports = sim.S.Raid.Reports.ToList();
        // Aşama 11: kanadın önceki saldırı baskını başarısızdı (tekrar baskını), baskının kurduğu kuşatma
        bool retry = lastFailed.TryGetValue(sector.Id, out bool lf) && lf;
        int sg0 = SiegeCount(sim);
        var res = sim.ResolveRaid(result);
        if (!res.Ok) throw new Exception("raid " + res.Msg);
        NoteSieges(sim, sg0, "baskin");
        ChooseReward(sim);
        var log = sim.S.RaidLog.Last();
        var rw = log.LiberationReward;
        int civ = Math.Max(0, log.Collateral) * sim.B.liberationTrustPerCivilian;
        int cnt = sim.S.Counterattacks - c0;
        Add("raids", 1);
        if (!log.Success) { Add("fails", 1); pkgFails.TryGetValue(log.Region, out int pf); pkgFails[log.Region] = pf + 1; }
        if (log.MissionKind == "server") { Add("srvRaids", 1); if (!log.Success) Add("srvFails", 1); }
        Add("collat", Math.Max(0, log.Collateral));
        Add("civLoss", civ);
        Add("counterRaid", cnt);
        // Aşama 11: yeniden tamamlanan bölge savunma açıkken bölge güvenini vermez (§9.3, RegionRepeat; kapalıyken hep false)
        if (log.Success) { Add("civGain", rw.TrustFromCivilians); if (rw.RegionCompleted && !rw.RegionRepeat) Add("regionGain", sim.B.liberationTrustPerRegion); }
        Add("raidDeaths", sim.Board.People.Count(p => !p.Alive) - dead0);
        Add("raidsT" + tier, 1); if (!log.Success) Add("failsT" + tier, 1); Add("collatT" + tier, Math.Max(0, log.Collateral));
        Add("deadT" + tier, sim.Board.People.Count(p => !p.Alive) - dead0);
        Add("downT" + tier, rw.Squad.Count(x => x.Down && !x.Dead));
        // Aşama 0: tur, hamle, düşen; "kurtarılan" düşüp ölmeyen uzmandır (tesiste düşen ancak çıkarılırsa yaşar)
        int down = result.Down, rescued = rw.Squad.Count(x => x.Down && !x.Dead), died = sim.Board.People.Count(p => !p.Alive) - dead0;
        Add("turns", result.Turns); Add("plTurns", plTurns); Add("moves", moves); Add("acts", acts);
        Add("down", down); Add("rescued", rescued);
        if (again) { Add("recapRaids", 1); if (log.Success) Add("recapSectors", 1); }
        if (log.Success) everCaptured.Add(sector.Id);
        if (log.Success && rw.RegionCompleted)
        {
            // Yeniden tamamlanan bölge bugün bölge fonunu ve bölge güvenini yeniden verir (tesis belgesi §9.3 bunu keser)
            // Güven: bölge güveni sivil kazancı/kaybıyla tek ChangeTrust'ta toplanır ve tavanla kırpılır; etkin pay sayılır
            int eff = sim.S.Trust - Math.Max(0, Math.Min(sim.TrustMax, t0 + rw.TrustFromCivilians - civ));
            // Aşama 11: verilen fon gerçek değerle (savunma açıkken yeniden tamamlanan bölge fon vermez: recapDenied)
            if (regionAgain)
            {
                Add("recapRegions", 1); Add("recapFunds", rw.RegionRepeat ? 0 : sim.B.liberationCaptureFunds); Add("recapTrust", eff);
                if (rw.RegionRepeat) Add("recapDenied", 1);
            }
            everCompleted.Add(sector.Region);
        }
        FacilityCounters(sim, reports, rooms);
        // Aşama 11: izlerin defteri (I3) ve emirde görülen izler; kanadın son saldırı sonucu
        Add("marksMade", rw.MarksNew); Add("marksGone", rw.MarksGone); Add("marksWiped", rw.MarksWiped);
        Add("marksSeen", rp.Marks?.Count ?? 0);
        if (retry) { Add("retryRaids", 1); if (log.Success) Add("retryWins", 1); }
        lastFailed[sector.Id] = !log.Success;
        // Aşama 8: ölenle silinen ekipman (kelepçenin tükenmesi ayrı) ve kullanılan türler
        foreach (var g in gearBefore)
            if (!sim.Board.Equipment.Any(e => e.Id == g.Id))
            {
                var owner = g.OwnerId == null ? null : sim.Board.Person(g.OwnerId);
                Add(owner != null && !owner.Alive ? "gearLost" : g.Kind == FacilityGear.Clamp ? "clampSpent" : "gearGone", 1);
            }
        foreach (var kv in GearUse(rooms)) if (kv.Value > 0) usedKinds.Add(kv.Key);
        Add("dogSteps", dogSteps); Add("capSteps", capSteps);
        // Aşama 10: önlem altında oynanan baskın (sınıf × kademe) ve cezalı taşıma (taşınan eşyanın sınıfında önlem var)
        if (rp.Counters != null && rp.Counters.Count == 6)
        {
            for (int i = 0; i < 6; i++) if (rp.Counters[i] >= 1) Add("cmRaids_" + Corrigible.Core.FacilityCounters.Classes[i] + "_" + rp.Counters[i], 1);
            foreach (var q in rp.Squad ?? new List<SquadSlot>())
            {
                if (string.IsNullOrEmpty(q.Gear)) continue;
                Add("carryItems", 1);
                if (GearClasses(q.Gear).Any(i => rp.Counters[i] >= 1)) Add("penCarry", 1); else Add("penCarry", 0);
            }
        }
        if (RaidLines)
            Console.WriteLine("R\t" + runKey + "\tday=" + sim.S.Day + "\tkind=" + log.MissionKind + "\ttier=" + tier + "\tregion=" + log.Region
                + "\tok=" + (log.Success ? 1 : 0) + "\tretreat=" + (result.Retreat ? 1 : 0) + "\tturns=" + result.Turns
                + "\tmax=" + maxSum + "\tplTurns=" + plTurns + "\tmoves=" + moves + "\tacts=" + acts + "\tdown=" + down
                + "\trescued=" + rescued + "\tdied=" + died + "\tcollat=" + Math.Max(0, log.Collateral) + "\tagain=" + (again ? 1 : 0)
                + "\tevent=" + (B.Params.Event ?? "-") + "\tend=" + RaidEnd(B, result)
                + FacilityLine(sim, reports, rooms) + GearLine(rec, carry, rooms, dogSteps, capSteps)
                + CounterLine(sim, rp, reports, rooms, rw, emit, half) + MarkRaidLine(rp, rw, retry)
                + KatarLine(sim, rp, reports, rooms, result, rw));
        if (sim.S.Ending == "dagilma") lastHit = civ > 0 && cnt > 0 ? "sivil+karsi" : cnt > 0 ? "karsi_baskin" : "sivil";
        return true;
    }

    // ---- Katar ve Yalpa (aşama 12) ----
    // Harekât toplamları (C satırının sonu; M'ye girmez: M'nin sıralı anahtarları aşama 11 satırıyla aynı kalır)
    static Dictionary<string, double> K12;
    static void Add12(string k, double v) { K12.TryGetValue(k, out double o); K12[k] = o + v; }
    static readonly string[] MissKeys = { "tek", "acted", "gate", "pol", "auto" };
    static readonly string[] SlideKeys = { "p", "e", "govde", "yuk", "hurda", "diger" };
    static readonly string[] EdgeSrcKeys = { "emp", "sab", "kosu", "evre", "diger" };
    static int Sum(List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms, Func<RaidBot.Tally, Dictionary<string, int>> map, string k) =>
        rooms.Sum(x => map(x.t).TryGetValue(k, out int n) ? n : 0);

    // Aşama 12, R satırı (en sona): ayir/kenar politikalar; det verilen AYIR, detNo Katar'ın dönüş geçişinde verilmeyen
    // AYIR'ın nedeni (tek: ayakta Teknisyen yok, gate: Teknisyen kapıda değil, acted: yeteneği harcandı, pol: politika istemedi, auto:
    // kendiliğinden geçiş); fol raporlara yazılan takipçi, folRooms takipçiyle oynanan ve takipsiz temiz sayılacak dönüş odası; edgeM /
    // edgeP kenardan düşen makine / uzman oda başına (G/A/H gidiş, a/g dönüş), edgeX köpek/tutsak/aygıt, edgeSrc kenardan düşen makinenin
    // kaynağı; eDied kenardan düşüp ölen (0 olmalı), eInj kenardan düşenlerin yatışı (vardiya, toplam); tilt oda başına yatış, slide kayan
    // şeyler türe göre, tiltHit yatışta çarpan birim (uzman/makine); evP/evE olay hasarı (ekip/makine, odaların BState sayaçları; her olayda)
    static string KatarLine(Simulation sim, RaidParams rp, List<RoomReport> reps, List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms,
        RaidResult result, LiberationRaidReward rw)
    {
        int S(Func<RaidBot.Tally, int> f) => rooms.Sum(x => f(x.t));
        string PerRoom(Func<RaidBot.Tally, int> f) => string.Join("/", rooms.Select(x => RoomKey(x.rep) + f(x.t)));
        int fol = 0, folRooms = 0;
        for (int i = 0; i < reps.Count; i++)
        {
            var r = reps[i];
            if (r.Follow == null || r.Follow.Count == 0) continue;
            fol += r.Follow.Count;
            if (r.Room > 1 && RaidChain.IsClean(RaidChain.LastVisit(reps.Take(i + 1).ToList(), r.Room - 1))) folRooms++;
        }
        var over = result.Overboard ?? new List<int>();
        var squad = rp.Squad ?? new List<SquadSlot>();
        int eDied = over.Count(i => rw != null && i >= 0 && i < rw.Squad.Count && rw.Squad[i].Dead);
        int eInj = over.Where(i => i >= 0 && i < squad.Count).Sum(i => sim.Board.Person(squad[i].PersonId)?.Down ?? 0);
        int det = S(t => t.Detaches), edgeM = S(t => t.EdgeMachines), edgeP = S(t => t.EdgeTeam);
        int tiltP = S(t => t.TiltHurtP), slides = SlideKeys.Sum(k => Sum(rooms, t => t.Slides, k));
        string ev = rp.Event ?? "-";
        Add12("katarRaids", ev == "katar" ? 1 : 0); Add12("yalpaRaids", ev == "yalpa" ? 1 : 0);
        Add12("det", det); Add12("fol", fol); Add12("folRooms", folRooms); Add12("edgeM", edgeM); Add12("edgeP", edgeP);
        Add12("fellInj", eInj); Add12("eDied", eDied); Add12("slides", slides); Add12("tiltHitP", tiltP);
        foreach (var k in MissKeys) Add12("detNo_" + k, Sum(rooms, t => t.DetachMiss, k));
        return "\tayir=" + Ayir + "\tkenar=" + Kenar
            + "\tdet=" + det + "\tdetNo=" + string.Join("/", MissKeys.Select(k => k + ":" + Sum(rooms, t => t.DetachMiss, k)))
            + "\tfol=" + fol + "\tfolRooms=" + folRooms
            + "\tedgeM=" + PerRoom(t => t.EdgeMachines) + "\tedgeP=" + PerRoom(t => t.EdgeTeam) + "\tedgeX=" + S(t => t.EdgeOther)
            + "\tedgeSrc=" + string.Join("/", EdgeSrcKeys.Select(k => k + ":" + Sum(rooms, t => t.EdgeBy, k)))
            + "\teDied=" + eDied + "\teInj=" + eInj
            + "\ttilt=" + PerRoom(t => t.Tilts)
            + "\tslide=" + string.Join("/", SlideKeys.Select(k => k + ":" + Sum(rooms, t => t.Slides, k)))
            + "\ttiltHit=p:" + tiltP + "/e:" + S(t => t.TiltHurtE)
            + "\tevP=" + rooms.Sum(x => x.b.EvDamageTeam) + "\tevE=" + rooms.Sum(x => x.b.EvDamageMachines);
    }

    // ---- kalıcı tahta ve savunma (aşama 11) ----
    // Emirdeki izler oda başına (G/A/H; RaidParams.Rooms sırasıyla)
    static string MarksPerRoom(RaidParams rp)
    {
        var rooms = rp.Rooms ?? new List<string>();
        var marks = rp.Marks ?? new List<FacilityMark>();
        return string.Join("/", rooms.Select((id, i) => "GAH"[Math.Min(i, 2)] + marks.Count(m => m.Room == id).ToString()));
    }

    // Aşama 11, R satırı (sona): retry kanadın önceki saldırı baskını başarısızdı, mk emirdeki izler oda başına, mkNew bu
    // baskının bıraktığı yeni iz, mkKind emirdeki iz türleri (nesne: soğutma + kopya dolabı, engel: tel + kalas)
    static string MarkRaidLine(RaidParams rp, LiberationRaidReward rw, bool retry)
    {
        var m = rp.Marks ?? new List<FacilityMark>();
        int K(params string[] kinds) => m.Count(x => kinds.Contains(x.Kind));
        return "\tretry=" + (retry ? 1 : 0) + "\tmk=" + MarksPerRoom(rp) + "\tmkNew=" + (rw?.MarksNew ?? 0)
            + "\tmkKind=duvar:" + K("duvar") + "/kepenk:" + K("kepenk") + "/donanim:" + K("donanim") + "/nesne:" + K("cool", "spawner")
            + "/engel:" + K("tel", "kalas") + "/hurda:" + K("hurda");
    }

    // Kuşatma kararı (CampaignBot.Siege) ve savunma emri verildiyse savunmanın oynanması (S satırı). Savunma oynandıysa true (o vardiya
    // baskın yok). Savunma kapalıyken (facilityDefenseEnabled=false) hiçbir şey yapmaz.
    static bool Siege(Simulation sim)
    {
        if (!sim.DefenseOn) return false;
        var entry = sim.PendingSiege;
        int index = entry == null ? -1 : sim.Facility.Sieges.IndexOf(entry);
        bool due = sim.SiegeDecisionDue;
        string region0 = sim.S.Liberation.CurrentRegion;
        if (!planner.Siege(sim))
        {
            if (due && sim.S.Ending == "dagilma") lastHit = "birak";
            return false;
        }
        PlayDefense(sim, entry, index, region0);
        return true;
    }

    static void PlayDefense(Simulation sim, FacilitySiegeEntry entry, int index, string region0)
    {
        var rp = sim.S.Raid.Params;
        int day = sim.S.Day, tier = rp.Tier;
        int plTurns = 0, moves = 0, acts = 0, dogSteps = 0, capSteps = 0;
        var bot = new RaidBot { Alternatives = Alt, Devices = Dev, Fallen = Dusen, Gear = GearPolicy, Retreat = Cekil, Detach = Ayir, Edge = Kenar };
        ApplyWeights(bot.W, BotWeights);
        bot.OnTurn = (st, plan) =>
        {
            plTurns++;
            foreach (var s in plan)
            {
                var su = st.Units.FirstOrDefault(x => x.Id == s.Unit);
                int n = (s.Move ? 1 : 0) + (s.Act ? 1 : 0);
                if (Baskin.IsDog(su)) dogSteps += n;
                else if (Baskin.IsCaptive(su)) capSteps += n;
                else { if (s.Move) moves++; if (s.Act) acts++; }
            }
        };
        var rooms = new List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)>();
        int m0 = 0, a0 = 0;
        bot.OnRoom = (b, rep, t) => { rooms.Add((b, rep, t, moves - m0, acts - a0)); m0 = moves; a0 = acts; };
        string rec = string.Join("+", CampaignBot.GearRecipes(sim).Select(r => r.Output));
        string carry = string.Join("/", rp.Squad.Select(q => q.Gear ?? "-"));
        var result = bot.PlayFacility(sim);
        var reports = sim.S.Raid.Reports.ToList();
        int t0 = sim.S.Trust, dead0 = sim.Board.People.Count(p => !p.Alive);
        var res = sim.ResolveRaid(result);
        if (!res.Ok) throw new Exception("savunma " + res.Msg);
        ChooseReward(sim);
        var log = sim.S.RaidLog.Last();
        var rw = log.LiberationReward;
        int died = sim.Board.People.Count(p => !p.Alive) - dead0, trust = sim.S.Trust - t0;
        int left = RaidChain.Abandoned(rp, reports);
        bool ok = log.Success;
        var last = reports[reports.Count - 1];
        var final = rooms[rooms.Count - 1].b;
        // Dalgalar ve makineler oda başına (bir dalga tek odaya gelir), düşürülen makine, rölenin son canı (hedef odasına girildiyse)
        int waves = rooms.Sum(x => Baskin.DefenseWaves(x.b).Count(w => w.Arrived > 0));
        int wm = rooms.Sum(x => Baskin.DefenseWaves(x.b).Sum(w => w.Arrived));
        int kills = rooms.Sum(x => x.b.Units.Count(u => u.Side == 'e' && u.Dead && !Baskin.IsGadget(u)));
        int relay = Baskin.RelayHp(final), relayMax = Baskin.RelayMax(final);
        Add("defWin", ok ? 1 : 0); Add("defLoss", ok ? 0 : 1);
        Add("defLeft" + Math.Min(2, left), 1);
        if (!ok && left >= 2) { Add("defLoss2", 1); loss2Day = day; }
        Add("defTrust", trust); Add("defDeaths", died); Add("defExtracted", result.Extracted?.Count ?? 0); Add("defTurns", result.Turns);
        Add("marksMade", rw.MarksNew); Add("marksGone", rw.MarksGone); Add("marksWiped", rw.MarksWiped); Add("marksSeen", rp.Marks?.Count ?? 0);
        foreach (var kv in GearUse(rooms)) if (kv.Value > 0) usedKinds.Add(kv.Key);
        if (sim.S.Ending == "dagilma") lastHit = "savunma";
        if (!RaidLines) return;
        var region = sim.LiberationRegion(rp.Region);
        int wing = region == null ? 0 : region.Sectors.FindIndex(x => x.Id == rp.SectorId) + 1;
        int S(Func<RaidBot.Tally, int> f) => rooms.Sum(x => f(x.t));
        bool tally = rp.Counters != null && rp.Counters.Count == 6;
        int cap = Math.Max(0, sim.B.facilityTallyCap);
        string tl = tally ? string.Join("/", Corrigible.Core.FacilityCounters.RaidTally(reports, cap)) : "-";
        string tlb = tally ? string.Join("/", RaidBot.TallyMap(rooms.Select(x => x.t), rp.Counters, sim.B.facilityTallyAmbush).Select(v => Math.Min(cap, v))) : "-";
        bool again = sim.S.RaidLog.Count(l => l.SectorId == rp.SectorId && l.Success && l.Mode != Simulation.DefenseMode) > 1;
        Console.WriteLine("S\t" + runKey + "\tday=" + day + "\ttier=" + tier + "\tregion=" + rp.Region + "\twing=" + wing
            + "\tsetup=" + (entry?.Day ?? -1) + "\tsrc=" + (index >= 0 && siegeSrc.TryGetValue(index, out string src) ? src : "-")
            + "\tsame=" + (rp.Region == region0 ? 1 : 0) + "\tok=" + (ok ? 1 : 0) + "\tleft=" + left + "\tendRoom=" + "GAH"[Math.Min(2, last.Room - 1)]
            + "\tturns=" + result.Turns + "\tplTurns=" + plTurns + "\tmoves=" + moves + "\tacts=" + acts
            + "\trt=" + string.Join("/", rooms.Select(x => "GAH"[Math.Min(2, x.rep.Room - 1)].ToString() + (Math.Min(x.b.Turn, x.b.MaxTurns) - Baskin.StartTurn(x.b) + 1)))
            + "\twaves=" + waves + "\twm=" + wm + "\tkills=" + kills
            + "\trelay=" + (relay >= 0 ? relay.ToString() : "-") + "\trelayHit=" + (relay >= 0 ? (relayMax - relay).ToString() : "-")
            + "\tdown=" + result.Down + "\tdied=" + died + "\textracted=" + (result.Extracted?.Count ?? 0) + "\ttrust=" + trust
            + "\tcollat=" + Math.Max(0, log.Collateral) + "\tmk=" + MarksPerRoom(rp) + "\twiped=" + rw.MarksWiped
            + "\tax=pusu:" + S(t => t.Ambush) + "/kosu:" + S(t => t.Rush) + "/surukle:" + S(t => t.Drag) + "/firlat:" + S(t => t.Throw)
            + "/ortak:" + S(t => t.Joint) + "/menfez:" + S(t => t.Vent) + "\tamb=" + S(t => t.AmbushHits)
            + GearLine(rec, carry, rooms, dogSteps, capSteps)
            + "\ttl=" + tl + "\ttlb=" + tlb + "\tcm=" + Levels(rp.Counters)
            + "\tsavun=" + Savun + "\tcekil=" + Cekil + "\tdusen=" + Dusen + "\tdev=" + Dev
            + "\tagain=" + (again ? 1 : 0) + "\tperkSiper=" + ((rp.Perks ?? new List<string>()).Contains(FacilityGear.Cover) ? 1 : 0)
            // Aşama 12: savunmada Katar ve Yalpa kapalı (K21): kenar, takip ve yatış 0 olmalı
            + "\tedgeM=" + S(t => t.EdgeMachines) + "\tedgeP=" + S(t => t.EdgeTeam) + "\tfol=" + reports.Sum(r => r.Follow?.Count ?? 0)
            + "\ttilt=" + S(t => t.Tilts));
    }

    // Baskının bitişi (metin değil durum; Mono konsolu Türkçe karakteri bozar): zafer, sure (süre doldu; tesisin giriş ve ara
    // odasında süre sonu geri çekilme de yazar, bu yüzden önce sınanır), cekilme, tasiyici (ikmal kaçışında malzemeyi taşıyan düştü,
    // ötekiler çıktı), ekip (bütün ekip düştü), diger (bugün yalnız ikmal kasasının imhası). B son odanın tahtasıdır.
    static string RaidEnd(Baskin.BState B, RaidResult r) =>
        r.Success ? "zafer" : B.Turn > B.MaxTurns ? "sure" : r.Retreat ? "cekilme" : B.Escape && B.Params.MissionKind == "loot" ? "tasiyici"
        : B.Units.Any(u => Baskin.IsSpecialist(u) && !u.Dead && !u.Out) ? "diger" : "ekip";

    // ---- tesis baskını (aşama 2) ----
    static string RoomKey(RoomReport r) => r.Return ? "ga"[r.Room - 1].ToString() : "GAH"[r.Room - 1].ToString();

    // Yürünerek geçilen temiz oda: geçişten sonraki oda (ya da yürüyerek çıkışta tesisin girişi) ile geçilen oda arasında kalan odalar
    static int Walked(List<RoomReport> reps)
    {
        int n = 0;
        for (int i = 0; i < reps.Count; i++)
        {
            var r = reps[i];
            if (r.End != "pass" || !r.Return && r.Room < 3) continue;
            int next = i + 1 < reps.Count ? reps[i + 1].Room : 0;
            n += r.Room - 1 - next;
        }
        return n;
    }

    static void FacilityCounters(Simulation sim, List<RoomReport> reps, List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms)
    {
        var last = reps[reps.Count - 1];
        Add("fRaids", 1);
        Add("fRooms", reps.Count);
        Add("fWalked", Walked(reps));
        Add("fDevHits", rooms.Sum(x => x.t.DeviceHits));
        Add("fGoals", rooms.Count(x => x.t.DeviceGoal));
        Add("fDevBroken", reps.Count(r => r.Device != ""));
        Add("fStuck", last.Team.Count(m => m.State == "l"));
        Add("fEnd_" + (last.End == "pass" ? "x" : RoomKey(last)), 1);
        // Aşama 6: düşen (son raporda "d"), sürüklenerek çıkarılan, temiz odadan toplanıp çıkarılan, taşınmayan (öldü)
        Add("fFallen", Fallen(last).Count());
        Add("fCarried", Fallen(last).Count(m => m.Body == 2 && m.Picked == 0));
        Add("fCollected", Fallen(last).Count(m => m.Body == 2 && m.Picked != 0));
        Add("fAbandoned", Fallen(last).Count(m => m.Body != 2));
    }

    // Aşama 6: son raporda düşen kişi (geride kalan "l" hariç); çıkarılan Body 2 (Picked 1: dönüşte temiz odadan toplandı)
    static IEnumerable<RoomMember> Fallen(RoomReport last) => last.Team.Where(m => m.State == "d");

    // Aşama 6 kapanışı: son raporda düşen ("d") ya da geride kalan ("l") her kişinin sonu, kadro sırasıyla: kategori + düştüğü oda +
    // gövdenin tahtada son göründüğü oda (bırakıldığı ya da çıkarıldığı oda; hiç tahtada değilse "-"). Kategori: c sürüklenerek
    // çıkarıldı, k temiz odadan toplanıp çıkarıldı, n son nefesle çıkarıldı (son tahtasında Unit.Breath; c ile k gibi Body 2, carried'ın
    // alt kümesi), x baskın kaybedildi, b ayaktakiler varken bırakıldı (o odanın raporunda ayakta biri var), h o odada ayakta kimse
    // kalmadı (hepsi düştü ya da geride kaldı; baskın yine kazanılmış olabilir), l geride kaldı. Oda: G/A/H gidiş, g/a dönüş.
    static List<string> Fates(List<RoomReport> reps, List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms)
    {
        var last = reps[reps.Count - 1];
        var list = new List<string>();
        for (int i = 0; i < last.Team.Count; i++)
        {
            var m = last.Team[i];
            if (m.State == "a") continue;
            int j = reps.FindIndex(r => i < r.Team.Count && r.Team[i].State != "a");
            int k = rooms.FindLastIndex(x => x.b.Units.Any(u => Baskin.IsSpecialist(u) && u.Slot == i));
            var unit = k < 0 ? null : rooms[k].b.Units.First(u => Baskin.IsSpecialist(u) && u.Slot == i);
            // Aşama 12: e Katar'ın kenarından düştü ("k"; yaşar, yatar)
            char cat = m.State == "l" ? 'l' : m.State == "k" ? 'e'
                : m.Body == 2 ? (m.Picked != 0 ? 'k' : unit != null && unit.Breath ? 'n' : 'c')
                : last.End == "lost" ? 'x'
                : k >= 0 && rooms[k].rep.Team.Any(t => t.State == "a") ? 'b' : 'h';
            list.Add(cat + (j < 0 ? "-" : RoomKey(reps[j])) + (k < 0 ? "-" : RoomKey(rooms[k].rep)));
        }
        return list;
    }

    static string FacilityLine(Simulation sim, List<RoomReport> reps, List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms)
    {
        var last = reps[reps.Count - 1];
        var fates = Fates(reps, rooms);
        string DevIn(int room) => reps.Any(r => r.Room == room && r.Device != "") ? "1" : "0";
        var kinds = reps.Where(r => r.Device != "").Select(r => r.Device).Distinct().ToList();
        var mid = sim.FacilityPlan?.Room(sim.S.RaidLog.Last().SectorId, 2);
        return "\trooms=" + reps.Count
            + "\trt=" + string.Join("/", rooms.Select(x => RoomKey(x.rep) + x.rep.Turns))
            + "\trmv=" + string.Join("/", rooms.Select(x => RoomKey(x.rep) + x.moves))
            + "\trac=" + string.Join("/", rooms.Select(x => RoomKey(x.rep) + x.acts))
            + "\twalked=" + Walked(reps)
            + "\tdevG=" + DevIn(1) + "\tdevA=" + DevIn(2) + "\tdevK=" + (kinds.Count == 0 ? "-" : string.Join("+", kinds))
            + "\tdevHits=" + rooms.Sum(x => x.t.DeviceHits) + "\tgoals=" + rooms.Count(x => x.t.DeviceGoal)
            + "\tstuck=" + last.Team.Count(m => m.State == "l")
            + "\tpart=" + (mid != null && mid.ShutterPartition ? 1 : 0)
            + "\tendRoom=" + (last.End == "pass" ? "x" : RoomKey(last))
            + "\tdev=" + Dev
            // Aşama 6: düşen politikası, düşen/taşınan/toplanan/taşınmayan, eylem kullanımı, pusu isabeti, kepenk ve büyük sandık
            // açıldı mı (0/1), yükün yere düşmesi ve yerden alınması
            + "\tdusen=" + Dusen
            + "\tfallen=" + Fallen(last).Count() + "\tcarried=" + Fallen(last).Count(m => m.Body == 2 && m.Picked == 0)
            + "\tcollected=" + Fallen(last).Count(m => m.Body == 2 && m.Picked != 0) + "\tabandoned=" + Fallen(last).Count(m => m.Body != 2)
            + "\tax=pusu:" + rooms.Sum(x => x.t.Ambush) + "/kosu:" + rooms.Sum(x => x.t.Rush) + "/surukle:" + rooms.Sum(x => x.t.Drag)
            + "/firlat:" + rooms.Sum(x => x.t.Throw) + "/ortak:" + rooms.Sum(x => x.t.Joint) + "/menfez:" + rooms.Sum(x => x.t.Vent)
            + "\tamb=" + rooms.Sum(x => x.t.AmbushHits)
            + "\tshut=" + (reps.Any(r => r.Opened.Count > 0) ? 1 : 0)
            + "\tcrate=" + (reps.Any(r => r.Crate) ? 1 : 0)
            + "\tcdrop=" + rooms.Sum(x => x.t.CargoDrops) + "\tcpick=" + rooms.Sum(x => x.t.CargoPicks)
            // Aşama 6 kapanışı: son nefeste ekiple giden gövde (oda başına, Unit.Breath)
            + "\tbreath=" + rooms.Sum(x => x.b.Units.Count(u => u.Breath))
            // Son nefesle çıkarılan (carried'ın alt kümesi) ve düşenlerin/geride kalanların sonu (Fates)
            + "\tbsaved=" + fates.Count(f => f[0] == 'n')
            + "\tfate=" + (fates.Count == 0 ? "-" : string.Join(",", fates));
    }

    // ---- tesis ekipmanı (aşama 8) ----
    // Harekâtta en az bir kez kullanılan ekipman türleri (C satırının gearUsed'i)
    static HashSet<string> usedKinds;

    // Baskında tür başına ekipman kullanımı (eylem sayısı; köpek: havla + getir + kokla)
    static Dictionary<string, int> GearUse(List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms) =>
        new Dictionary<string, int>
        {
            [FacilityGear.Wire] = rooms.Sum(x => x.t.Wire + x.t.Plank), [FacilityGear.Charge] = rooms.Sum(x => x.t.Charge),
            [FacilityGear.Turret] = rooms.Sum(x => x.t.Turret), [FacilityGear.Decoy] = rooms.Sum(x => x.t.Decoy),
            [FacilityGear.Winch] = rooms.Sum(x => x.t.Winch), [FacilityGear.Clamp] = rooms.Sum(x => x.t.Clamp),
            [FacilityGear.Dog] = rooms.Sum(x => x.t.Bark + x.t.Fetch + x.t.Sniff),
        };

    // Aşama 8, R satırı: politikalar, emirde açık ekipman tarifleri (rec), kadronun yardımcı eşyası (carry), eylem
    // kullanımı (gx), efektlerden etkiler (gfx), köpek ve tutsak adımları (uzman moves/acts'ten ayrı)
    static string GearLine(string rec, string carry, List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms, int dogSteps, int capSteps)
    {
        int S(Func<RaidBot.Tally, int> f) => rooms.Sum(x => f(x.t));
        return "\todul=" + Odul + "\tarac=" + Arac + "\tgear=" + GearPolicy
            + "\trec=" + (rec == "" ? "-" : rec) + "\tcarry=" + carry
            + "\tgx=wire:" + S(t => t.Wire) + "/plank:" + S(t => t.Plank) + "/charge:" + S(t => t.Charge) + "/turret:" + S(t => t.Turret)
            + "/decoy:" + S(t => t.Decoy) + "/winch:" + S(t => t.Winch) + "/clamp:" + S(t => t.Clamp) + "/bark:" + S(t => t.Bark)
            + "/fetch:" + S(t => t.Fetch) + "/sniff:" + S(t => t.Sniff) + "/command:" + S(t => t.Command)
            + "\tgfx=thit:" + S(t => t.TurretHits) + "/wpull:" + S(t => t.WinchPulls) + "/dstep:" + S(t => t.DecoySteps)
            + "/cap:" + S(t => t.Captures) + "/rel:" + S(t => t.Releases) + "/dogdown:" + S(t => t.DogDowns)
            + "/scrap:" + S(t => t.Scraps) + "/brk:" + S(t => t.Breaks) + "/plate:" + S(t => t.Plates)
            + "\tdogSteps=" + dogSteps + "\tcapSteps=" + capSteps;
    }

    // ---- önlem defteri (aşama 10) ----
    // Ekipmanın önlem sınıfları (FacilityCounters.Classes dizini): örme seti örme, lokum yıkım, taret ve yem aygıt, vinç aygıt ve itme
    // (parazit kurulumunu, cıvata çekişini keser), kelepçe ele, köpek köpek. "Cezalı taşıma" bunlardan birinde önlem olmasıdır.
    static int[] GearClasses(string kind)
    {
        switch (kind)
        {
            case FacilityGear.Wire: return new[] { 2 };
            case FacilityGear.Charge: return new[] { 1 };
            case FacilityGear.Turret: case FacilityGear.Decoy: return new[] { 5 };
            case FacilityGear.Winch: return new[] { 5, 0 };
            case FacilityGear.Clamp: return new[] { 3 };
            case FacilityGear.Dog: return new[] { 4 };
            default: return new int[0];
        }
    }

    static string Levels(IList<int> l) => l == null || l.Count != 6 ? "-" : string.Join("/", Corrigible.Core.FacilityCounters.Classes.Select((c, i) => c + ":" + l[i]));

    // Aşama 10, R satırı (capSteps'ten sonra): cm emirdeki kademeler (sınıf:kademe, /), tl Core'un bu baskından saydığı
    // (tavanlı, FacilityCounters.RaidTally; defter kapalıyken -), tlb botun eylem/efekt sayaçlarından aynı eşleme (RaidBot.TallyMap,
    // tavanlı; tl ile aynı olmalı), learn bu baskının tamamladığı bölgede alınan önlem (sınıf:kademe ya da -), ans emirde açık cevaplar,
    // cmfx önlem efektleri (bolt cıvatanın direndiği itme/çekiş, unbolt Söküm, dwall yıkılan çift cidar, cut kesici kolun ek hasarı,
    // jam parazitin kısalttığı aygıt, ebrk kırılan koku yayıcı, emit yayıcının susturduğu köpek-tur, mpull vincin makine çekişi),
    // plates kadronun plakası, sqHp/sqMv/sqPw kadronun toplam canı/yürüyüşü/gücü, rook çaylak, half yarım tayın; tak=hic yalnız TAK=hic'te
    static string CounterLine(Simulation sim, RaidParams rp, List<RoomReport> reps, List<(Baskin.BState b, RoomReport rep, RaidBot.Tally t, int moves, int acts)> rooms,
        LiberationRaidReward rw, int emit, bool half)
    {
        int S(Func<RaidBot.Tally, int> f) => rooms.Sum(x => f(x.t));
        bool tally = rp.Counters != null && rp.Counters.Count == 6;
        int cap = Math.Max(0, sim.B.facilityTallyCap);
        string tl = tally ? string.Join("/", Corrigible.Core.FacilityCounters.RaidTally(reps, cap)) : "-";
        string tlb = tally ? string.Join("/", RaidBot.TallyMap(rooms.Select(x => x.t), rp.Counters, sim.B.facilityTallyAmbush).Select(v => Math.Min(cap, v))) : "-";
        var squad = rp.Squad ?? new List<SquadSlot>();
        return "\tcm=" + Levels(rp.Counters) + "\ttl=" + tl + "\ttlb=" + tlb
            + "\tlearn=" + (string.IsNullOrEmpty(rw?.Countermeasure) ? "-" : rw.Countermeasure)
            + "\tans=" + (rp.Answers == null || rp.Answers.Count == 0 ? "-" : string.Join("+", rp.Answers))
            + "\tcmfx=bolt:" + S(t => t.Bolted) + "/unbolt:" + S(t => t.Unbolts) + "/dwall:" + S(t => t.DoubleWalls) + "/cut:" + S(t => t.Cuts)
            + "/jam:" + S(t => t.Jams) + "/ebrk:" + S(t => t.EmitterBreaks) + "/emit:" + emit + "/mpull:" + S(t => t.MachinePulls)
            + "\tplates=" + squad.Sum(q => q.Armor) + "\tsqHp=" + squad.Sum(q => q.Hp) + "\tsqMv=" + squad.Sum(q => q.Move)
            + "\tsqPw=" + squad.Sum(q => q.Power) + "\trook=" + squad.Count(q => q.Rookie) + "\thalf=" + (half ? 1 : 0)
            + (Tak == "hic" ? "\ttak=hic" : "");
    }

    // REPLAY=1: kayıt baştan yeniden oynatılır; gün, fon, güven, alarm, RNG, baskın günlüğü (başarı, oda raporları) ve kişiler eşit
    // olmalı (FacilityKit.State kalıbı). Mono'da JsonUtility yok: kayıt nesnesi doğrudan verilir.
    static string State(Simulation sim) =>
        string.Join("\n", new[]
        {
            "d" + sim.S.Day + " f" + sim.S.Fon.ToString("R", CultureInfo.InvariantCulture) + " t" + sim.S.Trust
                + " a" + sim.S.Alarm.ToString("R", CultureInfo.InvariantCulture) + " rng" + sim.S.Rng.State + " raids" + sim.S.RaidLog.Count,
            string.Join("\n", sim.S.RaidLog.Select(l => (l.Success ? "W " : "L ") + l.SectorId + " " + string.Join(" / ", l.Rooms))),
            string.Join(";", sim.Board.People.Select(p => p.Id + ":" + (p.Alive ? 1 : 0) + ":" + p.Hp + ":" + p.Down)),
            "saha " + (sim.B.infiltratorFieldEnabled ? string.Join(";", sim.Admission.Files.Select(f => f.PersonId + ":" + f.Revealed + ":" + f.Resident.Isolated + ":" + f.FieldDecisionDue)) : ""),
            "unvan " + string.Join(";", sim.Board.People.Select(p => p.Id + ":" + (int)p.Titles + ":" + string.Join(",", p.Deeds ?? Array.Empty<int>()))),
            "plan " + string.Join(",", sim.S.Liberation.Facility.Known) + " i" + sim.S.Liberation.Facility.Intel,
            // Aşama 8: açılan tarifler, kazanımlar, bekleyen ödül, ödülü verilen bölgeler ve pano ekipmanı (eksik case sessiz geçer)
            "odul " + string.Join(",", sim.S.Liberation.Facility.Recipes) + " | " + string.Join(",", sim.S.Liberation.Facility.Perks)
                + " | " + sim.S.Liberation.Facility.PendingReward + " | " + string.Join(",", sim.S.Liberation.Regions.Where(x => x.Rewarded).Select(x => x.Id)),
            "esya " + string.Join(";", sim.Board.Equipment.Select(e => e.Id + ":" + e.Kind + ":" + (e.OwnerId ?? "-") + ":" + e.Charges + ":" + e.Rest)),
            // Aşama 10: önlem defteri (sayaçlar, kademeler, öğrenme günlüğü; FacilityKit.CounterLine biçimi) ve iş emirleri
            "onlem " + string.Join(",", sim.S.Liberation.Facility.Tally) + " | " + string.Join(",", sim.S.Liberation.Facility.Counter) + " | "
                + string.Join(",", sim.S.Liberation.Facility.Learned.Select(e => e.Day + ":" + e.Region + ":" + e.Class + ":" + e.Level)),
            "is " + string.Join(";", sim.Board.Orders.Select(o => o.Id + ":" + o.RecipeId + ":" + o.Remaining + ":" + (o.Started ? 1 : 0) + ":"
                + string.Join(",", o.Reserves.Select(m => m.Kind + m.Quantity)))),
            // Aşama 11: izler ve onarım günlüğü, kuşatma günlüğü, karşı saldırı sayaçları, alınmış sektörler, günlüğün kipi
            // (FacilityKit.MarkLine ve SiegeLine biçimi)
            "iz " + string.Join(";", sim.S.Liberation.Facility.Marks.Select(MarkKey)) + " | " + string.Join(";", sim.S.Liberation.Facility.Repairs.Select(MarkKey)),
            "kusatma " + string.Join(";", sim.S.Liberation.Facility.Sieges.Select(e => e.Sector + ":" + e.Region + ":" + e.Day + ":" + e.Due + ":" + e.Outcome + ":" + e.Closed + ":" + e.Abandoned))
                + " | c" + sim.S.Counterattacks + ":" + (sim.S.LastCounterattack ?? "-") + ":" + sim.S.LastCounterattackDay + " | "
                + string.Join(",", sim.S.Liberation.Regions.SelectMany(r => r.Sectors).Where(x => x.Captured).Select(x => x.Id + "@" + x.CapturedDay))
                + " | " + string.Join("", sim.S.RaidLog.Select(l => l.Mode == Simulation.DefenseMode ? "D" : "R")),
        });

    static string MarkKey(FacilityMark m) => m.Room + ":" + m.Kind + ":" + m.X + ":" + m.Y + ":" + m.Hp + ":" + m.Max + ":" + m.Day;

    // Yalnız HURDA_DENE=1 ölçümünde ilk köpeğe bir kez uygula. Asıl harekâtın envanteri ve kaydı korunur.
    static void MeasureDogScrap(Simulation sim, string id)
    {
        var record = sim.Record;
        var copy = new GameRecord { seed = record.seed, balance = record.balance, day = sim.S.Day,
            map = record.map, facility = record.facility, commands = record.commands.ToList() };
        var probe = Replay.Restore(copy);
        if (probe.ReplayFailures != 0 || probe.Board.EquipmentItem(id)?.Kind != FacilityGear.Dog)
            throw new Exception("Hurda denemesinin başlangıcı kayıttan üretilemedi.");
        int metal = probe.Board.Material("metal").Quantity, commands = copy.commands.Count;
        var result = probe.ScrapEquipment(id);
        Add("dogScrapTried", 1);
        Add("dogScrapAccepted", result.Ok ? 1 : 0);
        Add("dogScrapKept", probe.Board.EquipmentItem(id) != null ? 1 : 0);
        Add("dogScrapMetal", probe.Board.Material("metal").Quantity - metal);
        Add("dogScrapRecorded", copy.commands.Count - commands);
    }

    static void CheckReplay(Simulation sim, uint seed)
    {
        sim.Record.day = sim.S.Day;
        string want = State(sim);
        var r = Replay.Restore(sim.Record);
        if (r.ReplayFailures != 0) throw new Exception("replay " + seed + ": " + r.ReplayFailures + " komut başarısız");
        string got = State(r);
        if (got != want) throw new Exception("replay " + seed + ": durum farklı\n" + want + "\n----\n" + got);
    }

    static List<string> Dismissed = new List<string>();
    static void Door(Simulation sim, string policy)
    {
        var g = sim.Admission.Group;
        if (g.Count == 0) return;
        // Aşama 11: kapı yolunun karşı saldırısı ve kuşatması (ChangeAlarm'ın dördüncü yolu)
        int c0 = sim.S.Counterattacks, sg0 = SiegeCount(sim);
        DoorPick(sim, policy, g);
        Add("counterDoor", sim.S.Counterattacks - c0); NoteSieges(sim, sg0, "kapi");
    }

    static void DoorPick(Simulation sim, string policy, List<CommunityApplicant> g)
    {
        int alive = sim.Board.People.Count(p => p.Alive);
        bool full = alive >= sim.B.boardResidentCapacity;
        CommunityApplicant pick = null;
        switch (policy)
        {
            case "hic": break;
            case "kefilli": pick = g.Where(c => c.Tier == 0).FirstOrDefault(); break;
            case "comert": case "alyolla": pick = g.OrderByDescending(c => c.Perks.Count).ThenByDescending(c => c.Tier).First(); break;
            case "karisik": pick = alive < 6 ? g.OrderBy(c => c.Tier).First() : g.Where(c => c.Tier == 0).FirstOrDefault(); break;
        }
        string replace = null;
        if (pick != null && full && policy == "alyolla")
        {
            // Al ve yolla: dolu sığınakta yeni cömert aday, bu politikayla alınmış en eski kişinin yerine gelir (yolluğuyla).
            replace = Dismissed.FirstOrDefault(id => sim.Admission.File(id) is var f && f != null && !f.Departed && f.Resident.Alive && sim.Board.People.Contains(f.Resident));
            if (replace == null) pick = null;
        }
        if (pick == null || full && replace == null) { sim.PassCandidates(); return; }
        if (!sim.ChooseCandidate(pick.Resident.Id, replace).Ok) { sim.PassCandidates(); return; }
        Add("accepted", 1);
        if (pick.Identity.Robot) Add("acceptedRobot", 1);
        if (replace != null) { Dismissed.Remove(replace); Add("replaced", 1); CountSent(sim, replace); }
        if (policy == "alyolla") Dismissed.Add(pick.Resident.Id);
    }

    static void CountSent(Simulation sim, string id)
    {
        var f = sim.Admission.File(id);
        if (f == null) return;
        Add(f.Identity.Robot ? "sentR" : "sentH", 1);
    }

    static void Decide(Simulation sim, string policy, string door)
    {
        var e = sim.PendingShelterEvent;
        if (e == null) return;
        var open = sim.ShelterEventOptions().Where(o => o.Open).ToList();
        ShelterEventOption pick = null;
        if (door == "alyolla" && e.PersonId != null && Dismissed.Contains(e.PersonId)) pick = open.FirstOrDefault(o => o.Id == "yolla");
        if (pick == null)
            switch (policy)
            {
                case "tecrit": pick = open.FirstOrDefault(o => o.Id == "tecrit" || o.Id == "sorgula") ?? open.First(o => o.Safe); break;
                case "yolla": pick = open.FirstOrDefault(o => o.Id == "yolla" || o.Id == "sok") ?? open.First(o => o.Safe); break;
                case "ucuz":
                    if (e.Kind == "iz" || e.Kind == "ikinci_iz")
                    {
                        var ign = open.FirstOrDefault(o => o.Id == "yoksay");
                        bool warns = ign != null && ign.Cost.Contains("eşiğe ulaşır");
                        pick = !warns && ign != null ? ign : open.FirstOrDefault(o => o.Id == "tecrit") ?? open.FirstOrDefault(o => o.Id == "yolla") ?? open.First(o => o.Safe);
                    }
                    else if (e.Kind == "sabotaj")
                    {
                        // Son sakinin sökümü kapalıysa tek açık karar sorgudur.
                        string preferred = sim.LocationCompromised ? "sorgula" : "sok";
                        pick = open.FirstOrDefault(o => o.Id == preferred) ?? open.First();
                    }
                    else pick = open.FirstOrDefault(o => o.Safe) ?? open[0];
                    break;
                default: pick = open.FirstOrDefault(o => o.Safe) ?? open[0]; break;
            }
        bool sending = pick.Id == "yolla" && e.PersonId != null;
        string pid = e.PersonId;
        if (sending) { CountSent(sim, pid); if (e.Kind == "iz" || e.Kind == "ikinci_iz") Add(sim.Admission.File(pid).Identity.Robot ? "izSentR" : "izSentH", 1); }
        int t0 = sim.S.Trust, c0 = sim.S.Counterattacks, sg0 = SiegeCount(sim);
        var r = sim.DecideShelterEvent(pick.Id);
        if (!r.Ok) throw new Exception("karar " + r.Msg);
        NoteSieges(sim, sg0, "olay");
        if (sending) Dismissed.Remove(pid);
        int cnt = sim.S.Counterattacks - c0;
        Add("counterEvent", cnt);
        int d = sim.S.Trust - t0 + cnt * sim.B.liberationCounterattackTrust;
        if (d < 0) Add("evTrustLoss", -d); else Add("evTrustGain", d);
        if (sim.S.Ending == "dagilma") lastHit = cnt > 0 ? "karsi_olay" : "olay";
    }

    static int lastProgressDay, lastCaptured;
    static void Switch(Simulation sim, string policy)
    {
        int captured = sim.S.Liberation.Regions.Sum(r => r.CapturedCount);
        if (captured != lastCaptured) { lastCaptured = captured; lastProgressDay = sim.S.Day; }
        if (policy == "hic" || sim.LastSwitchCheck() != null) return;
        var reasons = sim.LastSwitchReasons();
        bool pull = policy == "salter" ? reasons.Contains("konum") || reasons.Contains("kadro")
            : policy == "durgun" && (reasons.Contains("konum") || reasons.Contains("kadro") || sim.S.Day + 1 >= 30 && sim.S.Day - lastProgressDay >= 12);
        if (pull)
        {
            var r = sim.PullLastSwitch();
            if (!r.Ok) throw new Exception("şalter " + r.Msg);
        }
    }

    // Tek kipte (kayıt 21) Balance'tan kalkan alanlar. raidFacilityEnabled=true tek kipe eşittir ve sessizce yok sayılır (tesis4–17
    // listeleri ve Library/Calisma kilit betikleri değişmeden koşar); öbür her değer yok olan tek tahtayı ister ve koşuyu durdurur.
    static readonly string[] RetiredFields = { "raidFacilityEnabled", "liberationLootExtraTurns", "liberationServerExtraTurns" };

    static void ApplyBal(Balance bal, string spec)
    {
        if (string.IsNullOrEmpty(spec)) return;
        foreach (var kv in spec.Split(';'))
        {
            if (kv.Trim() == "") continue;
            var parts = kv.Split('=');
            string key = parts[0].Trim();
            if (Array.IndexOf(RetiredFields, key) >= 0)
            {
                if (key == "raidFacilityEnabled" && parts.Length == 2 && bool.TryParse(parts[1].Trim(), out bool on) && on) continue;
                throw new Exception("emekli alan: " + kv.Trim() + " (tek tahta kaldırıldı, kayıt 21)");
            }
            var fi = typeof(Balance).GetField(parts[0].Trim(), BindingFlags.Public | BindingFlags.Instance);
            if (fi == null) throw new Exception("alan yok: " + parts[0]);
            string v = parts[1].Trim();
            if (fi.FieldType == typeof(int)) fi.SetValue(bal, int.Parse(v, CultureInfo.InvariantCulture));
            else if (fi.FieldType == typeof(double)) fi.SetValue(bal, double.Parse(v, CultureInfo.InvariantCulture));
            else if (fi.FieldType == typeof(bool)) fi.SetValue(bal, bool.Parse(v));
            else throw new Exception("tür: " + parts[0]);
        }
    }

    static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    public static void Main(string[] args)
    {
        uint s0 = uint.Parse(args[0]), s1 = uint.Parse(args[1]);
        string door = args[2], ev = args[3], sw = args[4], tag = args[5];
        string spec = Environment.GetEnvironmentVariable("BAL");
        bool measureScrap = Environment.GetEnvironmentVariable("HURDA_DENE") == "1";
        for (uint seed = s0; seed <= s1; seed++)
        {
            var bal = new Balance();
            ApplyBal(bal, spec);
            var sim = Simulation.NewGame(seed, bal);
            M = new Dictionary<string, double>();
            planner = new CampaignBot { Retire = Retire, OldBot = OldBot, Odul = Odul, Arac = Arac, TakeGear = Tak != "hic", Savun = Savun };
            siegeSrc = new Dictionary<int, string>(); lastFailed = new Dictionary<string, bool>(); loss2Day = -1;
            usedKinds = new HashSet<string>();
            K12 = new Dictionary<string, double>();
            Dismissed.Clear();
            lastProgressDay = 0; lastCaptured = 0; lastHit = null;
            pkgOrder = new List<string> { sim.S.Liberation.CurrentRegion };
            pickRng = new Rng(seed * 2654435761u + 17u);
            pkgTier = new Dictionary<string, int>(); pkgFails = new Dictionary<string, int>();
            everCaptured = new HashSet<string>(); everCompleted = new HashSet<string>();
            runKey = "tag=" + tag + "\tdoor=" + door + "\tev=" + ev + "\tsw=" + sw + "\tseed=" + seed;
            var kinds = new Dictionary<string, int>();
            bool sawUnder = false, sawFall = false, at60 = false;
            int starving = 0, firstStarve = -1, firstLow = -1, maxHungry = 0;
            double minSup = 1e9;
            int trust20 = -1, trust40 = -1; var supAt = new List<string>();
            for (int t = 0; t < 120 && sim.S.Ending == null; t++)
            {
                if (measureScrap && !M.ContainsKey("dogScrapTried"))
                {
                    var dog = sim.Board.Equipment.FirstOrDefault(e => e.Kind == FacilityGear.Dog);
                    if (dog != null) MeasureDogScrap(sim, dog.Id);
                }
                ChooseReward(sim);
                Door(sim, door);
                Decide(sim, ev, door);
                Switch(sim, sw);
                if (sim.S.Ending != null) break;
                Plan(sim);
                // Aşama 11: kuşatma kararı; savunma oynandıysa o vardiya baskın yok
                bool defended = Siege(sim);
                if (sim.S.Ending != null) break;
                if (!defended) Raid(sim);
                if (sim.S.Ending != null) break;
                Decide(sim, ev, door);
                // Aynı baskında birden fazla kişi yakalanırsa sıradaki saha tutanağı ilk karardan sonra açılır.
                while (sim.S.Ending == null && sim.PendingShelterEvent?.Area == "saha")
                    Decide(sim, ev, door);
                if (sim.S.Ending != null) break;
                if (sim.S.Day == 60) at60 = true;
                if (sim.S.Day % 10 == 0) supAt.Add(sim.Shelter.Supplies.ToString("0", CultureInfo.InvariantCulture) + "/" + sim.BoardConsumption.ToString("0.0", CultureInfo.InvariantCulture));
                if (sim.S.Day == 20) trust20 = sim.S.Trust;
                if (sim.S.Day == 40) trust40 = sim.S.Trust;
                int tt0 = sim.S.Trust, c0 = sim.S.Counterattacks, sg0 = SiegeCount(sim);
                var pr = sim.PrintShift();
                if (!pr.Ok) throw new Exception($"fiş {seed} {sim.S.Day} {pr.Msg}");
                sim.FileShift(sim.Board.ReceiptId);
                NoteSieges(sim, sg0, "vardiya");
                // Aşama 8: bu kapanışta üretilen ekipman ve dinlenen köpek
                foreach (var k in sim.Board.LastCompleted) if (FacilityGear.IsGear(k)) Add("made_" + k, 1);
                // Aşama 10: ekipman dışı üretim (zırh, kitler, bakım kiti, EMP hücresi; üretim yolunun ayrıştırması)
                foreach (var k in sim.Board.LastCompleted) if (!FacilityGear.IsGear(k)) Add("prod_" + k, 1);
                if (sim.Board.Equipment.Any(e => e.Kind == FacilityGear.Dog && e.Rest > 0)) Add("dogRest", 1);
                int cnt = sim.S.Counterattacks - c0;
                Add("counterShift", cnt);
                int d = sim.S.Trust - tt0 + cnt * sim.B.liberationCounterattackTrust;
                if (d < 0) Add("stepTrustLoss", -d);
                if (sim.S.Ending == "dagilma") lastHit = cnt > 0 ? "karsi_vardiya" : "vadeli";
                if (sim.UnderstaffedCounting) sawUnder = true;
                if (sim.Shelter.FallPending) sawFall = true;
                if (sim.Shelter.Starving) { starving++; if (firstStarve < 0) firstStarve = sim.S.Day; }
                maxHungry = Math.Max(maxHungry, sim.Shelter.HungryStreak);
                minSup = Math.Min(minSup, sim.Shelter.Supplies);
                var pe = sim.PendingShelterEvent;
                if (pe != null && pe.OpenedShift == sim.S.Day)
                {
                    kinds.TryGetValue(pe.Kind, out int kc); kinds[pe.Kind] = kc + 1;
                    if (pe.Kind == "erzak_azaliyor" && firstLow < 0) firstLow = sim.S.Day;
                }
                else if (pe != null) Add("pendingOld", 1);
            }
            string end = sim.S.Ending ?? "suruyor";
            var o = new List<string>
            {
                "tag=" + tag, "door=" + door, "ev=" + ev, "sw=" + sw, "seed=" + seed, "end=" + end, "day=" + sim.S.Day,
                "tone=" + (sim.VictoryTone ?? "-"), "swkind=" + (end == "salter" ? sim.Shelter.SwitchKind : "-"),
                "at60=" + (at60 ? 1 : 0), "under=" + (sawUnder ? 1 : 0), "fall=" + (sawFall ? 1 : 0),
                "counter=" + sim.S.Counterattacks, "trust=" + sim.S.Trust, "minTrust=" + sim.Shelter.MinTrust,
                "trust20=" + trust20, "trust40=" + trust40,
                "regions=" + sim.S.Liberation.Regions.Count(r => r.Completed), "captured=" + sim.S.Liberation.Regions.Sum(r => r.CapturedCount),
                "alive=" + sim.Board.People.Count(p => p.Alive), "dead=" + sim.Board.People.Count(p => !p.Alive),
                "deserted=" + sim.Shelter.Deserted, "exposure=" + sim.Shelter.Exposure,
                "starving=" + starving, "firstStarve=" + firstStarve, "firstLow=" + firstLow, "maxHungry=" + maxHungry, "minSup=" + F(minSup),
                "lastHit=" + (lastHit ?? "-"), "sup10=" + string.Join(",", supAt),
                "kinds=" + string.Join(",", kinds.OrderBy(x => x.Key).Select(x => x.Key + ":" + x.Value)),
                // Harita: anahtar (0 dünya, 1 üretilmiş), parmak izi, üreticinin denemesi (−1 yedek), kıta ve bağlantı sayısı
                "mapgen=" + sim.B.liberationMapGen, "pick=" + Pick, "map=" + sim.MapFingerprint, "mapAttempt=" + (sim.Map?.Attempt.ToString() ?? "-"),
                "continents=" + (sim.Map?.Continents.ToString() ?? "-"), "links=" + sim.Links.Count,
                // pkg: paket:sıra:kademe:başarısız (sıra seçilme sırası, karargâh 1; kademe ilk baskındaki; alınmamış paket yazılmaz)
                "pkg=" + string.Join(",", pkgOrder.Select((id, i) => id + ":" + (i + 1) + ":" + (pkgTier.TryGetValue(id, out int pt) ? pt : 0)
                    + ":" + (pkgFails.TryGetValue(id, out int pf) ? pf : 0))),
                // pkgDist: paketin karargâhtan çizge uzaklığı (yalnız üretilmiş harita)
                "pkgDist=" + (sim.Map == null ? "-" : string.Join(",", sim.Map.Regions.Select(r => r.Id + ":" + r.Distance))),
            };
            // Aşama 8: ödül, emir ve kullanım sayaçları (M'ye harekâtın sonunda; anahtar sırası M'nin sıralamasında)
            Add("rwTarif", planner.RewardRecipe); Add("rwKazanim", planner.RewardPerk);
            Add("orders", planner.Orders);
            Add("ordersDraftLeft", planner.DraftsCancelled + sim.Board.Orders.Count(x => !x.Started));
            if (measureScrap) Add("normalScraps", sim.Record.commands.Count(c => c.kind == "board_scrap"));
            foreach (var kv in M.OrderBy(x => x.Key)) o.Add(kv.Key + "=" + F(kv.Value));
            o.Add("recipes=" + (sim.Facility.Recipes.Count == 0 ? "-" : string.Join("+", sim.Facility.Recipes)));
            o.Add("perks=" + (sim.Facility.Perks.Count == 0 ? "-" : string.Join("+", sim.Facility.Perks)));
            o.Add("gearUsed=" + (usedKinds.Count == 0 ? "-" : string.Join("+", FacilityGear.Kinds.Where(usedKinds.Contains))));
            // Aşama 10: öğrenmeler (sınıf:kademe:gün, sırayla), son kademeler (R cm biçimi), öğrenme sayısı, seçilen cevap tarifi
            var fs = sim.Facility;
            o.Add("learns=" + (fs.Learned.Count == 0 ? "-" : string.Join(",", fs.Learned.Select(e => e.Class + ":" + e.Level + ":" + e.Day))));
            o.Add("cmEnd=" + Levels(fs.Counter));
            o.Add("nLearn=" + fs.Learned.Count);
            o.Add("rwCevap=" + planner.RewardAnswer);
            // Aşama 11: kuşatma (günlükten: kurulan, savunulan, bırakılan, kendiliğinden; kanatsız anlık = düşüşle açıklanmayan sayaç),
            // savunma reddi, izlerin son hâli ve onarım (I3: marksMade − marksGone − marksRepaired − marksWiped = marksEnd), −3'lük kayıptan
            // sonraki beş vardiyada dağılma, politikalar
            var sg = fs.Sieges;
            int nYield = sg.Count(e => e.Outcome == Simulation.SiegeYielded), nAuto = sg.Count(e => e.Outcome == Simulation.SiegeAuto);
            int nLost = sg.Count(e => e.Outcome == Simulation.SiegeLost);
            o.Add("siege=" + sg.Count);
            o.Add("siegeNone=" + (sim.S.Counterattacks - nYield - nAuto - nLost));
            o.Add("defend=" + sg.Count(e => e.Outcome == Simulation.SiegeHeld || e.Outcome == Simulation.SiegeLost || e.Outcome == Simulation.SiegeDefending));
            o.Add("yield=" + nYield);
            o.Add("autoYield=" + nAuto);
            o.Add("defNoSquad=" + planner.DefendRefused);
            o.Add("marksRepaired=" + fs.Repairs.Count);
            o.Add("marksEnd=" + fs.Marks.Count);
            o.Add("defLossDag5=" + (end == "dagilma" && loss2Day >= 0 && sim.S.Day - loss2Day <= 5 ? 1 : 0));
            o.Add("savun=" + Savun);
            o.Add("cekil=" + Cekil);
            // Aşama 12: Katar ve Yalpa toplamları (R satırının KatarLine'ından), politikalar
            double V(string k) => K12.TryGetValue(k, out double v) ? v : 0;
            foreach (var k in new[] { "katarRaids", "yalpaRaids", "det" }) o.Add(k + "=" + F(V(k)));
            o.Add("detNo=" + string.Join("/", MissKeys.Select(k => k + ":" + F(V("detNo_" + k)))));
            foreach (var k in new[] { "fol", "folRooms", "edgeM", "edgeP", "fellInj", "eDied", "slides", "tiltHitP" }) o.Add(k + "=" + F(V(k)));
            o.Add("ayir=" + Ayir);
            o.Add("kenar=" + Kenar);
            if (sim.FacilityTitlesOn)
            {
                var awards = sim.S.RaidLog.Where(l => l.LiberationReward?.Squad.Any(s => s.NewTitles != FacilityTitle.None) == true).ToArray();
                var people = awards.SelectMany(l => l.LiberationReward.Squad).ToArray();
                foreach (var title in FacilityTitles.Order)
                    o.Add("title_" + FacilityTitles.Id(title) + "=" + people.Count(p => FacilityTitles.Has(p.NewTitles, title)));
                o.Add("title_first=" + (awards.FirstOrDefault()?.Day ?? -1));
                o.Add("title_alive=" + sim.Board.People.Count(p => p.Alive && p.Titles != FacilityTitle.None));
            }
            if (sim.B.infiltratorFieldEnabled)
            {
                // Yalnız koşu sonu ölçümü; karar politikası gizli kimliği okumaz.
                var agents = sim.Admission.Files.Where(f => !f.Founder && f.Identity?.Robot == true).ToArray();
                var turned = sim.S.RaidLog.Where(l => l.LiberationReward != null).SelectMany(l => l.LiberationReward.Squad).Where(s => s.Turned).ToArray();
                o.Add("field_agents=" + agents.Length);
                o.Add("field_silent=" + agents.Count(f => f.Identity.TraceDelay > sim.B.infiltratorTellMaxDelay));
                o.Add("field_turn=" + turned.Length);
                o.Add("field_capture=" + turned.Count(s => s.Subdued));
            }
            if (end == "silme")
                o.Add("crit=" + string.Join(",", sim.EndReport.Criteria.Select(x => x.Id + ":" + x.Value + "/" + x.Limit + ":" + (x.Met ? 1 : 0))));
            if (ReplayCheck) { CheckReplay(sim, seed); o.Add("replay=ok"); }
            Console.WriteLine("C\t" + string.Join("\t", o));
        }
    }
}
