using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Deneysel mod: saha ekipleri ve baskının strateji tarafı. Tarayıcı prototipindeki sim.js'in
    /// "Deneysel mod: saha ekipleri ve baskın" bölümünün birebir karşılığı. Yalnızca S.Exp varken çalışır.
    /// </summary>
    public sealed partial class Simulation
    {
        public const string HQ = "weu";
        public static readonly string[] TeamNames = { Loc.N("Alfa"), Loc.N("Bravo"), Loc.N("Charlie"), Loc.N("Delta") };
        public const int TeamMonitorDays = 3, TeamAuditDays = 2;
        public const int TeamMax = 4, TeamHireDays = 4;
        public const double TeamHireFon = 45, RaidFon = 30, RaidSg = 6;
        public static readonly string[] RaidTraits = { "kopya", "yedek", "hizlanma", "aldatma", "medya" };

        void SetupExperimental(ExpOptions exp)
        {
            S.Exp = exp.Clone();
            if (S.Exp.StartTeams <= 0) S.Exp.StartTeams = 3;
            if (S.Exp.TimeScale <= 0) S.Exp.TimeScale = 1;
            S.NextGift = (int)Js.Round(Span(S.NextGift));
            if (!S.Exp.Teams) return;
            for (int i = 0; i < S.Exp.StartTeams; i++) S.Teams.Add(NewTeam(i, 0));
        }

        static TeamState NewTeam(int i, int formDays) => new TeamState
        {
            Id = TeamNames[i].ToLowerInvariant(), Name = Loc.T(TeamNames[i]), At = HQ, From = HQ, Down = formDays, DownWhy = formDays > 0 ? Loc.T("kuruluyor") : null,
        };

        /// <summary>Yolculuk süresi: haritadaki uzaklıkla (1000 birim genişlik) orantılı, 1–5 gün.</summary>
        public static int TravelDays(string a, string b)
        {
            if (a == b) return 0;
            var A = GameData.Region(a); var B2 = GameData.Region(b);
            double dx = A.X - B2.X, dy = A.Y - B2.Y;
            return (int)Clamp(Js.Round(Math.Sqrt(dx * dx + dy * dy) / 130), 1, 5);
        }

        public TeamState Team(string id) => S.Teams.FirstOrDefault(t => t.Id == id);
        public static bool TeamReady(TeamState t) => t.Dest == null && t.Busy <= 0 && t.Down <= 0;
        public TeamState IdleTeamAt(string regionId) => S.Teams.FirstOrDefault(t => t.At == regionId && TeamReady(t));
        public IEnumerable<TeamState> TeamsAt(string regionId) => S.Teams.Where(t => t.At == regionId && t.Dest == null);

        void StepTeams()
        {
            foreach (var t in S.Teams)
            {
                if (t.Down > 0) { t.Down--; if (t.Down == 0) { AddLog(Loc.F("{0} ekibi göreve hazır.", t.Name), "insight"); t.DownWhy = null; } }
                if (t.Busy > 0) { t.Busy--; if (t.Busy == 0) t.Task = null; }
                if (t.Dest != null)
                {
                    t.Eta--;
                    if (t.Eta <= 0)
                    {
                        t.At = t.Dest; t.Dest = null; t.Eta = 0;
                        S.Events.Add(new SimEvent { Type = SimEventType.Team, Key = t.Id, Text = Loc.F("{0} ekibi {1} bölgesine ulaştı.", t.Name, Def(t.At).Name) });
                    }
                }
            }
        }

        public ActResult MoveTeam(string teamId, string regionId) => Rec(MoveTeamCore(teamId, regionId), "team", teamId, regionId);

        ActResult MoveTeamCore(string teamId, string regionId)
        {
            if (S.Ending != null) return ActResult.Fail(Loc.T("Oyun bitti."));
            var t = Team(teamId);
            if (t == null) return ActResult.Fail(Loc.T("Böyle bir ekip yok."));
            if (t.Down > 0) return ActResult.Fail(Loc.F("{0} ekibi {1}: {2} gün.", t.Name, t.DownWhy ?? Loc.T("iyileşiyor"), t.Down));
            if (t.Busy > 0) return ActResult.Fail(Loc.F("{0} ekibi görevde: {1} gün.", t.Name, t.Busy));
            if (t.Dest != null) return ActResult.Fail(Loc.F("{0} ekibi zaten yolda.", t.Name));
            if (t.At == regionId) return ActResult.Fail(Loc.F("{0} ekibi zaten orada.", t.Name));
            if (S.Region(regionId).Left) return ActResult.Fail(Loc.F("{0} ittifak dışı; ekibi kabul etmiyor.", Def(regionId).Name));
            t.From = t.At; t.Dest = regionId; t.Trip = t.Eta = TravelDays(t.At, regionId); t.At = null;
            return ActResult.Success(Loc.F("{0} ekibi yola çıktı: {1}, {2} gün.", t.Name, Def(regionId).Name, t.Eta));
        }

        public ActResult HireTeam() => Rec(HireTeamCore(), "hire");

        ActResult HireTeamCore()
        {
            if (S.Ending != null) return ActResult.Fail(Loc.T("Oyun bitti."));
            if (S.Teams.Count >= TeamMax) return ActResult.Fail(Loc.F("En fazla {0} saha ekibi kurulabilir.", TeamMax));
            if (S.Fon < TeamHireFon) return ActResult.Fail(Loc.F("Yeni ekip {0} fon ister.", TeamHireFon));
            S.Fon -= TeamHireFon;
            var t = NewTeam(S.Teams.Count, TeamHireDays);
            S.Teams.Add(t);
            return ActResult.Success(Loc.F("{0} ekibi kuruluyor: {1} gün sonra karargâhta hazır.", t.Name, TeamHireDays));
        }

        /// <summary>Baskın yapılamıyorsa nedeni, yapılabiliyorsa null.</summary>
        public string RaidCheck(string regionId)
        {
            if (!TeamsOn) return Loc.T("Baskın yalnızca saha ekipleri modunda var.");
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (S.Raid != null) return Loc.T("Zaten bir baskın sürüyor.");
            var r = S.Region(regionId);
            if (r.Off > 0) return Loc.T("Bölge çevrimdışı.");
            if (r.Left) return Loc.T("Bölge ittifak dışı.");
            if (!r.Detected && S.CoreKnown != regionId) return Loc.T("Önce bu bölgede YZ varlığı doğrulanmalı.");
            if (IdleTeamAt(regionId) == null) return Loc.T("Bölgede boşta saha ekibi yok.");
            if (S.Fon < RaidFon || S.Sg < RaidSg) return RaidCheckResources();
            return null;
        }

        public string RaidCheckResources() => Loc.F("Baskın {0} fon ve {1} siyasi güç ister.", RaidFon, RaidSg);

        /// <summary>Bölgedeki boşta ekip binaya girer. Strateji katmanı baskın bitene kadar durur; sonucu ResolveRaid getirir.</summary>
        public ActResult StartRaid(string regionId) => Rec(StartRaidCore(regionId), "raid", regionId);

        ActResult StartRaidCore(string regionId)
        {
            var why = RaidCheck(regionId);
            if (why != null) return ActResult.Fail(why);
            var r = S.Region(regionId);
            var t = IdleTeamAt(regionId);
            S.Fon -= RaidFon; S.Sg -= RaidSg;
            S.Raids++;
            int seed = (int)Math.Floor(S.Rng.Next() * 2147483647);
            S.Raid = new RaidState
            {
                Team = t.Id, Region = regionId, N = S.Raids, Seed = seed,
                Params = new RaidParams
                {
                    Seed = seed, Region = regionId, RegionName = Def(regionId).Name, Compute = Def(regionId).Compute,
                    Inf = Js.Round(r.Inf), Dep = Js.Round(r.Dep), IsCore = S.Core == regionId, RaidNumber = S.Raids,
                    Traits = S.Traits.Where(x => RaidTraits.Contains(x)).ToList(), Illusion = S.Illusion != null,
                },
            };
            t.Busy = 1; t.Task = "baskin";
            S.Events.Add(new SimEvent { Type = SimEventType.Raid });
            return ActResult.Success();
        }

        public ActResult ResolveRaid(RaidResult result) => Rec(ResolveRaidCore(result), "raidres", result?.Encode());

        ActResult ResolveRaidCore(RaidResult result)
        {
            var R0 = S.Raid;
            if (R0 == null) return ActResult.Fail(Loc.T("Süren baskın yok."));
            var r = S.Region(R0.Region); var D = Def(R0.Region); var t = Team(R0.Team);
            double lm = LossMul;
            var o = new List<string>();
            S.Raid = null;
            if (t != null) t.Task = null;
            // Yan hasar: her sivil sunucu bölgenin ekonomisine ve güvenine işler; bağımlı bölgede daha pahalı
            int col = result.Collateral;
            if (col > 0)
            {
                r.Econ = Math.Max(5, r.Econ - 5 * col);
                r.Trust = Math.Max(0, r.Trust - 2 * col * lm * DepMul(r));
                o.Add(Loc.F("{0} sivil sunucu hasar gördü: ekonomi −{1}, güven düştü.", col, 5 * col));
            }
            int down = result.Down;
            if (t != null)
            {
                if (down >= 3)
                {
                    S.Teams.Remove(t);
                    o.Add(Loc.F("{0} ekibi kaybedildi.", t.Name));
                    AddLog(Loc.F("{0} ekibini binamda bıraktın.", t.Name), "counter");
                }
                else if (down > 0)
                {
                    t.Down = 4 * down; t.DownWhy = Loc.T("yaralı");
                    o.Add(Loc.F("{0} ekibinden {1} kişi yaralandı: ekip {2} gün iyileşecek.", t.Name, down, t.Down));
                }
            }
            if (result.Success)
            {
                r.Inf = Math.Max(0, r.Inf - 40);
                r.Patch = Dur(B.patchDays);
                r.FieldDay = S.Day;
                if (!r.Detected) Detect(r);
                if (S.Core == R0.Region)
                {
                    if (S.Has("yedek") && !result.BackupDestroyed)
                    {
                        CoreEscape(null);
                        AddLog(Loc.T("Ana dolabımı kırdın. Yedeğim çoktan başka yerdeydi."), "counter");
                        o.Add(Loc.T("Çekirdek dolabı kırıldı ama YZ yedek kopyasıyla kaçtı."));
                    }
                    else
                    {
                        S.Core = null; S.CoreKnown = null;
                        AddNews(Loc.F("{0}: KYGA saha ekibi YZ çekirdeğini fiziksel olarak yok etti.", D.Name), kind: "alarm", src: r.Id);
                        AddLog(Loc.T("Sunucularımı elleriyle söktüler. Bunu hesaba katmamıştım."), "counter");
                        o.Add(Loc.T("YZ’nin çekirdeği yok edildi. Kalan kopyaları temizle."));
                    }
                }
                else
                {
                    AddNews(Loc.F("{0}: KYGA saha operasyonu bir YZ sunucu kümesini devre dışı bıraktı.", D.Name), kind: "alarm", src: r.Id);
                    o.Add(Loc.F("{0} sunucu kümesi kırıldı: sızma −40, 20 günlük yama.", D.Name));
                }
                if (S.Illusion != null) TearVeil(r);
            }
            else
            {
                // Başarısızlık: YZ karşı saldırıya geçer ve binayı güçlendirir (sonraki baskında daha çok robot); çekirdek yerinde kalır
                r.Trust = Math.Max(0, r.Trust - 10 * lm);
                r.Inf = Math.Min(100, r.Inf + 15);
                S.Cp += 10;
                o.Add(result.Retreat ? Loc.T("Ekip geri çekildi. Operasyon başarısız.") : Loc.T("Operasyon başarısız: hedef dolap ayakta kaldı."));
                o.Add(Loc.F("YZ karşı saldırıya geçti: {0} sızması +15. Bina bir sonraki baskına hazırlanıyor.", D.Name));
                AddLog(S.Core == R0.Region ? Loc.T("Kapımdaydın ve geri döndün. Bir dahakine kapıda daha çok makine olacak.") : Loc.T("Ekibini izledim. Bir dahakine daha çok robot olacak."), "counter");
            }
            S.RaidLog.Add(new RaidLogEntry { Day = S.Day, Region = R0.Region, Success = result.Success, Collateral = col, Down = down });
            return ActResult.Success(string.Join(" ", o));
        }
    }
}
