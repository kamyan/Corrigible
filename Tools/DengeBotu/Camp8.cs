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
//        BAL="alan=değer;alan=değer" Balance'ı değiştirir, ALT=n RaidBot seçenek sayısı (varsayılan 8).
// Her harekât için bir satır yazar ("C\t" + anahtar=değer, sekmeyle); özet ozet.py.
public static class Program
{
    static readonly bool OldBot = Environment.GetEnvironmentVariable("OLDBOT") == "1";
    static bool Fit(Simulation sim, ResidentState p) => p.Alive && p.Down <= 0 && p.Hp > 0 && !p.Restricted && !p.Isolated
        && sim.S.Day >= (sim.Admission.File(p.Id)?.IdleUntil ?? -1);

    // Camp8r (aşama 8 doğrulaması): RETIRE=1 sunumun RetireJobs'unu taklit eder: yayındaki panoda yuvası olmayan görevler
    // (care, gather, watch, contact) kilitsiz planda Boşta'ya iner, bot onlara kimseyi atamaz.
    static readonly bool Retire = Environment.GetEnvironmentVariable("RETIRE") == "1";
    // ALT=n: RaidBot seçenek sayısı (varsayılan 8; "usta" bot sınaması için büyütülür).
    static readonly int Alt = int.TryParse(Environment.GetEnvironmentVariable("ALT"), out int alt) && alt > 0 ? alt : 8;
    static void Plan(Simulation sim)
    {
        var board = sim.Board;
        if (Retire)
            foreach (var rp in board.People.Where(x => x.Alive && (x.Job == "care" || x.Job == "gather" || x.Job == "watch" || x.Job == "contact")).ToList())
                sim.AssignResident(rp.Id, "rest");
        if (!board.AutomaticProduction) sim.EnableAutomaticProduction();
        foreach (string role in PlanningBoardData.FieldJobs)
        {
            if (board.Equipment.Any(e => e.Kind == "kit_" + role && e.Charges > 0)) continue;
            var recipe = sim.BoardRecipes.FirstOrDefault(r => r.Id == "kit_" + role);
            if (recipe == null) continue;
            foreach (var m in recipe.Inputs)
            {
                int have = board.ProductionInputs.Where(i => i.Kind == m.Kind).Sum(i => i.Quantity);
                if (have < m.Quantity) sim.StageProductionMaterial(m.Kind, m.Quantity);
            }
            break;
        }
        foreach (string role in PlanningBoardData.FieldJobs)
        {
            var holder = board.People.FirstOrDefault(p => Fit(sim, p) && board.Equipment.Any(e => e.OwnerId == p.Id && e.Kind == "kit_" + role && e.Charges > 0));
            if (holder == null)
            {
                var kit = board.Equipment.FirstOrDefault(e => e.Kind == "kit_" + role && e.Charges > 0 && e.OwnerId == null);
                // Camp8: yatan/uygun olmayan kişinin devredilebilir kiti de alınır (OLDBOT=1: Camp6 davranışı, yalnız depodaki kit).
                if (kit == null && !OldBot)
                    kit = board.Equipment.FirstOrDefault(e => e.Kind == "kit_" + role && e.Charges > 0 && !e.Bound && e.OwnerId != null
                        && board.Person(e.OwnerId) is var ow && ow != null && !Fit(sim, ow));
                var cand = board.People.Where(p => Fit(sim, p) && !PlanningBoardData.FieldJobs.Contains(p.Job)
                        && !board.Equipment.Any(e => e.OwnerId == p.Id && e.Bound && PlanningBoardData.EquipmentSlot(e.Kind) == "kit"))
                    .OrderBy(p => OldBot || !board.Equipment.Any(e => e.OwnerId == p.Id && PlanningBoardData.EquipmentSlot(e.Kind) == "kit") ? 0 : 1)
                    .ThenByDescending(p => p.Skill(role)).FirstOrDefault();
                if (kit != null && cand != null && sim.EquipResident(kit.Id, cand.Id).Ok) holder = cand;
            }
            if (holder != null && holder.Job != role) sim.AssignResident(holder.Id, role);
        }
        foreach (string job in Retire ? new[] { "craft", "cook" } : new[] { "craft", "cook", "gather" })
        {
            if (board.People.Any(p => p.Alive && p.Job == job)) continue;
            var p2 = board.People.Where(p => Fit(sim, p) && p.Job == "rest").OrderByDescending(p => p.Skill(job)).FirstOrDefault();
            // Camp8: eksik rol kiti varken üretici yoksa toplayıcı ya da bakıcı üretime alınır.
            if (p2 == null && job == "craft" && !OldBot && PlanningBoardData.FieldJobs.Any(r => !board.Equipment.Any(e => e.Kind == "kit_" + r && e.Charges > 0)))
                p2 = board.People.Where(p => Fit(sim, p) && (p.Job == "gather" || p.Job == "care")).OrderByDescending(p => p.Skill(job)).FirstOrDefault();
            if (p2 != null) sim.AssignResident(p2.Id, job);
        }
    }

    // ---- ölçüm sayaçları (harekât başına sıfırlanır) ----
    static Dictionary<string, double> M;
    static void Add(string k, double v) { M.TryGetValue(k, out double o); M[k] = o + v; }
    static string lastHit;

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
            var f = sim.LiberationFrontier.FirstOrDefault();
            if (f == null || !sim.SelectLiberationRegion(f).Ok) return false;
        }
        var sector = sim.NextLiberationSector(lib.CurrentRegion);
        if (sector == null || !sim.StartLiberationRaid(sector.Id).Ok) return false;
        var B = Baskin.Create(sim.S.Raid.Params);
        new RaidBot { Alternatives = Alt }.Play(B);
        int t0 = sim.S.Trust, c0 = sim.S.Counterattacks, dead0 = sim.Board.People.Count(p => !p.Alive), tier = sim.Tier;
        var res = sim.ResolveRaid(Baskin.Result(B));
        if (!res.Ok) throw new Exception("raid " + res.Msg);
        var log = sim.S.RaidLog.Last();
        var rw = log.LiberationReward;
        int civ = Math.Max(0, log.Collateral) * sim.B.liberationTrustPerCivilian;
        int cnt = sim.S.Counterattacks - c0;
        Add("raids", 1);
        if (!log.Success) Add("fails", 1);
        if (log.MissionKind == "server") { Add("srvRaids", 1); if (!log.Success) Add("srvFails", 1); }
        Add("collat", Math.Max(0, log.Collateral));
        Add("civLoss", civ);
        Add("counterRaid", cnt);
        if (log.Success) { Add("civGain", rw.TrustFromCivilians); if (rw.RegionCompleted) Add("regionGain", sim.B.liberationTrustPerRegion); }
        Add("raidDeaths", sim.Board.People.Count(p => !p.Alive) - dead0);
        Add("raidsT" + tier, 1); if (!log.Success) Add("failsT" + tier, 1); Add("collatT" + tier, Math.Max(0, log.Collateral));
        Add("deadT" + tier, sim.Board.People.Count(p => !p.Alive) - dead0);
        Add("downT" + tier, rw.Squad.Count(x => x.Down && !x.Dead));
        if (sim.S.Ending == "dagilma") lastHit = civ > 0 && cnt > 0 ? "sivil+karsi" : cnt > 0 ? "karsi_baskin" : "sivil";
        return true;
    }

    static List<string> Dismissed = new List<string>();
    static void Door(Simulation sim, string policy)
    {
        var g = sim.Admission.Group;
        if (g.Count == 0) return;
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
                    else if (e.Kind == "sabotaj") pick = sim.LocationCompromised ? open.FirstOrDefault(o => o.Id == "sorgula") ?? open.First(o => o.Id == "sok") : open.First(o => o.Id == "sok");
                    else pick = open.FirstOrDefault(o => o.Safe) ?? open[0];
                    break;
                default: pick = open.FirstOrDefault(o => o.Safe) ?? open[0]; break;
            }
        bool sending = pick.Id == "yolla" && e.PersonId != null;
        string pid = e.PersonId;
        if (sending) { CountSent(sim, pid); if (e.Kind == "iz" || e.Kind == "ikinci_iz") Add(sim.Admission.File(pid).Identity.Robot ? "izSentR" : "izSentH", 1); }
        int t0 = sim.S.Trust, c0 = sim.S.Counterattacks;
        var r = sim.DecideShelterEvent(pick.Id);
        if (!r.Ok) throw new Exception("karar " + r.Msg);
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

    static void ApplyBal(Balance bal, string spec)
    {
        if (string.IsNullOrEmpty(spec)) return;
        foreach (var kv in spec.Split(';'))
        {
            if (kv.Trim() == "") continue;
            var parts = kv.Split('=');
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
        for (uint seed = s0; seed <= s1; seed++)
        {
            var bal = new Balance();
            ApplyBal(bal, spec);
            var sim = Simulation.NewGame(seed, bal);
            M = new Dictionary<string, double>();
            Dismissed.Clear();
            lastProgressDay = 0; lastCaptured = 0; lastHit = null;
            var kinds = new Dictionary<string, int>();
            bool sawUnder = false, sawFall = false, at60 = false;
            int starving = 0, firstStarve = -1, firstLow = -1, maxHungry = 0;
            double minSup = 1e9;
            int trust20 = -1, trust40 = -1; var supAt = new List<string>();
            for (int t = 0; t < 120 && sim.S.Ending == null; t++)
            {
                Door(sim, door);
                Decide(sim, ev, door);
                Switch(sim, sw);
                if (sim.S.Ending != null) break;
                Plan(sim);
                Raid(sim);
                if (sim.S.Ending != null) break;
                Decide(sim, ev, door);
                if (sim.S.Ending != null) break;
                if (sim.S.Day == 60) at60 = true;
                if (sim.S.Day % 10 == 0) supAt.Add(sim.Shelter.Supplies.ToString("0", CultureInfo.InvariantCulture) + "/" + sim.BoardConsumption.ToString("0.0", CultureInfo.InvariantCulture));
                if (sim.S.Day == 20) trust20 = sim.S.Trust;
                if (sim.S.Day == 40) trust40 = sim.S.Trust;
                int tt0 = sim.S.Trust, c0 = sim.S.Counterattacks;
                var pr = sim.PrintShift();
                if (!pr.Ok) throw new Exception($"fiş {seed} {sim.S.Day} {pr.Msg}");
                sim.FileShift(sim.Board.ReceiptId);
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
            };
            foreach (var kv in M.OrderBy(x => x.Key)) o.Add(kv.Key + "=" + F(kv.Value));
            if (end == "silme")
                o.Add("crit=" + string.Join(",", sim.EndReport.Criteria.Select(x => x.Id + ":" + x.Value + "/" + x.Limit + ":" + (x.Met ? 1 : 0))));
            Console.WriteLine("C\t" + string.Join("\t", o));
        }
    }
}
